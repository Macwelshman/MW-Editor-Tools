using System;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private void AlignEffectSource(Payload target,NetworkEntry entry) {
            if(entry.EffectEntry==null)return;
            var expected=target.Components.OfType<EffectSource>().SingleOrDefault();var current=entry.Asset.GetComponent<EffectSource>();
            if(ReferenceEquals(expected,null)){if(!ReferenceEquals(current,null))ApplyPrefabsSystem.RemoveComponent<EffectSource>(entry.Asset);return;}
            if(ReferenceEquals(current,null)){current=entry.Asset.AddComponent<EffectSource>();entry.Asset.components.Remove(current);entry.Asset.components.Insert(Math.Min(Array.IndexOf(target.Components,expected),entry.Asset.components.Count),current);
                foreach(var image in new[]{entry.Before,entry.After}){for(int i=0;i<image.Components.Length;i++)if(ReferenceEquals(image.Components[i],expected))image.Components[i]=current;foreach(var f in image.Fields.Where(f=>ReferenceEquals(f.Owner,expected)))f.Owner=current;}}
            else if(!ReferenceEquals(current,expected))throw new InvalidOperationException("Effect source identity changed before restoration.");
        }
    }
}
