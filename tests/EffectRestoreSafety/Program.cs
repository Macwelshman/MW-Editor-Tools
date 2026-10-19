using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Mathematics;
namespace Unity.Collections {
enum Allocator{Temp}
struct NativeArray<T>:IDisposable{internal T[] Values;internal NativeArray(int count,Allocator a){Values=new T[count];}internal T this[int i]{get=>Values[i];set=>Values[i]=value;}public IEnumerator<T> GetEnumerator()=>((IEnumerable<T>)Values).GetEnumerator();public void Dispose(){}}
}
namespace Unity.Entities {
interface IComponentData{}
struct Entity{internal int Id;internal static Entity Null=>default;public static bool operator ==(Entity a,Entity b)=>a.Id==b.Id;public static bool operator !=(Entity a,Entity b)=>a.Id!=b.Id;public override bool Equals(object o)=>o is Entity e&&e.Id==Id;public override int GetHashCode()=>Id;}
class Buffer<T>:IEnumerable<T>{internal List<T> Values=new();internal int Length=>Values.Count;internal T this[int i]=>Values[i];internal void Add(T item)=>Values.Add(item);IEnumerator<T> IEnumerable<T>.GetEnumerator()=>throw new NotImplementedException();System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>throw new NotImplementedException();}
struct ComponentType{internal Type Type;internal Type GetManagedType()=>Type;}
struct Archetype{internal Type[] Types;internal Unity.Collections.NativeArray<ComponentType> GetComponentTypes(Unity.Collections.Allocator a)=>new(){Values=Types.Select(t=>new ComponentType{Type=t}).ToArray()};}
class Manager {
internal bool HasBuffer<T>(Entity id)=>Buffers.ContainsKey((id.Id,typeof(T)));internal Buffer<T> AddBuffer<T>(Entity id)=>GetBuffer<T>(id);
internal Dictionary<(int,Type),object> Buffers=new();internal Buffer<T> GetBuffer<T>(Entity id){if(!Buffers.TryGetValue((id.Id,typeof(T)),out var b))Buffers[(id.Id,typeof(T))]=b=new Buffer<T>();return(Buffer<T>)b;}
internal Dictionary<(int,Type),object> Data=new();internal HashSet<int> Live=new();internal Dictionary<int,bool> Enabled=new();internal int Created;
internal bool Exists(Entity id)=>Live.Contains(id.Id);internal bool HasComponent<T>(Entity id)=>Data.ContainsKey((id.Id,typeof(T)));internal T GetComponentData<T>(Entity id)=>(T)Data[(id.Id,typeof(T))];
internal void SetComponentData<T>(Entity id,T data)=>Data[(id.Id,typeof(T))]=data;internal void AddComponentData<T>(Entity id,T data)=>SetComponentData(id,data);internal void RemoveComponent<T>(Entity id)=>Data.Remove((id.Id,typeof(T)));
internal void SetComponentEnabled<T>(Entity id,bool enabled)=>Enabled[id.Id]=enabled;
internal void CreateEntity(Archetype a,Unity.Collections.NativeArray<Entity> output){Created++;output[0]=new(){Id=90};Live.Add(90);if(a.Types.Contains(typeof(Game.Common.Owner)))SetComponentData(output[0],new Game.Common.Owner{m_Owner=new(){Id=2}});}
}
class World{internal Manager EntityManager=new();internal Game.Tools.ToolSystem Tools=new();internal T GetExistingSystemManaged<T>()where T:new()=>typeof(T)==typeof(Game.Tools.ToolSystem)?(T)(object)Tools:new T();}
}
namespace Game.Effects{struct EnabledEffect{internal int m_Index;}}
namespace Game.Rendering {struct CullingInfo:Unity.Entities.IComponentData{} class PreCullingSystem{internal Unity.Entities.World World;}}
namespace MW.EditorTools{static class Mod{internal static Logger Log=new();internal class Logger{internal void Info(string s){}}}}
namespace Game.Common{struct Owner:Unity.Entities.IComponentData{internal Unity.Entities.Entity m_Owner;}struct Deleted:Unity.Entities.IComponentData{}struct Updated:Unity.Entities.IComponentData{}struct BatchesUpdated:Unity.Entities.IComponentData{}struct EffectsUpdated:Unity.Entities.IComponentData{}struct Created:Unity.Entities.IComponentData{}struct Applied:Unity.Entities.IComponentData{}struct PseudoRandomSeed:Unity.Entities.IComponentData{internal int m_Seed;}}
namespace Game.Objects{struct SubObject{internal Unity.Entities.Entity m_SubObject;}struct Transform:Unity.Entities.IComponentData{internal float3 m_Position;internal quaternion m_Rotation;}struct Surface:Unity.Entities.IComponentData{internal int Value;}struct Elevation:Unity.Entities.IComponentData{internal float m_Elevation;}struct Decoration:Unity.Entities.IComponentData{}}
namespace Game.Prefabs {
class PrefabBase{internal Unity.Entities.Entity Entity;}
class PrefabSystem{internal Unity.Entities.Entity GetEntity(PrefabBase p)=>p.Entity;}
struct PrefabRef:Unity.Entities.IComponentData{internal Unity.Entities.Entity m_Prefab;}
struct EffectData:Unity.Entities.IComponentData{}
struct ObjectData{internal Unity.Entities.Archetype m_Archetype;}
}
namespace Game.Tools{class ToolBaseSystem{} class DefaultToolSystem:ToolBaseSystem{private Unity.Entities.Entity m_LastRaycastEntity;}class ToolSystem{internal ToolBaseSystem activeTool;}struct EditorContainer:Unity.Entities.IComponentData{internal Unity.Entities.Entity m_Prefab;internal float3 m_Scale;internal float m_Intensity;internal int m_GroupIndex;}struct LocalTransformCache:Unity.Entities.IComponentData{internal float3 Position;internal int Index;}}
namespace MW.EditorTools.Modules.BulldozerRecovery {
sealed partial class BulldozerTrace {
private Unity.Entities.World world=new();
private static BulldozerTrace active;
private readonly System.Reflection.FieldInfo defaultRaycastEntity=typeof(Game.Tools.DefaultToolSystem).GetField("m_LastRaycastEntity",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
private class Context{internal bool EditorAvailable=true;}private Context context=new();
private static void Guard(Action action)=>action();
private static HashSet<string> EffectKnown=new(){typeof(Game.Objects.Transform).FullName,typeof(Game.Common.Owner).FullName};
private class ImageValue{internal Game.Prefabs.PrefabBase Prefab,Container;internal float3 Position,Scale;internal quaternion Rotation;internal float Intensity;internal int Group;internal bool HasLocal,HasElevation;internal Game.Objects.Elevation Elevation;}
private class EffectImage{internal ImageValue Value;internal Game.Tools.LocalTransformCache RawLocal;internal bool HasSeed,HasSurface,HasDecoration,Decoration,HasEnabledBuffer;internal Game.Common.PseudoRandomSeed Seed;internal Game.Objects.Surface Surface;}
private class EffectEntry{internal Unity.Entities.Entity Live;internal EffectImage Before;}
private void PutMain<T>(Unity.Entities.Entity id,T value)where T:unmanaged,Unity.Entities.IComponentData=>world.EntityManager.SetComponentData(id,value);
private class Foreign{}
internal static void Run(){int n=0;void Check(bool v,string s){if(!v)throw new Exception(s);n++;}
    var t=new BulldozerTrace();var em=t.world.EntityManager;var id=new Unity.Entities.Entity{Id=1};var effect=new Unity.Entities.Entity{Id=10};var placeholder=new Unity.Entities.Entity{Id=11};em.Live.UnionWith(new[]{1,10,11});em.SetComponentData(effect,new Game.Prefabs.EffectData());
    var parent=new Unity.Entities.Entity{Id=55};em.Live.Add(parent.Id);var links=em.GetBuffer<Game.Objects.SubObject>(parent);links.Add(new(){m_SubObject=new(){Id=77}});
    bool enumerationThrows=false;try{links.Any();}catch(NotImplementedException){enumerationThrows=true;}Check(enumerationThrows,"buffer double reproduces native explicit-enumerator failure");
    em.SetComponentData(id,new Game.Common.Owner{m_Owner=parent});t.LinkIsolatedEffect(parent,id);
    Check(links.Length==2&&links[1].m_SubObject==id,"actual isolated linking uses indexed native-compatible access");
    Check(links[0].m_SubObject.Id==77,"existing sibling membership/order is preserved");
    bool duplicate=false;try{t.LinkIsolatedEffect(parent,id);}catch(NotSupportedException){duplicate=true;}Check(duplicate&&links.Length==2,"duplicate link refuses without changing parent buffer");
    em.SetComponentData(id,new Game.Common.Owner{m_Owner=new(){Id=66}});bool wrongOwner=false;try{t.LinkIsolatedEffect(parent,id);}catch(NotSupportedException){wrongOwner=true;}Check(wrongOwner&&links.Length==2,"ownership mismatch refuses before parent buffer mutation");
    var image=new EffectImage{Value=new(){Prefab=new(){Entity=placeholder},Container=new(){Entity=effect},Position=new(3,4,5),Rotation=quaternion.RotateY(.4f),Scale=new(2,3,4),Intensity=.7f,Group=3,HasLocal=true,HasElevation=true,Elevation=new(){m_Elevation=4}},RawLocal=new(){Position=new(3,4,5),Index=7},HasEnabledBuffer=true,HasSeed=true,Seed=new(){m_Seed=42},HasSurface=true,Surface=new(){Value=9},HasDecoration=true,Decoration=false};
    em.SetComponentData(id,123);em.AddBuffer<Game.Effects.EnabledEffect>(id).Add(new(){m_Index=37});t.WriteEffectValues(id,image);
    Check(em.GetBuffer<Game.Effects.EnabledEffect>(id).Length==1&&em.GetBuffer<Game.Effects.EnabledEffect>(id)[0].m_Index==37,"moving effect values never copies or resets native allocation indices");
    Check(em.GetComponentData<Game.Objects.Transform>(id).m_Position.Equals(image.Value.Position)&&em.GetComponentData<Game.Objects.Transform>(id).m_Rotation.Equals(image.Value.Rotation),"actual effect pose restored");
    var container=em.GetComponentData<Game.Tools.EditorContainer>(id);Check(container.m_Prefab.Id==10&&container.m_Scale.Equals(image.Value.Scale)&&container.m_Intensity==.7f&&container.m_GroupIndex==3,"effect identity/scale/intensity/group restored");
    Check(em.GetComponentData<Game.Tools.LocalTransformCache>(id).Index==7,"original raw attachment cache preserved");
    Check(em.GetComponentData<Game.Objects.Elevation>(id).m_Elevation==4,"original elevation restored");
    Check(em.GetComponentData<Game.Common.PseudoRandomSeed>(id).m_Seed==42&&em.GetComponentData<Game.Objects.Surface>(id).Value==9,"seed and surface restored");
    Check(!em.Enabled[id.Id]&&em.HasComponent<Game.Objects.Decoration>(id),"decoration enable state restored");
    Check(em.HasComponent<Game.Common.Updated>(id)&&em.HasComponent<Game.Common.BatchesUpdated>(id),"native update tags emitted");Check(em.GetComponentData<int>(id)==123,"unrelated typed state untouched");
    Check(em.HasComponent<Game.Common.EffectsUpdated>(id),"move restoration emits native editor effect refresh signal");
    var defaultTool=new Game.Tools.DefaultToolSystem();t.world.Tools.activeTool=defaultTool;t.defaultRaycastEntity.SetValue(defaultTool,id);t.InvalidateEffectHover(id,parent);
    Check((Unity.Entities.Entity)t.defaultRaycastEntity.GetValue(defaultTool)==Unity.Entities.Entity.Null,"move Undo invalidates matching native hover cache");
    t.defaultRaycastEntity.SetValue(defaultTool,parent);t.InvalidateEffectHover(id,parent);
    Check((Unity.Entities.Entity)t.defaultRaycastEntity.GetValue(defaultTool)==Unity.Entities.Entity.Null,"bound move Undo invalidates exact owner hover cache");
    var other=new Unity.Entities.Entity{Id=44};t.defaultRaycastEntity.SetValue(defaultTool,other);t.InvalidateEffectHover(id,parent);
    Check((Unity.Entities.Entity)t.defaultRaycastEntity.GetValue(defaultTool)==other,"move Undo preserves hover cache for another entity");
    em.Live.Remove(10);int count=em.Data.Count;bool refused=false;try{t.WriteEffectValues(id,image);}catch(NotSupportedException){refused=true;}Check(refused&&em.Data.Count==count,"missing effect prefab refuses before first write");em.Live.Add(10);
    var archetype=new Unity.Entities.Archetype{Types=new[]{typeof(Game.Objects.Transform),typeof(Game.Common.Owner)}};em.SetComponentData(placeholder,new Game.Prefabs.ObjectData{m_Archetype=archetype});
    var entry=new EffectEntry{Live=id,Before=image};t.CreateStandaloneEffect(entry);
    Check(em.HasBuffer<Game.Effects.EnabledEffect>(entry.Live)&&em.GetBuffer<Game.Effects.EnabledEffect>(entry.Live).Length==0,"fresh restored effect gets empty native effect registry rather than copied allocation indices");
    Check(entry.Live.Id==90&&em.Created==1,"deletion Undo creates one fresh native-archetype entity");Check(!em.HasComponent<Game.Common.Owner>(entry.Live),"standalone creation cannot inherit unrelated ownership");
    Check(em.GetComponentData<Game.Prefabs.PrefabRef>(entry.Live).m_Prefab.Id==11,"original editor placeholder prefab assigned");
    Check(em.HasComponent<Game.Common.Created>(entry.Live)&&em.HasComponent<Game.Common.Applied>(entry.Live),"creation enters native initialization path");
    Check(em.HasComponent<Game.Common.EffectsUpdated>(entry.Live),"fresh deletion restoration also requests native effect refresh");
    Check(!t.EffectRenderReady(entry.Live),"render refresh is pending until native consumer runs");
    t.FlushEffectRenderRefresh();Check(!t.EffectRenderReady(entry.Live),"fresh creation cannot refresh before native initialization");
    em.RemoveComponent<Game.Common.Created>(entry.Live);t.FlushEffectRenderRefresh();Check(!t.EffectRenderReady(entry.Live),"render allocation must exist before refresh submission");
    em.SetComponentData(entry.Live,new Game.Rendering.CullingInfo());em.RemoveComponent<Game.Common.Updated>(entry.Live);em.RemoveComponent<Game.Common.BatchesUpdated>(entry.Live);em.RemoveComponent<Game.Common.EffectsUpdated>(entry.Live);
    active=t;BeforeEffectRendering(new(){World=new Unity.Entities.World()});Check(!t.EffectRenderReady(entry.Live),"another world cannot consume a queued effect refresh");
    BeforeEffectRendering(new(){World=t.world});Check(t.EffectRenderReady(entry.Live)&&em.HasComponent<Game.Common.Updated>(entry.Live)&&em.HasComponent<Game.Common.BatchesUpdated>(entry.Live)&&em.HasComponent<Game.Common.EffectsUpdated>(entry.Live),"native culling callback reissues target tags after intervening cleanup");
    em.RemoveComponent<Game.Common.EffectsUpdated>(entry.Live);BeforeEffectRendering(new(){World=t.world});Check(!em.HasComponent<Game.Common.EffectsUpdated>(entry.Live),"refresh is submitted once rather than holding update tags indefinitely");
    t.QueueEffectRenderRefresh(entry.Live);t.context.EditorAvailable=false;t.FlushEffectRenderRefresh();Check(t.EffectRenderReady(entry.Live)&&!em.HasComponent<Game.Common.EffectsUpdated>(entry.Live),"leaving editor cancels refresh without writes");t.context.EditorAvailable=true;
    t.QueueEffectRenderRefresh(entry.Live);t.ClearEffectRenderRefresh();Check(t.EffectRenderReady(entry.Live),"clearing recovery cancels queued refresh");
    archetype.Types=new[]{typeof(Foreign)};em.SetComponentData(placeholder,new Game.Prefabs.ObjectData{m_Archetype=archetype});int created=em.Created;refused=false;try{t.CreateStandaloneEffect(entry);}catch(NotSupportedException){refused=true;}Check(refused&&em.Created==created,"unknown archetype refused before entity creation");
    image.Value.HasLocal=image.Value.HasElevation=image.HasSeed=image.HasSurface=image.HasDecoration=false;t.WriteEffectValues(id,image);
    Check(!em.HasComponent<Game.Tools.LocalTransformCache>(id)&&!em.HasComponent<Game.Objects.Elevation>(id)&&!em.HasComponent<Game.Common.PseudoRandomSeed>(id)&&!em.HasComponent<Game.Objects.Surface>(id)&&!em.HasComponent<Game.Objects.Decoration>(id),"original optional-component absence restored");
    Console.WriteLine("PASS: "+n+" actual-source effect restore assertions; native rendering remains unproven.");
}
}}
class Program{static void Main()=>MW.EditorTools.Modules.BulldozerRecovery.BulldozerTrace.Run();}
