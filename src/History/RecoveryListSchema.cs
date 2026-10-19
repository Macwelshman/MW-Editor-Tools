using System;
using System.Collections;
using Game.Prefabs;

namespace MW.EditorTools.History
{
    internal static class RecoveryListSchema
    {
        internal static readonly Type[] Components = { typeof(ObjectSubObjects), typeof(ObjectSubAreas), typeof(ObjectSubNets), typeof(EffectSource), typeof(ObjectSubLanes) };
        internal static readonly Type[] Elements = { typeof(ObjectSubObjectInfo), typeof(ObjectSubAreaInfo), typeof(ObjectSubNetInfo), typeof(EffectSource.EffectSettings), typeof(ObjectSubLaneInfo) };
        internal static readonly string[] Members = { "m_SubObjects", "m_SubAreas", "m_SubNets", "m_Effects", "m_SubLanes" };
        internal static readonly string[] Labels = { "Object", "Area", "Net", "Effect", "Lane" };

        // Operate on a detached copy and the underlying index, never the filtered UI rows.
        internal static PrefabSnapshot AfterDeletion(IList storage, int index, bool clear)
        {
            if (storage == null || (!clear && (index < 0 || index >= storage.Count)))
                throw new InvalidOperationException("Invalid underlying deletion index.");
            var detached = (IList)PrefabSnapshot.Take(storage).Materialize();
            if (storage is Array original)
            {
                var target = Array.CreateInstance(original.GetType().GetElementType(), clear ? 0 : original.Length - 1);
                if (!clear)
                {
                    int at = 0;
                    for (int i = 0; i < detached.Count; i++) if (i != index) target.SetValue(detached[i], at++);
                }
                detached = target;
            }
            else if (clear) detached.Clear();
            else detached.RemoveAt(index);
            return PrefabSnapshot.Take(detached);
        }
    }
}
