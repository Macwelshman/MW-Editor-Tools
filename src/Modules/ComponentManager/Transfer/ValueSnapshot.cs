using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace MW.EditorTools.Modules.ComponentManager.Transfer
{
    internal sealed class ValueSnapshot
    {
        public readonly string AdapterId, SourceName, SourceType;
        public readonly bool Active;
        internal readonly ComponentSettingsImage Settings;
        private readonly Dictionary<string, byte[]> values;
        public readonly ReadOnlyCollection<string> Fields;
        public ValueSnapshot(string adapter, string name, string type, bool active, IDictionary<string, byte[]> fields, ComponentSettingsImage settings=null)
        {
            Settings=settings;
            AdapterId = adapter; SourceName = name; SourceType = type; Active = active;
            values = fields.ToDictionary(p => p.Key, p => (byte[])p.Value.Clone(), StringComparer.Ordinal);
            Fields = Array.AsReadOnly(values.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray());
        }
        public byte[] Get(string field) { return (byte[])values[field].Clone(); }
        public string Fingerprint
        {
            get
            {
                using (var stream = new MemoryStream())
                using (var writer = new BinaryWriter(stream))
                using (var hash = SHA256.Create())
                {
                    writer.Write(AdapterId); writer.Write(Active);writer.Write(Settings?.Fingerprint??"");
                    foreach (var field in Fields) { writer.Write(field); var bytes = values[field]; writer.Write(bytes.Length); writer.Write(bytes); }
                    writer.Flush(); return Convert.ToBase64String(hash.ComputeHash(stream.ToArray()));
                }
            }
        }
        public ValueSnapshot Merge(ValueSnapshot source, ICollection<string> fields)
        {
            if (AdapterId != source.AdapterId) throw new InvalidOperationException("Adapter mismatch.");
            if(source.Settings!=null)return new ValueSnapshot(AdapterId,SourceName,SourceType,Active,new Dictionary<string,byte[]>(),source.Settings);
            var result = values.ToDictionary(p => p.Key, p => p.Value);
            foreach (var field in source.Fields) if (fields == null || fields.Contains(field)) result[field] = source.Get(field);
            return new ValueSnapshot(AdapterId, SourceName, SourceType, Active, result);
        }
    }

    internal sealed class TransferClipboard
    {
        public ValueSnapshot[] Entries { get; private set; } = new ValueSnapshot[0];
        public long Revision { get; private set; }
        public string Summary { get { return Entries.Length == 0 ? "Clipboard empty." : Entries.Length + " component(s) from " + Entries[0].SourceName + "."; } }
        public void Replace(IEnumerable<ValueSnapshot> entries) { Entries = entries.ToArray(); Revision++; }
        public void Clear() { Replace(new ValueSnapshot[0]); }
        public string Copy(Game.Prefabs.PrefabBase source, int[] indices)
        {
            var captures = new List<ValueSnapshot>(); var reports = new List<string>(); var seen = new HashSet<Type>();
            foreach (int index in indices.Distinct())
            {
                if (source.components == null || index < 0 || index >= source.components.Count) throw new InvalidOperationException("Component list changed.");
                var component = source.components[index];
                if (component == null) { reports.Add("Missing component skipped."); continue; }
                var adapter = Adapters.For(component.GetType());
                if (!seen.Add(component.GetType())) throw new InvalidOperationException("Duplicate supported component; clipboard preserved.");
                if (adapter == null)
                {
                    reports.Add(component.GetType().Name + ": skipped — settings copying is unavailable for this component type.");
                }
                else {try {if(!ReferenceEquals(component.prefab,source))throw new InvalidOperationException("Component ownership changed.");captures.Add(adapter.Capture(component, source));}catch(NotSupportedException error){reports.Add(component.GetType().Name+": skipped — "+error.Message);}}
            }
            if (captures.Count != 0) Replace(captures);
            reports.Insert(0, captures.Count == 0 ? "Nothing supported copied; clipboard preserved." : "Copied " + captures.Count + " component(s).");
            return string.Join("\n", reports);
        }
    }
}

