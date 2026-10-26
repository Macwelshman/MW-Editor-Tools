using System;
using System.Linq;
using Game.Prefabs;
using Unity.Entities;
using MW.EditorTools.Modules.ComponentManager.Transfer;
class Program {
 static int count;
 static void Check(bool value,string label){if(!value)throw new Exception(label);count++;}
 static void Refuses(Action action,string label){try{action();}catch(InvalidOperationException){count++;return;}throw new Exception(label);}
 static void Main(){
 var world=new World();var registry=new PrefabSystem();world.Systems[typeof(PrefabSystem)]=registry;
 var source=new PrefabBase{name="source"};var rich=(RichSettings)source.AddComponent(typeof(RichSettings));var reference=new PrefabBase();
 rich.Number=7.25f;rich.Items=new[]{new SettingsDto{Label="original",Values=new[]{3,8},Reference=reference},null};rich.List=new(){rich.Items[0]};rich.Curve=new(new UnityEngine.Keyframe{time=2,value=5,inWeight=.3f});
 var structural=(StructuralComponent)source.AddComponent(typeof(StructuralComponent));structural.UnadaptedSetting="copied";
 var clipboard=new TransferClipboard();clipboard.Copy(source,new[]{0,1});var fingerprint=clipboard.Entries[0].Fingerprint;
 rich.Items[0].Values[0]=99;rich.List.Clear();rich.Curve.keys[0]=default;
 Check(clipboard.Entries[0].Fingerprint==fingerprint,"source mutation changed clipboard");
 var dest=new PrefabBase{name="dest"};var existing=(RichSettings)dest.AddComponent(typeof(RichSettings));existing.Number=1;existing.active=false;
 var target=new TransferTarget(world,dest,dest);var session=new TransferSession();int writes=0;session.BeforeWrite=()=>writes++;
 session.Plan(target,clipboard,TransferMode.SettingsOnly);Check(session.Preview.Changes.Count==1,"replace-present mode adds missing");session.Apply(clipboard,target);
 Check(existing.Number==7.25f&&!existing.active&&dest.components.Count==1,"replace settings or activation");
 Check(existing.Items[0].Values[0]==3&&existing.List.Count==1&&existing.Items[1]==null&&ReferenceEquals(existing.Items[0].Reference,reference),"nested values/nulls/references");
 Check(existing.Curve.keys[0].value==5&&existing.Curve.keys[0].inWeight==.3f,"curve snapshot");
 Check(!ReferenceEquals(existing.Items,rich.Items)&&!ReferenceEquals(existing.Items[0].Values,rich.Items[0].Values),"mutable settings shared");
 session.Restore(target);Check(existing.Number==1&&!existing.active,"restore existing");
 session.Plan(target,clipboard,TransferMode.ComponentsOnly);Check(session.Preview.Changes.Count==1,"add-missing changes existing");session.Apply(clipboard,target);
 var added=dest.components.OfType<StructuralComponent>().Single();Check(added.UnadaptedSetting=="copied"&&added.active&&existing.Number==1&&ReferenceEquals(added.prefab,dest),"add-missing lost settings/owner/activation");
 session.Restore(target);Check(dest.components.Count==1,"restore addition");
 session.Plan(target,clipboard,TransferMode.ComponentsAndSettings);Check(session.Preview.Changes.Count==2,"replace-plus-add selection");session.Apply(clipboard,target);Check(existing.Number==7.25f&&dest.components.Count==2,"replace-plus-add result");
 existing.Items[0].Values[0]=22;Refuses(()=>session.Restore(target),"restore overwrote later edit");
 existing.Items[0].Values[0]=3;session.Restore(target);
 session.Plan(target,clipboard,TransferMode.SettingsOnly);existing.Number=2;Refuses(()=>session.Apply(clipboard,target),"stale settings accepted");
 session.Plan(target,clipboard,TransferMode.SettingsOnly);clipboard.Replace(clipboard.Entries);Refuses(()=>session.Apply(clipboard,target),"stale clipboard accepted");
 var empty=new PrefabBase();var emptyTarget=new TransferTarget(world,empty,empty);session.Plan(emptyTarget,clipboard,TransferMode.ComponentsAndSettings);registry.FailUpdates=1;Refuses(()=>session.Apply(clipboard,emptyTarget),"refresh failure accepted");Check(empty.components.Count==0,"failed transaction left additions");
 empty.FailAddition=typeof(StructuralComponent);session.Plan(emptyTarget,clipboard,TransferMode.ComponentsAndSettings);Refuses(()=>session.Apply(clipboard,emptyTarget),"native add failure accepted");Check(empty.components.Count==0,"partial additions leaked");empty.FailAddition=null;
 var deps=new PrefabBase();deps.AddComponent(typeof(RequiringComponent));deps.AddComponent(typeof(StructuralComponent));clipboard.Copy(deps,new[]{0,1});session.Plan(emptyTarget,clipboard,TransferMode.ComponentsAndSettings);
 Check(session.Preview.Changes.Count==2&&session.Preview.Changes[0].Type==typeof(StructuralComponent),"bulk dependency ordering");session.Apply(clipboard,emptyTarget);Check(empty.components.Count==2,"bulk dependency transfer");session.Restore(emptyTarget);
 clipboard.Copy(deps,new[]{0});session.Plan(emptyTarget,clipboard,TransferMode.ComponentsAndSettings);Check(!session.CanApply,"missing dependency accepted");
 var bad=new PrefabBase();bad.AddComponent(typeof(PrivateSettings));var rev=clipboard.Revision;clipboard.Copy(bad,new[]{0});Check(clipboard.Revision==rev,"opaque-only copy destroyed clipboard");
 var cyclic=(CyclicSettings)bad.AddComponent(typeof(CyclicSettings));cyclic.Loop=new();cyclic.Loop.Next=cyclic.Loop;clipboard.Copy(bad,new[]{1});Check(clipboard.Revision==rev,"cyclic graph copied");
 var callback=(UnsupportedSettings)bad.AddComponent(typeof(UnsupportedSettings));callback.Callback=()=>{};clipboard.Copy(bad,new[]{2});Check(clipboard.Revision==rev,"delegate copied");
 structural.active=false;clipboard.Copy(source,new[]{1});session.Plan(emptyTarget,clipboard,TransferMode.ComponentsOnly);session.Apply(clipboard,emptyTarget);
 Check(!empty.components.Single().active&&((StructuralComponent)empty.components.Single()).UnadaptedSetting=="copied","inactive addition lost source state");session.Restore(emptyTarget);
 clipboard.Copy(source,new[]{0});session.Plan(emptyTarget,clipboard,TransferMode.ComponentsAndSettings);session.Apply(clipboard,emptyTarget);var first=(RichSettings)empty.components.Single();
 var other=new PrefabBase();var otherTarget=new TransferTarget(world,other,other);session.Plan(otherTarget,clipboard,TransferMode.ComponentsAndSettings);session.Apply(clipboard,otherTarget);var second=(RichSettings)other.components.Single();
 first.Items[0].Values[0]=200;Check(second.Items[0].Values[0]==99&&!ReferenceEquals(first.Items,second.Items),"two destinations share copied mutable settings");
 session.Plan(otherTarget,clipboard,TransferMode.SettingsOnly);var writeCount=writes;second.Number=100;Refuses(()=>session.Apply(clipboard,otherTarget),"later scalar edit accepted");Check(writes==writeCount,"refused operation cleared Undo slot");Check(writes>0,"successful writes did not invalidate Undo slot");
 var finite=rich.Number;rich.Number=float.NaN;rev=clipboard.Revision;clipboard.Copy(source,new[]{0});Check(clipboard.Revision==rev,"nonfinite setting replaced clipboard");rich.Number=finite;
 empty.isReadOnly=true;Refuses(()=>session.Plan(emptyTarget,clipboard,TransferMode.ComponentsOnly),"read-only target accepted");
 var directClipboard=new TransferClipboard();directClipboard.Copy(source,new[]{0,1});
 foreach(var pasteMode in new[]{TransferMode.ComponentsOnly,TransferMode.SettingsOnly,TransferMode.ComponentsAndSettings}){
  var directDest=new PrefabBase{name="direct"};var directExisting=(RichSettings)directDest.AddComponent(typeof(RichSettings));directExisting.Number=-12;directExisting.active=false;
  var directTarget=new TransferTarget(world,directDest,directDest);var direct=new TransferSession();
  direct.Paste(directTarget,directClipboard,pasteMode);
  Check(directDest.components.Count==(pasteMode==TransferMode.SettingsOnly?1:2),"one-click paste mode addition policy");
  Check(directExisting.Number==(pasteMode==TransferMode.ComponentsOnly?-12:rich.Number)&&!directExisting.active,"one-click paste settings/activation policy");
  Check(direct.Summary.Contains(pasteMode==TransferMode.ComponentsOnly?"unchanged":"replace settings"),"one-click report lost after apply");
  direct.InvalidatePreview();Check(direct.Summary.Length>0,"selection refresh erased paste report");
  direct.ClearReport();Check(direct.Summary.Length==0&&direct.CanRestore,"asset report clear lost restore or retained report");
  if(pasteMode==TransferMode.SettingsOnly){direct.Paste(directTarget,directClipboard,TransferMode.ComponentsOnly);direct.Restore(directTarget);Check(directDest.components.Count==1,"latest paste restore");}
  else {var beforeNoOp=direct.Summary;direct.Paste(directTarget,directClipboard,TransferMode.ComponentsOnly);Check(direct.CanRestore,"no-op paste lost restore");direct.Restore(directTarget);Check(directDest.components.Count==1&&directExisting.Number==-12,"one-click paste restore");}
 }
 var failureDest=new PrefabBase();var failureTarget=new TransferTarget(world,failureDest,failureDest);var directFailure=new TransferSession();registry.FailUpdates=1;
 Refuses(()=>directFailure.Paste(failureTarget,directClipboard,TransferMode.ComponentsAndSettings),"one-click refresh failure accepted");Check(failureDest.components.Count==0,"one-click rollback leaked components");
 failureDest.isReadOnly=true;Refuses(()=>directFailure.Paste(failureTarget,directClipboard,TransferMode.ComponentsAndSettings),"one-click read-only accepted");
 PackChecks.Run(world,registry,Check,Refuses);
 foreach(var action in new[]{"paste","preview","apply"}){
  var immediateDest=new PrefabBase{name="Immediate"};var immediateTarget=new TransferTarget(world,immediateDest,immediateDest);var immediate=new TransferSession();int commits=0;immediate.BeforeWrite=()=>commits++;
  immediate.ExecutePasteCommand(action,immediateTarget,directClipboard,TransferMode.ComponentsAndSettings);
  Check(immediateDest.components.Count==2&&commits==1&&immediate.CanRestore&&!immediate.CanApply,"paste compatibility action commits immediately without a second apply: "+action);
  immediate.ExecutePasteCommand("recover",immediateTarget,directClipboard,TransferMode.ComponentsAndSettings);Check(immediateDest.components.Count==0&&!immediate.CanRestore,"Recover restores the latest paste: "+action);
 }
 Console.WriteLine("PASS: "+count+" actual-source component transfer checks (managed doubles; native editor acceptance pending).");
 }
}
