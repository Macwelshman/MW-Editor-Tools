using System;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private void ApplyIsolatedEffectSource(IsolatedEffectOwner target) {
            var expected=target.Source.Components.OfType<EffectSource>().SingleOrDefault();
            var source=target.Asset.GetComponent<EffectSource>();
            if(ReferenceEquals(expected,null)){
                if(!ReferenceEquals(source,null))ApplyPrefabsSystem.RemoveComponent<EffectSource>(target.Asset);return;
            }
            var fields=target.Source.Fields.Where(f=>ReferenceEquals(f.Owner,expected)).ToArray();
            var values=fields.Select(f=>f.Data.Materialize()).ToArray();
            if(ReferenceEquals(source,null)){
                source=target.Asset.AddComponent<EffectSource>();target.Asset.components.Remove(source);
                target.Asset.components.Insert(Array.IndexOf(target.Source.Components,expected),source);
            }
            // Only EffectSource fields are written. In particular, do not call
            // UpdatePrefab or put Updated/SaveInstance on the parent.
            for(int i=0;i<fields.Length;i++)fields[i].Field.SetValue(source,values[i]);
            source.active=target.Source.Active[Array.IndexOf(target.Source.Components,expected)];
        }
    }
}
