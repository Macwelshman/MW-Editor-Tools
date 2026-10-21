using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Colossal.UI.Binding;
using Game;
using Game.Prefabs;
using Game.SceneFlow;
using Game.UI;
using Game.UI.Editor;
using Game.Tools;
using MW.EditorTools.History;
using MW.EditorTools.Services;
using Unity.Entities;
using UnityEngine.InputSystem;

namespace MW.EditorTools.Modules.PropReplacer
{
    public sealed partial class PropReplacerSystem : UISystemBase
    {
        [Serializable] public sealed class RowState { public int index; public string type, value; }
        [Serializable] public sealed class HoverText { public string[] samples; }
        [Serializable] public sealed class State { public bool available, hasRow, canPaste; public int selected; public RowState[] rows; public string asset, rowType, current, clipboard, status; }
        private sealed class Row { internal PrefabBase Root; internal PrefabBase[] Ancestors; internal ComponentBase Owner; internal FieldInfo ListField, ReferenceField; internal IList List; internal object Value; internal int Index; internal string Label; }
        private sealed class Clip { internal Type RowType; internal PrefabBase Replacement; internal string SourceAsset, Label; }
        private static readonly string[] References={nameof(ObjectSubObjectInfo.m_Object),nameof(ObjectSubAreaInfo.m_AreaPrefab),nameof(ObjectSubNetInfo.m_NetPrefab),nameof(EffectSource.EffectSettings.m_Effect),nameof(ObjectSubLaneInfo.m_LanePrefab)};
        private string browserCandidate; private float browserCandidateTime=-100;
        private PrefabBase browserCandidateAsset;
        private PrefabBase reportAsset;
        private EditorContextService context; private ValueBinding<string> binding; private string published,status="Hover an element in the viewport and press Ctrl+C, or choose a row below."; private Clip clipboard; private PrefabBase selectedRoot; private object selectedValue; private FieldInfo hoverEntity; private Func<bool> shortcutAllowed;
        internal Action BeforeWrite;
        public override GameMode gameMode=>GameMode.Editor;
        protected override void OnCreate(){base.OnCreate();AddBinding(binding=new ValueBinding<string>("mwPropReplacer","state","{}"));AddBinding(new TriggerBinding<string>("mwPropReplacer","command",Command));AddBinding(new TriggerBinding<string>("mwPropReplacer","hoverCopy",HoverCopy));AddBinding(new TriggerBinding<string>("mwPropReplacer","hoverCandidate",value=>{browserCandidate=value;browserCandidateAsset=context?.Asset;browserCandidateTime=UnityEngine.Time.realtimeSinceStartup;}));}
        internal void Initialize(EditorContextService value,Action beforeWrite,Func<bool> allowShortcut){context=value;BeforeWrite=beforeWrite;shortcutAllowed=allowShortcut;hoverEntity=typeof(DefaultToolSystem).GetField("m_LastRaycastEntity",BindingFlags.Instance|BindingFlags.NonPublic)??throw new NotSupportedException("Viewport hover API unavailable.");Enabled=true;}
        internal void Release(){Enabled=false;pendingViewportClipboard=null;requiredViewportHit=null;context=null;BeforeWrite=null;CanRecordUndo=null;RecordUndo=null;shortcutAllowed=null;hoverEntity=null;clipboard=null;selectedRoot=null;selectedValue=null;published=null;}
        private void Command(string command)
        {
            TrackReportAsset();
            try
            {
                if(context==null||!context.Available||context.DialogBusy)throw new InvalidOperationException("Select an editable element row and close open dialogs.");
                var rows=ResolveRows();
                if(command!=null&&command.StartsWith("select|",StringComparison.Ordinal))
                {
                    int index;if(!int.TryParse(command.Substring(7),out index)||index<0||index>=rows.Length)throw new InvalidOperationException("The element list changed. Choose the row again.");
                    selectedRoot=rows[index].Root;selectedValue=rows[index].Value;status="Chosen "+rows[index].Label+" row "+(rows[index].Index+1)+".";
                }
                else if(command=="copy") Copy(Chosen(rows));
                else if(command=="paste") Replace(Chosen(rows),false);
                else if(command=="batch") Replace(Chosen(rows),true);
                else if(command=="clear"){clipboard=null;status="Element clipboard cleared.";}
                else return;
            }
            catch(Exception error){status=error.Message;MW.EditorTools.Mod.Log.Warn("Prop Replacer: "+error.Message);}
            Publish();
        }
        private void Copy(Row row)
        {
            if(row==null)throw new InvalidOperationException("Select an element row in Object Info first.");
            var replacement=row.ReferenceField.GetValue(row.Value) as PrefabBase;
            if(replacement==null)throw new InvalidOperationException("The selected row has no replaceable prefab reference.");
            clipboard=new Clip{RowType=row.Value.GetType(),Replacement=replacement,SourceAsset=row.Root.name,Label=row.Label};
            status="Copied "+replacement.name+" from "+row.Root.name+".";
        }
        private void CopyHover()
        {
            TrackReportAsset();
            MW.EditorTools.Mod.Log.Info("Prop Replacer browser candidate at Ctrl+C: age="+(UnityEngine.Time.realtimeSinceStartup-browserCandidateTime)+" data="+(browserCandidate??"none"));
            if(browserCandidate!=null&&UnityEngine.Time.realtimeSinceStartup-browserCandidateTime<.75f){var previous=clipboard;HoverCopy(browserCandidate);if(!ReferenceEquals(previous,clipboard))return;}
            CopyViewport();
        }
        private void CopyViewport()
        {
            var tool=World.GetExistingSystemManaged<ToolSystem>()?.activeTool as DefaultToolSystem;
            if(tool==null)throw new InvalidOperationException("Use the default selection tool and hover an element before pressing Ctrl+C.");
            var id=ViewportHit();var em=World.EntityManager;
            if(id==Entity.Null||!em.Exists(id)||em.HasComponent<Game.Common.Deleted>(id))throw new InvalidOperationException("No live element is highlighted under the cursor.");
            var prefabs=World.GetExistingSystemManaged<PrefabSystem>();PrefabBase replacement=null;Type rowType=null;string label=null;
            if(em.HasComponent<Game.Tools.EditorContainer>(id))
            {
                var container=em.GetComponentData<Game.Tools.EditorContainer>(id);replacement=prefabs?.GetPrefab<PrefabBase>(container.m_Prefab);
                if(replacement is EffectPrefab){rowType=typeof(EffectSource.EffectSettings);label="Effect";}
                else if(replacement is NetLanePrefab){rowType=typeof(ObjectSubLaneInfo);label="Lane";}
            }
            if(rowType==null&&em.HasComponent<PrefabRef>(id))replacement=prefabs?.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(id));
            if(rowType==null&&em.HasComponent<Game.Areas.Area>(id)){rowType=typeof(ObjectSubAreaInfo);label="Area";}
            else if(rowType==null&&em.HasComponent<Game.Net.Edge>(id)){rowType=typeof(ObjectSubNetInfo);label="Net";}
            else if(rowType==null&&em.HasComponent<Game.Net.Node>(id)){rowType=typeof(ObjectSubNetInfo);label="Net";}
            else if(rowType==null&&em.HasComponent<Game.Net.Lane>(id)){rowType=typeof(ObjectSubLaneInfo);label="Lane";}
            else if(rowType==null&&em.HasComponent<Game.Objects.Object>(id)){rowType=typeof(ObjectSubObjectInfo);label="Object";}
            if(rowType==null||replacement==null)throw new InvalidOperationException("The highlighted entity is not a supported object, area, network, lane or effect element.");
            clipboard=new Clip{RowType=rowType,Replacement=replacement,SourceAsset=context.Asset?.name??"viewport",Label=label};
            status="Ctrl+C copied highlighted "+label.ToLowerInvariant()+" "+replacement.name+".";MW.EditorTools.Mod.Log.Info("Prop Replacer: "+status+" entity="+id);
        }
        private void HoverCopy(string json)
        {
            TrackReportAsset();
            try
            {
                if(shortcutAllowed?.Invoke()!=true)return;
                var data=UnityEngine.JsonUtility.FromJson<HoverText>(json);var samples=data?.samples?.Where(s=>!string.IsNullOrWhiteSpace(s)).Select(s=>s.Trim()).Distinct().Take(8).ToArray()??new string[0];
                if(samples.Length==0){MW.EditorTools.Mod.Log.Info("Prop Replacer browser Ctrl+C: hover contains no row text.");return;}
                var matches=ResolveRows().Select(r=>new{Row=r,Prefab=r.ReferenceField.GetValue(r.Value) as PrefabBase}).Where(x=>x.Prefab!=null&&samples.Any(s=>s.Equals(x.Prefab.name,StringComparison.Ordinal)||s.StartsWith(x.Prefab.name+"\n",StringComparison.Ordinal)||s.Contains("\n"+x.Prefab.name+"\n"))).ToArray();
                var distinct=matches.Length==0?0:matches.Any(x=>x.Row.Value.GetType()!=matches[0].Row.Value.GetType()||!ReferenceEquals(x.Prefab,matches[0].Prefab))?2:1;
                if(distinct!=1){MW.EditorTools.Mod.Log.Info("Prop Replacer browser Ctrl+C unresolved: matches="+distinct+" samples="+string.Join(" | ",samples.Select(s=>s.Length>120?s.Substring(0,120):s)));return;}
                var match=matches[0];clipboard=new Clip{RowType=match.Row.Value.GetType(),Replacement=match.Prefab,SourceAsset=match.Row.Root.name,Label=match.Row.Label};status="Ctrl+C copied highlighted browser "+match.Row.Label.ToLowerInvariant()+" "+match.Prefab.name+".";MW.EditorTools.Mod.Log.Info("Prop Replacer: "+status);
            }
            catch(Exception error){MW.EditorTools.Mod.Log.Warn("Prop Replacer browser Ctrl+C failed: "+error.Message);}
            Publish();
        }
        private void Replace(Row row,bool batch)
        {
            if(foliageFailed||foliageWitnesses.Length>0)throw new InvalidOperationException(foliageFailed?"Reload the disposable session before further replacements.":"Wait for foliage regeneration to finish before replacing again.");
            if(clipboard==null||clipboard.Replacement==null)throw new InvalidOperationException("Copy an element row first.");
            if(row==null)throw new InvalidOperationException("Select the destination element row first.");
            if(row.Value.GetType()!=clipboard.RowType)throw new InvalidOperationException("Source and destination element types differ.");
            if(!row.ReferenceField.FieldType.IsInstanceOfType(clipboard.Replacement))throw new InvalidOperationException("The copied prefab is incompatible with this destination field.");
            if(row.Root.isReadOnly)throw new InvalidOperationException("The destination asset is read-only.");
            var oldReference=row.ReferenceField.GetValue(row.Value) as PrefabBase;
            if(oldReference==null)throw new InvalidOperationException("The destination row has no replaceable prefab reference.");
            if(ReferenceEquals(oldReference,clipboard.Replacement)){status="Destination already uses "+clipboard.Replacement.name+".";return;}
            if(CanRecordUndo?.Invoke()!=true||RecordUndo==null)throw new InvalidOperationException("Enable Undo recovery and finish pending editor operations before replacing.");
            var before=PrefabSnapshot.Take(row.ListField.GetValue(row.Owner));int changed=0;bool dirty=row.Root.isDirty;
            var indices=Enumerable.Range(0,row.List.Count).Where(i=>row.List[i]!=null&&row.List[i].GetType()==clipboard.RowType&&(batch?ReferenceEquals(row.ReferenceField.GetValue(row.List[i]),oldReference):ReferenceEquals(row.List[i],row.Value))).ToArray();
            var selectedAsset=context.Asset;var replacement=clipboard.Replacement;
            var foliage=PlanFoliageReplacement(row,batch,oldReference);
            try
            {
                BeforeWrite?.Invoke();
                foreach(var value in row.List)
                {
                    if(value==null||value.GetType()!=clipboard.RowType)continue;
                    if(!batch&&!ReferenceEquals(value,row.Value))continue;
                    if(batch&&!ReferenceEquals(row.ReferenceField.GetValue(value),oldReference))continue;
                    row.ReferenceField.SetValue(value,clipboard.Replacement);changed++;
                }
                if(changed==0)throw new InvalidOperationException("No matching elements were found.");
                CreateFoliageCandidates(foliage);
                row.Root.isDirty=true;Refresh(row.Root,row.Ancestors);
                WatchFoliageCandidates(foliage,row,before);
                var after=PrefabSnapshot.Take(row.ListField.GetValue(row.Owner));
                RecordUndo(MakeReplacementUndo(row,before,after,oldReference,replacement,indices,selectedAsset,ReplacementGuards(row)));
                status=(batch?"Replaced "+changed+" matching "+row.Label.ToLowerInvariant()+" element(s)":"Replaced selected "+row.Label.ToLowerInvariant())+" with "+clipboard.Replacement.name+". Destination placement and row settings were preserved.";
            }
            catch(Exception error)
            {
                foliageWitnesses=new FoliageWitness[0];foliagePlan=null;
                try{RemoveFoliageCandidates(foliage);}catch(Exception cleanup){MW.EditorTools.Mod.Log.Error(cleanup,"Foliage candidate cleanup failed; reload without saving.");}
                try{row.ListField.SetValue(row.Owner,before.Materialize());row.Root.isDirty=dirty;Refresh(row.Root,row.Ancestors);}catch(Exception rollback){MW.EditorTools.Mod.Log.Error(rollback,"Prop Replacer rollback failed.");}
                throw new InvalidOperationException("Replacement failed; the previous list was restored. "+error.Message,error);
            }
        }
        private void PasteHover(bool batch)
        {
            if(clipboard?.Replacement==null)throw new InvalidOperationException("Copy an element with Ctrl+C first.");
            var fresh=browserCandidate!=null&&UnityEngine.Time.realtimeSinceStartup-browserCandidateTime<.75f;
            var samples=fresh?UnityEngine.JsonUtility.FromJson<HoverText>(browserCandidate)?.samples??new string[0]:new string[0];
            if(samples.Length==0){PasteViewport(batch);return;}
            if(!ReferenceEquals(browserCandidateAsset,context.Asset))throw new InvalidOperationException("The hovered asset changed. Hover the destination again.");
            var rows=ResolveRows().Where(r=>r.Value.GetType()==clipboard.RowType).ToArray();
            var match=HoverRowPolicy.Resolve(samples,rows.Select(r=>r.Index).ToArray(),rows.Select(r=>(r.ReferenceField.GetValue(r.Value) as PrefabBase)?.name).ToArray());
            if(match<0){
                bool browserRow=ResolveRows().Any(r=>{var p=r.ReferenceField.GetValue(r.Value) as PrefabBase;return p!=null&&samples.Any(s=>string.Equals(s?.Trim(),p.name,StringComparison.Ordinal));});
                if(browserRow)throw new InvalidOperationException("Destination row is ambiguous or has a different element type. Hover its Element header and try again.");
                PasteViewport(batch);return;
            }
            var row=rows[match];selectedRoot=row.Root;selectedValue=row.Value;
            Replace(row,batch);MW.EditorTools.Mod.Log.Info("Prop Replacer Ctrl+V: "+status+" underlyingIndex="+row.Index);
        }
        private void PasteViewport(bool batch){var viewport=ResolveViewportRow();Replace(viewport,batch);MW.EditorTools.Mod.Log.Info("Prop Replacer viewport paste: "+status+" underlyingIndex="+viewport.Index);}
        private Row[] ResolveRows(PrefabBase target=null)
        {
            var root=target??context.Asset;if(root==null)return new Row[0];var result=new System.Collections.Generic.List<Row>();
            for(int i=0;i<RecoveryListSchema.Components.Length;i++)
            {
                var reference=RecoveryListSchema.Elements[i].GetField(References[i],BindingFlags.Public|BindingFlags.Instance);
                if(reference==null||!typeof(PrefabBase).IsAssignableFrom(reference.FieldType))throw new NotSupportedException("Unsupported element reference: "+RecoveryListSchema.Elements[i].FullName+"."+References[i]);
                foreach(var owner in root.components.Where(c=>c!=null&&c.GetType()==RecoveryListSchema.Components[i]))
                {
                    if(!ReferenceEquals(owner.prefab,root))throw new InvalidOperationException("Element component ownership is invalid.");
                    var field=owner.GetType().GetField(RecoveryListSchema.Members[i]);var list=field?.GetValue(owner) as IList;
                    if(list==null)continue;
                    for(int at=0;at<list.Count;at++)if(list[at]!=null&&list[at].GetType()==RecoveryListSchema.Elements[i])result.Add(new Row{Root=root,Owner=owner,ListField=field,ReferenceField=reference,List=list,Value=list[at],Index=at,Label=RecoveryListSchema.Labels[i]});
                }
            }
            return result.ToArray();
        }
        private Row Chosen(Row[] rows)
        {
            if(!ReferenceEquals(selectedRoot,context.Asset)){selectedRoot=context.Asset;selectedValue=null;}
            var matches=rows.Where(r=>ReferenceEquals(r.Value,selectedValue)).ToArray();
            if(matches.Length==1)return matches[0];
            selectedValue=null;return null;
        }
        private void Refresh(PrefabBase root,PrefabBase[] ancestors=null)
        {
            var prefabs=World.GetExistingSystemManaged<PrefabSystem>();Entity entity;
            if(prefabs==null||!prefabs.TryGetEntity(root,out entity)||!World.EntityManager.Exists(entity))throw new InvalidOperationException("Destination is no longer registered.");
            World.GetExistingSystemManaged<EditorErrorPanelSystem>()?.OnPrefabModified();prefabs.UpdatePrefab(root);root.asset?.MarkDirty();
            if(ancestors!=null)foreach(var parent in ancestors){if(parent==null||parent.isReadOnly||!prefabs.TryGetEntity(parent,out entity)||!World.EntityManager.Exists(entity))throw new InvalidOperationException("A nested parent is no longer writable or registered.");prefabs.UpdatePrefab(parent);parent.asset?.MarkDirty();}
            if(ReferenceEquals(context.Asset,root)){var method=typeof(InspectorPanelSystem).GetMethod("RefreshSections",BindingFlags.NonPublic|BindingFlags.Instance);method?.Invoke(context.Inspector,null);}
        }
        private void TrackReportAsset()
        {
            var asset = context?.Asset;
            if (!ReferenceEquals(reportAsset, asset)) { reportAsset = asset; status = ""; }
        }
        protected override void OnUpdate(){base.OnUpdate();TrackReportAsset();CheckFoliageWitnesses();PollClipboardShortcuts();Publish();}
        private void Publish()
        {
            Row[] rows=new Row[0];Row row=null;try{if(context?.Available==true){rows=ResolveRows();row=Chosen(rows);}}catch{}
            var current=row?.ReferenceField.GetValue(row.Value) as PrefabBase;
            var state=new State{available=context?.EditorAvailable==true,hasRow=row!=null,canPaste=row!=null&&clipboard!=null&&row.Value.GetType()==clipboard.RowType,selected=Array.FindIndex(rows,r=>ReferenceEquals(r.Value,row?.Value)),rows=rows.Select((r,i)=>new RowState{index=i,type=r.Label+" "+(r.Index+1),value=(r.ReferenceField.GetValue(r.Value) as PrefabBase)?.name??"Missing reference"}).ToArray(),asset=context?.Asset?.name??"No asset selected",rowType=row?.Label??"No element row chosen",current=current?.name??"",clipboard=clipboard==null?"Clipboard empty":clipboard.Replacement.name+" ("+clipboard.Label+" from "+clipboard.SourceAsset+")",status=status};
            var json=UnityEngine.JsonUtility.ToJson(state);if(json!=published){published=json;binding.Update(json);}
        }
    }
}
