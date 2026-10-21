using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Prefabs;
using Game.UI.Editor;
using Game.UI.Widgets;

namespace MW.EditorTools.Modules.ComponentManager.GameIntegration
{
    internal static class ComponentHeadingReader
    {
        // Verified native RefreshSections copy callback captures the actual component.
        // Never match translated headings, type names or visible list positions.
        internal static Dictionary<ComponentBase, string> Read(InspectorPanelSystem inspector)
        {
            var result = new Dictionary<ComponentBase, string>();
            if (typeof(InspectorPanelSystem).Module.ModuleVersionId.ToString() != "e15551e9-3dea-4855-8dde-1da3cea7d7ef") return result;
            Walk(((IEditorPanel)inspector).children, "editorPanel", inspector, result, 0);
            return result;
        }

        private static void Walk(IList<IWidget> widgets, string parent, InspectorPanelSystem inspector,
            Dictionary<ComponentBase, string> result, int depth)
        {
            if (widgets == null || depth > 24) return;
            foreach (var widget in widgets)
            {
                if (widget == null) continue;
                string path = parent + "." + widget.path.ToString();
                var section = widget as EditorSection;
                var callback = section?.onCopy;
                var target = callback?.Target;
                if (target != null && callback.GetInvocationList().Length == 1 &&
                    target.GetType().DeclaringType == typeof(InspectorPanelSystem))
                {
                    var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                    var component = target.GetType().GetField("comp", flags)?.GetValue(target) as ComponentBase;
                    var owners = target.GetType().GetFields(flags).Where(f => f.FieldType == typeof(InspectorPanelSystem)).ToArray();
                    var owner = owners.Length == 1 ? owners[0].GetValue(target) : null;
                    if (component != null && ReferenceEquals(owner, inspector))
                    {
                        // Duplicate mappings are deliberately unavailable.
                        if (result.ContainsKey(component)) result[component] = "";
                        else result.Add(component, path);
                    }
                }
                Walk(widget.visibleChildren, path, inspector, result, depth + 1);
            }
        }
    }
}
