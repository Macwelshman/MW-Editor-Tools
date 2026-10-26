using System;
using System.Linq;
using System.Collections.Generic;
using Game.Prefabs;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private static bool IsMarkerMoveGroup(NestedNode[] group)=>group!=null&&group.Length==1&&BikeRackMoveEligibility.Dispatch(group[0].SpawnAccess.Present,IsBikeRackMoveGroup(group));
        private static bool IsBikeRackMoveGroup(NestedNode[] group,bool standalone=false){if(group==null||group.Length!=1)return false;var n=group[0];var s=n.SpawnAccess;return n.Parent==-1&&n.Prefab is StaticObjectPrefab&&(!s.HasMarker||s.HasSpawn)&&BikeRackMovePolicy.Supports(n.Prefab.name,!standalone,group.Length,s.HasSpawn,s.HasTakeoff,s.HasAccess,s.HasRoute,n.Lanes.HasLanes);}
        private static bool SupportedMarkerMoveGroup(NestedNode[] group,bool standalone){
            if(group==null||group.Length!=1)return false;var n=group[0];var s=n.SpawnAccess;
            return IsBikeRackMoveGroup(group,standalone)||n.Parent==-1&&MarkerMovePolicy.Supports(n.Prefab.name,!standalone,group.Length,n.Prefab is MarkerObjectPrefab,s.HasSpawn,s.HasTakeoff,s.HasAccess,s.HasRoute,n.Lanes.HasLanes);
        }
        private bool MarkerSourceMatches(MoveEntry e,NetworkEntry after,NestedNode moved){
            var before=e.BoundBefore;var old=before.Before;var current=after.Before;
            if(!old.Components.SequenceEqual(current.Components)||!old.Active.SequenceEqual(current.Active))return false;
            var ancillary=new[]{"ObjectSubObjects.m_SubObjects","ObjectSubAreas.m_SubAreas","ObjectSubNets.m_SubNets","ObjectSubLanes.m_SubLanes","EffectSource.m_Effects"};
            if(old.Fields.Where(f=>!ancillary.Contains(f.Owner.GetType().Name+"."+f.Field.Name)).Any(f=>!f.Data.ReferencesValid||!f.Data.Matches(f.Field.GetValue(f.Owner))))return false;
            foreach(var field in old.Fields.Where(f=>ancillary.Contains(f.Owner.GetType().Name+"."+f.Field.Name)&&!(f.Owner is ObjectSubObjects))){
                if(field.Owner is ObjectSubLanes&&field.Field.Name=="m_SubLanes"){
                    if(!NetworkDescriptorPolicy.Set((ObjectSubLaneInfo[])field.Data.Materialize(),(ObjectSubLaneInfo[])field.Field.GetValue(field.Owner),NetworkDescriptorPolicy.LaneMatches))return false;
                }else if(!NetworkAncillaryMatches(before,field))return false;
            }
            var rows=NetworkRows<ObjectSubObjectInfo>(old,"ObjectSubObjects.m_SubObjects");var afterRows=NetworkRows<ObjectSubObjectInfo>(current,"ObjectSubObjects.m_SubObjects");
            var root=e.Before[0];var oldIndex=BoundDescriptorIndex.ResolveNative(rows,root.Prefab,root.Local,before.Transform,root.Data.Transform);var newIndex=BoundDescriptorIndex.ResolveNative(afterRows,moved.Prefab,moved.Local,before.Transform,moved.Data.Transform);
            var roots=before.Props.Select(g=>g[0].Original==e.Target?moved:g[0]).ToArray();
            return MarkerMovePolicy.SourceRows(rows,afterRows,oldIndex,newIndex,roots.Select(n=>n.Prefab).ToArray(),roots.Select(n=>n.Local).ToArray(),roots.Select(n=>n.Data.Transform).ToArray(),before.Transform)&&NetworkRowsMatchGraph(current,after.Network);
        }
        private AreaCompanionNetwork.Anchor[] MarkerLayoutPeers(NetworkEntry entry,NestedNode target){
            if(!target.SpawnAccess.HasSpawn)return null;
            var em=world.EntityManager;var prefab=world.GetExistingSystemManaged<PrefabSystem>().GetEntity(target.Prefab);
            if(!em.HasComponent<SpawnLocationData>(prefab))return null;var profile=em.GetComponentData<SpawnLocationData>(prefab);
            if(profile.m_ActivityMask.m_Mask!=0||!(profile.m_ConnectionType==RouteConnectionType.Pedestrian||profile.m_ConnectionType==RouteConnectionType.Cargo||profile.m_ConnectionType==RouteConnectionType.Road&&profile.m_RoadTypes!=Game.Net.RoadTypes.None))return null;
            var nodes=entry.Props.SelectMany(g=>g).Where(n=>n.SpawnAccess.HasSpawn&&n.SpawnAccess.Spawn.m_GroupIndex==target.SpawnAccess.Spawn.m_GroupIndex).ToArray();
            // Heterogeneous/nested groups retain the original guarded chain route.
            if(nodes.Any(n=>n.Parent!=-1||n.Owner!=entry.Owner||!ReferenceEquals(n.Prefab,target.Prefab)))return null;
            return nodes.Select(n=>new AreaCompanionNetwork.Anchor{Id=n.Original,Owner=n.Owner,Prefab=n.Prefab,Transform=n.Data.Transform,HasLocal=true,Local=AreaCompanionEffect.DetachedLocal(n.Local)}).ToArray();
        }
        private bool CaptureMarkerMoveAfter(MoveEntry e,NetworkEntry after,NestedNode[] moved){
            var before=e.BoundBefore;var old=e.Before[0];var live=moved[0];
            if(!e.Bound||!SupportedMarkerMoveGroup(moved,false)||old.Original!=live.Original||old.Owner!=live.Owner||old.Local.m_Probability!=live.Local.m_Probability||old.Local.m_GroupIndex!=live.Local.m_GroupIndex||
                !ReferenceEquals(before.Asset,after.Asset)||!before.Transform.Equals(after.Transform)||!MarkerSourceMatches(e,after,live))throw new NotSupportedException("Marker move changed its source, ownership or settings beyond placement.");
            var bike=IsBikeRackMoveGroup(e.Before);string reason;MarkerMoveNetwork.Proof proof;
            var oldPeers=bike?null:MarkerLayoutPeers(before,old);var newPeers=bike?null:MarkerLayoutPeers(after,live);
            if(bike){
                var em=world.EntityManager;var prefab=world.GetExistingSystemManaged<PrefabSystem>().GetEntity(old.Prefab);
                if(old.SpawnAccess.HasSpawn){
                    if(!em.HasComponent<SpawnLocationData>(prefab))throw new NotSupportedException("Bike rack spawn definition is missing.");
                    var profile=em.GetComponentData<SpawnLocationData>(prefab);
                    if(profile.m_ConnectionType!=RouteConnectionType.Parking||profile.m_RoadTypes!=Game.Net.RoadTypes.Bicycle||profile.m_ActivityMask.m_Mask!=0)throw new NotSupportedException("Bike rack spawn profile differs.");
                }else if(old.SpawnAccess.Present||live.SpawnAccess.Present||em.HasComponent<SpawnLocationData>(prefab)||!old.Lanes.HasLanes||!live.Lanes.HasLanes||(old.Prefab.GetComponent<ObjectSubLanes>()?.m_SubLanes?.Length??0)==0)throw new NotSupportedException("Bike rack lane-only definition or companion profile differs.");
                if(old.Lanes.HasBuffer!=live.Lanes.HasBuffer)throw new NotSupportedException("Bike rack owned-lane buffer schema differs.");
                proof=BikeRackMovePolicy.Validate(before.Network,after.Network,old.Lanes.Graph,live.Lanes.Graph,e.Target,old.Data.Transform,live.Data.Transform,before.Transform,out reason);
                if(!CompanionSourcesValid(old.Lanes.Sources(),live.Lanes.Sources()))throw new NotSupportedException("Bike rack has owned lane entities outside the captured after graph.");
            }else proof=MarkerMoveNetwork.Validate(before.Network,after.Network,e.Target,out reason,before.Transform,oldPeers,newPeers);
            if(!bike&&proof==null&&oldPeers!=null&&newPeers!=null){
                // Preserve the accepted virtual-deletion chain proof even when a
                // complete directional layout cannot be proved (for example ties).
                proof=MarkerMoveNetwork.Validate(before.Network,after.Network,e.Target,out var chainReason,before.Transform);
                if(proof==null)reason+="; chain proof: "+chainReason;
            }
            if(proof==null)throw new NotSupportedException(reason);
            if(!CompanionSourcesValid(before.Network.Items.Select(i=>i.Id),after.Network.Items.Select(i=>i.Id)))throw new NotSupportedException("Marker move has original network entities outside the regenerated graph.");
            // Network changes were proved above. All other companion images retain
            // the existing guarded comparison, including complete source witnesses.
            if(!BoundCompanionsMap(before.CompanionMap.WithNetwork(after.Network),after.CompanionMap,before.Owner,out var exact,after.Asset,true))throw new NotSupportedException("Marker move changed area/effect companions or referenced prefabs.");
            var map=EntityReferenceMap.WithOwnerPrefab(EntityReferenceMap.Copy(proof.References),before.Owner,before.OwnerPrefab,after.OwnerPrefab);
            if(!MapAreaLanes(before.CompanionMap,after.CompanionMap,map,true))throw new NotSupportedException("Marker move changed generated area lanes.");
            var oldOther=before.Props.Where(g=>g[0].Original!=e.Target).ToArray();var newOther=after.Props.Where(g=>g[0].Original!=e.Target).ToArray();var matched=MatchForest(oldOther,newOther);
            if(matched==null||!AddForestMap(oldOther,matched,map)||!CompanionSourcesValid(oldOther.SelectMany(GroupSources),newOther.SelectMany(GroupSources)))throw new NotSupportedException("Marker move changed retained objects or their reference mapping.");
            var allowed=new HashSet<Entity>(after.Network.Items.Where(i=>i.HasLane).Select(i=>i.Id).Concat(after.CompanionMap.AreaLanes.Values.SelectMany(g=>g.Items).Where(i=>i.HasLane).Select(i=>i.Id)));
            if(bike&&live.Lanes.Graph!=null)allowed.UnionWith(live.Lanes.Graph.Items.Where(i=>i.HasLane).Select(i=>i.Id));
            var rows=after.Asset.GetComponent<ObjectSubObjects>()?.m_SubObjects;
            for(int g=0;g<oldOther.Length;g++){
                var expected=oldOther[g].Select((node,n)=>{var current=matched[g][n];if(!node.SpawnAccess.NativeConnectionsMatch(current.SpawnAccess,proof.ChangedLanes,allowed,map))throw new NotSupportedException("Marker move changed retained spawn/access data outside its connection closure.");var copy=node.DetachedCopy();copy.SpawnAccess=current.SpawnAccess.DetachedCopy();return copy;}).ToArray();
                int index=BoundDescriptorIndex.ResolveNative(rows,expected[0].Prefab,expected[0].Local,before.Transform,expected[0].Data.Transform);
                if(!NestedGroupAtIndex(expected,matched[g],index,exact))throw new NotSupportedException("Marker move changed retained prop values or attachment data.");
            }
            var movedLinks=new HashSet<Entity>(proof.ChangedLanes.Concat(old.SpawnAccess.References.Where(r=>r.HasLane).Select(r=>r.Id)));
            movedLinks.Add(Entity.Null); // The moved marker alone may gain its first proved connection.
            if(!old.SpawnAccess.NativeConnectionsMatch(live.SpawnAccess,movedLinks,allowed,map))throw new NotSupportedException("Moved marker connections changed outside the captured owner graph or companion settings changed.");
            if(bike){
                var comparison=old.DetachedCopy();comparison.Local=live.Local;comparison.Lanes=live.Lanes;comparison.SpawnAccess=live.SpawnAccess.DetachedCopy();comparison.Effects=live.Effects;
                comparison.Data=new MainPropData(live.Data.Transform,old.Data.Seed,old.Data.Surface,old.Data.Colours,old.Data.CustomColours);
                int index=BoundDescriptorIndex.ResolveNative(rows,live.Prefab,live.Local,before.Transform,live.Data.Transform);
                if(!NestedGroupAtIndex(new[]{comparison},moved,index))throw new NotSupportedException("Bike rack move changed prop values beyond placement.");
            }
            after.MarkerMove=true;return true;
        }
    }
}

