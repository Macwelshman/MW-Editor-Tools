using System;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;

namespace MW.EditorTools.Modules.ComponentManager.Transfer
{
    internal sealed class TransferTarget
    {
        public readonly World World;
        public readonly PrefabBase Prefab, Parent;
        public TransferTarget(World world, PrefabBase prefab, PrefabBase parent) { World = world; Prefab = prefab; Parent = parent; }
        public static TransferTarget Resolve(World world, PrefabBase prefab, object selection)
        {
            PrefabBase root = null;
            var tools = world.GetExistingSystemManaged<ToolSystem>(); var prefabs = world.GetExistingSystemManaged<PrefabSystem>();
            if (tools != null && prefabs != null && world.EntityManager.Exists(tools.selected) && world.EntityManager.HasComponent<PrefabRef>(tools.selected))
            {
                PrefabBase candidate;
                if (prefabs.TryGetPrefab<PrefabBase>(world.EntityManager.GetComponentData<PrefabRef>(tools.selected), out candidate))
                {
                    var geometry = candidate as ObjectGeometryPrefab;
                    if (ReferenceEquals(candidate, prefab) || (geometry != null && geometry.m_Meshes != null && geometry.m_Meshes.Any(m => m != null && ReferenceEquals(m.m_Mesh, prefab)))) root = candidate;
                }
            }
            if (root == null && (ReferenceEquals(selection, prefab) || (!(prefab is RenderPrefabBase) && selection is ComponentBase && ReferenceEquals(((ComponentBase)selection).prefab, prefab)))) root = prefab;
            if (root == null) throw new InvalidOperationException("Nested owner cannot be verified. Select the prefab or a mesh belonging to the selected object.");
            return new TransferTarget(world, prefab, root);
        }
        public void Validate()
        {
            if (World == null || !World.IsCreated || Prefab == null || Parent == null) throw new InvalidOperationException("Destination/session unavailable.");
            if (Prefab.isReadOnly || Parent.isReadOnly) throw new InvalidOperationException("Destination or parent is read-only. Use editable asset copies.");
            var registry = World.GetExistingSystemManaged<PrefabSystem>(); Entity entity;
            foreach (var prefab in ReferenceEquals(Prefab, Parent) ? new[] { Prefab } : new[] { Prefab, Parent })
                if (registry == null || !registry.TryGetEntity(prefab, out entity) || !World.EntityManager.Exists(entity)) throw new InvalidOperationException("Destination/parent is no longer registered.");
            if (!ReferenceEquals(Parent, Prefab))
            {
                var geometry = Parent as ObjectGeometryPrefab;
                if (geometry == null || geometry.m_Meshes == null || !geometry.m_Meshes.Any(m => m != null && ReferenceEquals(m.m_Mesh, Prefab)))
                    throw new InvalidOperationException("Mesh no longer belongs to the captured parent.");
            }
        }
        public ComponentBase Exact(Type type)
        {
            ComponentBase result = null;
            if (Prefab.components != null) foreach (var component in Prefab.components)
            {
                if (component == null || !type.IsAssignableFrom(component.GetType())) continue;
                if (component.GetType() != type || result != null || !ReferenceEquals(component.prefab, Prefab)) throw new InvalidOperationException("Ambiguous/derived component or incorrect owner.");
                result = component;
            }
            return result;
        }
        public ComponentBase[] Identities() { return Prefab.components == null ? new ComponentBase[0] : Prefab.components.ToArray(); }
        public string Fingerprint()
        {
            return string.Join(";", Identities().Select(c => c == null ? "missing" : c.GetType().AssemblyQualifiedName + ":" + c.active + ":"
                + SettingsFingerprint(c)));
        }
        private string SettingsFingerprint(ComponentBase component){try{return Adapters.For(component.GetType())?.Capture(component,Prefab).Fingerprint??"unsupported";}catch(NotSupportedException){return "unsupported";}}
        public void Refresh()
        {
            Validate();
            var registry = World.GetExistingSystemManaged<PrefabSystem>();
            var errors = World.GetExistingSystemManaged<Game.UI.Editor.EditorErrorPanelSystem>(); if (errors != null) errors.OnPrefabModified();
            if (Prefab is RenderPrefabBase)
            {
                var colors = World.GetExistingSystemManaged<Game.Rendering.MeshColorSystem>(); if (colors != null) colors.SetOverride(Entity.Null, null, -1);
            }
            registry.UpdatePrefab(Prefab); if (Prefab.asset != null) Prefab.asset.MarkDirty();
            if (!ReferenceEquals(Prefab, Parent)) { registry.UpdatePrefab(Parent); if (Parent.asset != null) Parent.asset.MarkDirty(); }
        }
    }
}

