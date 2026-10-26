using System;
using System.Linq;
using System.Collections.Generic;
using Game.Prefabs;
using Game.UI.Editor;
using Colossal.UI.Binding;
using UnityEngine;
using MW.EditorTools.Modules.ComponentManager.GameIntegration;
using MW.EditorTools.Modules.ComponentManager.Transfer;
using MW.EditorTools.Modules.ComponentManager.Pack;
namespace MW.EditorTools.Modules.ComponentManager.Systems {
 public sealed partial class ComponentToolSystem {
  private static Dictionary<int,string[]> ReadFieldChoices(string json)=>FieldChoiceReader.Read(json);
  [Serializable] private sealed class PackRequest {public string action="",token="",scanToken="",planToken="",kind="",pattern=null,locale="",text="";public bool usePattern=false;public string start="100",step="1";public int[] ids=null;}
  [Serializable] private sealed class PackRow {public int id,current,priority;public string name,reason;public bool eligible;}
  [Serializable] private sealed class PackState {public string scanToken="",planToken="",status="",nameClipboard="Empty",descriptionClipboard="Empty";public PackRow[] rows=new PackRow[0],preview=new PackRow[0];public bool canApply,canRestorePriorities,canRestoreLocale,hasName,hasDescription;}
  private ValueBinding<string> packBinding;private PackState packState=new PackState();
  private PriorityBatch priorities;private readonly LocalizationTransfer localisation=new LocalizationTransfer();
  private PrefabBase[] scanned=new PrefabBase[0];private long packRevision;private Action packBeforeWrite;
  private void CreatePackBindings(){priorities=new PriorityBatch(World);AddBinding(packBinding=new ValueBinding<string>("mwComponentTool","packState","{}"));}
  private void ClearPackSession(){scanned=new PrefabBase[0];priorities=new PriorityBatch(World);localisation.Clear();packState=new PackState();packRevision++;PublishPack();}
  private void PublishPack(){packState.hasName=localisation.Has("name");packState.hasDescription=localisation.Has("description");packState.nameClipboard=localisation.Summary("name");packState.descriptionClipboard=localisation.Summary("description");packState.canRestoreLocale=localisation.CanRestore;packState.canRestorePriorities=priorities?.CanRestore==true;packBinding?.Update(PackJson.Write(packState));}
  private bool TryPackCommand(string command){
   if(command==null||!command.StartsWith("pack|",StringComparison.Ordinal))return false;
   try{
    if(command.Length>100000)throw new ArgumentException("Pack request too large.");var request=PackJson.Read<PackRequest>(command.Substring(5));if(request==null)throw new ArgumentException("Invalid pack request.");
    priorities.BeforeWrite=packBeforeWrite;localisation.BeforeWrite=packBeforeWrite;
    if(request.action=="scan"){
     priorities.Invalidate();packState.canApply=false;packState.preview=new PackRow[0];scanned=PriorityBatch.Workspace(World);
     if(scanned.Length>1000)throw new InvalidOperationException("Workspace exceeds 1000 assets. Reduce it first.");
     packState.scanToken=(++packRevision).ToString();packState.planToken="";
     packState.rows=scanned.Select((asset,i)=>{var target=new TransferTarget(World,asset,asset);string reason="";UIObject ui=null;try{target.Validate();ui=target.Exact(typeof(UIObject)) as UIObject;if(ui==null)reason="UI Object component missing";}catch(Exception e){reason=e.Message;}return new PackRow{id=i,name=asset.name,current=ui?.m_Priority??0,eligible=reason=="",reason=reason};}).ToArray();
     int eligible=packState.rows.Count(row=>row.eligible);
     packState.status=scanned.Length==0?"No assets found in the editor workspace. Load pack assets into the workspace and scan again.":$"Found {scanned.Length} workspace {(scanned.Length==1?"asset":"assets")}; {eligible} can be numbered. Choose assets below, then preview.";
     MW.EditorTools.Mod.Log.Info("Priority scan: "+packState.status);
    }else if(request.action=="priorityPreview"){
     priorities.Invalidate();packState.canApply=false;packState.preview=new PackRow[0];packState.planToken="";
     if(request.scanToken!=packState.scanToken||request.ids==null||request.ids.Length==0||request.ids.Distinct().Count()!=request.ids.Length||request.ids.Any(i=>i<0||i>=scanned.Length))throw new InvalidOperationException("Choose assets from the current workspace scan.");
     int start,step;if(!int.TryParse(request.start,System.Globalization.NumberStyles.AllowLeadingSign,System.Globalization.CultureInfo.InvariantCulture,out start)||!int.TryParse(request.step,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out step)||step<=0)throw new ArgumentException("Use a whole-number start and positive increment within the 32-bit range.");
     priorities.Plan(request.ids.OrderBy(i=>i).Select(i=>scanned[i]).ToArray(),start,step,request.usePattern?(request.pattern??PriorityOrder.DefaultPattern):null);
     packState.preview=priorities.Preview.Select(c=>new PackRow{name=c.Asset.name,current=c.Before,priority=c.After}).ToArray();packState.planToken=(++packRevision).ToString();packState.canApply=true;packState.status="Preview only. Review every asset and priority before Apply.";
    }else if(request.action=="priorityApply"){
     if(!packState.canApply||request.planToken!=packState.planToken)throw new InvalidOperationException("Priority preview changed. Preview again.");
     priorities.Apply();packState.canApply=false;packState.status="Priorities applied to the previewed assets. Thumbnails preserved; save assets normally when satisfied.";
    }else if(request.action=="priorityRestore"){priorities.Restore();packState.status="Previous priorities restored. Save assets normally when satisfied.";}
    else if(request.action=="localeClear"){localisation.Clear();packState.status="Localisation clipboards cleared.";}
    else if(request.action=="localeCopy"||request.action=="localePaste"||request.action=="localeRestore"||request.action=="localeAddName"||request.action=="localeAddDescription"){
     PrefabBase prefab;object selection;var fresh=EditorSelectionReader.Read(World,out prefab,out selection);TrackSelection(fresh,prefab,selection);
     if(!fresh.hasAsset||request.token!=fresh.selectionToken)throw new InvalidOperationException("Asset selection changed. Try again.");
     var inspector=World.GetExistingSystemManaged<InspectorPanelSystem>();
     packState.status=request.action=="localeCopy"?localisation.Copy(inspector,prefab,request.kind):request.action=="localeAddDescription"?localisation.AddDescription(inspector,TransferTarget.Resolve(World,prefab,selection),request.locale,request.text):request.action=="localeAddName"?localisation.AddName(inspector,TransferTarget.Resolve(World,prefab,selection),request.locale,request.text):request.action=="localePaste"?localisation.Paste(inspector,TransferTarget.Resolve(World,prefab,selection),request.kind):localisation.Restore(inspector,TransferTarget.Resolve(World,prefab,selection));state=fresh;Decorate();Publish();
    }else throw new ArgumentException("Unknown pack action.");
   }catch(Exception error){packState.status="Not completed: "+(error.InnerException?.Message??error.Message);MW.EditorTools.Mod.Log.Warn("Pack editing: "+packState.status);}
   PublishPack();return true;
  }
 }
}
