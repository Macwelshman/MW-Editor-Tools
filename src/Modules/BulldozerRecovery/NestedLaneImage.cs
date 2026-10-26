using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    // Native regeneration owns lane creation. Never copy lane entity pointers.
    internal sealed class NestedLaneImage {
        internal bool HasBuffer;
        internal AreaCompanionNetwork Graph;
        internal bool HasLanes=>Graph!=null&&Graph.Items.Length>0;
        internal bool SupportsDefinition(int count)=>count==0||(Graph!=null&&Graph.RootLanes.Length>0);
        internal bool Matches(NestedLaneImage actual)=>actual!=null&&HasBuffer==actual.HasBuffer&&
            (Graph==null?actual.Graph==null:actual.Graph!=null&&Graph.TryMap(actual.Graph,true,out _));
        internal IEnumerable<PrefabBase> References()=>Graph==null?Enumerable.Empty<PrefabBase>():Graph.Items.SelectMany(i=>new[]{i.Prefab,i.Container}).Concat(Graph.Anchors.Select(a=>a.Prefab)).Where(p=>p!=null);
        internal IEnumerable<Entity> Sources()=>Graph==null?Enumerable.Empty<Entity>():Graph.Items.Select(i=>i.Id);
    }
}
