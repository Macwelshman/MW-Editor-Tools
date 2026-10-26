using System;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Reflection;
using Game.Prefabs;using Unity.Entities;using Unity.Collections;using Unity.Mathematics;using MW.EditorTools.History;
namespace UnityEngine {public static class Time{public static float realtimeSinceStartup;}public struct Keyframe{}public enum WrapMode{Default}public class AnimationCurve{public Keyframe[] keys;public WrapMode preWrapMode,postWrapMode;public AnimationCurve(params Keyframe[] k){keys=k;}}}
namespace Unity.Collections { public enum Allocator{Temp,TempJob}public sealed class NativeArray<T>:List<T>,IDisposable{public NativeArray(IEnumerable<T> v):base(v){}public int Length=>Count;public void Dispose(){}} }
namespace Unity.Entities {
 public interface IComponentData{}
 public struct Entity:IEquatable<Entity>{public int Index;public bool Equals(Entity e)=>Index==e.Index;public override bool Equals(object o)=>o is Entity e&&Equals(e);public override int GetHashCode()=>Index;public static bool operator==(Entity a,Entity b)=>a.Equals(b);public static bool operator!=(Entity a,Entity b)=>!a.Equals(b);}
 public struct ComponentType{public Type Type;public int TypeIndex;public static ComponentType ReadOnly<T>()=>new(){Type=typeof(T),TypeIndex=typeof(T)==typeof(Game.Areas.Navigation)?101:typeof(T)==typeof(Game.Areas.HangaroundLocation)?102:typeof(T)==typeof(Game.Net.SubLane)?103:0};}
 public struct EntityArchetype{public int[] Types;public NativeArray<ComponentType> GetComponentTypes(Allocator a)=>new(Types.Select(t=>new ComponentType{TypeIndex=t}));}
 public sealed class DynamicBuffer<T>:List<T>{public int Length=>Count;}
 public sealed class Query:IDisposable{internal EntityManager em;internal ComponentType[] types;public NativeArray<Entity> ToEntityArray(Allocator a)=>new(em.data.Where(p=>types.All(t=>p.Value.ContainsKey(t.Type)||p.Value.ContainsKey(typeof(DynamicBuffer<>).MakeGenericType(t.Type)))).Select(p=>new Entity{Index=p.Key}));public void Dispose(){}}
 public sealed class EntityManager {
  internal Dictionary<int,Dictionary<Type,object>> data=new();int next=1;public int PrefabWrites,FailPrefabWriteAt;public Entity New(){var e=new Entity{Index=next++};data[e.Index]=new();return e;}
  public void CompleteAllTrackedJobs(){}
  public bool Exists(Entity e)=>data.ContainsKey(e.Index);public bool HasComponent<T>(Entity e)=>data[e.Index].ContainsKey(typeof(T));public bool HasBuffer<T>(Entity e)=>data[e.Index].ContainsKey(typeof(DynamicBuffer<T>));public T GetComponentData<T>(Entity e)=>(T)data[e.Index][typeof(T)];public void SetComponentData<T>(Entity e,T v){if(typeof(T)==typeof(PrefabRef)&&++PrefabWrites==FailPrefabWriteAt)throw new Exception("reference write fault");data[e.Index][typeof(T)]=v;}public void AddComponent<T>(Entity e)=>SetComponentData(e,default(T));public void Remove<T>(Entity e)=>data[e.Index].Remove(typeof(T));public void RemoveComponent<T>(Entity e){data[e.Index].Remove(typeof(T));data[e.Index].Remove(typeof(DynamicBuffer<T>));}public DynamicBuffer<T> AddBuffer<T>(Entity e){var b=new DynamicBuffer<T>();data[e.Index][typeof(DynamicBuffer<T>)]=b;return b;}public DynamicBuffer<T> GetBuffer<T>(Entity e,bool read=false)=>(DynamicBuffer<T>)data[e.Index][typeof(DynamicBuffer<T>)];public void Buffer<T>(Entity e,IEnumerable<T> values){var b=new DynamicBuffer<T>();b.AddRange(values);data[e.Index][typeof(DynamicBuffer<T>)]=b;}
  public Query CreateEntityQuery(params ComponentType[] types)=>new(){em=this,types=types};
 }
 public sealed class TestWorld{public EntityManager EntityManager=new();public PrefabSystem Registry=new();public Game.Rendering.AreaBatchSystem Renderer=new();public T GetExistingSystemManaged<T>()where T:class=>(Registry as T)??(Renderer as T);}
}
namespace Game.Common{public struct Created{}public struct Updated{}public struct Deleted{}public struct Owner{public Entity m_Owner;}}
namespace Game.Tools{public struct Temp{}public struct LocalNodeCache{public float3 m_Position;public int m_ParentMesh;}}
namespace Game.Objects{public struct Transform{public float3 m_Position;public quaternion m_Rotation;}}
namespace Game.Net{public struct SubLane{public Entity m_SubLane;public int m_PathMethods;}}
namespace Game.Areas{public struct Navigation:IComponentData{}public struct HangaroundLocation:IComponentData{}public struct Batch{public int m_BatchIndex,m_VisibleCount,m_AllocatedSize,m_MetaIndex;public Colossal.Collections.NativeHeapBlock m_BatchAllocation;}[Flags]public enum AreaFlags{Complete=1,CounterClockwise=2,NoTriangles=4,Slave=8}public struct Area{public AreaFlags m_Flags;}public struct Surface{}public struct Space{}public struct AreaSearchItem{public Entity m_Area;public int m_Triangle;public AreaSearchItem(Entity a,int t){m_Area=a;m_Triangle=t;}}public struct Node{public float3 m_Position;public float m_Elevation;}public struct Triangle{}public struct SubArea{public Entity m_Area;}}
namespace Game.Prefabs{
 public class ComponentBase{public PrefabBase prefab;public bool active=true;}
 public class PrefabBase{public string name="test";public bool isReadOnly,isDirty;public List<ComponentBase> components=new();}
 public class ObjectSubObjectInfo{}public class EffectSource{public class EffectSettings{}public class AnimationProperties{}}
 public class AreaPrefab:PrefabBase{}public class SurfacePrefab:AreaPrefab{}public class SpacePrefab:AreaPrefab{}
 public class ObjectSubAreas:ComponentBase{public ObjectSubAreaInfo[] m_SubAreas;}
 public class ObjectSubAreaInfo{public AreaPrefab m_AreaPrefab;public float3[] m_NodePositions;public int[] m_ParentMeshes;public int settings;}
 public class ObjectSubNetInfo{public PrefabBase m_NetPrefab;public Colossal.Mathematics.Bezier4x3 m_BezierCurve;public int2 m_ParentMesh,m_NodeIndex;}
 public class ObjectSubLaneInfo{public PrefabBase m_LanePrefab;public Colossal.Mathematics.Bezier4x3 m_BezierCurve;public int2 m_ParentMesh,m_NodeIndex;}
 public struct RenderedAreaData{public int m_BatchIndex;}public struct AreaData{public EntityArchetype m_Archetype;}public struct AreaGeometryData{public int m_Type,m_Flags;}
 public struct PrefabRef{public Entity m_Prefab;}public struct SubArea{public Entity m_Prefab;public int2 m_NodeRange;}public struct SubAreaNode{public float3 m_Position;public int m_ParentMesh;}
 public sealed class PrefabSystem{internal Dictionary<PrefabBase,Entity> ids=new();internal bool pending;public bool TryGetEntity(PrefabBase p,out Entity id)=>ids.TryGetValue(p,out id);}
}
namespace MW.EditorTools {static class Mod{internal static Logger Log=new();internal class Logger{internal void Info(string s){}internal void Error(Exception e,string s){}}}}
namespace MW.EditorTools.Modules.PropReplacer{
 public sealed partial class PropReplacerSystem{
  private sealed class Row{internal PrefabBase Root;internal PrefabBase[] Ancestors;internal ComponentBase Owner;internal FieldInfo ListField;internal IList List;internal int Index;}
  private sealed class Context{internal bool EditorAvailable=true,DialogBusy;internal PrefabBase Asset;}
  private sealed class Clip{internal PrefabBase Replacement;}
  private sealed class Guard{internal PrefabSnapshot[] Values=Array.Empty<PrefabSnapshot>();internal bool Matches()=>true;}
  private Guard[] ReplacementGuards(Row r)=>new[]{new Guard()};
  private static void VerifyAreaRenderApi(){}
  private static void VerifySurfaceApi(PrefabSystem registry,PrefabBase root){if(registry.pending)throw new InvalidOperationException("pending native refresh");}
  private Unity.Entities.ComponentLookup<T> GetComponentLookup<T>(bool read)=>new(){em=World.EntityManager};
  private EntityManager EntityManager=>World.EntityManager;
  private bool Enabled=true;
  private TestWorld World=new();private Context context=new();private Clip clipboard=new();private object selectedValue,pendingViewportClipboard;private string status;private Action BeforeWrite;private Action<ReplacementUndoAction> RecordUndo;
  private int refreshes,destructive,failRefresh,clears;private bool swapRoot;private Row fixtureRow;private Entity rootId,oldId,newId,owner,child,companion;private AreaPrefab oldPrefab,newPrefab;private ReplacementUndoAction undo;
  private void Refresh(PrefabBase root){
   refreshes++;if(failRefresh>0){failRefresh--;throw new Exception("refresh fault");}
   var em=World.EntityManager;var id=World.Registry.ids[root];var compiled=em.GetBuffer<SubArea>(id);var source=((ObjectSubAreas)fixtureRow.Owner).m_SubAreas;
   if(compiled.Where((c,i)=>c.m_Prefab!=World.Registry.ids[source[i].m_AreaPrefab]).Any())destructive++;
   if(swapRoot){var fresh=em.New();em.Buffer(fresh,compiled.ToArray());em.Buffer(fresh,em.GetBuffer<SubAreaNode>(id).ToArray());World.Registry.ids[root]=fresh;em.SetComponentData(owner,new PrefabRef{m_Prefab=fresh});rootId=fresh;}
  }
  static int checks;static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
  static void Refuses(Action action,string label){try{action();throw new Exception("ACCEPTED: "+label);}catch(Exception e){if(e.Message.StartsWith("ACCEPTED:"))throw;checks++;}}
  static PropReplacerSystem Setup(bool batch=false){
   var s=new PropReplacerSystem();s.SetSurfaceRenderer(true);var em=s.World.EntityManager;var reg=s.World.Registry;
   var root=new PrefabBase();s.oldPrefab=new SurfacePrefab{name="old"};s.newPrefab=new SurfacePrefab{name="new"};
   foreach(var p in new PrefabBase[]{root,s.oldPrefab,s.newPrefab})reg.ids[p]=em.New();
   s.rootId=reg.ids[root];s.oldId=reg.ids[s.oldPrefab];s.newId=reg.ids[s.newPrefab];em.SetComponentData(s.oldId,new RenderedAreaData{m_BatchIndex=3});em.SetComponentData(s.newId,new RenderedAreaData{m_BatchIndex=7});
   foreach(var id in new[]{s.oldId,s.newId}){em.SetComponentData(id,new AreaData{m_Archetype=new EntityArchetype{Types=new[]{1,2,3}}});em.SetComponentData(id,new AreaGeometryData{m_Type=1,m_Flags=0});}
   var points=new[]{new float3(0,0,0),new float3(2,0,0),new float3(0,0,2)};
   var second=points.Select(p=>p+new float3(8,0,0)).ToArray();
   var component=new ObjectSubAreas{prefab=root,m_SubAreas=new[]{new ObjectSubAreaInfo{m_AreaPrefab=s.oldPrefab,m_NodePositions=points,settings=73},new ObjectSubAreaInfo{m_AreaPrefab=s.oldPrefab,m_NodePositions=second,settings=29}}};root.components.Add(component);
   em.Buffer(s.rootId,new[]{new SubArea{m_Prefab=s.oldId,m_NodeRange=new int2(0,3)},new SubArea{m_Prefab=s.oldId,m_NodeRange=new int2(3,6)}});em.Buffer(s.rootId,points.Concat(second).Select(p=>new SubAreaNode{m_Position=p,m_ParentMesh=-1}));
   s.owner=em.New();em.SetComponentData(s.owner,new PrefabRef{m_Prefab=s.rootId});em.SetComponentData(s.owner,new Game.Objects.Transform{m_Rotation=quaternion.identity});var children=new List<Game.Areas.SubArea>();
   foreach(var boundary in new[]{points,second}){var id=em.New();if(children.Count==0)s.child=id;em.SetComponentData(id,new PrefabRef{m_Prefab=s.oldId});em.SetComponentData(id,new Game.Common.Owner{m_Owner=s.owner});em.SetComponentData(id,new Game.Areas.Surface());em.SetComponentData(id,new Game.Areas.Batch{m_BatchIndex=3,m_VisibleCount=1});em.SetComponentData(id,new Game.Areas.Area{m_Flags=Game.Areas.AreaFlags.Complete|Game.Areas.AreaFlags.CounterClockwise});em.Buffer(id,boundary.Select(p=>new Game.Areas.Node{m_Position=p,m_Elevation=4}));em.Buffer(id,boundary.Select(p=>new Game.Tools.LocalNodeCache{m_Position=p,m_ParentMesh=-1}));em.Buffer(id,new[]{new Game.Areas.Triangle()});children.Add(new Game.Areas.SubArea{m_Area=id});}
   em.Buffer(s.owner,children);s.companion=em.New();em.SetComponentData(s.companion,new Game.Common.Owner{m_Owner=s.child});
   s.context.Asset=root;s.clipboard.Replacement=s.newPrefab;s.fixtureRow=new Row{Root=root,Owner=component,ListField=typeof(ObjectSubAreas).GetField("m_SubAreas"),List=component.m_SubAreas,Index=0};s.BeforeWrite=()=>s.clears++;s.RecordUndo=a=>s.undo=a;return s;
  }
  void Settle(){
   if(surfaceRenderPending!=null){FlushSurfaceRender();foreach(var id in surfaceRenderPending.Live){var index=World.EntityManager.GetComponentData<RenderedAreaData>(surfaceRenderPending.To).m_BatchIndex;World.EntityManager.SetComponentData(id,new Game.Areas.Batch{m_BatchIndex=index,m_VisibleCount=1});}FlushSurfaceRender();}
   foreach(var id in World.EntityManager.data.Keys.ToArray())World.EntityManager.Remove<Game.Common.Updated>(new Entity{Index=id});}
  private void AllocateSelected(int batchIndex) {
   var b=World.EntityManager.GetComponentData<Game.Areas.Batch>(child);
   b.m_BatchIndex=batchIndex;b.m_VisibleCount=1;b.m_AllocatedSize=3;b.m_MetaIndex=0;
   World.Renderer.Allocated(child,batchIndex);World.EntityManager.SetComponentData(child,b);
  }
  private void ToSpaces() {
   var em=World.EntityManager;var registry=World.Registry;
   registry.ids.Remove(oldPrefab);registry.ids.Remove(newPrefab);
   oldPrefab=new SpacePrefab{name="old area"};newPrefab=new SpacePrefab{name="new area"};
   registry.ids[oldPrefab]=oldId;registry.ids[newPrefab]=newId;
   foreach(var row in ((ObjectSubAreas)fixtureRow.Owner).m_SubAreas)row.m_AreaPrefab=oldPrefab;
   foreach(var child in em.GetBuffer<Game.Areas.SubArea>(owner)){em.Remove<Game.Areas.Surface>(child.m_Area);em.Remove<Game.Areas.Batch>(child.m_Area);em.SetComponentData(child.m_Area,new Game.Areas.Space());}
   em.Remove<RenderedAreaData>(oldId);em.Remove<RenderedAreaData>(newId);
   clipboard.Replacement=newPrefab;SetSurfaceRenderer(false);
  }
  public static void Run(){
   var s=Setup();var em=s.World.EntityManager;var before=PrefabSnapshot.Take(((ObjectSubAreas)s.fixtureRow.Owner).m_SubAreas);var boundary=em.GetBuffer<Game.Areas.Node>(s.child).ToArray();var cache=em.GetBuffer<Game.Tools.LocalNodeCache>(s.child).ToArray();var links=em.GetBuffer<Game.Areas.SubArea>(s.owner).ToArray();
   s.swapRoot=true;s.ReplaceSurface(s.fixtureRow,false,s.oldPrefab);
   Check(s.destructive==0&&s.refreshes==1,"compiled source synchronized before native comparison");
   Check(em.GetComponentData<PrefabRef>(s.child).m_Prefab==s.newId,"live selected surface reference changed");Check(em.GetComponentData<PrefabRef>(links[1].m_Area).m_Prefab==s.oldId,"unselected surface retained");
   Check(boundary.SequenceEqual(em.GetBuffer<Game.Areas.Node>(s.child))&&cache.SequenceEqual(em.GetBuffer<Game.Tools.LocalNodeCache>(s.child)),"boundary/elevation/cache preserved");Check(links.SequenceEqual(em.GetBuffer<Game.Areas.SubArea>(s.owner))&&em.GetComponentData<Game.Common.Owner>(s.companion).m_Owner==s.child,"entity identities/owner links/companion references preserved");Check(((ObjectSubAreas)s.fixtureRow.Owner).m_SubAreas[0].settings==73,"destination row settings preserved");
   Check(!s.undo.CanUndo,"pending native area update blocks Undo");s.Settle();Check(s.undo.CanUndo,"settled replacement enables Undo despite native prefab entity transition");s.context.Asset=s.newPrefab;Check(s.undo.CanUndo,"related replacement-prefab inspector transition preserves Undo");s.context.Asset=s.fixtureRow.Root;Check(s.undo.Undo(),"surface Undo applies");Check(before.Matches(((ObjectSubAreas)s.fixtureRow.Owner).m_SubAreas)&&em.GetComponentData<PrefabRef>(s.child).m_Prefab==s.oldId&&s.destructive==0,"Undo restores source/live references without regeneration");Check(!s.undo.CanUndo,"single action consumed");
   s.Settle();s.ReplaceSurface(s.fixtureRow,true,s.oldPrefab);s.Settle();Check(em.GetBuffer<Game.Areas.SubArea>(s.owner).All(x=>em.GetComponentData<PrefabRef>(x.m_Area).m_Prefab==s.newId),"batch updates every exact selected live row");Check(s.undo.Undo()&&s.destructive==0,"batch Undo uses same non-destructive path");
   s=Setup();s.failRefresh=1;before=PrefabSnapshot.Take(((ObjectSubAreas)s.fixtureRow.Owner).m_SubAreas);Refuses(()=>s.ReplaceSurface(s.fixtureRow,false,s.oldPrefab),"refresh failure");Check(before.Matches(((ObjectSubAreas)s.fixtureRow.Owner).m_SubAreas)&&s.World.EntityManager.GetComponentData<PrefabRef>(s.child).m_Prefab==s.oldId&&s.World.EntityManager.GetBuffer<SubArea>(s.rootId)[0].m_Prefab==s.oldId,"failed refresh rolls back all three source/cache/live reference layers");
   s=Setup();s.World.EntityManager.PrefabWrites=0;s.World.EntityManager.FailPrefabWriteAt=2;before=PrefabSnapshot.Take(((ObjectSubAreas)s.fixtureRow.Owner).m_SubAreas);Refuses(()=>s.ReplaceSurface(s.fixtureRow,true,s.oldPrefab),"partial batch reference write failure");
   Check(before.Matches(((ObjectSubAreas)s.fixtureRow.Owner).m_SubAreas)&&s.World.EntityManager.GetBuffer<Game.Areas.SubArea>(s.owner).All(x=>s.World.EntityManager.GetComponentData<PrefabRef>(x.m_Area).m_Prefab==s.oldId)&&s.World.EntityManager.GetBuffer<SubArea>(s.rootId).All(x=>x.m_Prefab==s.oldId),"partial live batch write rolls back source, compiled cache and every live reference");
   s=Setup();s.failRefresh=2;Refuses(()=>s.ReplaceSurface(s.fixtureRow,false,s.oldPrefab),"rollback refresh failure");Check(s.surfaceFailed,"rollback failure latches replacement refusal");
   foreach(var fault in new Action<PropReplacerSystem>[] {
    x=>x.World.Registry.pending=true,
    x=>x.SetSurfaceRenderer(false),
    x=>x.World.EntityManager.SetComponentData(x.newId,new RenderedAreaData{m_BatchIndex=-1}),
    x=>x.World.EntityManager.data[x.owner.Index].Remove(typeof(DynamicBuffer<Game.Areas.SubArea>)),
    x=>x.World.EntityManager.Buffer(x.child,new[]{new Game.Tools.LocalNodeCache()}),
    x=>x.World.EntityManager.SetComponentData(x.child,new Game.Areas.Area{m_Flags=Game.Areas.AreaFlags.Complete|Game.Areas.AreaFlags.Slave}),
    x=>x.World.EntityManager.AddComponent<Game.Common.Created>(x.child),
    x=>x.World.EntityManager.SetComponentData(x.newId,new AreaData{m_Archetype=new EntityArchetype{Types=new[]{1,9}}}),
    x=>x.World.EntityManager.SetComponentData(x.newId,new AreaGeometryData{m_Type=2}),
    x=>x.fixtureRow.Ancestors=new[]{new PrefabBase()},
    x=>x.World.EntityManager.SetComponentData(x.owner,new Game.Common.Owner{m_Owner=x.rootId}),
    x=>x.World.EntityManager.GetBuffer<Game.Areas.SubArea>(x.owner).Add(new Game.Areas.SubArea{m_Area=x.child}),
    x=>{var b=x.World.EntityManager.GetBuffer<SubAreaNode>(x.rootId);b[0]=new SubAreaNode{m_Position=new float3(9),m_ParentMesh=-1};},
    x=>{var a=(ObjectSubAreas)x.fixtureRow.Owner;a.m_SubAreas[1].m_NodePositions=a.m_SubAreas[0].m_NodePositions.ToArray();var b=x.World.EntityManager.GetBuffer<SubAreaNode>(x.rootId);for(int n=0;n<3;n++)b[n+3]=b[n];}
   }) {s=Setup();fault(s);Refuses(()=>s.ReplaceSurface(s.fixtureRow,false,s.oldPrefab),"unsafe preflight");Check(s.clears==0&&s.refreshes==0&&s.World.EntityManager.GetComponentData<PrefabRef>(s.child).m_Prefab==s.oldId,"preflight rejection preserves previous Undo and live surface");}
   s=Setup();var extraOwner=s.World.EntityManager.New();s.World.EntityManager.SetComponentData(extraOwner,new PrefabRef{m_Prefab=s.rootId});s.World.EntityManager.SetComponentData(extraOwner,new Game.Objects.Transform{m_Rotation=quaternion.identity});var extraChildren=new List<Game.Areas.SubArea>();
   foreach(var source in ((ObjectSubAreas)s.fixtureRow.Owner).m_SubAreas){var id=s.World.EntityManager.New();s.World.EntityManager.SetComponentData(id,new PrefabRef{m_Prefab=s.oldId});s.World.EntityManager.SetComponentData(id,new Game.Common.Owner{m_Owner=extraOwner});s.World.EntityManager.SetComponentData(id,new Game.Areas.Surface());s.World.EntityManager.SetComponentData(id,new Game.Areas.Batch{m_BatchIndex=3,m_VisibleCount=1});s.World.EntityManager.SetComponentData(id,new Game.Areas.Area{m_Flags=Game.Areas.AreaFlags.Complete});s.World.EntityManager.Buffer(id,source.m_NodePositions.Select(p=>new Game.Areas.Node{m_Position=p}));s.World.EntityManager.Buffer(id,source.m_NodePositions.Select(p=>new Game.Tools.LocalNodeCache{m_Position=p,m_ParentMesh=-1}));s.World.EntityManager.Buffer(id,new[]{new Game.Areas.Triangle()});extraChildren.Add(new Game.Areas.SubArea{m_Area=id});}
   s.World.EntityManager.Buffer(extraOwner,extraChildren);s.ReplaceSurface(s.fixtureRow,true,s.oldPrefab);s.Settle();Check(extraChildren.All(c=>s.World.EntityManager.GetComponentData<PrefabRef>(c.m_Area).m_Prefab==s.newId),"every live owner of the changed prefab receives the selected references");Check(s.undo.Undo()&&extraChildren.All(c=>s.World.EntityManager.GetComponentData<PrefabRef>(c.m_Area).m_Prefab==s.oldId),"Undo restores all live owners without changing their identities");
   s=Setup();s.ReplaceSurface(s.fixtureRow,false,s.oldPrefab);s.Settle();var metadata=s.World.EntityManager.GetComponentData<Game.Areas.Area>(s.child);metadata.m_Flags^=Game.Areas.AreaFlags.CounterClockwise;s.World.EntityManager.SetComponentData(s.child,metadata);Check(!s.undo.CanUndo,"external metadata mutation refuses Undo");
   s=Setup();s.ReplaceSurface(s.fixtureRow,false,s.oldPrefab);s.Settle();s.World.EntityManager.GetBuffer<Game.Areas.Node>(s.child)[0]=new Game.Areas.Node{m_Position=new float3(99)};Check(!s.undo.CanUndo,"external boundary mutation refuses Undo");
   s=Setup();s.ReplaceSurface(s.fixtureRow,false,s.oldPrefab);s.World.EntityManager.Remove<Game.Common.Updated>(s.child);s.FlushSurfaceRender();Check(s.surfaceRenderSent&&s.World.EntityManager.HasComponent<Game.Common.Updated>(s.child),"render consumer resubmits Updated even when earlier lifecycle cleanup consumed UI tag");s.FlushSurfaceRender();Check(s.surfaceRenderPending!=null&&!s.undo.CanUndo,"old visible material batch cannot report completion or enable Undo");s.World.EntityManager.SetComponentData(s.child,new Game.Areas.Batch{m_BatchIndex=7,m_VisibleCount=1});s.FlushSurfaceRender();s.Settle();Check(s.surfaceRenderPending==null&&s.undo.CanUndo,"verified replacement material batch completes pending operation and enables Undo");Check(s.undo.Undo()&&s.surfaceRenderPending!=null,"Undo queues original material refresh through identical path");s.Settle();Check(s.World.EntityManager.GetComponentData<Game.Areas.Batch>(s.child).m_BatchIndex==3,"Undo restores original render batch in native refresh model");
   s=Setup();s.ReplaceSurface(s.fixtureRow,false,s.oldPrefab);for(int i=0;i<181;i++)s.FlushSurfaceRender();Check(s.surfaceFailed&&s.surfaceRenderPending==null,"material completion timeout fails closed without rewriting native allocations");
   s=Setup();s.ReplaceSurface(s.fixtureRow,false,s.oldPrefab);UnityEngine.Time.realtimeSinceStartup+=11f;s.CheckSurfaceRenderDeadline();Check(s.surfaceFailed&&s.surfaceRenderPending==null,"independent UI watchdog catches a renderer callback that never ran");
   s=Setup();s.ReplaceSurface(s.fixtureRow,false,s.oldPrefab);s.Settle();var rebound=(ObjectSubAreaInfo[])PrefabSnapshot.Take(((ObjectSubAreas)s.fixtureRow.Owner).m_SubAreas).Materialize();((ObjectSubAreas)s.fixtureRow.Owner).m_SubAreas=rebound;
   Check(s.undo.CanUndo,"identical source array rebuilt by native editor is accepted by data/reference checks");Check(s.undo.Undo(),"Undo restores a source whose managed array identity changed without a data change");
   s=Setup();s.ReplaceSurface(s.fixtureRow,false,s.oldPrefab);s.Settle();((ObjectSubAreas)s.fixtureRow.Owner).m_SubAreas[0].settings++;
   Check(!s.undo.CanUndo&&s.surfaceUndoReason.Contains("source list changed"),"changed destination settings still refuse Undo with an explicit readiness reason");
   s=Setup();s.AllocateSelected(3);s.ReplaceSurface(s.fixtureRow,false,s.oldPrefab);
   Check(s.World.Renderer.m_NativeBatchData[3].m_AreaMetaData.Count==0&&s.World.Renderer.m_AreaBufferAllocator==1,"native old material allocation released before prefab switch, allocator value written back");
   Check(s.World.EntityManager.GetComponentData<Game.Areas.Batch>(s.child).m_AllocatedSize==0&&s.World.EntityManager.GetComponentData<PrefabRef>(s.child).m_Prefab==s.newId,"new material never inherits old allocation");
   s.Settle();s.AllocateSelected(7);Check(s.undo.Undo(),"allocated replacement reverses through identical native release path");Check(s.World.Renderer.m_NativeBatchData[7].m_AreaMetaData.Count==0&&s.World.Renderer.m_AreaBufferAllocator==2,"Undo releases replacement material allocation before restoring original reference");
   s=Setup();s.AllocateSelected(3);s.World.Renderer.m_NativeBatchData[3].m_AreaMetaData[0]=new(){m_Entity=s.companion};
   Refuses(()=>s.ReplaceSurface(s.fixtureRow,false,s.oldPrefab),"foreign allocation metadata");Check(s.clears==0&&s.refreshes==0&&s.World.EntityManager.GetComponentData<PrefabRef>(s.child).m_Prefab==s.oldId,"allocation preflight refuses without source writes, refresh or previous Undo clear");
   s=Setup();s.AllocateSelected(3);var hidden=s.World.EntityManager.GetComponentData<Game.Areas.Batch>(s.child);hidden.m_VisibleCount=-1;s.World.EntityManager.SetComponentData(s.child,hidden);
   Refuses(()=>s.ReplaceSurface(s.fixtureRow,false,s.oldPrefab),"pending culling allocation");Check(s.clears==0&&s.World.Renderer.m_AreaBufferAllocator==0,"pending native allocation is never rewritten");
   s=Setup();s.AllocateSelected(3);s.failRefresh=1;before=PrefabSnapshot.Take(((ObjectSubAreas)s.fixtureRow.Owner).m_SubAreas);
   Refuses(()=>s.ReplaceSurface(s.fixtureRow,false,s.oldPrefab),"allocated material refresh rollback");Check(before.Matches(((ObjectSubAreas)s.fixtureRow.Owner).m_SubAreas)&&s.surfaceRenderPending?.To==s.oldId,"rollback restores source and queues original material after native allocation release");s.Settle();Check(!s.surfaceFailed,"successful rollback material refresh does not latch an unrelated permanent failure");
   s=Setup();s.ToSpaces();before=PrefabSnapshot.Take(((ObjectSubAreas)s.fixtureRow.Owner).m_SubAreas);var spaceNodes=s.World.EntityManager.GetBuffer<Game.Areas.Node>(s.child).ToArray();var spaceLinks=s.World.EntityManager.GetBuffer<Game.Areas.SubArea>(s.owner).ToArray();
   s.ReplaceSurface(s.fixtureRow,false,s.oldPrefab);Check(s.surfaceRenderPending==null&&s.World.EntityManager.GetComponentData<PrefabRef>(s.child).m_Prefab==s.newId,"Space replacement works without material renderer or Batch component");
   Check(spaceNodes.SequenceEqual(s.World.EntityManager.GetBuffer<Game.Areas.Node>(s.child))&&spaceLinks.SequenceEqual(s.World.EntityManager.GetBuffer<Game.Areas.SubArea>(s.owner))&&s.World.EntityManager.GetComponentData<Game.Common.Owner>(s.companion).m_Owner==s.child,"area boundary, identity, ownership and companion reference preserved");s.Settle();Check(s.undo.Undo()&&before.Matches(((ObjectSubAreas)s.fixtureRow.Owner).m_SubAreas),"area Undo restores original complete source");
   s=Setup();s.ToSpaces();s.ReplaceSurface(s.fixtureRow,true,s.oldPrefab);s.Settle();Check(s.undo.Undo()&&s.destructive==0,"area batch and Undo retain all live identities without native recreation");
   s=Setup();s.ToSpaces();s.World.EntityManager.SetComponentData(s.newId,new AreaData{m_Archetype=new EntityArchetype{Types=new[]{1,2,3,101,102,103}}});
   s.ReplaceSurface(s.fixtureRow,false,s.oldPrefab);Check(s.World.EntityManager.HasComponent<Game.Areas.Navigation>(s.child)&&s.World.EntityManager.HasComponent<Game.Areas.HangaroundLocation>(s.child)&&s.World.EntityManager.HasBuffer<Game.Net.SubLane>(s.child),"area conversion adds verified stateless behaviour tags and empty lane buffer");s.Settle();Check(s.undo.Undo(),"area layout conversion Undo applies");Check(!s.World.EntityManager.HasComponent<Game.Areas.Navigation>(s.child)&&!s.World.EntityManager.HasComponent<Game.Areas.HangaroundLocation>(s.child)&&!s.World.EntityManager.HasBuffer<Game.Net.SubLane>(s.child),"Undo restores original marker and buffer shape");
   s=Setup();s.ToSpaces();s.World.EntityManager.SetComponentData(s.oldId,new AreaData{m_Archetype=new EntityArchetype{Types=new[]{1,2,3,101,103}}});s.World.EntityManager.SetComponentData(s.newId,new AreaData{m_Archetype=new EntityArchetype{Types=new[]{1,2,3,102}}});
   foreach(var c in s.World.EntityManager.GetBuffer<Game.Areas.SubArea>(s.owner)){s.World.EntityManager.AddComponent<Game.Areas.Navigation>(c.m_Area);s.World.EntityManager.Buffer(c.m_Area,new[]{new Game.Net.SubLane{m_SubLane=s.companion,m_PathMethods=57}});}
   s.ReplaceSurface(s.fixtureRow,false,s.oldPrefab);Check(!s.World.EntityManager.HasComponent<Game.Areas.Navigation>(s.child)&&s.World.EntityManager.GetBuffer<Game.Net.SubLane>(s.child)[0].m_SubLane==s.companion&&s.World.EntityManager.GetBuffer<Game.Net.SubLane>(s.child)[0].m_PathMethods==57,"conversion retains nonempty companion lane references when destination no longer requires Navigation");s.Settle();Check(s.undo.Undo()&&s.World.EntityManager.HasComponent<Game.Areas.Navigation>(s.child)&&s.World.EntityManager.GetBuffer<Game.Net.SubLane>(s.child)[0].m_SubLane==s.companion&&s.World.EntityManager.GetBuffer<Game.Net.SubLane>(s.child)[0].m_PathMethods==57,"Undo restores Navigation with exact companion lane reference");
   s=Setup();s.ToSpaces();s.World.EntityManager.SetComponentData(s.newId,new AreaData{m_Archetype=new EntityArchetype{Types=new[]{1,2,3,101,103}}});s.ReplaceSurface(s.fixtureRow,false,s.oldPrefab);s.Settle();s.World.EntityManager.GetBuffer<Game.Net.SubLane>(s.child).Add(new Game.Net.SubLane{m_SubLane=s.companion});Check(!s.undo.CanUndo,"new external lane references block layout Undo instead of discarding them");
   Check(ViewportListIndex.Resolve(null,100,79)==79,"unfiltered list retains exact source index across pages");
   Check(ViewportListIndex.Resolve(new[]{9,79,2},3,79)==1,"filtered list uses native source-index mapping rather than visible order");
   Check(ViewportListIndex.Resolve(new[]{9,2},2,79)==-1,"filtered-out source never marks a different item");
   Check(ViewportListIndex.Resolve(new[]{79,79},2,79)==-1,"duplicate filtered mapping is refused");
   Check(ViewportListIndex.Resolve(new[]{79},2,79)==-1&&ViewportListIndex.Resolve(null,4097,1)==-1,"stale or over-budget native filter refused");
   Check(ViewportRowType.Resolve(false,false,false,true,false)==typeof(ObjectSubNetInfo),"network source row uses actual network shape, independent of a display marker category");
   Check(ViewportRowType.Resolve(false,true,false,true,true)==typeof(ObjectSubLaneInfo),"lane editor container wins over underlying generated network shape");
   Check(ViewportRowType.Resolve(true,false,false,false,false)==typeof(EffectSource.EffectSettings),"effect container resolves effect rows");
   Console.WriteLine($"PASS: {checks} actual-source surface transaction, native-comparison, rollback, ownership, compatibility and Undo checks. Managed ECS doubles; native acceptance pending.");
  }
 }
}
class Program{static void Main()=>MW.EditorTools.Modules.PropReplacer.PropReplacerSystem.Run();}
