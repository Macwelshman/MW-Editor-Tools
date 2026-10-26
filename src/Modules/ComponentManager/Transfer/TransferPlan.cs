using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Game.UI.Widgets;

namespace MW.EditorTools.Modules.ComponentManager.Transfer
{
    internal enum TransferMode { SettingsOnly, ComponentsAndSettings, ComponentsOnly }
    internal sealed class PlannedChange
    {
        public Type Type;
        public ValueAdapter Adapter;
        public ComponentBase Existing;
        public ValueSnapshot Before, After, Source;
    }
    internal sealed class TransferPlan
    {
        public readonly TransferTarget Target;
        public readonly TransferMode Mode;
        public readonly long ClipboardRevision;
        public readonly ComponentBase[] Identities;
        public readonly string Fingerprint;
        public readonly List<PlannedChange> Changes = new List<PlannedChange>();
        public readonly List<string> Reports = new List<string>();
        public TransferPlan(TransferTarget target, TransferClipboard clipboard, TransferMode mode)
        {
            Target = target; Mode = mode; ClipboardRevision = clipboard.Revision;
            target.Validate(); Identities = target.Identities(); Fingerprint = target.Fingerprint();
            if (clipboard.Entries.Length == 0) throw new InvalidOperationException("Copy components from the source asset first.");
            foreach (var snapshot in clipboard.Entries)
            {
                var adapter = Adapters.For(snapshot.AdapterId);
                var type = adapter == null ? ResolveType(snapshot.AdapterId) : adapter.Type;
                string label = type == null ? snapshot.AdapterId : type.Name;
                try
                {
                    if (type == null) throw new InvalidOperationException("Unknown game component type.");
                    string metadata = MenuCompatibility(type, target.Prefab); if (metadata != null) throw new InvalidOperationException(metadata);
                    var existing = target.Exact(type);
                    if (existing != null && mode == TransferMode.ComponentsOnly) { Reports.Add(label + ": unchanged (already present)." ); continue; }
                    if (existing == null && mode == TransferMode.SettingsOnly) { Reports.Add(label + ": skipped (not present)." ); continue; }
                    if (adapter == null) throw new InvalidOperationException("Settings copying is unsupported for this component.");
                    var change = new PlannedChange { Type = type, Adapter = adapter, Existing = existing, Source = snapshot };
                    if (existing != null) change.Before = adapter.Capture(existing, target.Prefab);
                    if (adapter != null)
                    {
                        
                        if (change.Before != null) change.After = change.Before.Merge(snapshot, null);
                        else
                        {
                            var defaults = (ComponentBase)UnityEngine.ScriptableObject.CreateInstance(type);
                            try { change.After = adapter.Capture(defaults, target.Prefab).Merge(snapshot, null).WithActive(snapshot.Active); }
                            finally { UnityEngine.Object.Destroy(defaults); }
                        }
                        string invalid = adapter.Validate(change.After); if (invalid != null) throw new InvalidOperationException(invalid);
                        if (change.Before != null)
                        {
                            invalid = adapter.Validate(change.Before); if (invalid != null) throw new InvalidOperationException("Cannot safely restore destination: " + invalid);
                            if (type == typeof(ColorProperties) && (!change.Before.Get("Channel bindings").SequenceEqual(snapshot.Get("Channel bindings"))
                                || !change.Before.Get("External source").SequenceEqual(snapshot.Get("External source")))) throw new InvalidOperationException("Rendering channel/external-source semantics differ.");
                        }
                    }
                    Changes.Add(change);
                    Reports.Add(label + ": " + (existing == null ? "add" + " and copy settings" + " (copied activation)." + (adapter == null ? " Automatic restore unavailable; remove through Object Info if needed." : "")
                        : "replace settings; activation preserved."));
                }
                catch (Exception error) { Reports.Add(label + ": skipped — " + error.Message); }
            }
            bool removed;
            do {
                removed=false;
                foreach(var change in Changes.ToArray()){
                    var error=Compatibility(change.Type,target.Prefab,Changes);
                    if(error!=null){Changes.Remove(change);Reports.RemoveAll(r=>r.StartsWith(change.Type.Name+":",StringComparison.Ordinal));Reports.Add(change.Type.Name+": skipped — "+error);removed=true;}
                }
            }while(removed);
            var ordered=new List<PlannedChange>();var pending=new List<PlannedChange>(Changes);
            while(pending.Count>0){
                var next=pending.FirstOrDefault(c=>Dependencies(c.Type).All(required=>!pending.Any(p=>p!=c&&required.IsAssignableFrom(p.Type))));
                if(next==null)throw new InvalidOperationException("Cyclic bulk component dependencies.");
                ordered.Add(next);pending.Remove(next);
            }
            Changes.Clear();Changes.AddRange(ordered);
        }
        private static Type[] Dependencies(Type type)=>Attribute.GetCustomAttributes(type,typeof(ComponentRequirementAttribute),true).Cast<ComponentRequirementAttribute>().SelectMany(a=>a.componentTypes??new Type[0]).ToArray();
        public void CheckCurrent(long clipboardRevision)
        {
            Target.Validate(); var now = Target.Identities();
            if (ClipboardRevision != clipboardRevision || now.Length != Identities.Length || now.Where((c, i) => !ReferenceEquals(c, Identities[i])).Any()
                || Fingerprint != Target.Fingerprint()) throw new InvalidOperationException("Destination or clipboard changed. Preview again.");
            foreach (var change in Changes)
            { string error = Compatibility(change.Type, Target.Prefab, Changes); if (error != null) throw new InvalidOperationException(error); }
        }
        internal static Type ResolveType(string id)
        {
            if (!id.StartsWith("Component:", StringComparison.Ordinal)) return null;
            var type = typeof(ComponentBase).Assembly.GetType(id.Substring(10), false);
            return type != null && typeof(ComponentBase).IsAssignableFrom(type) && !typeof(PrefabBase).IsAssignableFrom(type) && !type.IsAbstract && !type.ContainsGenericParameters ? type : null;
        }
        private static string MenuCompatibility(Type type, PrefabBase destination)
        {
            // Public metadata only; no reflection reads/writes of runtime payloads.
            var menus = Attribute.GetCustomAttributes(type, typeof(ComponentMenuAttribute), true).Cast<ComponentMenuAttribute>().ToArray();
            if (menus.Length != 1 || menus[0].hideInEditor) return "Missing/ambiguous or hidden native component menu metadata.";
            var allowed = menus[0].requiredPrefab;
            if (allowed == null) return "Unknown prefab compatibility.";
            if (allowed.Length > 0 && !allowed.Any(t => t != null && t.IsAssignableFrom(destination.GetType()))) return "Incompatible destination prefab.";
            return null;
        }
        internal static string Compatibility(Type type, PrefabBase destination, List<PlannedChange> changes=null){return MenuCompatibility(type,destination)??Requirements(type,destination,new HashSet<Type>(),changes??new List<PlannedChange>());}
        private static string Requirements(Type type, PrefabBase destination, HashSet<Type> path,List<PlannedChange> changes)
        {
            if (!path.Add(type)) return "Cyclic component requirements require manual configuration.";
            foreach (var requirement in Attribute.GetCustomAttributes(type, typeof(ComponentRequirementAttribute), true).Cast<ComponentRequirementAttribute>())
            {
                if (requirement.componentTypes == null) return "Unknown component requirements.";
                foreach (var required in requirement.componentTypes)
                {
                    if (required == null || destination.components == null || (!destination.components.Any(c => c != null && required.IsAssignableFrom(c.GetType()) && c.active) && !changes.Any(c=>required.IsAssignableFrom(c.Type)&&c.After!=null&&c.After.Active)))
                        return "Missing active dependency: " + (required == null ? "unknown" : required.Name) + ". Add/configure it first.";
                    string nested = Requirements(required, destination, path,changes); if (nested != null) return nested;
                }
            }
            path.Remove(type);
            return null;
        }
    }
}


