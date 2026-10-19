using System;using System.Collections.Generic;using System.Linq;
namespace Unity.Collections {enum Allocator{Temp} sealed class NativeArray<T>:IDisposable{T[] values;internal NativeArray(int n,Allocator a){values=new T[n];}internal T this[int i]{get=>values[i];set=>values[i]=value;}public void Dispose(){}}}
namespace Unity.Entities {
 interface IComponentData{}
 struct Entity:IEquatable<Entity>{internal int Id;internal static Entity Null=>default;public bool Equals(Entity e)=>Id==e.Id;public override bool Equals(object o)=>o is Entity e&&Equals(e);public override int GetHashCode()=>Id;public static bool operator==(Entity a,Entity b)=>a.Equals(b);public static bool operator!=(Entity a,Entity b)=>!a.Equals(b);}
 struct EntityArchetype{}
 sealed class Buffer<T>:List<T>{internal int Length=>Count;}
 sealed class Manager {
  internal Dictionary<Entity,Dictionary<Type,object>> Items=new();internal int Next=1;internal bool FailLink;internal Entity Last;
  internal Entity Make(){var id=new Entity{Id=Next++};Items[id]=new();return id;}
  internal bool Exists(Entity id)=>Items.ContainsKey(id);internal bool HasComponent<T>(Entity id)=>Items[id].ContainsKey(typeof(T));internal bool HasBuffer<T>(Entity id)=>Items[id].ContainsKey(typeof(Buffer<T>));
  internal T Get<T>(Entity id)=>(T)Items[id][typeof(T)];internal void SetComponentData<T>(Entity id,T v)=>Items[id][typeof(T)]=v;internal void AddComponentData<T>(Entity id,T v)=>SetComponentData(id,v);internal void RemoveComponent<T>(Entity id)=>Items[id].Remove(typeof(T));internal void SetComponentEnabled<T>(Entity id,bool v)=>Items[id][typeof(bool)]=v;
  internal Buffer<T> GetBuffer<T>(Entity id){if(FailLink&&typeof(T)==typeof(Game.Objects.SubObject))throw new Exception("link failure");return (Buffer<T>)Items[id][typeof(Buffer<T>)];}internal Buffer<T> AddBuffer<T>(Entity id){var b=new Buffer<T>();Items[id][typeof(Buffer<T>)]=b;return b;}
  internal void CreateEntity(EntityArchetype a,Unity.Collections.NativeArray<Entity> values){values[0]=Last=Make();AddComponentData(Last,new Game.Objects.Surface());}
  internal void DestroyEntity(Entity id)=>Items.Remove(id);
 }
 sealed class World{internal Manager EntityManager=new();}
}
namespace Game.Prefabs {struct PrefabRef:Unity.Entities.IComponentData{internal Unity.Entities.Entity m_Prefab;}}
namespace Game.Tools {struct LocalTransformCache:Unity.Entities.IComponentData{internal int m_PrefabSubIndex,Pose;}}
namespace Game.Common {struct Owner:Unity.Entities.IComponentData{internal Unity.Entities.Entity m_Owner;}struct PseudoRandomSeed:Unity.Entities.IComponentData{internal int Value;}struct Applied:Unity.Entities.IComponentData{}struct Created:Unity.Entities.IComponentData{}struct Updated:Unity.Entities.IComponentData{}}
namespace Game.Objects {
 struct Transform:Unity.Entities.IComponentData{internal int Value;}struct Surface:Unity.Entities.IComponentData{}struct Elevation:Unity.Entities.IComponentData{internal int Value;}struct Plant:Unity.Entities.IComponentData{internal int Value;}struct Tree:Unity.Entities.IComponentData{internal int Value;}struct Color:Unity.Entities.IComponentData{internal int Value;}struct Decoration:Unity.Entities.IComponentData{}struct SubObject{internal Unity.Entities.Entity m_SubObject;}
}
namespace Game.Rendering {struct MeshColor{internal int Value;}struct CustomMeshColor{internal int Value;}}
namespace Game.Effects {struct EnabledEffect{internal int Handle;}}
namespace MW.EditorTools.Modules.BulldozerRecovery {class Stub{}}
namespace MW.EditorTools {static class Mod{internal static Logger Log=new();internal class Logger{internal bool Fail;internal void Info(string value){if(Fail)throw new Exception("post-link failure");}}}}
namespace MW.EditorTools.Modules.PropReplacer {
 public sealed partial class PropReplacerSystem {
  private Unity.Entities.World World=new();
  private class FoliageWitness{}
  internal static void Run(){int checks=0;void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
   var a=new PropReplacerSystem();var em=a.World.EntityManager;var owner=em.Make();var donor=em.Make();em.AddBuffer<Game.Objects.SubObject>(owner).Add(new Game.Objects.SubObject{m_SubObject=donor});em.AddComponentData(donor,new Game.Objects.Transform{Value=99});var prefab=em.Make();var companion=em.Make();em.AddBuffer<Game.Objects.SubObject>(donor).Add(new Game.Objects.SubObject{m_SubObject=companion});em.AddBuffer<Game.Effects.EnabledEffect>(donor).Add(new Game.Effects.EnabledEffect{Handle=777});
   var c=new FoliageCandidate{Owner=owner,Prefab=prefab,Pose=new Game.Objects.Transform{Value=15},Seed=new Game.Common.PseudoRandomSeed{Value=42},Local=new Game.Tools.LocalTransformCache{m_PrefabSubIndex=3,Pose=15},Plant=true,PlantValue=new Game.Objects.Plant{Value=7},Tree=true,TreeValue=new Game.Objects.Tree{Value=8},Color=true,ColorValue=new Game.Objects.Color{Value=9},Decoration=true,DecorationEnabled=true,Elevation=true,ElevationValue=new Game.Objects.Elevation{Value=10},Colors=new[]{new Game.Rendering.MeshColor{Value=11}},Custom=new[]{new Game.Rendering.CustomMeshColor{Value=12}}};var plan=new List<FoliageCandidate>{c};
   a.CreateFoliageCandidates(plan);
   Check(!em.HasBuffer<Game.Objects.SubObject>(c.Created)&&em.GetBuffer<Game.Objects.SubObject>(donor)[0].m_SubObject==companion,"Donor companion identity never copied or moved");Check(!em.HasBuffer<Game.Effects.EnabledEffect>(c.Created)&&em.GetBuffer<Game.Effects.EnabledEffect>(donor)[0].Handle==777,"Donor native effect allocation never copied");
   Check(c.Created!=donor&&em.Exists(c.Created),"Fresh candidate identity");Check(em.Get<Game.Objects.Transform>(donor).Value==99,"Donor pose never written");Check(em.Get<Game.Objects.Transform>(c.Created).Value==15,"Destination pose applied");Check(em.Get<Game.Common.PseudoRandomSeed>(c.Created).Value==42,"Destination seed applied");Check(em.Get<Game.Tools.LocalTransformCache>(c.Created).m_PrefabSubIndex==3,"Destination descriptor index applied");Check(em.Get<Game.Prefabs.PrefabRef>(c.Created).m_Prefab==prefab,"Replacement prefab applied");Check(em.Get<Game.Common.Owner>(c.Created).m_Owner==owner,"Correct ownership");Check(!em.HasComponent<Game.Objects.Surface>(c.Created),"Absent destination surface preserved");Check(em.Get<Game.Objects.Plant>(c.Created).Value==7&&em.Get<Game.Objects.Tree>(c.Created).Value==8,"Vegetation typed state applied");Check(em.HasComponent<Game.Common.Created>(c.Created)&&em.HasComponent<Game.Common.Updated>(c.Created)&&em.HasComponent<Game.Common.Applied>(c.Created),"Native init tags applied");Check(em.GetBuffer<Game.Objects.SubObject>(owner).Count==2,"One candidate linked alongside donor");Check(em.GetBuffer<Game.Rendering.MeshColor>(c.Created)[0].Value==11&&em.GetBuffer<Game.Rendering.CustomMeshColor>(c.Created)[0].Value==12,"Detached colors applied");
   a.RemoveFoliageCandidates(plan);Check(!em.Exists(c.Created)&&em.Exists(donor)&&em.GetBuffer<Game.Objects.SubObject>(owner).Count==1,"Rollback removes only fresh candidate");a.RemoveFoliageCandidates(plan);Check(em.Exists(donor),"Cleanup idempotent");
   c.Created=default;MW.EditorTools.Mod.Log.Fail=true;try{a.CreateFoliageCandidates(plan);}catch{}MW.EditorTools.Mod.Log.Fail=false;a.RemoveFoliageCandidates(plan);Check(!em.Exists(c.Created)&&em.GetBuffer<Game.Objects.SubObject>(owner).Count==1,"Post-link failure cleanup");
   c.Created=default;em.FailLink=true;try{a.CreateFoliageCandidates(plan);}catch{}em.FailLink=false;a.RemoveFoliageCandidates(plan);Check(!em.Exists(c.Created)&&em.Exists(donor),"Pre-link failure cleanup");
   Console.WriteLine("PASS: "+checks+" actual-source foliage replacement creation/cleanup checks");
  }
 }
}
class Program{static void Main()=>MW.EditorTools.Modules.PropReplacer.PropReplacerSystem.Run();}
