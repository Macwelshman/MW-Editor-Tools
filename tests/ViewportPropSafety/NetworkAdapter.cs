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
namespace Game.Tools {struct Temp{}struct LocalCurveCache{public Bezier4x3 m_Curve;}}
namespace Game.Net {
    struct Edge{public Entity m_Start,m_End;}struct Node{public float3 m_Position;}
    struct Curve{public Bezier4x3 m_Bezier;}struct SubNet{public Entity m_SubNet;}
    struct ConnectedEdge{public Entity m_Edge;}
}
namespace MW.EditorTools.Modules.PropReplacer {
    public sealed partial class PropReplacerSystem {
        private readonly TestWorld World=new();
        private sealed class Row{internal object Value;}
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
            em.Put(end,new LocalTransformCache{m_ParentMesh=2});Check(system.ResolveViewportNetworkRows(rows,prefab,target,owner).Length==0,"Actual endpoint mesh must agree with source");em.Remove<LocalTransformCache>(end);
            Check(system.ResolveViewportNetworkRows(new[]{rows[0],rows[0]},prefab,target,owner).Length==2,"Coincident source rows remain ambiguous for caller refusal");
            em.Remove<Game.Net.Edge>(target);em.Remove<Game.Net.Curve>(target);em.Remove<LocalCurveCache>(target);
            em.Put(target,new Game.Net.Node{m_Position=live.a});em.Put(target,new LocalTransformCache{m_Position=cache.a,m_ParentMesh=-1});
            var pointCurve=new Bezier4x3{a=live.a,b=live.a,c=live.a,d=live.a};
            var lane=new ObjectSubLaneInfo{m_LanePrefab=prefab,m_BezierCurve=pointCurve,m_NodeIndex=new int2(3),m_ParentMesh=new int2(-1)};
            var laneRows=new[]{new Row{Value=lane}};
            Check(system.ResolveViewportNetworkRows(laneRows,prefab,target,owner).Length==1,"Actual point-lane adapter uses live node and local cache");
            Entity connected=5;em.Put(connected,new Game.Net.Edge{m_Start=target,m_End=end});em.Put(connected,new Game.Common.Owner{m_Owner=owner});
            em.Put(target,new List<Game.Net.ConnectedEdge>{new(){m_Edge=connected}});
            Refuse(()=>system.ResolveViewportNetworkRows(laneRows,prefab,target,owner),"Connected junction cannot be redirected to coincident point source");
            em.Remove<List<Game.Net.ConnectedEdge>>(target);links.Clear();Refuse(()=>system.ResolveViewportNetworkRows(laneRows,prefab,target,owner),"Missing attached source link refused");
            Console.WriteLine("PASS: "+count+" actual-source network adapter ownership/topology/pending-edit checks using managed ECS doubles.");
        }
    }
}
