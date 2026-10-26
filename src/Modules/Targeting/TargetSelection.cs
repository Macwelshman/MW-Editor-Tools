using System;
using System.Collections.Generic;

namespace MW.EditorTools.Modules.Targeting
{
    internal enum TargetKind { None, Prop, Foliage, Effect, Marker, Surface, Space, Network, Lane }

    // Presentation/selection only: never owns an Undo snapshot or live entity data.
    internal sealed class TargetSelection<T>
    {
        internal string Filter { get; private set; } = "all";
        internal T Hover { get; private set; }
        internal T Selected { get; private set; }
        internal bool HasHover { get; private set; }
        internal bool HasSelected { get; private set; }
        private object context;

        internal static bool ValidFilter(string value) => value == "all" || value == "props" || value == "foliage" ||
            value == "effects" || value == "markers" || value == "surfaces" || value == "spaces" || value == "networks" || value == "lanes";
        internal static bool Matches(string filter, TargetKind kind) => kind != TargetKind.None &&
            (filter == "all" || filter == Key(kind));
        private static string Key(TargetKind kind) => kind == TargetKind.Prop ? "props" : kind == TargetKind.Foliage ? "foliage" :
            kind == TargetKind.Effect ? "effects" : kind == TargetKind.Marker ? "markers" : kind == TargetKind.Surface ? "surfaces" :
            kind == TargetKind.Space ? "spaces" : kind == TargetKind.Network ? "networks" : kind == TargetKind.Lane ? "lanes" : "";

        internal bool SetContext(object value)
        {
            if (ReferenceEquals(context, value)) return false;
            context = value; Clear(); return true;
        }
        internal bool SetFilter(string value)
        {
            if (!ValidFilter(value)) return false;
            if (Filter != value) { Filter = value; Clear(); }
            return true;
        }
        internal void SetHover(T value, TargetKind kind)
        {
            HasHover = Matches(Filter, kind);
            Hover = HasHover ? value : default(T);
        }
        internal bool SelectHover() { if (!HasHover) return false; Selected = Hover; HasSelected = true; return true; }
        internal void ClearSelected() { Selected = default(T); HasSelected = false; }
        internal void Clear() { ClearSelected(); HasHover = false; Hover = default(T); }
        internal bool TryTarget(Func<T, bool> valid, out T result)
        {
            result = HasSelected ? Selected : Hover;
            if ((!HasSelected && !HasHover) || !valid(result))
            {
                // An invalid explicit selection must never silently fall back to a
                // different hovered object during the same clipboard action.
                Clear(); result = default(T); return false;
            }
            return true;
        }
    }
}
