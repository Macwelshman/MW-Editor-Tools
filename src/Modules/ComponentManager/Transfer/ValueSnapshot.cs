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
        internal string[] SelectedFields;
        internal ValueSnapshot WithActive(bool active)=>new ValueSnapshot(AdapterId,SourceName,SourceType,active,values,Settings);
        internal string[] AvailableFields => Settings?.Names ?? Fields.ToArray();
        internal ValueSnapshot SelectFields(string[] names) {
            if(names==null||names.Length==0||names.Distinct().Count()!=names.Length||names.Any(n=>!AvailableFields.Contains(n)))throw new InvalidOperationException("Choose valid component fields.");
            var result=new ValueSnapshot(AdapterId,SourceName,SourceType,Active,values,Settings?.Select(names));
            result.SelectedFields=(string[])names.Clone();return result;
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
                    writer.Write(AdapterId); writer.Write(Active);writer.Write(string.Join(",",SelectedFields??new string[0]));writer.Write(Settings?.Fingerprint??"");
                    foreach (var field in Fields) { writer.Write(field); var bytes = values[field]; writer.Write(bytes.Length); writer.Write(bytes); }
                    writer.Flush(); return Convert.ToBase64String(hash.ComputeHash(stream.ToArray()));
                }
            }
        }
        public ValueSnapshot Merge(ValueSnapshot source, ICollection<string> fields)
        {
            if (AdapterId != source.AdapterId) throw new InvalidOperationException("Adapter mismatch.");
            if(source.Settings!=null)return new ValueSnapshot(AdapterId,SourceName,SourceType,Active,new Dictionary<string,byte[]>(),Settings.Merge(source.Settings));
            var result = values.ToDictionary(p => p.Key, p => p.Value);
            foreach (var field in source.Fields) if ((fields ?? source.SelectedFields) == null || (fields ?? source.SelectedFields).Contains(field)) result[field] = source.Get(field);
            return new ValueSnapshot(AdapterId, SourceName, SourceType, Active, result);
        }
    }

    internal sealed class TransferClipboard
    {
        public ValueSnapshot[] Entries { get; private set; } = new ValueSnapshot[0];
        public long Revision { get; private set; }
        public string Summary { get { return Entries.Length == 0 ? "Clipboard empty." : Entries.Length + (Entries.Length == 1 ? " component from " : " components from ") + Entries[0].SourceName + "."; } }
        public void Replace(IEnumerable<ValueSnapshot> entries) { Entries = entries.ToArray(); Revision++; }
        public void Clear() { Replace(new ValueSnapshot[0]); }
        public string Copy(Game.Prefabs.PrefabBase source, int[] indices, IDictionary<int,string[]> fieldChoices=null)
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
                else {try {if(!ReferenceEquals(component.prefab,source))throw new InvalidOperationException("Component ownership changed.");var image=adapter.Capture(component, source);string[] chosen;
                    if(fieldChoices!=null&&fieldChoices.TryGetValue(index,out chosen)) { if(chosen.Length==0){reports.Add(component.GetType().Name+": no fields selected; skipped.");continue;} }
                    else chosen=image.AvailableFields;
                    if(component.GetType().FullName=="Game.Prefabs.UIObject")chosen=chosen.Where(n=>n!="m_Icon").ToArray();
                    if(chosen.Length==0&&image.AvailableFields.Length==0){captures.Add(image);continue;}
                    if(chosen.Length==0){reports.Add(component.GetType().Name+": no transferable fields; skipped.");continue;}
                    captures.Add(image.SelectFields(chosen));}catch(NotSupportedException error){reports.Add(component.GetType().Name+": skipped — "+error.Message);}}
            }
            if (captures.Count != 0) Replace(captures);
            reports.Insert(0, captures.Count == 0 ? "Nothing supported copied; clipboard preserved." : "Copied " + captures.Count + (captures.Count == 1 ? " component." : " components."));
            return string.Join("\n", reports);
        }
    }
}

