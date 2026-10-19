using System.Collections.Generic;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class PropComponentSchema {
        internal static readonly HashSet<string> Known=new HashSet<string>{
            "Game.Objects.Color","Game.Objects.Decoration","Game.Effects.EnabledEffect","Game.Prefabs.PrefabRef","Game.Rendering.CullingInfo","Game.Objects.Surface","Game.Common.PseudoRandomSeed","Game.Objects.Transform",
            "Game.Rendering.MeshColor","Game.Rendering.MeshBatch","Game.Net.SubNet","Game.Objects.SubObject","Game.Areas.SubArea","Game.Rendering.CustomMeshColor",
            "Game.Tools.Hidden","Game.Objects.Object","Game.Objects.ObjectGeometry","Game.Objects.Static","Unity.Entities.Simulate","Game.Common.BatchesUpdated"
        };
    }
}
