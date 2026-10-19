using System;
using System.Linq;
using System.Reflection;
using Game.Prefabs;

namespace MW.EditorTools.Modules.ComponentManager.Transfer {
    internal sealed class ComponentSettingsImage {
        private readonly FieldInfo[] fields;
        private readonly SettingsValue[] values;
        internal readonly Type Type;
        internal string Fingerprint => string.Join(";",fields.Select((f,i)=>f.DeclaringType.FullName+"."+f.Name+":"+values[i].Fingerprint));
        private ComponentSettingsImage(ComponentBase component) {
            Type=component.GetType();
            var list=new System.Collections.Generic.List<FieldInfo>();
            for(var type=Type;type!=typeof(ComponentBase);type=type.BaseType){
                if(type==null||typeof(PrefabBase).IsAssignableFrom(type))throw new NotSupportedException("Only attached component settings can be copied.");
                foreach(var field in type.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly)){
                    if(field.IsNotSerialized)continue;
                    if(!field.IsPublic){if(field.GetCustomAttributes(false).Any(a=>a.GetType().Name=="SerializeField"||a.GetType().Name=="SerializeReference"))throw new NotSupportedException("Private serialized field: "+field.Name);continue;}
                    if(field.IsInitOnly)throw new NotSupportedException("Read-only setting: "+field.Name);
                    list.Add(field);
                }
            }
            fields=list.OrderBy(f=>f.DeclaringType.FullName+"."+f.Name,StringComparer.Ordinal).ToArray();
            values=fields.Select(f=>SettingsValue.Take(f.GetValue(component))).ToArray();
            if(values.Sum(v=>v.Weight)>100000)throw new NotSupportedException("Component settings exceed snapshot budget.");
            try{foreach(var value in values)if(!value.Matches(value.Materialize()))throw new NotSupportedException("Settings reconstruction changes values.");}
            catch(Exception error){throw new NotSupportedException("Settings cannot be reconstructed: "+error.Message,error);}
        }
        internal static ComponentSettingsImage Capture(ComponentBase component)=>new ComponentSettingsImage(component);
        internal void Apply(ComponentBase component){
            if(component.GetType()!=Type||values.Any(v=>!v.ReferencesValid))throw new InvalidOperationException("Settings type or referenced prefab is unavailable.");
            var materialized=values.Select(v=>v.Materialize()).ToArray();
            for(int i=0;i<fields.Length;i++)fields[i].SetValue(component,materialized[i]);
        }
        internal bool ReferencesValid=>values.All(v=>v.ReferencesValid);
    }
}
