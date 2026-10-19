using System;using System.Collections;using System.Collections.Generic;using System.Linq;using System.Reflection;using Game.Prefabs;using MW.EditorTools.History;
namespace UnityEngine {public struct Keyframe{}public enum WrapMode{Default}public class AnimationCurve{public Keyframe[] keys;public WrapMode preWrapMode,postWrapMode;public AnimationCurve(params Keyframe[] k){keys=k;}}}
namespace Game.Prefabs {
 public class PrefabBase{public string name="test";public bool isReadOnly,isDirty;public List<ComponentBase> components=new();}
 public class ComponentBase{public PrefabBase prefab;public bool active=true;}
 public class ObjectSubObjects:ComponentBase{public ObjectSubObjectInfo[] m_SubObjects;}
 public class ObjectSubObjectInfo{public PrefabBase m_Object;public int position,probability,group,parent;}
 public class ObjectSubAreas:ComponentBase{public ObjectSubAreaInfo[] m_SubAreas;}public class ObjectSubAreaInfo{}
 public class ObjectSubNets:ComponentBase{public ObjectSubNetInfo[] m_SubNets;}public class ObjectSubNetInfo{}
 public class ObjectSubLanes:ComponentBase{public ObjectSubLaneInfo[] m_SubLanes;}public class ObjectSubLaneInfo{}
 public class EffectSource:ComponentBase{public EffectSettings[] m_Effects;public class EffectSettings{}public class AnimationProperties{}}
}
namespace MW.EditorTools {static class Mod{internal static Logger Log=new();internal class Logger{internal void Info(string s){}internal void Error(Exception e,string s){}}}}
namespace MW.EditorTools.Modules.PropReplacer {
 public sealed partial class PropReplacerSystem {
  private sealed class Row {internal PrefabBase Root;internal PrefabBase[] Ancestors;internal ComponentBase Owner;internal FieldInfo ListField,ReferenceField;internal IList List;internal object Value;internal int Index;internal string Label;}
  private sealed class Context{internal bool EditorAvailable=true,DialogBusy;internal PrefabBase Asset;}
  private sealed class FoliageWitness{}private sealed class FoliageCandidate{}
  private Context context=new();private bool foliageFailed;private FoliageWitness[] foliageWitnesses=Array.Empty<FoliageWitness>();private List<FoliageCandidate> foliagePlan;private object selectedValue,pendingViewportClipboard;private string status;
  private int refreshes,failRefresh;private int[] planned;private PrefabBase plannedDestination;
  private List<FoliageCandidate> PlanFoliageReplacement(Row r,bool batch,PrefabBase old,PrefabBase destination,int[] indices){planned=indices.ToArray();plannedDestination=destination;return new();}
  private void CreateFoliageCandidates(List<FoliageCandidate> p){}private void RemoveFoliageCandidates(List<FoliageCandidate> p){}
  private void WatchFoliageCandidates(List<FoliageCandidate> p,Row r,PrefabSnapshot s){}
  private void Refresh(PrefabBase r,PrefabBase[] a){refreshes++;if(failRefresh>0){failRefresh--;throw new Exception("refresh fault");}}
  private static int checks;private static void Check(bool ok,string s){if(!ok)throw new Exception(s);checks++;}
  private static (PropReplacerSystem,ObjectSubObjects,ReplacementUndoAction,PrefabBase,PrefabBase) Setup(bool batch){
   var s=new PropReplacerSystem();var root=new PrefabBase();s.context.Asset=root;var old=new PrefabBase();var replacement=new PrefabBase();
   var owner=new ObjectSubObjects{prefab=root,m_SubObjects=new[]{new ObjectSubObjectInfo{m_Object=old,position=3,probability=73,group=4,parent=-1},new ObjectSubObjectInfo{m_Object=replacement,position=9},new ObjectSubObjectInfo{m_Object=old,position=17}}};root.components.Add(owner);
   var before=PrefabSnapshot.Take(owner.m_SubObjects);owner.m_SubObjects[0].m_Object=replacement;if(batch)owner.m_SubObjects[2].m_Object=replacement;
   var row=new Row{Root=root,Owner=owner,ListField=typeof(ObjectSubObjects).GetField("m_SubObjects"),ReferenceField=typeof(ObjectSubObjectInfo).GetField("m_Object"),List=owner.m_SubObjects,Value=owner.m_SubObjects[0],Index=0};
   var action=s.MakeReplacementUndo(row,before,PrefabSnapshot.Take(owner.m_SubObjects),old,replacement,batch?new[]{0,2}:new[]{0},root,s.ReplacementGuards(row));return(s,owner,action,old,replacement);
  }
  internal static int Run(){
   foreach(bool batch in new[]{false,true}){var(s,o,a,old,replacement)=Setup(batch);Check(a.CanUndo,"New action ready");Check(a.Undo(),"Undo succeeds");Check(ReferenceEquals(o.m_SubObjects[0].m_Object,old)&&ReferenceEquals(o.m_SubObjects[2].m_Object,old),"Changed original references restored");Check(ReferenceEquals(o.m_SubObjects[1].m_Object,replacement),"Pre-existing replacement row retained");Check(o.m_SubObjects[0].position==3&&o.m_SubObjects[0].probability==73&&o.m_SubObjects[0].group==4&&o.m_SubObjects[0].parent==-1,"Placement and all settings restored");Check(s.planned.SequenceEqual(batch?new[]{0,2}:new[]{0})&&ReferenceEquals(s.plannedDestination,old),"Reverse foliage plan only includes changed rows and original prefab");Check(!a.CanUndo&&!a.Undo()&&s.refreshes==1,"One successful action consumed exactly once");}
   var x=Setup(false);x.Item2.m_SubObjects[1].position++;Check(!x.Item3.CanUndo&&!x.Item3.Undo()&&x.Item1.refreshes==0,"Unrelated source changes refuse before write");
   x=Setup(false);x.Item1.context.Asset=new();Check(!x.Item3.CanUndo,"Asset switch refuses");
   x=Setup(false);x.Item1.context.Asset=null;Check(x.Item3.ContextValid&&x.Item3.CanUndo&&x.Item3.Undo(),"Regeneration inspector deselection retains and restores pinned source");
   x=Setup(false);x.Item1.context.Asset=x.Item5;Check(x.Item3.ContextValid&&x.Item3.CanUndo&&x.Item3.Undo(),"Regeneration selection of replacement prefab retains pinned source");
   x=Setup(false);x.Item1.context.Asset=x.Item4;Check(x.Item3.ContextValid&&x.Item3.CanUndo,"Selection of original prefab retains action");
   x=Setup(false);x.Item1.context.DialogBusy=true;Check(!x.Item3.CanUndo,"Dialog refuses");
   x=Setup(false);x.Item1.context.EditorAvailable=false;Check(!x.Item3.CanUndo,"Editor exit refuses");
   x=Setup(false);x.Item2.active=false;Check(!x.Item3.CanUndo,"Component settings change refuses");
   x=Setup(false);x.Item2.prefab=new();Check(!x.Item3.CanUndo,"Component owner change refuses");
   x=Setup(false);x.Item1.context.Asset.components.Add(new ComponentBase());Check(!x.Item3.CanUndo,"Component topology change refuses");
   x=Setup(false);x.Item1.context.Asset.isReadOnly=true;Check(!x.Item3.CanUndo,"Read-only refuses");
   x=Setup(false);x.Item1.foliageWitnesses=new[]{new FoliageWitness()};Check(!x.Item3.CanUndo,"Native foliage settling blocks Undo");x.Item1.foliageWitnesses=Array.Empty<FoliageWitness>();Check(x.Item3.CanUndo,"Settled foliage permits Undo");x.Item1.foliageFailed=true;Check(!x.Item3.CanUndo,"Failed native regeneration blocks writes");
   x=Setup(true);x.Item1.failRefresh=1;try{x.Item3.Undo();throw new Exception("Expected refresh fault");}catch(Exception e)when(e.Message=="refresh fault"){}Check(ReferenceEquals(x.Item2.m_SubObjects[0].m_Object,x.Item5)&&ReferenceEquals(x.Item2.m_SubObjects[2].m_Object,x.Item5),"Refresh failure restores complete post-paste source");Check(x.Item3.CanUndo,"Rolled-back action not consumed");
   x=Setup(false);x.Item1.failRefresh=2;try{x.Item3.Undo();}catch{}Check(x.Item1.foliageFailed&&!x.Item3.CanUndo,"Rollback failure disables further writes");
   var ready=false;var calls=0;var action=new ReplacementUndoAction("test",()=>ready,()=>{calls++;return false;});Check(!action.Undo()&&calls==0,"Unavailable action cannot execute");ready=true;Check(!action.Undo()&&action.CanUndo,"False completion keeps action");
   return checks;
  }
 }
}
namespace MW.EditorTools.Modules.DeletionProtection {
 public sealed partial class ListRecovery {
  private bool CanAct=true,OperationBusy,applying,enabledFeature=true,Saving;private sealed class Settings{internal bool recovery=true;}private Settings settings=new();private PrefabBase asset,shortcutAsset;private sealed class Context{internal PrefabBase Asset=new();internal bool EditorAvailable=true,DialogBusy;}private Context context=new();private string status;private Trace bulldozerTrace=new();private class Trace{internal void Invalidate(){}}
  private void Clear(string s){replacementUndo=null;shortcutReplacement=null;status=s;}
  internal static int Run(){var s=new ListRecovery();int calls=0;var a=new ReplacementUndoAction("batch replacement (2 elements)",()=>true,()=>{calls++;return true;});s.RecordReplacementUndo(a);if(!ReferenceEquals(s.replacementUndo,a)||s.asset!=s.context.Asset)throw new Exception("Shared slot record");s.UndoReplacement();s.UndoReplacementSafe();if(calls!=1||s.replacementUndo!=null||s.applying)throw new Exception("Shared slot consumption");s.RecordReplacementUndo(new("single",()=>true,()=>true));s.Clear("move committed");if(s.replacementUndo!=null)throw new Exception("Invalidation");s.CanAct=false;try{s.RecordReplacementUndo(a);throw new Exception("Expected refusal");}catch(InvalidOperationException){}int checks=4;
s=new ListRecovery();bool valid=true;s.RecordReplacementUndo(new("reset regression",()=>true,()=>true,()=>valid));var pinned=s.replacementUndo;s.shortcutReplacement=pinned;s.shortcutAsset=s.context.Asset;s.context.Asset=null;s.CanAct=false;s.CheckReplacementContext();if(!ReferenceEquals(s.replacementUndo,pinned)||!s.ReplacementRecoveryReady||!s.ShortcutContextValid)throw new Exception("Disabled/deselected inspector must retain available replacement Undo and queued target");checks++;
s.Saving=true;if(s.ReplacementRecoveryReady)throw new Exception("Save gate");s.Saving=false;s.context.DialogBusy=true;if(s.ReplacementRecoveryReady)throw new Exception("Dialog gate");s.context.DialogBusy=false;s.OperationBusy=true;if(s.ReplacementRecoveryReady)throw new Exception("Busy gate");s.OperationBusy=false;checks+=3;
valid=false;s.CheckReplacementContext();if(s.replacementUndo!=null||s.ShortcutContextValid)throw new Exception("Real context invalidation must cancel slot and queued action");checks++;
s=new ListRecovery();s.RecordReplacementUndo(new("first",()=>true,()=>true));s.shortcutReplacement=s.replacementUndo;s.RecordReplacementUndo(new("second",()=>true,()=>true));if(s.replacementUndo.Label!="second"||s.shortcutReplacement!=null)throw new Exception("New action replaces slot and cancels old queued target");checks++;
s=new ListRecovery();s.shortcutAsset=s.context.Asset;if(!s.ShortcutContextValid)throw new Exception("Existing deletion shortcut same-context gate");s.context.Asset=new();if(s.ShortcutContextValid)throw new Exception("Existing deletion shortcut changed-context gate");checks+=2;
return checks;}
  private void UndoReplacementSafe(){if(replacementUndo?.CanUndo==true)UndoReplacement();}
 }
}
class Program{static void Main(){var count=MW.EditorTools.Modules.PropReplacer.PropReplacerSystem.Run()+MW.EditorTools.Modules.DeletionProtection.ListRecovery.Run();Console.WriteLine("PASS: "+count+" actual-source replacement Undo snapshot, single/batch, foliage plan, shared slot and rollback checks.");}}
