#pragma warning disable CS0649,CS0414
using System;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
namespace Unity.Entities {
struct Entity:IEquatable<Entity>{internal int Id;internal static Entity Null=>default;public bool Equals(Entity e)=>Id==e.Id;public override bool Equals(object o)=>o is Entity e&&Equals(e);public override int GetHashCode()=>Id;public static bool operator ==(Entity a,Entity b)=>a.Equals(b);public static bool operator !=(Entity a,Entity b)=>!a.Equals(b);}
class World{internal Manager EntityManager=new();}
class Manager{internal bool Temp;internal bool HasComponent<T>(Entity e)=>Temp;
internal System.Collections.Generic.Dictionary<Type,object> Values=new();internal Type FailType;
internal System.Collections.Generic.List<T> GetBuffer<T>(Entity id)=>(System.Collections.Generic.List<T>)Values[typeof(T)];
internal bool HasBuffer<T>(Entity id)=>Values.ContainsKey(typeof(T));internal void RemoveComponent<T>(Entity id)=>Values.Remove(typeof(T));
internal void SetComponentData<T>(Entity id,T value){if(typeof(T)==FailType)throw new Exception("injected write failure");Values[typeof(T)]=value;}
}
}
namespace Unity.Jobs {struct JobHandle{internal int Id;}}
namespace UnityEngine {static class Time{internal static int frameCount=100;}}
namespace Unity.Collections {struct NativeList<T>{internal T[] Items;internal bool IsCreated=>Items!=null;internal T[] AsArray()=>Items;}}
namespace Game.Areas {enum AreaFlags {Complete=1}struct Node {internal float3 m_Position;internal float m_Elevation;}struct Expand{internal float3 m_Offset;}}
namespace Game.Common {struct Updated{}struct BatchesUpdated{}}
namespace MW.EditorTools {static class Mod {internal static Logger Log=new();internal class Logger{internal void Warn(string message){}internal void Info(string message){}}}}
namespace Game.Prefabs {class PrefabBase{internal string name="area";}class AreaPrefab:PrefabBase{}class ObjectSubAreaInfo{internal AreaPrefab m_AreaPrefab;internal float3[] m_NodePositions;internal int[] m_ParentMeshes;}}
namespace Game.Tools {
[Flags] enum TempFlags{Modify=1,Delete=2,Create=4}struct Temp{internal Unity.Entities.Entity m_Original;internal TempFlags m_Flags;}struct LocalNodeCache {internal float3 m_Position;internal int m_ParentMesh;}
struct ControlPoint{internal Unity.Entities.Entity m_OriginalEntity;}
enum ApplyMode{None,Apply,Clear}
class ToolBaseSystem{internal Unity.Entities.World World;internal bool Allow=true;internal bool GetAllowApply()=>Allow;}
class AreaToolSystem:ToolBaseSystem{internal enum Mode{Edit,Generate}internal Mode actualMode;internal ApplyMode applyMode;internal Unity.Entities.Entity recreate;internal int State;internal Unity.Collections.NativeList<ControlPoint> Points;}
}
namespace MW.EditorTools.Modules.BulldozerRecovery {
static class AreaRemovalPolicy{internal static bool ValidRow(Game.Prefabs.ObjectSubAreaInfo row)=>row!=null;}
sealed partial class BulldozerTrace {
private static BulldozerTrace active;
private Unity.Entities.World world=new();private Context context=new();private bool Busy,areaMoveInApply,areaMoveCommitted,areaMoveRestoring,areaMovePreview,areaMoveApplyAllowed,areaMoveApplyTarget;
private int areaMoveApplyState,areaMoveFrame,areaMoveStable,areaMoveLastFrame;private string Status;private AreaMoveEntry areaMoveCandidate;
private FieldInfo areaMoveStateField=typeof(Game.Tools.AreaToolSystem).GetField("State",BindingFlags.Instance|BindingFlags.NonPublic);
private FieldInfo areaMovePointsField=typeof(Game.Tools.AreaToolSystem).GetField("Points",BindingFlags.Instance|BindingFlags.NonPublic);
private string prior="prop move";private int captures,clears;private bool supported=true;
private class Context{internal bool EditorAvailable=true;}
private class AreaMoveEntry{internal Unity.Entities.Entity Target,Owner,Live;internal int AddedNode=-1;internal StandaloneAreaEntry Before,After;internal NetworkEntry BoundBefore,BoundAfter;internal bool Bound=>BoundBefore!=null;}
private class StandaloneAreaEntry{internal StandaloneAreaData Data;internal bool HasSeed,HasExpand;internal int Seed;internal Game.Areas.Expand[] Expand;internal Network Network;internal bool Valid=>Data!=null;}
private void PutMain<T>(Unity.Entities.Entity id,T value)=>world.EntityManager.SetComponentData(id,value);
private void BeginAreaMove(Unity.Entities.Entity e){captures++;areaMovePreview=true;areaMoveCandidate=supported?new AreaMoveEntry{Target=e}:null;}
private void CancelAreaMovePreview(){if(areaMoveCommitted||areaMoveRestoring)return;areaMoveCandidate=null;areaMovePreview=false;areaMoveInApply=false;}
private void ClearMovePrevious(){prior=null;clears++;}
private void ClearMove(){}
private bool previewTargetsValid=true,areaMoveHookFailed,previewTargetsThrow;
private bool AreaMovePreviewTargets(Game.Tools.AreaToolSystem tool,Unity.Entities.Entity target){if(previewTargetsThrow)throw new Exception("injected query failure");return previewTargetsValid;}
internal static void Run(){int n=0;void Check(bool value,string name){if(!value)throw new Exception("Area hooks: "+name);n++;}
var id=new Unity.Entities.Entity{Id=42};var tool=new Game.Tools.AreaToolSystem();active=null;
Check(BeforeAreaRecreate(tool,id),"unattached pass through");active=new();tool.World=active.world;
active.Busy=true;Check(!BeforeAreaRecreate(tool,id)&&active.captures==0&&active.prior!=null,"busy preview blocked without clearing slot");
var deps=new Unity.Jobs.JobHandle{Id=7};var result=new Unity.Jobs.JobHandle{Id=99};
Check(!BeforeAreaMoveApply(tool,deps,ref result,out var observe)&&!observe&&result.Id==7,"busy Apply returns original dependency");
result.Id=99;Check(!BeforeAreaMoveCancel(tool,deps,ref result,out observe)&&result.Id==7,"busy Cancel returns dependency");active.Busy=false;
Check(BeforeAreaRecreate(tool,id)&&active.areaMoveCandidate.Target==id&&active.prior!=null,"relocate captures while preserving slot");
tool.recreate=id;tool.State=1;tool.applyMode=Game.Tools.ApplyMode.Clear;
Check(BeforeAreaMoveApply(tool,deps,ref result,out observe)&&observe,"preview observed");AreaMoveAllowed(tool,tool.Allow);AfterAreaMoveApply(tool,observe);
Check(!active.areaMoveCommitted&&active.prior!=null,"preview does not commit or clear");
tool.Allow=false;BeforeAreaMoveApply(tool,deps,ref result,out observe);tool.applyMode=Game.Tools.ApplyMode.Apply;AreaMoveAllowed(tool,tool.Allow);AfterAreaMoveApply(tool,observe);
Check(!active.areaMoveCommitted&&active.prior!=null,"denial preserves previous even with stale Apply value");
tool.Allow=true;BeforeAreaMoveApply(tool,deps,ref result,out observe);AreaMoveAllowed(tool,tool.Allow);AfterAreaMoveApply(tool,observe);
Check(active.areaMoveCommitted&&active.prior!=null&&active.clears==0,"accepted relocation awaits stable native observation before slot replacement");
Check(active.areaMoveFrame==100&&active.areaMoveStable==0&&active.areaMoveLastFrame==-1,"stable window initialized");
AfterAreaMoveCancel(tool);Check(active.areaMoveCommitted&&active.areaMoveCandidate!=null,"late cancel cannot discard committed observation");
active.areaMoveCommitted=false;tool.State=0;AfterAreaMoveCancel(tool);Check(!active.areaMovePreview&&active.areaMoveCandidate==null&&active.prior!=null,"cancel preview preserves completed prior action");
tool.recreate=Unity.Entities.Entity.Null;tool.State=0;tool.applyMode=Game.Tools.ApplyMode.Clear;tool.Points=new(){Items=new[]{new Game.Tools.ControlPoint{m_OriginalEntity=id}}};
BeforeAreaMoveApply(tool,deps,ref result,out observe);AreaMoveAllowed(tool,tool.Allow);AfterAreaMoveApply(tool,observe);
Check(active.areaMoveCandidate.Target==id&&!active.areaMoveCommitted,"existing-node edit captures before native drag begins");
tool.State=2;BeforeAreaMoveApply(tool,deps,ref result,out observe);tool.applyMode=Game.Tools.ApplyMode.Apply;AreaMoveAllowed(tool,tool.Allow);AfterAreaMoveApply(tool,observe);
Check(active.areaMoveCommitted,"accepted node move commits");
active.areaMoveCommitted=false;active.CancelAreaMovePreview();active.supported=false;tool.State=0;tool.applyMode=Game.Tools.ApplyMode.Clear;
BeforeAreaMoveApply(tool,deps,ref result,out observe);AreaMoveAllowed(tool,tool.Allow);AfterAreaMoveApply(tool,observe);tool.State=2;BeforeAreaMoveApply(tool,deps,ref result,out observe);tool.applyMode=Game.Tools.ApplyMode.Apply;AreaMoveAllowed(tool,tool.Allow);AfterAreaMoveApply(tool,observe);
Check(active.prior==null&&active.clears==1&&!active.areaMovePreview,"unsupported completed edit clears stale slot");
active=new();tool.World=active.world;tool.State=0;tool.Points=new(){Items=new[]{new Game.Tools.ControlPoint{m_OriginalEntity=id},new Game.Tools.ControlPoint{m_OriginalEntity=new(){Id=43}}}};tool.applyMode=Game.Tools.ApplyMode.Clear;
BeforeAreaMoveApply(tool,deps,ref result,out observe);AreaMoveAllowed(tool,tool.Allow);AfterAreaMoveApply(tool,observe);Check(active.captures==0,"multiple targets not captured");
tool.Points=new(){Items=new[]{new Game.Tools.ControlPoint{m_OriginalEntity=id}}};active.world.EntityManager.Temp=true;
BeforeAreaMoveApply(tool,deps,ref result,out observe);AreaMoveAllowed(tool,tool.Allow);AfterAreaMoveApply(tool,observe);Check(active.captures==0,"temporary target refused");
active.world.EntityManager.Temp=false;tool.actualMode=Game.Tools.AreaToolSystem.Mode.Generate;tool.State=0;
BeforeAreaMoveApply(tool,deps,ref result,out observe);tool.applyMode=Game.Tools.ApplyMode.Apply;AreaMoveAllowed(tool,tool.Allow);AfterAreaMoveApply(tool,observe);Check(active.captures==0&&active.prior==null,"generation not recorded as move");
active=new();tool.World=active.world;BeforeAreaRecreate(tool,id);BeforeAreaRecreate(tool,Unity.Entities.Entity.Null);Check(!active.areaMovePreview&&active.prior!=null,"clearing recreate preserves prior action");
active=new();tool.World=active.world;tool.actualMode=Game.Tools.AreaToolSystem.Mode.Edit;tool.recreate=id;tool.State=1;BeforeAreaRecreate(tool,id);active.previewTargetsValid=false;
BeforeAreaMoveApply(tool,deps,ref result,out observe);tool.applyMode=Game.Tools.ApplyMode.Apply;AreaMoveAllowed(tool,tool.Allow);AfterAreaMoveApply(tool,observe);Check(!active.areaMoveCommitted&&active.prior==null,"multiple/new/deleted preview outputs cannot become move Undo");
active=new();tool.World=active.world;tool.recreate=id;tool.State=1;BeforeAreaRecreate(tool,id);active.previewTargetsThrow=true;tool.applyMode=Game.Tools.ApplyMode.Clear;
Check(BeforeAreaMoveApply(tool,deps,ref result,out observe)&&observe&&active.areaMoveHookFailed,"capture exception preserves native edit");tool.applyMode=Game.Tools.ApplyMode.Apply;AreaMoveAllowed(tool,tool.Allow);AfterAreaMoveApply(tool,observe);
Check(active.prior==null&&!active.areaMoveCommitted&&!active.areaMovePreview,"failed capture cannot leave stale previous Undo after accepted native edit");
active=new();tool.World=active.world;tool.Allow=true;tool.recreate=Unity.Entities.Entity.Null;tool.State=2;tool.Points=new(){Items=new[]{new Game.Tools.ControlPoint{m_OriginalEntity=new(){Id=77}}}};BeforeAreaRecreate(tool,id);
BeforeAreaMoveApply(tool,deps,ref result,out observe);Check(active.areaMoveApplyTarget,"verified native preview proves identity despite release cursor pointing elsewhere");AreaMoveAllowed(tool,true);tool.applyMode=Game.Tools.ApplyMode.Apply;AfterAreaMoveApply(tool,observe);Check(active.areaMoveCommitted,"snapped release cursor no longer discards accepted move");
Check(!AreaMovePolicy.PreviewActive(true,0,false,false),"idle active area tool permits previous Undo");
Check(AreaMovePolicy.PreviewActive(true,2,false,false),"active drag still blocks Undo");
Check(AreaMovePolicy.PreviewActive(true,0,true,false),"recreate preview blocks Undo before native state initialization");
Check(AreaMovePolicy.PreviewActive(true,0,false,true),"native Apply cannot release preview guard early");
Check(!AreaMovePolicy.PreviewActive(false,2,false,false),"leaving area tool ends uncommitted preview");
active=new();tool.World=active.world;active.areaMoveInApply=false;AreaMoveAllowed(tool,true);Check(!active.areaMoveApplyAllowed,"acceptance outside observed native Apply ignored");
active.areaMoveInApply=true;var other=new Game.Tools.AreaToolSystem{World=new Unity.Entities.World()};AreaMoveAllowed(other,true);Check(!active.areaMoveApplyAllowed,"other world's acceptance ignored");
AreaMoveAllowed(new Game.Tools.ToolBaseSystem{World=active.world},true);Check(!active.areaMoveApplyAllowed,"non-area tool acceptance leaves area observation untouched");
active=new();var manager=active.world.EntityManager;
var nodes=new[]{new Game.Areas.Node{m_Position=new float3(0,2,0),m_Elevation=1},new Game.Areas.Node{m_Position=new float3(4,2,0)},new Game.Areas.Node{m_Position=new float3(0,2,4)}};
var cache=nodes.Select(p=>new Game.Tools.LocalNodeCache{m_Position=p.m_Position,m_ParentMesh=-1}).ToArray();
var image=new StandaloneAreaEntry{Data=new StandaloneAreaData(Game.Areas.AreaFlags.Complete,nodes,cache),HasSeed=true,Seed=99};
image.HasExpand=true;image.Expand=new[]{new Game.Areas.Expand{m_Offset=new float3(3,2,1)}};manager.Values[typeof(Game.Areas.Expand)]=new System.Collections.Generic.List<Game.Areas.Expand>{new()};
manager.Values[typeof(Game.Areas.Node)]=new System.Collections.Generic.List<Game.Areas.Node>{new()};manager.Values[typeof(Game.Tools.LocalNodeCache)]=new System.Collections.Generic.List<Game.Tools.LocalNodeCache>{new()};manager.Values[typeof(string)]="companion sentinel";
nodes[0].m_Position.x=999;cache[0].m_ParentMesh=99;active.WriteAreaMoveGeometry(id,image);
Check(manager.GetBuffer<Game.Areas.Node>(id).SequenceEqual(image.Data.Nodes),"actual restore writes detached node positions and elevations");
Check(manager.GetBuffer<Game.Tools.LocalNodeCache>(id).SequenceEqual(image.Data.Local),"actual restore writes original cache attachments");
Check(manager.GetBuffer<Game.Areas.Expand>(id).SequenceEqual(image.Expand),"captured surface expansion geometry restored");
Check((int)manager.Values[typeof(int)]==99,"captured seed restored");
Check(manager.Values.ContainsKey(typeof(Game.Common.Updated))&&manager.Values.ContainsKey(typeof(Game.Common.BatchesUpdated)),"native geometry/render update tags issued");
Check((string)manager.Values[typeof(string)]=="companion sentinel","unrelated typed state untouched");
image.Data=new StandaloneAreaData(Game.Areas.AreaFlags.Complete,image.Data.Nodes,null);active.WriteAreaMoveGeometry(id,image);
Check(!manager.HasBuffer<Game.Tools.LocalNodeCache>(id),"native-added optional cache removed to restore original absence");
manager.FailType=typeof(Game.Common.Updated);bool failed=false;try{active.WriteAreaMoveGeometry(id,image);}catch(Exception){failed=true;}
Check(failed,"partial native write failure propagates to caller fail-stop path");
var sibling=new Unity.Entities.Entity{Id=43};
var targetTemp=new Game.Tools.Temp{m_Original=id,m_Flags=Game.Tools.TempFlags.Modify};
var siblingTemp=new Game.Tools.Temp{m_Original=sibling};
Check(AreaMovePreviewPolicy.Matches(new[]{targetTemp},id,null),"single standalone modified target accepted");
Check(AreaMovePreviewPolicy.Matches(new[]{siblingTemp,targetTemp},id,new[]{sibling}),"captured unchanged sibling permits bound move");
Check(AreaMovePreviewPolicy.Matches(new[]{targetTemp,siblingTemp},id,new[]{sibling}),"preview order does not matter");
Check(!AreaMovePreviewPolicy.Matches(new[]{targetTemp,siblingTemp},id,null),"standalone cannot acquire siblings");
Check(!AreaMovePreviewPolicy.Matches(new[]{targetTemp,siblingTemp},id,new[]{new Unity.Entities.Entity{Id=99}}),"unknown sibling refused");
Check(!AreaMovePreviewPolicy.Matches(new[]{targetTemp,targetTemp},id,new[]{sibling}),"duplicate target refused");
Check(!AreaMovePreviewPolicy.Matches(new[]{targetTemp,siblingTemp,siblingTemp},id,new[]{sibling}),"duplicate sibling refused");
siblingTemp.m_Flags=Game.Tools.TempFlags.Modify;Check(!AreaMovePreviewPolicy.Matches(new[]{targetTemp,siblingTemp},id,new[]{sibling}),"second modified polygon refused");
siblingTemp.m_Flags=Game.Tools.TempFlags.Delete;Check(!AreaMovePreviewPolicy.Matches(new[]{targetTemp,siblingTemp},id,new[]{sibling}),"deleted sibling refused");
siblingTemp.m_Flags=Game.Tools.TempFlags.Create;Check(!AreaMovePreviewPolicy.Matches(new[]{targetTemp,siblingTemp},id,new[]{sibling}),"created sibling refused");
siblingTemp=default;Check(!AreaMovePreviewPolicy.Matches(new[]{targetTemp,siblingTemp},id,new[]{sibling}),"new original-less polygon refused");
targetTemp.m_Flags=Game.Tools.TempFlags.Delete;Check(!AreaMovePreviewPolicy.Matches(new[]{targetTemp},id,null),"deleted target refused");
Check(!AreaMovePreviewPolicy.Matches(new[]{siblingTemp},id,new[]{sibling}),"missing target refused");
Check(!AreaMovePreviewPolicy.Matches(Array.Empty<Game.Tools.Temp>(),id,null),"empty preview refused");
active=new();tool.World=active.world;tool.actualMode=Game.Tools.AreaToolSystem.Mode.Edit;tool.recreate=Unity.Entities.Entity.Null;tool.State=0;tool.Points=new(){Items=new[]{new Game.Tools.ControlPoint{m_OriginalEntity=id}}};tool.applyMode=Game.Tools.ApplyMode.Clear;
BeforeAreaMoveCancel(tool,deps,ref result,out observe);AreaMoveAllowed(tool,true);tool.State=3;AfterAreaMoveCancel(tool,observe);
Check(active.areaMovePreview&&active.areaMoveCandidate!=null&&!active.areaMoveCommitted&&active.prior!=null,"right-click node removal captures before-image and preserves pending preview");
BeforeAreaMoveCancel(tool,deps,ref result,out observe);AreaMoveAllowed(tool,true);tool.State=0;tool.applyMode=Game.Tools.ApplyMode.Apply;AfterAreaMoveCancel(tool,observe);
Check(active.areaMoveCommitted&&active.prior!=null,"native accepted Remove completion commits for stable observation");
active=new();tool.World=active.world;BeforeAreaRecreate(tool,id);tool.State=3;
BeforeAreaMoveCancel(tool,deps,ref result,out observe);AreaMoveAllowed(tool,false);tool.State=0;tool.applyMode=Game.Tools.ApplyMode.Clear;AfterAreaMoveCancel(tool,observe);
Check(!active.areaMoveCommitted&&!active.areaMovePreview&&active.prior!=null,"denied removal retains previous completed Undo");
active=new();tool.World=active.world;BeforeAreaRecreate(tool,id);active.areaMoveInApply=true;tool.State=1;
Check(BeforeAreaMoveCancel(tool,deps,ref result,out observe)&&!observe,"nested native Cancel cleanup cannot start a second observation");AfterAreaMoveCancel(tool,observe);
Check(active.areaMoveCandidate!=null&&active.areaMoveInApply,"nested cleanup leaves outer Apply capture intact");
active=new();tool.World=active.world;tool.State=0;tool.applyMode=Game.Tools.ApplyMode.Clear;active.areaMoveStateField=null;
Check(BeforeAreaMoveCancel(tool,deps,ref result,out observe)&&observe&&active.areaMoveHookFailed,"removal capture failure preserves native tool execution");
tool.applyMode=Game.Tools.ApplyMode.Apply;AfterAreaMoveCancel(tool,observe);Check(active.prior==null&&!active.areaMovePreview,"accepted uncaptured removal clears stale slot without throwing during cleanup");
active=new();var writeManager=active.world.EntityManager;var originalThree=nodes.ToArray();var originalFour=nodes.Concat(new[]{new Game.Areas.Node{m_Position=new float3(2,2,2)}}).ToArray();
var threeCache=originalThree.Select(p=>new Game.Tools.LocalNodeCache{m_Position=p.m_Position,m_ParentMesh=-1}).ToArray();var fourCache=originalFour.Select(p=>new Game.Tools.LocalNodeCache{m_Position=p.m_Position,m_ParentMesh=-1}).ToArray();
writeManager.Values[typeof(Game.Areas.Node)]=originalFour.ToList();writeManager.Values[typeof(Game.Tools.LocalNodeCache)]=fourCache.ToList();
active.WriteAreaMoveGeometry(id,new StandaloneAreaEntry{Data=new StandaloneAreaData(Game.Areas.AreaFlags.Complete,originalThree,threeCache)});
Check(writeManager.GetBuffer<Game.Areas.Node>(id).SequenceEqual(originalThree)&&writeManager.GetBuffer<Game.Tools.LocalNodeCache>(id).SequenceEqual(threeCache),"Undo addition restores smaller node and attachment buffers exactly");
active.WriteAreaMoveGeometry(id,new StandaloneAreaEntry{Data=new StandaloneAreaData(Game.Areas.AreaFlags.Complete,originalFour,fourCache)});
Check(writeManager.GetBuffer<Game.Areas.Node>(id).SequenceEqual(originalFour)&&writeManager.GetBuffer<Game.Tools.LocalNodeCache>(id).SequenceEqual(fourCache),"Undo node deletion restores missing node and attachment buffers exactly");
CombineChecks(ref n);CacheChecks(ref n);active=null;Console.WriteLine("PASS: "+n+" actual-source area move hook/geometry assertions; native timing remains unproven.");
}
}}
class Program{static void Main()=>MW.EditorTools.Modules.BulldozerRecovery.BulldozerTrace.Run();}
