#pragma warning disable CS0649
using System;
using System.Collections.Generic;
using System.Linq;
namespace Unity.Collections {
enum Allocator {Temp}
sealed class NativeArray<T>:IDisposable {readonly T[] data;internal NativeArray(int n,Allocator a){data=new T[n];}internal T this[int i]{get=>data[i];set=>data[i]=value;}public void Dispose(){}}
}
namespace Unity.Entities {
struct Entity:IEquatable<Entity> {internal int Id;internal static Entity Null=>default;public bool Equals(Entity e)=>Id==e.Id;public override bool Equals(object o)=>o is Entity e&&Equals(e);public override int GetHashCode()=>Id;public static bool operator ==(Entity a,Entity b)=>a.Equals(b);public static bool operator !=(Entity a,Entity b)=>!a.Equals(b);}
sealed class Buffer<T>:List<T> {internal int Length=>Count;}
struct TypeInfo {internal Type Type;internal Type GetManagedType()=>Type;}
sealed class Types:List<TypeInfo>,IDisposable {public void Dispose(){}}
sealed class Archetype {internal Type[] Types=Array.Empty<Type>();internal Types GetComponentTypes(Unity.Collections.Allocator a){var r=new Types();r.AddRange(Types.Select(t=>new TypeInfo{Type=t}));return r;}}
sealed class Manager {
internal Dictionary<Entity,Dictionary<Type,object>> Items=new();internal int Next=10;internal bool FailAdd;internal Entity LastCreated;
internal Entity Make(){var e=new Entity{Id=Next++};Items[e]=new();return e;}
internal bool Exists(Entity e)=>Items.ContainsKey(e);
internal bool HasComponent<T>(Entity e)=>Items[e].ContainsKey(typeof(T));
internal bool HasBuffer<T>(Entity e)=>Items[e].ContainsKey(typeof(Buffer<T>));
internal T GetComponentData<T>(Entity e)=>(T)Items[e][typeof(T)];
internal Buffer<T> GetBuffer<T>(Entity e)=>(Buffer<T>)Items[e][typeof(Buffer<T>)];
internal Buffer<T> AddBuffer<T>(Entity e){var b=new Buffer<T>();Items[e][typeof(Buffer<T>)]=b;return b;}
internal void RemoveComponent<T>(Entity e)=>Items[e].Remove(typeof(T));
internal void SetComponentEnabled<T>(Entity e,bool v){Items[e][typeof(T)]=Activator.CreateInstance<T>();}
internal void DestroyEntity(Entity e)=>Items.Remove(e);
internal void CreateEntity(Archetype a,Unity.Collections.NativeArray<Entity> output){output[0]=LastCreated=Make();foreach(var t in a.Types)Items[LastCreated][t]=Activator.CreateInstance(t);}
internal void Put<T>(Entity e,T value){if(FailAdd&&typeof(T)==typeof(Game.Common.Owner))throw new Exception("injected owner write failure");Items[e][typeof(T)]=value;}
}
sealed class World {internal Manager EntityManager=new();internal Game.Prefabs.PrefabSystem Prefabs=new();internal T GetExistingSystemManaged<T>() where T:class=>Prefabs as T;}
}
namespace Game.Prefabs {
class PrefabBase {internal ObjectSubObjects Children;internal T GetComponent<T>() where T:class=>Children as T;}
class ObjectSubObjects {internal object[] m_SubObjects;}
class PrefabSystem {internal Unity.Entities.Entity Id;internal Unity.Entities.Entity GetEntity(PrefabBase p)=>Id;}
struct ObjectData {internal Unity.Entities.Archetype m_Archetype;}
struct PrefabRef {internal Unity.Entities.Entity m_Prefab;}
}
namespace Game.Common {struct Owner {internal Unity.Entities.Entity m_Owner;}struct Applied{}struct Created{}struct Updated{}}
namespace Game.Objects {struct SubObject {internal Unity.Entities.Entity m_SubObject;}struct Surface{}struct Decoration{}struct Plant{}struct Tree{}}
namespace Game.Rendering {struct MeshColor{}struct CustomMeshColor{}}
namespace MW.EditorTools {static class Mod {internal static Logger Log=new();internal class Logger {internal bool Fail;internal void Info(string s){if(Fail)throw new Exception("injected post-link failure");}}}}
namespace MW.EditorTools.Modules.BulldozerRecovery {
sealed partial class BulldozerTrace {
private Unity.Entities.World world=new();
private static readonly HashSet<string> MainKnownTypes=new(){"Game.Objects.Surface"};
private struct Value {internal int N;} private struct Pose {internal int N;} private struct Seed {internal int N;}
private class Data {internal Pose Transform;internal Seed Seed;internal Game.Objects.Surface Surface;internal Game.Rendering.MeshColor[] Colours;internal Game.Rendering.CustomMeshColor[] CustomColours;}
private class NestedNode {internal Unity.Entities.Entity Original;internal Game.Prefabs.PrefabBase Prefab;internal Value Local,Elevation,Color;internal Data Data=new();internal bool HasPlant,HasTree,HasPlaceholder,HasStreetLight,HasSurface,HasElevation,HasColor,HasDecoration,DecorationEnabled;internal Game.Objects.Plant Plant;internal Game.Objects.Tree Tree;internal int[] Effects=Array.Empty<int>(),EnabledEffects=Array.Empty<int>();}
private class Record {internal bool Area;internal Unity.Entities.Entity Owner,FoliageCandidate;internal NestedNode[] Nested;internal NestedNode[][] ForestFrom;}
private void PutMain<T>(Unity.Entities.Entity e,T value)=>world.EntityManager.Put(e,value);
private static bool SameNestedPlacement(NestedNode a,NestedNode b)=>ReferenceEquals(a.Prefab,b.Prefab)&&a.Data.Transform.N==b.Data.Transform.N;
internal static void Run(){
int count=0;void Check(bool v,string m){if(!v)throw new Exception(m);count++;}
var a=new BulldozerTrace();var em=a.world.EntityManager;var owner=em.Make();em.AddBuffer<Game.Objects.SubObject>(owner);
var prefab=new Game.Prefabs.PrefabBase();var source=new NestedNode{Original=new Unity.Entities.Entity{Id=1},Prefab=prefab,HasPlant=true,HasSurface=true};source.Data.Transform=new Pose{N=100};source.Data.Seed=new Seed{N=42};
var neighbour=em.Make();em.Put(neighbour,new Game.Common.Owner{m_Owner=owner});em.GetBuffer<Game.Objects.SubObject>(owner).Add(new Game.Objects.SubObject{m_SubObject=neighbour});
var retained=new NestedNode{Original=neighbour,Prefab=prefab};retained.Data.Transform=new Pose{N=200};
var record=new Record{Owner=owner,Nested=new[]{source},ForestFrom=new[]{new[]{retained}}};
var nativePrefab=em.Make();a.world.Prefabs.Id=nativePrefab;var arch=new Unity.Entities.Archetype{Types=new[]{typeof(Game.Objects.Surface)}};em.Put(nativePrefab,new Game.Prefabs.ObjectData{m_Archetype=arch});
int initial=em.Items.Count;
Check(a.PrepareFoliageCandidate(record,false)==Unity.Entities.Entity.Null&&em.Items.Count==initial,"Redo must not create a candidate");
record.Area=true;Check(a.PrepareFoliageCandidate(record,true)==Unity.Entities.Entity.Null,"Areas use existing path");record.Area=false;
source.HasPlant=false;Check(a.PrepareFoliageCandidate(record,true)==Unity.Entities.Entity.Null,"Nonfoliage uses existing path");source.HasPlant=true;
source.Effects=new[]{1};Check(a.PrepareFoliageCandidate(record,true)==Unity.Entities.Entity.Null,"Effects excluded");source.Effects=Array.Empty<int>();
prefab.Children=new(){m_SubObjects=new object[]{new()}};Check(a.PrepareFoliageCandidate(record,true)==Unity.Entities.Entity.Null,"Nested prefab excluded even if no children currently spawned");prefab.Children=null;
record.ForestFrom=Array.Empty<NestedNode[]>();Check(a.PrepareFoliageCandidate(record,true)==Unity.Entities.Entity.Null,"No neighbour needs no candidate");record.ForestFrom=new[]{new[]{retained}};
arch.Types=new[]{typeof(DateTime)};bool refused=false;try{a.PrepareFoliageCandidate(record,true);}catch(NotSupportedException){refused=true;}
Check(refused&&em.Items.Count==initial,"Unknown archetype refused before mutation");arch.Types=new[]{typeof(Game.Objects.Surface)};
em.FailAdd=true;refused=false;try{a.PrepareFoliageCandidate(record,true);}catch(Exception){refused=true;}em.FailAdd=false;
Check(refused&&em.Items.Count==initial&&em.GetBuffer<Game.Objects.SubObject>(owner).Count==1,"Partial creation cleans entity and leaves owner/neighbour intact");
var id=a.PrepareFoliageCandidate(record,true);record.FoliageCandidate=id;
Check(id!=Unity.Entities.Entity.Null&&id!=source.Original&&id!=neighbour,"Fresh entity identity");
Check(em.GetComponentData<Game.Common.Owner>(id).m_Owner==owner,"Correct owner");
Check(em.GetComponentData<Game.Prefabs.PrefabRef>(id).m_Prefab==nativePrefab,"Original prefab reference");
Check(em.GetComponentData<Seed>(id).N==source.Data.Seed.N&&em.GetComponentData<Pose>(id).N==100,"Typed writes applied");
Check(em.HasComponent<Game.Common.Created>(id)&&em.HasComponent<Game.Common.Applied>(id)&&em.HasComponent<Game.Common.Updated>(id),"Native initialization tags");
Check(em.GetBuffer<Game.Objects.SubObject>(owner).Count==2&&em.GetBuffer<Game.Objects.SubObject>(owner)[0].m_SubObject==neighbour,"Owner links fresh candidate and preserves neighbour");
Check(RetainedFoliageIdentitiesMatch(record,new[]{new[]{retained}}),"Retained identity accepted");
var replacement=new NestedNode{Original=id,Prefab=prefab};replacement.Data.Transform=retained.Data.Transform;
Check(!RetainedFoliageIdentitiesMatch(record,new[]{new[]{replacement}}),"Same placement with replaced neighbour refused");
a.RemoveFoliageCandidate(owner,id);Check(em.Items.Count==initial&&em.GetBuffer<Game.Objects.SubObject>(owner).Count==1&&em.Exists(neighbour),"Synchronous rollback removes only new candidate");
a.RemoveFoliageCandidate(owner,id);Check(em.Items.Count==initial,"Cleanup idempotent");
MW.EditorTools.Mod.Log.Fail=true;refused=false;try{a.PrepareFoliageCandidate(record,true);}catch(Exception){refused=true;}MW.EditorTools.Mod.Log.Fail=false;
Check(refused&&em.Items.Count==initial&&em.GetBuffer<Game.Objects.SubObject>(owner).Count==1&&em.Exists(neighbour),"Failure after owner linking cleans candidate and its link");
source.HasPlant=false;source.HasTree=true;id=a.PrepareFoliageCandidate(record,true);
Check(em.HasComponent<Game.Objects.Tree>(id),"Tree state included");a.RemoveFoliageCandidate(owner,id);
source.HasPlaceholder=true;Check(a.PrepareFoliageCandidate(record,true)==Unity.Entities.Entity.Null,"Placeholder excluded");source.HasPlaceholder=false;
source.HasStreetLight=true;Check(a.PrepareFoliageCandidate(record,true)==Unity.Entities.Entity.Null,"Streetlight excluded");source.HasStreetLight=false;
source.EnabledEffects=new[]{0};Check(a.PrepareFoliageCandidate(record,true)==Unity.Entities.Entity.Null,"Enabled effects excluded");source.EnabledEffects=Array.Empty<int>();
source.HasSurface=false;id=a.PrepareFoliageCandidate(record,true);Check(!em.HasComponent<Game.Objects.Surface>(id),"Absent surface preserved despite archetype default");a.RemoveFoliageCandidate(owner,id);
record.FoliageCandidate=Unity.Entities.Entity.Null;Check(RetainedFoliageIdentitiesMatch(record,new[]{new[]{replacement}}),"Existing noncandidate recovery identity behavior preserved");
Console.WriteLine("PASS: "+count+" bound foliage safety assertions (managed doubles; native scheduling unproven).");
}
}}
class Program {static void Main()=>MW.EditorTools.Modules.BulldozerRecovery.BulldozerTrace.Run();}

