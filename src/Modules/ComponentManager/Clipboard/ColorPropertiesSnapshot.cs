using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Game.Prefabs;
using Game.Rendering;
using Unity.Mathematics;
using UnityEngine;

namespace MW.EditorTools.Modules.ComponentManager.Clipboard
{
    // Immutable value data only. No component, owner, Unity object or ECS handles.
    internal sealed class ColorPropertiesSnapshot
    {
        public const int SchemaVersion = 1;
        public const string AdapterId = "ColorProperties";
        public readonly string TypeIdentity;
        public readonly int SourceComponentIndex;
        public readonly string SourceName;
        public readonly string SourcePrefabType;
        public readonly bool Active;
        public readonly int3 VariationRanges, AlphaRanges;
        public readonly ColorSourceType ExternalSource;
        public readonly ReadOnlyCollection<Variation> Variations;
        public readonly ReadOnlyCollection<Binding> Bindings;
        public readonly ReadOnlyCollection<Group> Groups;

        public sealed class Variation
        {
            public readonly ReadOnlyCollection<Color> Colors;
            public readonly string GroupName;
            public Variation(ColorProperties.VariationSet value)
            {
                Colors = value.m_Colors == null ? null : Array.AsReadOnly((Color[])value.m_Colors.Clone());
                GroupName = value.m_VariationGroup;
            }
        }
        public sealed class Binding
        {
            public readonly sbyte ChannelId;
            public readonly bool CanBeModifiedByExternal;
            public Binding(ColorProperties.ColorChannelBinding value)
            { ChannelId = value.m_ChannelId; CanBeModifiedByExternal = value.m_CanBeModifiedByExternal; }
        }
        public sealed class Group
        {
            public readonly string Name;
            public readonly int Probability;
            public readonly ColorSyncFlags MeshSyncMode;
            public readonly bool OverrideRandomness;
            public readonly int3 VariationRanges, AlphaRanges;
            public Group(ColorProperties.VariationGroup value)
            {
                Name = value.m_Name; Probability = value.m_Probability; MeshSyncMode = value.m_MeshSyncMode;
                OverrideRandomness = value.m_OverrideRandomness;
                VariationRanges = value.m_VariationRanges; AlphaRanges = value.m_AlphaRanges;
            }
        }

        public ColorPropertiesSnapshot(ColorProperties source, string sourceName, string sourcePrefabType, int sourceComponentIndex = -1)
        {
            if (source == null || source.GetType() != typeof(ColorProperties))
                throw new ArgumentException("Only exact ColorProperties components are supported.");
            TypeIdentity = typeof(ColorProperties).AssemblyQualifiedName;
            SourceComponentIndex = sourceComponentIndex;
            SourceName = sourceName; SourcePrefabType = sourcePrefabType;
            Active = source.active; VariationRanges = source.m_VariationRanges;
            AlphaRanges = source.m_AlphaRanges; ExternalSource = source.m_ExternalColorSource;
            Variations = Capture(source.m_ColorVariations, v => new Variation(v));
            Bindings = Capture(source.m_ChannelsBinding, b => new Binding(b));
            Groups = Capture(source.m_VariationGroups, g => new Group(g));
        }

        private static ReadOnlyCollection<TOut> Capture<TIn, TOut>(List<TIn> source, Func<TIn, TOut> capture)
            where TIn : class where TOut : class
        {
            if (source == null) return null;
            var values = new TOut[source.Count];
            for (int i = 0; i < values.Length; i++) values[i] = source[i] == null ? null : capture(source[i]);
            return Array.AsReadOnly(values);
        }

        public string Summary
        {
            get { return SourceName + ": Color Properties — " + (Variations == null ? 0 : Variations.Count)
                + " variations, " + (Bindings == null ? 0 : Bindings.Count) + " bindings, "
                + (Groups == null ? 0 : Groups.Count) + " groups. Paste validates settings and preserves destination active state."; }
        }
    }
}
