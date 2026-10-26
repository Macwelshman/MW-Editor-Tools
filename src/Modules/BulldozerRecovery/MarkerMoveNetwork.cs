using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Entities;
using Game.Net;
using Game.Pathfind;
using Unity.Mathematics;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class MarkerMoveNetwork {
        internal sealed class Proof {
            internal Dictionary<Entity,Entity> References;
            internal HashSet<Entity> ChangedLanes;
        }
        // A virtual deletion is a proof witness only, never a prefab or ECS write.
        // Removing the moved anchor from both images must leave the same graph.
        private static AreaCompanionNetwork WithoutMarker(AreaCompanionNetwork graph,Entity target,out string reason){
            reason=null;var incident=graph.Items.Where(i=>i.HasLane&&new[]{i.Lane.m_StartNode.GetOwnerIndex(),i.Lane.m_MiddleNode.GetOwnerIndex(),i.Lane.m_EndNode.GetOwnerIndex()}.Contains(target.Index)).ToArray();
            if(incident.Length<1||incident.Length>2||incident.Any(i=>!SpawnConnectionDeletion.Generated(i,graph))){reason="marker move chain topology unsupported; incident="+incident.Length+" target="+target+" lanes="+string.Join(" | ",incident.Select(AreaCompanionNetwork.Describe));return null;}
            var ids=new HashSet<Entity>(incident.Select(i=>i.Id));var result=SpawnConnectionDeletion.Subgraph(graph,ids);if(result==null){reason="marker move has unclosed retained graph";return null;}
            if(incident.Length==2){
                var neighbours=incident.SelectMany(SpawnConnectionDeletion.Endpoints).Where(n=>n!=target.Index).Distinct().ToArray();if(neighbours.Length!=2){reason="marker move has ambiguous neighbours";return null;}
                if(!result.Items.Any(i=>i.HasLane&&SpawnConnectionDeletion.Endpoints(i).OrderBy(n=>n).SequenceEqual(neighbours.OrderBy(n=>n)))){
                    var peers=neighbours.Select(n=>graph.Anchors.SingleOrDefault(a=>a.Id.Index==n)).ToArray();if(peers.Any(a=>a==null)){reason="marker move neighbour anchor is missing";return null;}
                    var bridge=incident[0].DetachedCopy();bridge.Lane=new Lane{m_StartNode=new PathNode(peers[0].Id,(ushort)0),m_MiddleNode=new PathNode(peers[0].Id,(ushort)3),m_EndNode=new PathNode(peers[1].Id,(ushort)0)};
                    bridge.Curve=new Curve{m_Bezier=NetUtils.StraightCurve(peers[0].Transform.m_Position,peers[1].Transform.m_Position),m_Length=math.distance(peers[0].Transform.m_Position,peers[1].Transform.m_Position)};
                    var link=graph.RootLanes.Where(l=>l.m_SubLane==bridge.Id).ToArray();if(link.Length!=1){reason="marker move incident root membership is missing";return null;}
                    result.Items=result.Items.Concat(new[]{bridge}).ToArray();result.RootLanes=result.RootLanes.Concat(link).ToArray();result.Anchors=result.Anchors.Concat(peers.Where(a=>!result.Anchors.Any(b=>b.Id==a.Id))).ToArray();
                }
            }
            if(SpawnConnectionDeletion.TryProject(graph,result,new[]{target},out reason)==null)return null;
            return result;
        }
        private static AreaCompanionNetwork WithPeers(AreaCompanionNetwork graph,AreaCompanionNetwork.Anchor[] peers){
            if(peers==null)return graph;
            foreach(var p in peers){var matches=graph.Anchors.Where(a=>a.Id==p.Id).ToArray();if(matches.Length>1||matches.Any(a=>a.Owner!=p.Owner||!ReferenceEquals(a.Prefab,p.Prefab)||!a.Transform.Equals(p.Transform)||a.HasLocal!=p.HasLocal||!a.Local.Equals(p.Local)))return null;}
            return new AreaCompanionNetwork{Owner=graph.Owner,Items=graph.Items,RootNets=graph.RootNets,RootLanes=graph.RootLanes,Anchors=graph.Anchors.Concat(peers.Where(p=>!graph.Anchors.Any(a=>a.Id==p.Id))).ToArray()};
        }
        internal static Proof Validate(AreaCompanionNetwork before,AreaCompanionNetwork after,Entity target,out string reason,Game.Objects.Transform? markerOwner=null,AreaCompanionNetwork.Anchor[] oldPeers=null,AreaCompanionNetwork.Anchor[] newPeers=null){
            reason=null;if(before==null||after==null||before.Owner!=after.Owner){reason="marker move owner differs";return null;}
            if(markerOwner.HasValue)after=before.MarkerHeightWitness(after,markerOwner.Value);
            before=WithPeers(before,oldPeers);after=WithPeers(after,newPeers);if(before==null||after==null||(oldPeers==null)!=(newPeers==null)){reason="complete spawn group differs from captured path anchors";return null;}
            var b=before.Anchors.Where(a=>a.Id==target).ToArray();var a=after.Anchors.Where(x=>x.Id==target).ToArray();
            if(b.Length==0&&a.Length==0){
                if(!before.TryMap(after,true,out var unchanged)||unchanged.Any(p=>p.Key!=p.Value)){reason="marker move changed a network outside its anchor closure";return null;}
                return new Proof{References=unchanged,ChangedLanes=new HashSet<Entity>()};
            }
            if(b.Length!=1||a.Length!=1||b[0].Owner!=before.Owner||a[0].Owner!=before.Owner||!ReferenceEquals(b[0].Prefab,a[0].Prefab)||b[0].HasLocal!=a[0].HasLocal){reason="marker move anchor identity or prefab differs";return null;}
            var templates=before.Items.Where(i=>SpawnConnectionDeletion.Generated(i,before)&&SpawnConnectionDeletion.Endpoints(i).Contains(target.Index)).ToArray();
            foreach(var incident in after.Items.Where(i=>i.HasLane&&new[]{i.Lane.m_StartNode.GetOwnerIndex(),i.Lane.m_MiddleNode.GetOwnerIndex(),i.Lane.m_EndNode.GetOwnerIndex()}.Contains(target.Index))){
                if(!templates.Any(t=>{var normalized=incident.DetachedCopy();normalized.Curve=t.Curve;return AreaCompanionNetwork.SameTypedData(t,normalized,true,true)&&t.Connection.m_AccessRestriction==incident.Connection.m_AccessRestriction;})){reason="marker move changed incident connection prefab or settings";return null;}
            }
            AreaCompanionNetwork oldCommon,newCommon;
            if(oldPeers!=null){if(!MarkerConnectionLayout.Common(before,after,oldPeers,newPeers,target,out oldCommon,out newCommon,out reason))return null;}
            else{
                oldCommon=WithoutMarker(before,target,out reason);if(oldCommon==null){reason="before "+reason;return null;}
                newCommon=WithoutMarker(after,target,out reason);if(newCommon==null){reason="after "+reason;return null;}
            }
            if(!oldCommon.TryMap(newCommon,true,out var common,true,true,true)){reason="marker move retained topology differs: "+oldCommon.ExplainMismatch(newCommon,true,true,true,true);return null;}
            if(oldCommon.Anchors.Any(x=>common[x.Id]!=x.Id||!x.Transform.Equals(newCommon.Anchors.Single(y=>y.Id==x.Id).Transform))){reason="marker move changed another anchor placement";return null;}
            var map=new Dictionary<Entity,Entity>{{before.Owner,after.Owner}};var changed=new HashSet<Entity>();
            foreach(var anchor in before.Anchors.Where(x=>x.Id!=target)){
                var current=after.Anchors.SingleOrDefault(x=>x.Id==anchor.Id);
                // Some unchanged objects cease to be path anchors after rewiring;
                // the full object forest still has to prove their exact identity/pose.
                if(current!=null&&(!anchor.Transform.Equals(current.Transform)||!ReferenceEquals(anchor.Prefab,current.Prefab)||anchor.Owner!=current.Owner||anchor.HasLocal!=current.HasLocal||(anchor.HasLocal&&!anchor.Local.Equals(current.Local)))){reason="marker move changed a retained object attachment";return null;}
                EntityReferenceMap.Add(map,anchor.Id,anchor.Id);
            }
            foreach(var item in before.Items){
                if(SpawnConnectionDeletion.Generated(item,before)){
                    if(SpawnConnectionDeletion.Endpoints(item).Contains(target.Index)){changed.Add(item.Id);continue;}
                    // Reuse the already guarded full mapping on a one-connection
                    // view, so only truly unchanged physical lanes enter the map.
                    var peers=before.Anchors.Where(x=>SpawnConnectionDeletion.Endpoints(item).Contains(x.Id.Index)).ToArray();
                    var expected=new AreaCompanionNetwork{Owner=before.Owner,Items=new[]{item},Anchors=peers,RootLanes=before.RootLanes.Where(l=>l.m_SubLane==item.Id).ToArray()};
                    var candidates=new List<Tuple<Entity,bool>>();
                    foreach(var live in after.Items.Where(i=>SpawnConnectionDeletion.Generated(i,after))){var view=new AreaCompanionNetwork{Owner=after.Owner,Items=new[]{live},Anchors=after.Anchors.Where(x=>SpawnConnectionDeletion.Endpoints(live).Contains(x.Id.Index)).ToArray(),RootLanes=after.RootLanes.Where(l=>l.m_SubLane==live.Id).ToArray()};if(expected.TryMap(view,true,out var laneMap,true,true,true))candidates.Add(Tuple.Create(live.Id,EntityReferenceMap.IsReversed(laneMap,item.Id)));}
                    if(candidates.Count>1){reason="marker move retained connection is ambiguous";return null;}
                    if(candidates.Count==0){changed.Add(item.Id);continue;}
                    if(!EntityReferenceMap.Add(map,item.Id,candidates[0].Item1)){reason="marker move reference mapping conflicts";return null;}
                    if(candidates[0].Item2)EntityReferenceMap.MarkReversed(map,new[]{item.Id});
                }else{
                    var live=after.Items.SingleOrDefault(i=>i.Id==item.Id);
                    if(live==null||!AreaCompanionNetwork.SameTypedData(item,live,true,true)||!EntityReferenceMap.Add(map,item.Id,item.Id)){reason="marker move changed an editable network companion";return null;}
                }
            }
            return new Proof{References=map,ChangedLanes=changed};
        }
    }
}


