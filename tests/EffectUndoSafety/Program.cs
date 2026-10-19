#pragma warning disable CS0649,CS0414
using System;
using System.Linq;
using System.Collections.Generic;
namespace Unity.Entities {
struct Entity:IEquatable<Entity>{internal int Id;internal static Entity Null=>default;public bool Equals(Entity e)=>Id==e.Id;public override bool Equals(object o)=>o is Entity e&&Equals(e);public override int GetHashCode()=>Id;public static bool operator ==(Entity a,Entity b)=>a.Equals(b);public static bool operator !=(Entity a,Entity b)=>!a.Equals(b);}
class Manager{internal HashSet<Entity> Live=new();internal bool Exists(Entity e)=>Live.Contains(e);internal void DestroyEntity(Entity e)=>Live.Remove(e);}
class World{internal Manager EntityManager=new();}
}
namespace Unity.Collections {struct NativeList<T>{internal T[] Items;internal bool IsCreated=>Items!=null;internal T[] AsArray()=>Items;}}
namespace UnityEngine{static class Time{internal static int frameCount;}}
namespace Game.Prefabs{class ComponentBase{internal bool active=true;}class EffectSource:ComponentBase{public object m_Effects,m_AnimationCurves; public EffectSource(){}}class PrefabBase{internal string name="Effect";internal List<ComponentBase> components=new();internal T GetComponent<T>()where T:ComponentBase=>components.OfType<T>().SingleOrDefault();internal T AddComponent<T>()where T:ComponentBase,new(){var c=new T();components.Add(c);return c;}}}
namespace Game.Tools {
class ApplyPrefabsSystem{internal static void RemoveComponent<T>(Game.Prefabs.PrefabBase p)where T:Game.Prefabs.ComponentBase=>p.components.RemoveAll(c=>c is T);}
enum ApplyMode{None,Apply,Clear}
class ObjectToolSystem{internal ApplyMode applyMode;}
class BulldozeToolSystem{internal enum Mode{MainElements,SubElements,Everything}internal Mode actualMode;internal Unity.Collections.NativeList<ControlPoint> Points;}
struct ControlPoint{internal Unity.Entities.Entity m_OriginalEntity;}
}
namespace MW.EditorTools{static class Mod{internal static Logger Log=new();internal class Logger{internal void Info(string s){}internal void Warn(string s){}}}}
namespace MW.EditorTools.Modules.BulldozerRecovery {
sealed partial class BulldozerTrace {
    private class Value{internal Game.Prefabs.PrefabBase Container=new();internal int Pose;}
    private class EffectImage{internal Unity.Entities.Entity Original,Owner;internal Value Value=new();internal bool Valid=true,HasSeed,HasSurface,HasDecoration,HasEnabledBuffer;}
    private class Data{internal Func<object> Copy;internal object Materialize()=>Copy();} private class FieldValue{internal Game.Prefabs.ComponentBase Owner;internal System.Reflection.FieldInfo Field;internal Data Data;}private class Payload{internal bool[] Active=Array.Empty<bool>();internal Game.Prefabs.ComponentBase[] Components=Array.Empty<Game.Prefabs.ComponentBase>();internal List<FieldValue> Fields=new();internal bool Matches(object asset)=>true;}
    private class Graph{internal bool TryMap(Graph g,bool exact,out object map){map=null;return true;}}
    private class NetworkEntry{internal bool Move;internal Payload Before=new(),After=new();internal Game.Prefabs.PrefabBase Asset=new();internal object Transform=new(),Effects=new();internal EffectEntry EffectEntry;internal Graph Network=new();}
    private int witnessPose;
    private string isolatedEffectReason="test mismatch"; private class IsolatedEffectOwner{internal int WitnessPose;internal Payload Source=new();internal Game.Prefabs.PrefabBase Asset=new();}
    private IsolatedEffectOwner CaptureIsolatedEffectOwner(Unity.Entities.Entity id)=>new(){WitnessPose=witnessPose};
    private static bool IsolatedChildrenSame(IsolatedEffectOwner a,IsolatedEffectOwner b)=>a.WitnessPose==b.WitnessPose;
    private static bool IsolatedSources(IsolatedEffectOwner owner)=>true;
    private bool IsolatedAfter(EffectEntry e,IsolatedEffectOwner owner,EffectImage target)=>ownerAfter;
    private bool CanIsolatedEffectUndo(EffectEntry e)=>ownerValid&&IsolatedChildrenSame(e.IsolatedAfter,CaptureIsolatedEffectOwner(e.Before.Owner));
    private void RestoreIsolatedEffect(EffectEntry e){if(e.Move)WriteEffectValues(e.Live,e.Before);else CreateStandaloneEffect(e);}
    private bool IsolatedEffectRestored(EffectEntry e)=>ownerValid&&SameEffect(e.Before,CaptureEffect(e.Live));
    private class Context{internal bool EditorAvailable=true,DialogBusy;}
    private Unity.Entities.World world=new();private Context context=new();private Func<bool> AutomaticCaptureAllowed=()=>true;
    private bool moveCommitted,moveRestoring,movePreviewEntered,objectAccepted,objectMoveApplying,AreaMovePreview,ownerValid=true,ownerAfter=true,settled=true;
    private Unity.Entities.Entity movePreviewTarget;private object moveCandidate;private bool MovePreview=>movePreviewEntered;private bool Busy=>EffectBusy;
    private string Status,previous="prop deletion";private int writes,creates,boundStarts;
    private Dictionary<Unity.Entities.Entity,EffectImage> images=new();
    private System.Reflection.FieldInfo controlPoints=typeof(Game.Tools.BulldozeToolSystem).GetField("Points",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
    private bool IsEffectContainer(Unity.Entities.Entity id)=>images.ContainsKey(id)&&world.EntityManager.Exists(id);
    private EffectImage CaptureEffect(Unity.Entities.Entity id){if(!IsEffectContainer(id)||!settled)throw new NotSupportedException("not settled");var e=images[id];return new(){Original=id,Owner=e.Owner,Valid=e.Valid,Value=new(){Container=e.Value.Container,Pose=e.Value.Pose},HasEnabledBuffer=e.HasEnabledBuffer,HasSeed=e.HasSeed,HasSurface=e.HasSurface,HasDecoration=e.HasDecoration};}
    private void TickEffectParentPreflight(){}
    private void ClearEffectRenderRefresh(){}
    private bool EffectRenderReady(Unity.Entities.Entity id)=>true;
    private void InvalidateEffectHover(Unity.Entities.Entity id,Unity.Entities.Entity owner){}
    private bool EffectSettled(Unity.Entities.Entity id)=>world.EntityManager.Exists(id)&&settled;
    private NetworkEntry CaptureMoveOwner(Unity.Entities.Entity id,bool effect=false)=>new();
    private string EffectReferenceDifference(EffectImage e)=>"test reference"; private string EffectDetails(EffectImage e)=>"test image"; private string EffectSourceDetails(NetworkEntry e)=>"test source"; private object EffectRows(Payload p)=>null;
    private static class EffectEditPolicy{internal static bool Sources(object rows,object effects,object transform)=>true;internal static bool Move(Value a,Value b,bool bound=false)=>ReferenceEquals(a.Container,b.Container);}
    private static bool SameEffect(EffectImage a,EffectImage b)=>a.Owner==b.Owner&&a.Value.Pose==b.Value.Pose&&ReferenceEquals(a.Value.Container,b.Value.Container);
    private void ClearMovePrevious(){previous=null;effectHistory.Clear();}
    private void ClearMove(){movePreviewEntered=false;moveCandidate=null;}
    private void WriteEffectValues(Unity.Entities.Entity id,EffectImage before){writes++;images[id]=before;}
    private void CreateStandaloneEffect(EffectEntry e){creates++;e.Live=new(){Id=90};world.EntityManager.Live.Add(e.Live);images[e.Live]=e.Before;}
    private bool EffectOwnerAfter(EffectEntry e,NetworkEntry owner,EffectImage actual)=>ownerAfter;
    private void PrepareEffectBoundEntry(EffectEntry e){e.BoundBefore.EffectEntry=e;}
    private bool NetworkOwnerValid(NetworkEntry e)=>ownerValid;
    private bool NetworkCompanionsMatch(NetworkEntry e,bool sources)=>true;
    private Graph CaptureAreaNetwork(Unity.Entities.Entity id,bool invalid=false,bool anchors=false)=>new();
    private void StartBoundRestoration(NetworkEntry e){boundStarts++;}
    private void Step(int frames=1){for(int i=0;i<frames;i++){UnityEngine.Time.frameCount++;TickEffects();}}
    private static (BulldozerTrace,Unity.Entities.Entity,Game.Tools.ObjectToolSystem) Ready(bool bound=false){var t=new BulldozerTrace();var id=new Unity.Entities.Entity{Id=1};t.images[id]=new(){Original=id,Owner=bound?new(){Id=2}:default};t.world.EntityManager.Live.Add(id);return(t,id,new(){applyMode=Game.Tools.ApplyMode.Apply});}
    internal static void Run(){int n=0;void Check(bool v,string s){if(!v)throw new Exception(s);n++;}
        var (t,id,tool)=Ready();Check(t.TryBeginEffectMove(id)&&t.previous!=null,"effect preview preserves previous slot");
        t.objectAccepted=false;t.objectMoveApplying=true;t.FinishEffectMove(tool);Check(t.effectMovePreview&&t.previous!=null,"native denial cannot commit or clear previous slot");
        t.movePreviewEntered=false;t.Step();Check(!t.effectMovePreview&&t.previous!=null,"cancelled effect preview preserves previous slot");
        t.TryBeginEffectMove(id);t.objectAccepted=t.objectMoveApplying=true;t.FinishEffectMove(tool);t.Step(4);Check(t.effectHistory.Latest==null&&t.previous!=null,"no-op move preserves prior action");
        t.TryBeginEffectMove(id);t.images[id].Value.Pose=5;t.objectAccepted=t.objectMoveApplying=true;t.FinishEffectMove(tool);t.Step(2);Check(t.effectHistory.Latest==null&&t.writes==0,"accepted move waits for complete stable observation");t.Step(3);
        Check(t.CanEffectUndo&&t.effectHistory.UndoCount==1&&t.previous==null,"supported effect move takes single last-action slot");
        t.UndoEffect();Check(t.writes==1&&t.images[id].Value.Pose==0,"standalone move routes to original-value restoration");t.Step(4);Check(t.effectHistory.Latest==null,"successful move Undo consumes slot");
        (t,id,tool)=Ready();t.TryBeginEffectMove(id);t.images[id].Value.Pose=7;t.objectAccepted=t.objectMoveApplying=true;t.FinishEffectMove(tool);t.Step(5);t.images[id].Value.Pose=8;
        Check(!t.CanEffectUndo,"stale after-image cannot be overwritten");t.UndoEffect();Check(t.writes==0,"stale preflight writes nothing");
        (t,id,tool)=Ready();var bulldozer=new Game.Tools.BulldozeToolSystem{actualMode=Game.Tools.BulldozeToolSystem.Mode.MainElements,Points=new(){Items=new[]{new Game.Tools.ControlPoint{m_OriginalEntity=id}}}};
        Check(t.BeginEffectDeletion(bulldozer)&&t.effectInDelete,"direct effect deletion starts capture");t.effectAccepted=false;t.EndEffectDeletion();Check(t.effectPending==null&&t.effectHistory.Latest==null,"denied deletion cannot record");
        t.BeginEffectDeletion(bulldozer);t.effectAccepted=true;t.world.EntityManager.DestroyEntity(id);t.EndEffectDeletion();t.Step(4);Check(t.CanEffectUndo,"completed deletion records after target disappears");t.UndoEffect();Check(t.creates==1&&t.effectPending.Live!=id,"standalone deletion routes to fresh native creation");t.Step(4);Check(t.effectHistory.Latest==null,"successful deletion Undo consumes slot");
        (t,id,tool)=Ready(true);t.TryBeginEffectMove(id);t.images[id].Value.Pose=9;t.objectAccepted=t.objectMoveApplying=true;t.FinishEffectMove(tool);t.Step(5);Check(!t.CanEffectUndo,"bound move waits beyond initial target settlement");t.witnessPose=1;t.Step(20);Check(!t.CanEffectUndo,"late child pose resets full after-image settlement");t.witnessPose=2;t.Step(29);Check(!t.CanEffectUndo,"another child update restarts stable observation");t.Step(2);Check(t.CanEffectUndo&&t.effectHistory.Latest.IsolatedAfter.WitnessPose==2,"bound move records final native child witness pose");t.witnessPose=3;Check(!t.CanEffectUndo,"child changes after readiness still block Undo");t.witnessPose=2;t.ownerValid=false;Check(!t.CanEffectUndo,"changed owner blocks bound Undo");t.ownerValid=true;t.UndoEffect();Check(t.boundStarts==0&&t.writes==1,"bound effect routes through isolated restoration without parent regeneration");t.Step(31);Check(t.effectHistory.Latest==null,"isolated bound move consumes slot after verification");
        (t,id,tool)=Ready(true);t.TryBeginEffectMove(id);t.images[id].Value.Pose=9;t.ownerAfter=false;t.objectAccepted=t.objectMoveApplying=true;t.FinishEffectMove(tool);t.Step(125);Check(t.effectHistory.Latest==null&&!t.EffectBusy&&t.writes==0,"failed owner/companion proof times out without restore writes");
        (t,id,tool)=Ready(true);bulldozer=new(){actualMode=Game.Tools.BulldozeToolSystem.Mode.Everything,Points=new(){Items=new[]{new Game.Tools.ControlPoint{m_OriginalEntity=id}}}};
        t.BeginEffectDeletion(bulldozer);t.effectAccepted=true;t.world.EntityManager.DestroyEntity(id);t.EndEffectDeletion();t.Step(5);Check(!t.CanEffectUndo,"bound deletion waits beyond initial disappearance");
        t.witnessPose=1;t.Step(20);Check(!t.CanEffectUndo,"late deletion child updates reset settlement");t.witnessPose=2;t.Step(31);Check(t.CanEffectUndo&&t.effectHistory.Latest.IsolatedAfter.WitnessPose==2,"bound deletion records final native witnesses");t.UndoEffect();t.Step(31);Check(t.effectHistory.Latest==null&&t.creates==1,"settled bound deletion restores and consumes one slot");
        (t,id,tool)=Ready();t.AutomaticCaptureAllowed=()=>false;t.TryBeginEffectMove(id);t.objectAccepted=t.objectMoveApplying=true;t.FinishEffectMove(tool);Check(t.previous==null&&t.effectHistory.Latest==null,"accepted uncaptured effect move invalidates stale action");
                (t,id,tool)=Ready(true);t.images[id].HasEnabledBuffer=true;t.TryBeginEffectMove(id);t.images[id].Value.Pose=9;t.images[id].HasEnabledBuffer=false;t.objectAccepted=t.objectMoveApplying=true;t.FinishEffectMove(tool);t.Step(125);Check(t.effectHistory.Latest==null&&t.writes==0,"native effect registry presence cannot disappear during a supported move");
        t=new();var isolatedSource=new Game.Prefabs.EffectSource();var untouched=new Game.Prefabs.ComponentBase();
        var iso=new IsolatedEffectOwner();iso.Asset.components.AddRange(new Game.Prefabs.ComponentBase[]{untouched,isolatedSource});
        var effectsField=typeof(Game.Prefabs.EffectSource).GetField("m_Effects");var curvesField=typeof(Game.Prefabs.EffectSource).GetField("m_AnimationCurves");
        iso.Source=new(){Components=new Game.Prefabs.ComponentBase[]{untouched,isolatedSource},Active=new[]{true,false},Fields=new(){new(){Owner=isolatedSource,Field=effectsField,Data=new(){Copy=()=>new List<int>{7,8}}},new(){Owner=isolatedSource,Field=curvesField,Data=new(){Copy=()=>new List<int>{9}}}}};
        t.ApplyIsolatedEffectSource(iso);var firstList=isolatedSource.m_Effects;
        Check(((List<int>)firstList).SequenceEqual(new[]{7,8})&&!isolatedSource.active,"isolated writer restores effect rows and original enabled state");
        Check(ReferenceEquals(iso.Asset.components[0],untouched),"isolated source write preserves unrelated component identity/order");
        ((List<int>)firstList).Clear();t.ApplyIsolatedEffectSource(iso);
        Check(!ReferenceEquals(firstList,isolatedSource.m_Effects)&&((List<int>)isolatedSource.m_Effects).Count==2,"isolated writer rematerializes detached lists for repeated use");
        iso.Asset.components.Remove(isolatedSource);t.ApplyIsolatedEffectSource(iso);var recreated=iso.Asset.GetComponent<Game.Prefabs.EffectSource>();
        Check(!ReferenceEquals(recreated,isolatedSource)&&iso.Asset.components.IndexOf(recreated)==1,"isolated last-effect source recreation preserves component order");
        Check(((List<int>)recreated.m_AnimationCurves).Single()==9&&!recreated.active,"isolated source recreation preserves ancillary source fields and enabled state");
        iso.Source=new(){Components=new Game.Prefabs.ComponentBase[]{untouched},Active=new[]{true}};t.ApplyIsolatedEffectSource(iso);
        Check(iso.Asset.GetComponent<Game.Prefabs.EffectSource>()==null&&ReferenceEquals(iso.Asset.components.Single(),untouched),"isolated rollback to source absence leaves unrelated components alone");
        t=new();var left=new Game.Prefabs.ComponentBase();var right=new Game.Prefabs.ComponentBase();var originalSource=new Game.Prefabs.EffectSource();
        var entry=new NetworkEntry{EffectEntry=new(),Before=new(){Components=new Game.Prefabs.ComponentBase[]{left,originalSource,right},Fields=new(){new(){Owner=originalSource}}},After=new(){Components=new Game.Prefabs.ComponentBase[]{left,right}}};
        entry.Asset.components.AddRange(new[]{left,right});t.AlignEffectSource(entry.Before,entry);
        var fresh=entry.Asset.GetComponent<Game.Prefabs.EffectSource>();Check(fresh!=null&&!ReferenceEquals(fresh,originalSource),"last effect source recreated rather than reusing removed component");
        Check(entry.Asset.components[0]==left&&entry.Asset.components[1]==fresh&&entry.Asset.components[2]==right,"effect source component order preserved");
        Check(entry.Before.Components[1]==fresh&&entry.Before.Fields[0].Owner==fresh,"detached payload rebound to fresh source component");
        t.AlignEffectSource(entry.After,entry);Check(entry.Asset.components.SequenceEqual(new[]{left,right}),"source rollback restores original component absence");
        var changedSource=new Game.Prefabs.EffectSource();entry.Asset.components.Insert(1,changedSource);bool refused=false;try{t.AlignEffectSource(entry.Before,entry);}catch(InvalidOperationException){refused=true;}
        Check(refused&&entry.Asset.GetComponent<Game.Prefabs.EffectSource>()==changedSource,"unrelated replacement source is never overwritten");
        Console.WriteLine("PASS: "+n+" actual-source effect lifecycle assertions; native timing remains unproven.");
    }
}}
class Program{static void Main()=>MW.EditorTools.Modules.BulldozerRecovery.BulldozerTrace.Run();}
