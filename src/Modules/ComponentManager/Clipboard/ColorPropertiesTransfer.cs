using System;
using System.Collections.Generic;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;

namespace MW.EditorTools.Modules.ComponentManager.Clipboard
{
    internal static class ColorPropertiesTransfer
    {
        // A conservative first trial: three rendering channels, complete group links,
        // finite colours and ranges that the native initializer does not need to clamp.
        public static string Validate(ColorPropertiesSnapshot value)
        {
            if (value == null) return "Copy Color Properties first.";
            if (value.Variations == null || value.Variations.Count == 0) return "At least one colour variation is required.";
            if (value.Bindings == null || value.Bindings.Count != 3) return "The trial requires exactly three channel bindings.";
            if (!Range(value.VariationRanges) || !Range(value.AlphaRanges)) return "Ranges must be between 0 and 100.";
            if (!Enum.IsDefined(typeof(Game.Rendering.ColorSourceType), value.ExternalSource)) return "Unknown external colour source.";
            var names = new HashSet<string>(StringComparer.Ordinal);
            if (value.Groups != null) foreach (var group in value.Groups)
            {
                if (group == null || string.IsNullOrEmpty(group.Name) || !names.Add(group.Name)) return "Missing, unnamed or duplicate variation group. Unnamed groups are deferred in this trial.";
                if (group.Probability < 0 || group.Probability > 100 || !Range(group.VariationRanges) || !Range(group.AlphaRanges))
                    return "Invalid group probability or ranges.";
                // Initial trial only transfers enum values explicitly declared by the game.
                if (!Enum.IsDefined(typeof(Game.Rendering.ColorSyncFlags), group.MeshSyncMode)) return "Unsupported colour sync flags.";
            }
            foreach (var variation in value.Variations)
            {
                if (variation == null || variation.Colors == null || variation.Colors.Count == 0) return "Missing variation or colour array.";
                if (!string.IsNullOrEmpty(variation.GroupName) && !names.Contains(variation.GroupName)) return "Variation references a missing group.";
                foreach (var color in variation.Colors)
                    if (!Finite(color.r) || !Finite(color.g) || !Finite(color.b) || !Finite(color.a)) return "Non-finite colour value.";
                foreach (var binding in value.Bindings)
                    if (binding == null || binding.ChannelId < 0 || binding.ChannelId >= variation.Colors.Count) return "Channel binding is outside a colour array.";
            }
            return null;
        }

        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static bool Range(int3 value)
        { return value.x >= 0 && value.x <= 100 && value.y >= 0 && value.y <= 100 && value.z >= 0 && value.z <= 100; }

        public static void Apply(ColorPropertiesSnapshot value, ColorProperties target)
        {
            if (target == null || target.GetType() != typeof(ColorProperties)) throw new ArgumentException("Exact Color Properties target required.");
            // Allocate everything before the first write. Each paste owns fresh nested data.
            var variations = Map(value.Variations, v => new ColorProperties.VariationSet {
                m_Colors = v.Colors == null ? null : new List<Color>(v.Colors).ToArray(), m_VariationGroup = v.GroupName });
            var bindings = Map(value.Bindings, b => new ColorProperties.ColorChannelBinding {
                m_ChannelId = b.ChannelId, m_CanBeModifiedByExternal = b.CanBeModifiedByExternal });
            var groups = Map(value.Groups, g => new ColorProperties.VariationGroup {
                m_Name = g.Name, m_Probability = g.Probability, m_MeshSyncMode = g.MeshSyncMode,
                m_OverrideRandomness = g.OverrideRandomness, m_VariationRanges = g.VariationRanges, m_AlphaRanges = g.AlphaRanges });
            target.m_ColorVariations = variations; target.m_ChannelsBinding = bindings; target.m_VariationGroups = groups;
            target.m_VariationRanges = value.VariationRanges; target.m_AlphaRanges = value.AlphaRanges;
            target.m_ExternalColorSource = value.ExternalSource;
            // Preserve destination activation: this trial changes settings, not archetypes.
        }

        private static List<TOut> Map<TIn, TOut>(IEnumerable<TIn> values, Func<TIn, TOut> map) where TIn : class where TOut : class
        {
            if (values == null) return null;
            var result = new List<TOut>();
            foreach (var value in values) result.Add(value == null ? null : map(value));
            return result;
        }

        public static bool Same(ColorPropertiesSnapshot a, ColorPropertiesSnapshot b)
        {
            return a.Active == b.Active && a.VariationRanges.Equals(b.VariationRanges) && a.AlphaRanges.Equals(b.AlphaRanges)
                && a.ExternalSource == b.ExternalSource
                && SameList(a.Variations, b.Variations, (x, y) => x.GroupName == y.GroupName && SameList(x.Colors, y.Colors, (c, d) => c.Equals(d)))
                && SameList(a.Bindings, b.Bindings, (x, y) => x.ChannelId == y.ChannelId && x.CanBeModifiedByExternal == y.CanBeModifiedByExternal)
                && SameList(a.Groups, b.Groups, (x, y) => x.Name == y.Name && x.Probability == y.Probability && x.MeshSyncMode == y.MeshSyncMode
                    && x.OverrideRandomness == y.OverrideRandomness && x.VariationRanges.Equals(y.VariationRanges) && x.AlphaRanges.Equals(y.AlphaRanges));
        }

        private static bool SameList<T>(IList<T> a, IList<T> b, Func<T, T, bool> same)
        {
            if (a == null || b == null) return a == null && b == null;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (ReferenceEquals(a[i], null) || ReferenceEquals(b[i], null))
                { if (!ReferenceEquals(a[i], null) || !ReferenceEquals(b[i], null)) return false; }
                else if (!same(a[i], b[i])) return false;
            }
            return true;
        }
    }
}
