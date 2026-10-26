using System;
using System.Linq;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private void AlignSpawnAnchorExpectations(NetworkEntry entry,NestedNode[][] original){
            if((entry.BoundRecord==null&&!entry.MarkerMove)||entry.Network.Anchors.Length==0)return;
            var before=original.SelectMany(g=>g).ToArray();var prepared=entry.Props.SelectMany(g=>g).ToArray();
            var anchors=entry.Network.Anchors.Select(anchor=>{
                var sources=before.Where(n=>n.Original==anchor.Id).ToArray();var targets=prepared.Where(n=>n.Original==anchor.Id).ToArray();
                if(sources.Length!=1||targets.Length!=1)throw new NotSupportedException("Spawn anchor does not uniquely belong to the captured object forest.");
                var source=sources[0];var target=targets[0];
                if(!ReferenceEquals(source.Prefab,anchor.Prefab)||source.Owner!=anchor.Owner||!source.Data.Transform.Equals(anchor.Transform)||!anchor.HasLocal||
                    !AreaCompanionEffect.DetachedLocal(source.Local).Equals(anchor.Local))throw new NotSupportedException("Spawn anchor pose, owner or original attachment differs from its captured object.");
                if(!source.Local.m_Rotation.Equals(target.Local.m_Rotation)&&!BoundDescriptorIndex.NativeRotation(entry.Transform,source.Data.Transform,source.Local).Equals(target.Local.m_Rotation))throw new NotSupportedException("Prepared spawn anchor rotation lacks a native serialization witness.");
                var unchanged=source.Local;unchanged.m_Position=target.Local.m_Position;unchanged.m_Rotation=target.Local.m_Rotation;unchanged.m_PrefabSubIndex=target.Local.m_PrefabSubIndex;
                if(source.Parent!=target.Parent||source.Depth!=target.Depth||source.Owner!=target.Owner||!ReferenceEquals(source.Prefab,target.Prefab)||!source.Data.Transform.Equals(target.Data.Transform)||
                    !unchanged.Equals(target.Local)||(source.Parent!=-1&&!source.Local.Equals(target.Local)))throw new NotSupportedException("Prepared spawn anchor changed world placement, attachments or nested local data.");
                // ForNativeSource already proved the prepared root's saved row.
                // Align only a detached expectation, leaving original snapshots intact.
                var copy=anchor.DetachedCopy();copy.Local=AreaCompanionEffect.DetachedLocal(target.Local);return copy;
            }).ToArray();
            entry.Network=new AreaCompanionNetwork{Owner=entry.Network.Owner,Items=entry.Network.Items,RootNets=entry.Network.RootNets,RootLanes=entry.Network.RootLanes,Anchors=anchors};
        }
    }
}
