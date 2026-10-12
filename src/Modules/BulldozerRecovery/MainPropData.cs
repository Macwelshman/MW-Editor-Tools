using System;
using System.Linq;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    // Typed ECS value images are separate from prefab reflection snapshots.
    internal sealed class MainPropData {
        internal readonly Game.Objects.Transform Transform;
        internal readonly Game.Common.PseudoRandomSeed Seed;
        internal readonly Game.Objects.Surface Surface;
        private readonly Game.Rendering.MeshColor[] colours;
        private readonly Game.Rendering.CustomMeshColor[] custom;
        internal Game.Rendering.MeshColor[] Colours=>colours?.ToArray();
        internal Game.Rendering.CustomMeshColor[] CustomColours=>custom?.ToArray();
        internal MainPropData(Game.Objects.Transform transform,Game.Common.PseudoRandomSeed seed,Game.Objects.Surface surface,Game.Rendering.MeshColor[] colours,Game.Rendering.CustomMeshColor[] custom) {
            if((colours?.Length??0)>256||(custom?.Length??0)>256)throw new NotSupportedException("Colour buffer exceeds test budget.");
            Transform=transform;Seed=seed;Surface=surface;this.colours=colours?.ToArray();this.custom=custom?.ToArray();
        }
        internal MainPropData Clone()=>new MainPropData(Transform,Seed,Surface,colours,custom);
        internal bool Matches(MainPropData other)=>other!=null&&Transform.Equals(other.Transform)&&Seed.Equals(other.Seed)&&Surface.Equals(other.Surface)&&Same(colours,other.colours)&&Same(custom,other.custom);
        private static bool Same<T>(T[] a,T[] b) where T:struct=>a==null?b==null:b!=null&&a.SequenceEqual(b);
    }
}
