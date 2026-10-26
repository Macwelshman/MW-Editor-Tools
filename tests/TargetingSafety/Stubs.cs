using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Entities
{
    public struct Entity : IEquatable<Entity>
    {
        public int Index, Version; public static Entity Null => default;
        public bool Equals(Entity value) => Index == value.Index && Version == value.Version;
        public override bool Equals(object value) => value is Entity entity && Equals(entity);
        public override int GetHashCode() => HashCode.Combine(Index, Version);
        public static bool operator ==(Entity a, Entity b) => a.Equals(b);
        public static bool operator !=(Entity a, Entity b) => !a.Equals(b);
    }
    public sealed class Buffer<T> : IEnumerable<T>
    {
        public readonly List<T> Items = new List<T>(); public int Length => Items.Count;
        public T this[int index] => Items[index];
        public IEnumerator<T> GetEnumerator() => Items.GetEnumerator(); IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public sealed class EntityManager
    {
        public int Writes, PresentationWrites;
        private static bool Presentation<T>() => typeof(T)==typeof(Game.Tools.Highlighted)||typeof(T)==typeof(Game.Common.BatchesUpdated);
        public void AddComponent<T>(Entity id) { Put(id,default(T)); }
        public void RemoveComponent<T>(Entity id) { Drop<T>(id); }
        private readonly Dictionary<Entity, Dictionary<Type, object>> values = new Dictionary<Entity, Dictionary<Type, object>>();
        public bool Exists(Entity id) => values.ContainsKey(id);
        public void Put<T>(Entity id, T value) { if(Presentation<T>())PresentationWrites++;else Writes++; if (!Exists(id)) values[id] = new Dictionary<Type, object>(); values[id][typeof(T)] = value; }
        public void Drop<T>(Entity id) { if(Presentation<T>())PresentationWrites++;else Writes++; values[id].Remove(typeof(T)); }
        public void Destroy(Entity id) { Writes++; values.Remove(id); }
        public bool HasComponent<T>(Entity id) => Exists(id) && values[id].ContainsKey(typeof(T));
        public T GetComponentData<T>(Entity id) => (T)values[id][typeof(T)];
        public void SetBuffer<T>(Entity id, params T[] items) { var b = new Buffer<T>(); b.Items.AddRange(items); Put(id, b); }
        public bool HasBuffer<T>(Entity id) => HasComponent<Buffer<T>>(id);
        public Buffer<T> GetBuffer<T>(Entity id, bool readOnly) { if (!readOnly) throw new Exception("Writable buffer access forbidden in targeting"); return GetComponentData<Buffer<T>>(id); }
    }
    public sealed class World
    {
        public EntityManager EntityManager = new EntityManager();
        public Dictionary<Type, object> Systems = new Dictionary<Type, object>();
        public T GetExistingSystemManaged<T>() where T : class => Systems.TryGetValue(typeof(T), out var value) ? (T)value : null;
        public void Register<T>(T value) { Systems[typeof(T)] = value; }
    }
}
namespace Unity.Jobs { public struct JobHandle { public static JobHandle CombineDependencies(JobHandle a, JobHandle b) => default; public void Complete() { } } }
namespace UnityEngine { public struct Color { public float r, g, b, a; public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; } } }
namespace Game { }
namespace Game.Common
{
    public struct BatchesUpdated { } public struct Deleted { } public struct Created { } public struct Updated { } public struct Owner { public Entity m_Owner; }
    [Flags] public enum TypeMask { None = 0, Terrain=1, StaticObjects = 2, Net = 8, Areas = 32, Lanes = 4096 }
    [Flags] public enum RaycastFlags { DebugDisable = 1, UIDisable = 2, ToolDisable = 4, FreeCameraDisable = 8, SubElements = 32, Placeholders = 64, Markers = 128,NoMainElements=256, Decals = 16384, EditorContainers = 32768 }
    public struct RaycastHit { public Entity m_HitEntity; public Unity.Mathematics.float3 m_HitPosition; } public struct RaycastResult { public Entity m_Owner; public RaycastHit m_Hit; }
}
namespace Game.Prefabs
{
    public class PrefabBase { public string name; public bool isReadOnly; }
    public class BuildingPrefab : PrefabBase { }
    public struct PrefabRef { public Entity m_Prefab; } public struct EffectData { } public struct ObjectGeometryData { public float3 m_Size; }
    public class PrefabSystem { public Dictionary<Entity, PrefabBase> Prefabs = new Dictionary<Entity, PrefabBase>(); public T GetPrefab<T>(Entity id) where T : PrefabBase => (T)Prefabs[id]; public T GetPrefab<T>(PrefabRef reference) where T : PrefabBase => GetPrefab<T>(reference.m_Prefab); }
}
namespace Game.Objects
{
    public struct Object { } public struct Tree { } public struct Plant { } public struct Marker { } public struct SpawnLocation { }
    public struct SubObject { public Entity m_SubObject; } public struct Transform { public float3 m_Position; }
}
namespace Game.Buildings { public struct Building { } public struct ServiceUpgrade { } }
namespace Game.Routes { public struct TakeoffLocation { } }
namespace Game.Net
{
    public struct Edge { } public struct Node { public float3 m_Position; } public struct Lane { } public struct SubLane { public Entity m_SubLane; } public struct SubNet { public Entity m_SubNet; }
    [Flags] public enum Layer { None = 0, All = -1 }
    public struct Curve { public Colossal.Mathematics.Bezier4x3 m_Bezier; }
}
namespace Game.Areas
{
    public struct Area { } public struct Surface { } public struct Space { } public struct SubArea { public Entity m_Area; } public struct Node { public float3 m_Position; }
    [Flags] public enum AreaTypeMask { None = 0, Spaces = 8, Surfaces = 16 }
}
namespace Game.Tools
{
    public struct Highlighted { }
    public struct Temp { } public struct EditorContainer { public Entity m_Prefab; }
    public class Action { public bool shouldBeEnabled, Pressed; public bool WasPressedThisFrame() => Pressed; }
    public class ToolBaseSystem
    {
        public World World; public bool Enabled;
        protected ToolSystem m_ToolSystem => World.GetExistingSystemManaged<ToolSystem>();
        protected ToolRaycastSystem m_ToolRaycastSystem => World.GetExistingSystemManaged<ToolRaycastSystem>();
        protected EntityManager EntityManager => World.EntityManager;
        public Action applyAction = new Action(), cancelAction = new Action();
        public virtual string toolID => "Default";
        public virtual Game.Prefabs.PrefabBase GetPrefab() => null;
        public virtual bool TrySetPrefab(Game.Prefabs.PrefabBase value) => false;
        public virtual void InitializeRaycast() { m_ToolRaycastSystem.raycastFlags &= Game.Common.RaycastFlags.DebugDisable | Game.Common.RaycastFlags.UIDisable | Game.Common.RaycastFlags.ToolDisable | Game.Common.RaycastFlags.FreeCameraDisable; }
        protected virtual void OnStartRunning() { } protected virtual void OnStopRunning() { }
        protected virtual Unity.Jobs.JobHandle OnUpdate(Unity.Jobs.JobHandle input) => input;
        public void Start() => OnStartRunning(); public void StopRunning() => OnStopRunning(); public void Tick() => OnUpdate(default);
    }
    public class DefaultToolSystem : ToolBaseSystem { }
    public class ToolSystem
    {
        private ToolBaseSystem tool;
        public ToolBaseSystem activeTool { get => tool; set { tool?.StopRunning(); tool = value; tool?.Start(); } }
    }
    public class ToolRaycastSystem
    {
        public Game.Common.TypeMask typeMask; public Game.Common.RaycastFlags raycastFlags;
        public Game.Net.Layer netLayerMask; public Game.Areas.AreaTypeMask areaTypeMask;
        public bool Hit; public Game.Common.RaycastResult Result;
        public bool GetRaycastResult(out Game.Common.RaycastResult value) { value = Result; return Hit; }
    }
}
namespace Game.Rendering
{
    public class OverlayRenderSystem
    {
        public static int Circles, Cyan, Gold, Lines, Curves;
        public enum StyleFlags { Grid=1,Projected=2,DepthFadeBelow=4 }
        public struct Buffer
        {
            public void DrawCircle(UnityEngine.Color color, float3 position, float size) { Circles++; if (color.b > .9f) Cyan++; else Gold++; }
            public void DrawCircle(UnityEngine.Color color,UnityEngine.Color fill,float width,StyleFlags flags,float2 direction,float3 position,float size) { if(fill.a!=0)throw new Exception("Opaque selection disk obscures target");DrawCircle(color,position,size); }
            public void DrawCurve(UnityEngine.Color color, Colossal.Mathematics.Bezier4x3 curve, float size) { Curves++; }
            public void DrawLine(UnityEngine.Color color, Colossal.Mathematics.Line3.Segment line, float size, bool flag) { Lines++; }
        }
        public Buffer GetBuffer(out Unity.Jobs.JobHandle writers) { writers = default; return new Buffer(); }
        public void AddBufferWriter(Unity.Jobs.JobHandle handle) { }
    }
}
namespace MW.EditorTools.Services { internal sealed class EditorContextService { internal bool Available = true, EditorAvailable = true, DialogBusy; internal Game.Prefabs.PrefabBase Asset; } }
namespace MW.EditorTools.Modules.DeletionProtection
{
    public class ListRecovery { internal World World; internal int Probes, DeepProbes; internal object UndoSlot = new object(); internal string TargetUndoFeedback(Entity id, bool selected) { if(selected&&World?.EntityManager.HasComponent<Game.Tools.Highlighted>(id)==true)throw new Exception("Presentation tag leaked into preflight"); Probes++; if (selected) DeepProbes++; return selected ? "Move preflight passed; capture rechecks companions." : "Candidate; click to check."; } }
}
namespace MW.EditorTools.Modules.BulldozerRecovery
{
    internal sealed partial class BulldozerTrace
    {
        internal World world; internal MW.EditorTools.Services.EditorContextService context;
        internal bool Busy, MovePreviewActive, disabled, effectDisabled, networkDisabled, areaMoveDisabled, moveDisabled, Compatible = true;
        internal string Status = "Undo unavailable: patch conflict.", Failure; internal int Captures; internal object UndoSlot = new object();
        private bool PatchesCompatible() => Compatible;
        private void Check() { Captures++; if (Failure != null) throw new NotSupportedException(Failure); }
        private object CaptureEffect(Entity id) { Check(); return null; }
        private object CaptureMoveOwner(Entity id, bool effect = false) { Check(); return null; }
        private object CaptureStandaloneArea(Entity id, bool bound) { Check(); return null; }
        private sealed class Node { internal Entity Owner; }
        private Node[] CaptureMoveGroup(Entity id) { Check(); return new[] { new Node { Owner = world.EntityManager.HasComponent<Game.Common.Owner>(id) ? world.EntityManager.GetComponentData<Game.Common.Owner>(id).m_Owner : Entity.Null } }; }
        private bool MoveGroupSettled(Node[] nodes) => true;
    }
}
namespace MW.EditorTools { internal static class Mod { internal static readonly TestLog Log = new TestLog(); internal sealed class TestLog { internal void Info(string message) { } } } }
