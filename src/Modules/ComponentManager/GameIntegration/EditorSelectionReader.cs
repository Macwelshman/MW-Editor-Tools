using System.Collections.Generic;
using Game.Prefabs;
using Game.UI.Editor;
using MW.EditorTools.Modules.ComponentManager.Diagnostics;
using Unity.Entities;

namespace MW.EditorTools.Modules.ComponentManager.GameIntegration
{
    internal static class EditorSelectionReader
    {
        public static InspectorState Read(World world)
        {
            PrefabBase prefab;
            object selection;
            return Read(world, out prefab, out selection);
        }

        public static InspectorState Read(World world, out PrefabBase prefab, out object selection)
        {
            prefab = null;
            selection = null;
            var result = new InspectorState { available = true };
            if (world == null || !world.IsCreated)
            {
                result.status = "The Editor world is unavailable.";
                return result;
            }
            var inspector = world.GetExistingSystemManaged<InspectorPanelSystem>();
            var panels = world.GetExistingSystemManaged<EditorPanelUISystem>();
            if (inspector == null || panels == null || !ReferenceEquals(panels.activePanel, inspector))
            {
                result.status = "Select an asset so its Object Info panel is active.";
                return result;
            }

            selection = inspector.selectedObject;
            if (selection == null)
                return result;
            result.selectionKind = selection.GetType().FullName;
            prefab = selection as PrefabBase;
            if (prefab == null)
            {
                var component = selection as ComponentBase;
                var mesh = selection as ObjectMeshInfo;
                var subObject = selection as ObjectSubObjectInfo;
                if (component != null) prefab = component.prefab;
                else if (mesh != null) prefab = mesh.m_Mesh;
                else if (subObject != null) prefab = subObject.m_Object;
            }
            if (prefab == null)
            {
                result.status = "This selection does not resolve to a supported prefab.";
                return result;
            }
            var prefabs = world.GetExistingSystemManaged<PrefabSystem>();
            Entity entity;
            if (prefabs == null || !prefabs.TryGetEntity(prefab, out entity) || !world.EntityManager.Exists(entity))
            {
                result.status = "The selected prefab is no longer registered. Select a current asset.";
                return result;
            }

            result.hasAsset = true;
            result.assetName = prefab.name;
            result.prefabType = prefab.GetType().FullName;
            var rows = new List<ComponentRow>();
            if (prefab.components != null)
            {
                for (int i = 0; i < prefab.components.Count; i++)
                {
                    var component = prefab.components[i];
                    rows.Add(new ComponentRow {
                        index = i,
                        valid = component != null,
                        typeName = component == null ? "Missing component reference" : component.GetType().FullName,
                        active = component != null && component.active
                    });
                }
            }
            result.components = rows.ToArray();
            result.status = "Copy from the source, select a destination, choose a mode and preview before applying.";
            return result;
        }
    }
}
