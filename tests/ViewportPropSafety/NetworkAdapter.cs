using System;
using System.Collections.Generic;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;
using Unity.Mathematics;
using Colossal.Mathematics;

namespace Unity.Entities {
    struct Entity : IEquatable<Entity> {
        public int Id;public bool Equals(Entity other)=>Id==other.Id;
        public override bool Equals(object value)=>value is Entity e&&Equals(e);
        public override int GetHashCode()=>Id;
        public static bool operator ==(Entity a,Entity b)=>a.Equals(b);
        public static bool operator !=(Entity a,Entity b)=>!a.Equals(b);
        public static implicit operator Entity(int value)=>new(){Id=value};
    }
    class TestWorld {public readonly TestEntities EntityManager=new();}
    // Deliberately has no IEnumerable implementation, like the supported native
    // access contract. A LINQ/foreach regression must not pass on a List double.
    class TestBuffer<T> {readonly List<T> items;public TestBuffer(List<T> values){items=values;}public int Length=>items.Count;public T this[int index]=>items[index];}
    class TestEntities {
        readonly Dictionary<Entity,Dictionary<Type,object>> data=new();
        public bool Exists(Entity id)=>data.ContainsKey(id);
        public bool HasComponent<T>(Entity id)=>Exists(id)&&data[id].ContainsKey(typeof(T));
        public T GetComponentData<T>(Entity id)=>(T)data[id][typeof(T)];
        public void Put<T>(Entity id,T value){if(!Exists(id))data[id]=new();data[id][typeof(T)]=value;}
        public void Remove<T>(Entity id)=>data[id].Remove(typeof(T));
        public bool HasBuffer<T>(Entity id)=>HasComponent<List<T>>(id);
        public TestBuffer<T> GetBuffer<T>(Entity id,bool readOnly=false)=>new(GetComponentData<List<T>>(id));
    }
}
namespace Game.Common {struct Owner{public Entity m_Owner;}struct Created{}struct Updated{}struct Deleted{}}
namespace Game.Tools {struct EditorContainer{public Entity m_Prefab;}struct Temp{}struct LocalCurveCache{public Bezier4x3 m_Curve;}}
namespace Game.Net {
    struct Edge{public Entity m_Start,m_End;}struct Node{public float3 m_Position;}
    struct Curve{public Bezier4x3 m_Bezier;}struct SubNet{public Entity m_SubNet;}
    struct ConnectedEdge{public Entity m_Edge;}
}
namespace MW.EditorTools.Modules.PropReplacer {
    public sealed partial class PropReplacerSystem {
        private readonly TestWorld World=new();
        private sealed class Row{internal object Value;internal int Index;}
        internal static void AdapterChecks(){
            int count=0;void Check(bool ok,string label){if(!ok)throw new Exception(label);count++;}
            void Refuse(Action action,string label){bool refused=false;try{action();}catch(InvalidOperationException){refused=true;}catch(NotSupportedException){refused=true;}Check(refused,label);}
            var system=new PropReplacerSystem();var em=system.World.EntityManager;
            Entity owner=1,target=2,start=3,end=4;
            em.Put(owner,new Game.Objects.Transform{m_Rotation=quaternion.identity});
            var links=new List<Game.Net.SubNet>{new(){m_SubNet=target}};em.Put(owner,links);
            var live=new Bezier4x3{a=new float3(0,.046f,0),b=new float3(3,.046f,0),c=new float3(7,.046f,0),d=new float3(10,.046f,0)};
            var cache=new Bezier4x3{a=new float3(0,0,0),b=new float3(3,0,0),c=new float3(7,0,0),d=new float3(10,0,0)};
            em.Put(target,new Game.Net.Edge{m_Start=start,m_End=end});
            em.Put(target,new Game.Net.Curve{m_Bezier=live});em.Put(target,new LocalCurveCache{m_Curve=cache});
            em.Put(start,new Game.Net.Node{m_Position=live.a});em.Put(end,new Game.Net.Node{m_Position=live.d});
            var prefab=new PrefabBase();
            var row=new ObjectSubNetInfo{m_NetPrefab=prefab,m_BezierCurve=live,m_ParentMesh=new int2(-1),m_NodeIndex=new int2(0,1)};
            var rows=new[]{new Row{Value=row}};
            Check(system.ResolveViewportNetworkRows(rows,prefab,target,owner).Length==1,"Actual network adapter projects live curve");
            links.Add(new(){m_SubNet=target});Refuse(()=>system.ResolveViewportNetworkRows(rows,prefab,target,owner),"Duplicate owner link refused");links.RemoveAt(1);
            em.Put(end,new Game.Common.Updated());Refuse(()=>system.ResolveViewportNetworkRows(rows,prefab,target,owner),"Pending endpoint edit refused");em.Remove<Game.Common.Updated>(end);
            em.Remove<Game.Net.Node>(end);Refuse(()=>system.ResolveViewportNetworkRows(rows,prefab,target,owner),"Missing live endpoint refused");em.Put(end,new Game.Net.Node{m_Position=live.d});
            em.Remove<Game.Net.Curve>(target);Refuse(()=>system.ResolveViewportNetworkRows(rows,prefab,target,owner),"Raw cache cannot stand in for missing live curve");em.Put(target,new Game.Net.Curve{m_Bezier=live});
            var reverseLive=new Bezier4x3{a=live.d,b=live.c,c=live.b,d=live.a};var reverseCache=new Bezier4x3{a=cache.d,b=cache.c,c=cache.b,d=cache.a};
            em.Put(target,new Game.Net.Curve{m_Bezier=reverseLive});em.Put(target,new LocalCurveCache{m_Curve=reverseCache});
            Check(system.ResolveViewportNetworkRows(rows,prefab,target,owner).Length==0,"reversed presentation support does not widen replacement dispatch");
            Check(system.ResolveViewportNetworkRows(rows,prefab,target,owner,true).Length==1,"read-only list mapping accepts the unique reversed curve with verified endpoints and owner");
            Check(system.ResolveViewportNetworkRows(new[]{rows[0],rows[0]},prefab,target,owner,true).Length==2,"ambiguous reversed rows remain ambiguous for observer refusal");
            em.Put(target,new Game.Net.Curve{m_Bezier=live});em.Put(target,new LocalCurveCache{m_Curve=cache});
            em.Put(end,new LocalTransformCache{m_ParentMesh=2});Check(system.ResolveViewportNetworkRows(rows,prefab,target,owner).Length==0,"Actual endpoint mesh must agree with source");em.Remove<LocalTransformCache>(end);
            Check(system.ResolveViewportNetworkRows(new[]{rows[0],rows[0]},prefab,target,owner).Length==2,"Coincident source rows remain ambiguous for caller refusal");
            Entity root=8,identity=9;
            em.Put(owner,new PrefabRef{m_Prefab=root});em.Put(target,new PrefabRef{m_Prefab=identity});
            var independent=new ObjectSubNetInfo{m_NetPrefab=prefab,m_BezierCurve=cache,m_ParentMesh=new int2(-1),m_NodeIndex=new int2(-1)};
            var proofRows=new[]{new Row{Value=independent,Index=0}};
            var compiled=new List<Game.Prefabs.SubNet>{new(){m_Prefab=identity,m_Curve=cache,m_NodeIndex=new int2(-1),m_ParentMesh=new int2(-1)}};em.Put(root,compiled);
            Check(system.ResolveViewportNetworkRows(proofRows,prefab,target,owner).Length==0,"native -1 endpoints and regenerated height do not widen write dispatch");
            Check(system.ResolveViewportNetworkRows(proofRows,prefab,target,owner,true).Length==1,"read-only compiled/cache proof maps independent -1 endpoints despite regenerated height");
            var shaped=live;shaped.b+=new float3(1,0,0);em.Put(target,new Game.Net.Curve{m_Bezier=shaped});
            Check(system.ResolveViewportNetworkRows(proofRows,prefab,target,owner,true).Length==1,"compiled/cache proof tolerates regenerated control handles for list display");
            var moved=shaped;moved.a+=new float3(2,0,0);em.Put(target,new Game.Net.Curve{m_Bezier=moved});Check(system.ResolveViewportNetworkRows(proofRows,prefab,target,owner,true).Length==0,"compiled proof cannot map an endpoint moved away from its source");em.Put(target,new Game.Net.Curve{m_Bezier=live});
            var stale=compiled[0];stale.m_NodeIndex=new int2(4,5);compiled[0]=stale;Check(system.ResolveViewportNetworkRows(proofRows,prefab,target,owner,true).Length==0,"compiled topology must match editable source exactly");stale.m_NodeIndex=new int2(-1);compiled[0]=stale;
            Check(system.ResolveViewportNetworkRows(new[]{proofRows[0],proofRows[0]},prefab,target,owner,true).Length==2,"compiled proof preserves ambiguity refusal");
            em.Remove<List<Game.Prefabs.SubNet>>(root);Check(system.ResolveViewportNetworkRows(proofRows,prefab,target,owner,true).Length==0,"raw cached curve without compiled proof is refused");
            // Exact diagnostic values captured in 0.4.249 (25 October 23:10).
            var roundedSource=new Bezier4x3{a=new float3(11.0013733f,0,8.000004f),b=new float3(11.0013733f,.000793457031f,0),c=new float3(11.0013733f,-.000213623047f,-8),d=new float3(11.0013733f,0,-16)};
            var roundedCache=roundedSource;roundedCache.b.y=.00103759766f;roundedCache.c.y=-.000305175781f;
            var roundedWorld=new Bezier4x3{a=new float3(8.974009f,511.9453f,1.59996414f),b=new float3(8.974009f,511.94635f,-6.40003967f),c=new float3(8.974009f,511.945f,-14.40004f),d=new float3(8.974009f,511.9453f,-22.40004f)};
            em.Put(owner,new Game.Objects.Transform{m_Position=new float3(-2.027365f,511.9453f,-6.40004f),m_Rotation=quaternion.identity});
            em.Put(target,new Game.Net.Curve{m_Bezier=roundedWorld});em.Put(target,new LocalCurveCache{m_Curve=roundedCache});
            em.Put(start,new Game.Net.Node{m_Position=roundedWorld.a});em.Put(end,new Game.Net.Node{m_Position=roundedWorld.d});
            var roundedDescriptor=new ObjectSubNetInfo{m_NetPrefab=prefab,m_BezierCurve=roundedSource,m_ParentMesh=new int2(-1),m_NodeIndex=new int2(2,3)};
            var roundedRows=new[]{new Row{Value=roundedDescriptor,Index=1}};
            var roundedCompiled=new List<Game.Prefabs.SubNet>{new(),new(){m_Prefab=identity,m_Curve=roundedSource,m_NodeIndex=new int2(2,3),m_ParentMesh=new int2(-1)}};em.Put(root,roundedCompiled);
            Check(system.ResolveViewportNetworkRows(roundedRows,prefab,target,owner).Length==0,"captured height rounding remains refused by strict write dispatch");
            var mapped=system.ResolveViewportNetworkRows(roundedRows,prefab,target,owner,true);Check(mapped.Length==1&&mapped[0].Index==1,"captured 0.244 mm control height rounding maps exact read-only element 2");
            var excessive=roundedCache;excessive.b.y+=.002f;em.Put(target,new LocalCurveCache{m_Curve=excessive});Check(system.ResolveViewportNetworkRows(roundedRows,prefab,target,owner,true).Length==0,"larger interior height change remains refused");
            var shifted=roundedCache;shifted.b.x+=.00025f;em.Put(target,new LocalCurveCache{m_Curve=shifted});Check(system.ResolveViewportNetworkRows(roundedRows,prefab,target,owner,true).Length==0,"horizontal control displacement does not gain height tolerance");
            shifted=roundedCache;shifted.a.y+=.00025f;em.Put(target,new LocalCurveCache{m_Curve=shifted});Check(system.ResolveViewportNetworkRows(roundedRows,prefab,target,owner,true).Length==0,"endpoint height retains strict tolerance");
            em.Put(target,new LocalCurveCache{m_Curve=roundedCache});Check(system.ResolveViewportNetworkRows(new[]{roundedRows[0],roundedRows[0]},prefab,target,owner,true).Length==2,"height rounding never resolves duplicate source ambiguity");
            var reverseRounded=new Bezier4x3{a=roundedCache.d,b=roundedCache.c,c=roundedCache.b,d=roundedCache.a};em.Put(target,new LocalCurveCache{m_Curve=reverseRounded});Check(system.ResolveViewportNetworkRows(roundedRows,prefab,target,owner,true).Length==1,"reversed cached orientation receives the same height-only comparison");
            Check(roundedDescriptor.m_BezierCurve.b.y==roundedSource.b.y&&roundedCompiled[1].m_Curve.b.y==roundedSource.b.y&&em.GetComponentData<Game.Net.Curve>(target).m_Bezier.b.y==roundedWorld.b.y,"read-only comparison never normalizes editable, compiled or live data");
            em.Put(owner,new Game.Objects.Transform{m_Rotation=quaternion.identity});em.Put(target,new Game.Net.Curve{m_Bezier=live});em.Put(target,new LocalCurveCache{m_Curve=cache});
            em.Put(start,new Game.Net.Node{m_Position=live.a});em.Put(end,new Game.Net.Node{m_Position=live.d});em.Remove<List<Game.Prefabs.SubNet>>(root);
            em.Remove<Game.Net.Edge>(target);em.Remove<Game.Net.Curve>(target);em.Remove<LocalCurveCache>(target);
            em.Put(target,new Game.Net.Node{m_Position=live.a});em.Put(target,new LocalTransformCache{m_Position=cache.a,m_ParentMesh=-1});
            var pointCurve=new Bezier4x3{a=live.a,b=live.a,c=live.a,d=live.a};
            var lane=new ObjectSubLaneInfo{m_LanePrefab=prefab,m_BezierCurve=pointCurve,m_NodeIndex=new int2(3),m_ParentMesh=new int2(-1)};
            var laneRows=new[]{new Row{Value=lane}};
            Check(system.ResolveViewportNetworkRows(laneRows,prefab,target,owner).Length==1,"Actual point-lane adapter uses live node and local cache");
            var laneSource=new ObjectSubLaneInfo{m_LanePrefab=prefab,m_BezierCurve=new Bezier4x3{a=cache.a,b=cache.a,c=cache.a,d=cache.a},m_NodeIndex=new int2(-1),m_ParentMesh=new int2(-1)};
            Entity laneIdentity=10;em.Put(target,new Game.Tools.EditorContainer{m_Prefab=laneIdentity});
            em.Put(root,new List<Game.Prefabs.SubLane>{new(){m_Prefab=laneIdentity,m_Curve=laneSource.m_BezierCurve,m_NodeIndex=laneSource.m_NodeIndex,m_ParentMesh=laneSource.m_ParentMesh}});
            Check(system.ResolveViewportNetworkRows(new[]{new Row{Value=laneSource,Index=0}},prefab,target,owner,true).Length==1,"lane list proof uses the actual editor-container prefab and independent endpoint defaults");
            em.Remove<Game.Tools.EditorContainer>(target);
            Entity connected=5;em.Put(connected,new Game.Net.Edge{m_Start=target,m_End=end});em.Put(connected,new Game.Common.Owner{m_Owner=owner});
            em.Put(target,new List<Game.Net.ConnectedEdge>{new(){m_Edge=connected}});
            Refuse(()=>system.ResolveViewportNetworkRows(laneRows,prefab,target,owner),"Connected junction cannot be redirected to coincident point source");
            em.Remove<List<Game.Net.ConnectedEdge>>(target);links.Clear();Refuse(()=>system.ResolveViewportNetworkRows(laneRows,prefab,target,owner),"Missing attached source link refused");
            Console.WriteLine("PASS: "+count+" actual-source network adapter ownership/topology/pending-edit checks using managed ECS doubles.");
        }
    }
}
