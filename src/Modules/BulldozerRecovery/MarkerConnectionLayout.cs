using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Game.Net;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    // Homogeneous, directly owned spawn groups only. RoadConnectionSystem chooses
    // a nearest peer in each signed dominant-axis bucket, then unions those pairs.
    internal static class MarkerConnectionLayout {
        internal static string Pair(int a,int b)=>Math.Min(a,b)+":"+Math.Max(a,b);
        internal static HashSet<string> Pairs(AreaCompanionNetwork.Anchor[] peers,out string reason){
            reason=null;if(peers==null||peers.Length<1||peers.Length>128||peers.Select(p=>p.Id).Distinct().Count()!=peers.Length||peers.Any(p=>!math.all(math.isfinite(p.Transform.m_Position)))){reason="invalid or oversized complete spawn group";return null;}
            var pairs=new HashSet<string>();
            foreach(var source in peers){
                var best=new float[6];var selected=new AreaCompanionNetwork.Anchor[6];var tied=new bool[6];for(int n=0;n<6;n++)best[n]=float.MaxValue;
                foreach(var peer in peers.Where(p=>p.Id!=source.Id)){
                    var delta=peer.Transform.m_Position-source.Transform.m_Position;var abs=math.abs(delta);var distance=math.length(delta);
                    if(!math.isfinite(distance)||distance==0){reason="coincident or nonfinite spawn candidates";return null;}
                    int axis=abs.x>=abs.y&&abs.x>=abs.z?0:abs.y>=abs.z?1:2;int bucket=axis*2+(delta[axis]<0?1:0);
                    if(distance<best[bucket]){best[bucket]=distance;selected[bucket]=peer;tied[bucket]=false;}
                    else if(distance==best[bucket])tied[bucket]=true;
                }
                for(int n=0;n<6;n++){if(tied[n]){reason="equidistant directional spawn candidates require native order proof";return null;}if(selected[n]!=null)pairs.Add(Pair(source.Id.Index,selected[n].Id.Index));}
            }
            return pairs;
        }
        private static AreaCompanionNetwork.Item[] Connections(AreaCompanionNetwork graph,AreaCompanionNetwork.Anchor[] peers,out string reason){
            reason=null;var indices=new HashSet<int>(peers.Select(p=>p.Id.Index));var items=graph.Items.Where(i=>i.HasLane&&new[]{i.Lane.m_StartNode.GetOwnerIndex(),i.Lane.m_MiddleNode.GetOwnerIndex(),i.Lane.m_EndNode.GetOwnerIndex()}.Any(indices.Contains)).ToArray();
            if(items.Any(i=>!SpawnConnectionDeletion.Generated(i,graph)||!SpawnConnectionDeletion.Endpoints(i).All(indices.Contains))){reason="spawn group has external or non-generated incident lanes";return null;}
            return items;
        }
        internal static bool Common(AreaCompanionNetwork before,AreaCompanionNetwork after,AreaCompanionNetwork.Anchor[] oldPeers,AreaCompanionNetwork.Anchor[] newPeers,Entity target,out AreaCompanionNetwork oldCommon,out AreaCompanionNetwork newCommon,out string reason){
            oldCommon=newCommon=null;reason=null;
            if(oldPeers.Length!=newPeers.Length||oldPeers.Count(p=>p.Id==target)!=1||oldPeers.Any(p=>p.Owner!=before.Owner||!ReferenceEquals(p.Prefab,oldPeers[0].Prefab))){reason="complete spawn group ownership/profile differs";return false;}
            foreach(var peer in oldPeers){var matches=newPeers.Where(p=>p.Id==peer.Id).ToArray();if(matches.Length!=1){reason="spawn group membership differs";return false;}var live=matches[0];if(live.Owner!=peer.Owner||!ReferenceEquals(live.Prefab,peer.Prefab)||live.HasLocal!=peer.HasLocal||(peer.Id!=target&&(!peer.Transform.Equals(live.Transform)||!peer.Local.Equals(live.Local)))){reason="another spawn peer pose or attachment differs";return false;}}
            var oldPairs=Pairs(oldPeers,out reason);if(oldPairs==null)return false;var newPairs=Pairs(newPeers,out reason);if(newPairs==null)return false;
            var oldItems=Connections(before,oldPeers,out reason);if(oldItems==null)return false;var newItems=Connections(after,newPeers,out reason);if(newItems==null)return false;
            var templates=oldItems;var methods=before.RootLanes.Where(l=>oldItems.Any(i=>i.Id==l.m_SubLane)).Select(l=>l.m_PathMethods).Distinct().ToArray();
            if(oldItems.Length>0&&methods.Length!=1){reason="spawn group root methods differ";return false;}
            bool Image(AreaCompanionNetwork graph,AreaCompanionNetwork.Anchor[] peers,AreaCompanionNetwork.Item[] items,HashSet<string> expected){
                var actual=items.Select(i=>Pair(i.Lane.m_StartNode.GetOwnerIndex(),i.Lane.m_EndNode.GetOwnerIndex())).ToArray();
                if(actual.Distinct().Count()!=actual.Length||!expected.SetEquals(actual))return false;
                foreach(var item in items){var endpoints=SpawnConnectionDeletion.Endpoints(item);var start=peers.Single(p=>p.Id.Index==endpoints[0]);var end=peers.Single(p=>p.Id.Index==endpoints[1]);
                    var straight=item.DetachedCopy();straight.Curve=new Curve{m_Bezier=NetUtils.StraightCurve(start.Transform.m_Position,end.Transform.m_Position),m_Length=math.distance(start.Transform.m_Position,end.Transform.m_Position)};
                    if(!AreaCompanionNetwork.SameTypedData(straight,item,true,true)||!templates.Any(t=>{var copy=item.DetachedCopy();copy.Curve=t.Curve;return AreaCompanionNetwork.SameTypedData(t,copy,true,true)&&t.Connection.m_AccessRestriction==item.Connection.m_AccessRestriction;}))return false;
                    var links=graph.RootLanes.Where(l=>l.m_SubLane==item.Id).ToArray();if(links.Length!=1||methods.Length!=1||links[0].m_PathMethods!=methods[0])return false;
                }
                return true;
            }
            if(!Image(before,oldPeers,oldItems,oldPairs)){reason="before spawn graph differs from native directional layout; expected="+string.Join(",",oldPairs)+" actual="+oldItems.Length;return false;}
            if(!Image(after,newPeers,newItems,newPairs)){reason="after spawn graph geometry/settings or native directional layout differs; expected="+string.Join(",",newPairs)+" actual="+newItems.Length;return false;}
            oldCommon=SpawnConnectionDeletion.Subgraph(before,new HashSet<Entity>(oldItems.Select(i=>i.Id)));newCommon=SpawnConnectionDeletion.Subgraph(after,new HashSet<Entity>(newItems.Select(i=>i.Id)));
            if(oldCommon==null||newCommon==null){reason="spawn group retained graph is unclosed";return false;}return true;
        }
    }
}
