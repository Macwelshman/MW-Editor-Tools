using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Entities;
// Isolated requirement checks. These doubles do not validate the installed game's runtime lifecycle.

namespace Unity.Entities
{
    public struct Entity { public int Id; public static Entity Null { get { return new Entity(); } } }
    public sealed class EntityManager {
        public object Data;
        public bool Exists(Entity entity) { return entity.Id != 0; }
        public bool HasComponent<T>(Entity entity) { return Data is T; }
        public T GetComponentData<T>(Entity entity) { return (T)Data; }
    }
    public sealed class World
    {
        public bool IsCreated = true;
        public EntityManager EntityManager = new EntityManager();
        public Dictionary<Type, object> Systems = new Dictionary<Type, object>();
        public T GetExistingSystemManaged<T>() where T : class
        { object system; return Systems.TryGetValue(typeof(T), out system) ? (T)system : null; }
    }
}
namespace UnityEngine
{
    public class ScriptableObject : Object { public static ScriptableObject CreateInstance(Type type) { return (ScriptableObject)Activator.CreateInstance(type); } }
    public class Object { public static void Destroy(object value) { } }
}
namespace Game.Prefabs
{
    public class ComponentBase : UnityEngine.ScriptableObject { public PrefabBase prefab; public bool active = true; }
    public class PrefabBase : ComponentBase {
        public string name = "Test asset"; public List<ComponentBase> components = new List<ComponentBase>(); public bool isReadOnly, isDirty; public TestAsset asset;
        public Type FailAddition;public string NativeUIIcon;public UIGroupPrefab NativeUIGroup;
        public ComponentBase AddComponent(Type type) {
            if (type == FailAddition) throw new InvalidOperationException("Injected addition failure");
            if (components.Exists(c => c != null && c.GetType() == type)) return null;
            var added = (ComponentBase)UnityEngine.ScriptableObject.CreateInstance(type); added.prefab = this;if(added is UIObject ui){ui.m_Icon=NativeUIIcon;ui.m_Group=NativeUIGroup;} components.Add(added); isDirty = true; return added;
        }
        public void Remove(Type type) { int index = components.FindIndex(c => c != null && c.GetType() == type); if(index >= 0)components.RemoveAt(index); isDirty = true; }
    }
    public class RenderPrefabBase : PrefabBase { }
    public class RenderPrefab : RenderPrefabBase { }
    public struct PrefabRef { }
    public class TestAsset { public int Marks; public bool MarkDirty() { Marks++; return true; } }
    public class ObjectMeshInfo { public PrefabBase m_Mesh; }
    public class ObjectSubObjectInfo { public PrefabBase m_Object; }
    public class PrefabSystem
    {
        public bool Registered = true;
        public bool EntityExists = true;
        public int Updates, FailUpdates;
        public void UpdatePrefab(PrefabBase prefab)
        { Updates++; if (FailUpdates > 0) { FailUpdates--; throw new InvalidOperationException("Injected update failure"); } }
        public bool TryGetEntity(PrefabBase prefab, out Entity entity)
        { entity = new Entity { Id = EntityExists ? 1 : 0 }; return Registered; }
        public PrefabBase Root;
        public bool TryGetPrefab<T>(PrefabRef reference, out T prefab) where T : PrefabBase { prefab = Root as T; return prefab != null; }
    }
}
namespace Game.UI.Editor
{
    public class InspectorPanelSystem { public object selectedObject; }
    public class EditorPanelUISystem { public object activePanel; }
    public class EditorErrorPanelSystem { public void OnPrefabModified() { } }
}

// Value and stale-selection tests use doubles; they do not prove live Editor behaviour.

