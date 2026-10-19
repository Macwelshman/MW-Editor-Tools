using System.Globalization;

namespace MW.EditorTools.Modules.ComponentManager.GameIntegration
{
    internal sealed class SelectionRevision
    {
        private object selection, prefab;
        private object[] components = new object[0];
        private long revision;

        public string Observe(object currentSelection, object currentPrefab, object[] currentComponents)
        {
            bool changed = !ReferenceEquals(selection, currentSelection) || !ReferenceEquals(prefab, currentPrefab)
                || components.Length != currentComponents.Length;
            for (int i = 0; !changed && i < components.Length; i++)
                changed = !ReferenceEquals(components[i], currentComponents[i]);
            if (changed)
            {
                revision++;
                selection = currentSelection; prefab = currentPrefab;
                components = (object[])currentComponents.Clone();
            }
            return currentPrefab == null ? "" : revision.ToString(CultureInfo.InvariantCulture);
        }

        public void Clear()
        {
            selection = null; prefab = null; components = new object[0]; revision++;
        }
    }
}
