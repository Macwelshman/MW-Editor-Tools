using System;
using System.Collections.Generic;
namespace Unity.Entities {
 public struct Entity : IEquatable<Entity> { public int Index,Version; public static Entity Null=>default; public bool Equals(Entity b)=>Index==b.Index&&Version==b.Version; public override bool Equals(object b)=>b is Entity e&&Equals(e); public override int GetHashCode()=>HashCode.Combine(Index,Version); public static bool operator==(Entity a,Entity b)=>a.Equals(b);public static bool operator!=(Entity a,Entity b)=>!a.Equals(b); }
 public class Buffer<T>:List<T>{public int Length=>Count;}
 public class EntityManager { public readonly Dictionary<Entity,Dictionary<Type,object>> Data=new();public int Writes;
 public bool Exists(Entity id)=>Data.ContainsKey(id);public bool HasComponent<T>(Entity id)=>Exists(id)&&Data[id].ContainsKey(typeof(T));public bool HasBuffer<T>(Entity id)=>HasComponent<Buffer<T>>(id);public Buffer<T> GetBuffer<T>(Entity id,bool readOnly){if(!readOnly)throw new Exception("Writable ECS access");return (Buffer<T>)Data[id][typeof(Buffer<T>)];}
 public void Put<T>(Entity id,T v){Writes++;if(!Exists(id))Data[id]=new();Data[id][typeof(T)]=v;}
 }
 public class World { public bool IsCreated=true; public EntityManager EntityManager=new();public Game.Rendering.BatchManagerSystem Manager;public T GetExistingSystemManaged<T>() where T:class=>Manager as T; }
}
namespace Unity.Jobs {public struct JobHandle {public static int Completions; public void Complete(){Completions++;}public static JobHandle CombineDependencies(JobHandle a,JobHandle b)=>default;}}
namespace UnityEngine {public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}public Color linear=>new(r*r,g*g,b*b,a);}}
namespace Game {public class GameSystemBase {public Unity.Entities.World World;public bool Enabled=true;protected Unity.Entities.EntityManager EntityManager=>World.EntityManager;protected Unity.Jobs.JobHandle Dependency;protected virtual void OnUpdate(){} public void Tick(){if(Enabled)OnUpdate();}}}
namespace Game.Common {public struct Deleted{}}
namespace Game.Tools {public struct Error{}public struct Warning{}public struct Override{}public struct Temp{}}
namespace Game.Net {public struct Lane{}public struct Edge{}public struct Node{}}
namespace Game.Rendering {
 public struct CullingData{} public struct BatchData{}
 public struct MeshBatch {public int m_GroupIndex,m_InstanceIndex;public byte m_MeshGroup,m_MeshIndex,m_TileIndex;}
 public struct InstanceData {public Unity.Entities.Entity m_Entity;public byte m_MeshGroup,m_MeshIndex,m_TileIndex;}
 public struct GroupData {public Unity.Entities.Entity m_Mesh;public bool Missing;public bool GetPropertyIndex(int property,out int slot){slot=property;return !Missing;}}
 public enum ObjectProperty{OutlineColors=7}public enum NetProperty{OutlineColors=13}public enum LaneProperty{OutlineColors=5}
 public class BatchManagerSystem {
 public Store Store=new();public int Readers,Writers;
 public Colossal.Rendering.NativeBatchGroups<CullingData,GroupData,BatchData,InstanceData> GetNativeBatchGroups(bool readOnly,out Unity.Jobs.JobHandle jobs){if(!readOnly)throw new Exception("Group writes forbidden");jobs=default;return new(){Store=Store};}
 public Colossal.Rendering.NativeBatchInstances<CullingData,GroupData,BatchData,InstanceData> GetNativeBatchInstances(bool readOnly,out Unity.Jobs.JobHandle jobs){if(readOnly)throw new Exception("Wrong writer dependency");jobs=default;return new(){Store=Store};}
 public void AddNativeBatchGroupsReader(Unity.Jobs.JobHandle h){Readers++;}public void AddNativeBatchInstancesWriter(Unity.Jobs.JobHandle h){Writers++;}
 }
 public class Store {public bool Created=true;public Dictionary<int,GroupData> Groups=new();public Dictionary<(int,int),InstanceData> Identities=new();public Dictionary<(int,int,int),UnityEngine.Color> Colours=new();public int Writes,FailAt=-1;
 public void Set(UnityEngine.Color c,int g,int p,int i){Writes++;if(Writes==FailAt)throw new Exception("Injected renderer write failure");Colours[(g,p,i)]=c;}}
}
namespace Colossal.Rendering {
 public struct NativeBatchGroups<A,B,C,D> {public Game.Rendering.Store Store;public bool IsCreated=>Store.Created;public int GetGroupCount()=>100;public bool IsValidGroup(int g)=>Store.Groups.ContainsKey(g);public Game.Rendering.GroupData GetGroupData(int g)=>Store.Groups[g];}
 public struct NativeBatchInstances<A,B,C,D> {public Game.Rendering.Store Store;public bool IsCreated=>Store.Created;public int GetInstanceCount(int g)=>100;public Game.Rendering.InstanceData GetInstanceData(int g,int i)=>Store.Identities.TryGetValue((g,i),out var value)?value:default;public ParallelInstanceWriter AsParallelInstanceWriter()=>new(){Store=Store};
 public struct ParallelInstanceWriter {public Game.Rendering.Store Store;public bool GetPropertyValue<T>(out T value,int g,int p,int i){if(Store.Colours.TryGetValue((g,p,i),out var c)){value=(T)(object)c;return true;}value=default;return false;} public void SetPropertyValue<T>(T c,int g,int p,int i)=>Store.Set((UnityEngine.Color)(object)c,g,p,i);}}
}
namespace MW.EditorTools {internal static class Mod {public static readonly Logger Log=new();internal class Logger {public int Errors;public void Info(string s){}public void Error(Exception e,string s){Errors++;}}}}
namespace MW.EditorTools.Modules.Targeting {public class EditorTargetingTool {internal Unity.Entities.Entity Hover,Selected;internal bool Ready=true;internal HashSet<Unity.Entities.Entity> Owned=new();internal string Status;
 internal void GetOutlineTargets(out Unity.Entities.Entity hover,out Unity.Entities.Entity selected){hover=Ready?Hover:default;selected=Ready?Selected:default;}
 internal bool OwnsNativeHighlight(Unity.Entities.Entity id)=>Owned.Contains(id);internal void SetOutlineStatus(string v){Status=v;}}}