namespace UnityEngine
{
    public struct Color { public float r, g, b, a; public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;} }
}
namespace Unity.Mathematics
{
    public struct int3 { public int x, y, z; public int3(int x,int y,int z){this.x=x;this.y=y;this.z=z;} }
}
namespace Game.Rendering
{
    public class MeshColorSystem { public void SetOverride(Unity.Entities.Entity entity, Game.Prefabs.PrefabBase prefab, int index) { } }
    public enum ColorSourceType { None, Parent }
    public enum ColorSyncFlags { None, SameIndex }
}
namespace Game.Prefabs
{
    public class ColorProperties : ComponentBase
    {
        public class VariationSet { public UnityEngine.Color[] m_Colors; public string m_VariationGroup; }
        public class ColorChannelBinding { public sbyte m_ChannelId; public bool m_CanBeModifiedByExternal; }
        public class VariationGroup
        {
            public string m_Name; public int m_Probability; public Game.Rendering.ColorSyncFlags m_MeshSyncMode;
            public bool m_OverrideRandomness; public Unity.Mathematics.int3 m_VariationRanges, m_AlphaRanges;
        }
        public List<VariationSet> m_ColorVariations;
        public List<ColorChannelBinding> m_ChannelsBinding;
        public List<VariationGroup> m_VariationGroups;
        public Unity.Mathematics.int3 m_VariationRanges, m_AlphaRanges;
        public Game.Rendering.ColorSourceType m_ExternalColorSource;
    }
}


namespace Unity.Mathematics
{
    public struct float2 { public float x,y; public float2(float x,float y){this.x=x;this.y=y;} }
}
namespace Colossal.Mathematics
{
    public struct Bounds2 { public Unity.Mathematics.float2 min,max; public Bounds2(Unity.Mathematics.float2 min,Unity.Mathematics.float2 max){this.min=min;this.max=max;} }
}
namespace Game.Tools { public class ToolSystem { public Entity selected; } }
namespace Game.UI.Widgets
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true, Inherited=true)]
    public class ComponentRequirementAttribute : Attribute { public Type[] componentTypes; public ComponentRequirementAttribute(params Type[] types){componentTypes=types;} }
}
namespace Game.Prefabs
{
    [AttributeUsage(AttributeTargets.Class, Inherited=true)]
    public class ComponentMenuAttribute : Attribute { public Type[] requiredPrefab; public bool hideInEditor; public ComponentMenuAttribute(params Type[] types){requiredPrefab=types;} }
    [ComponentMenu(typeof(PrefabBase))]
    public class StructuralComponent : ComponentBase { public string UnadaptedSetting = "Default"; }
    [ComponentMenu(typeof(PrefabBase)), Game.UI.Widgets.ComponentRequirement(typeof(StructuralComponent))]
    public class RequiringComponent : ComponentBase { }
    public class ObjectGeometryPrefab : PrefabBase { public ObjectMeshInfo[] m_Meshes; }
    [ComponentMenu(typeof(PrefabBase))]
    public class BuildingTerraformOverride : ComponentBase
    {
        public Unity.Mathematics.float2 m_LevelMinOffset,m_LevelMaxOffset,m_LevelFrontLeft,m_LevelFrontRight,m_LevelBackLeft,m_LevelBackRight,m_SmoothMinOffset,m_SmoothMaxOffset;
        public float m_HeightOffset;public bool m_DontRaise,m_DontLower;public SubLot[] m_AdditionalSmoothAreas;
        public class SubLot {public Colossal.Mathematics.Bounds2 m_Area;public float m_HeightOffset;public bool m_Circular,m_DontRaise,m_DontLower;}
    }
}


namespace Game.Prefabs {
 public class UIGroupPrefab:PrefabBase {}
 [ComponentMenu(typeof(PrefabBase))] public class UIObject:ComponentBase {public UIGroupPrefab m_Group;public int m_Priority;public string m_Icon;public bool m_IsDebugObject;}
}
namespace MW.EditorTools.Modules.ComponentManager.Pack {
 internal sealed partial class PriorityBatch {internal static Game.Prefabs.PrefabBase[] TestWorkspace;internal static Game.Prefabs.PrefabBase[] Workspace(Unity.Entities.World world)=>TestWorkspace;}
}

namespace Game.UI.Widgets {public interface IWidget {}}
namespace Game.UI.Editor {
 public class LocalizationField {
  public class LocalizationFieldEntry {public string localeId,text;public LocalizationFieldEntry(string locale,string value){localeId=locale;text=value;}}
  private List<LocalizationFieldEntry> entries=new();
  public IReadOnlyList<LocalizationFieldEntry> localization=>entries;
  public void Initialize(IEnumerable<LocalizationFieldEntry> values){entries=values.Select(e=>new LocalizationFieldEntry(e.localeId,e.text)).ToList();}
  public IEnumerable<LocalizationFieldEntry> ValidEntries()=>entries.Where(e=>!string.IsNullOrWhiteSpace(e.text));
  public void SetPropertiesChanged(){}
 }
}
