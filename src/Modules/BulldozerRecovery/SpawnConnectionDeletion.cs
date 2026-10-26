using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Game.Net;
using Game.Pathfind;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    // Read-only proof of a native deletion's after graph. The original full graph
    // remains the Undo image; this projection is never written into the asset.
    internal static class SpawnConnectionDeletion {
        internal sealed class Projection {
            internal AreaCompanionNetwork Network;
            internal Dictionary<Entity,Entity> RetainedMap;
            internal HashSet<Entity> Removed;
        }
        internal static bool Generated(AreaCompanionNetwork.Item i,AreaCompanionNetwork g)=>AreaCompanionNetwork.GeneratedConnection(i)&&i.Owner==g.Owner&&!g.RootNets.Contains(i.Id)&&i.Connections.Length==0&&i.SubNets.Length==0&&i.SubLanes.Length==0;
        internal static int[] Endpoints(AreaCompanionNetwork.Item i)=>new[]{i.Lane.m_StartNode.GetOwnerIndex(),i.Lane.m_EndNode.GetOwnerIndex()};
        internal static AreaCompanionNetwork Subgraph(AreaCompanionNetwork source,HashSet<Entity> removed){
            var kept=source.Items.Where(i=>!removed.Contains(i.Id)).Select(i=>{var copy=i.DetachedCopy();copy.Connections=i.Connections.Where(id=>!removed.Contains(id)).ToArray();copy.SubNets=i.SubNets.Where(id=>!removed.Contains(id)).ToArray();copy.SubLanes=i.SubLanes.Where(l=>!removed.Contains(l.m_SubLane)).ToArray();return copy;}).ToArray();
            var known=new HashSet<int>(kept.Select(i=>i.Id.Index)){source.Owner.Index};var needed=new HashSet<Entity>();
            foreach(var index in kept.Where(i=>i.HasLane).SelectMany(i=>new[]{i.Lane.m_StartNode.GetOwnerIndex(),i.Lane.m_MiddleNode.GetOwnerIndex(),i.Lane.m_EndNode.GetOwnerIndex()}).Where(i=>i!=0&&!known.Contains(i))){
                var matches=source.Anchors.Where(a=>a.Id.Index==index).ToArray();if(matches.Length!=1)return null;
                var anchor=matches[0];var seen=new HashSet<Entity>();while(anchor.Id!=source.Owner){if(!seen.Add(anchor.Id))return null;needed.Add(anchor.Id);if(anchor.Owner==source.Owner)break;anchor=source.Anchors.SingleOrDefault(a=>a.Id==anchor.Owner);if(anchor==null)return null;}
            }
            return new AreaCompanionNetwork{Owner=source.Owner,Items=kept,Anchors=source.Anchors.Where(a=>needed.Contains(a.Id)).ToArray(),RootNets=source.RootNets.Where(id=>!removed.Contains(id)).ToArray(),RootLanes=source.RootLanes.Where(l=>!removed.Contains(l.m_SubLane)).ToArray()};
        }
        private static bool SamePair(AreaCompanionNetwork.Item i,int a,int b)=>Endpoints(i).OrderBy(n=>n).SequenceEqual(new[]{a,b}.OrderBy(n=>n));
        private static bool Straight(AreaCompanionNetwork.Item item,AreaCompanionNetwork graph){
            var a=graph.Anchors.SingleOrDefault(x=>x.Id.Index==item.Lane.m_StartNode.GetOwnerIndex());var b=graph.Anchors.SingleOrDefault(x=>x.Id.Index==item.Lane.m_EndNode.GetOwnerIndex());
            if(a==null||b==null)return false;var expected=item.DetachedCopy();expected.Curve=new Curve{m_Bezier=NetUtils.StraightCurve(a.Transform.m_Position,b.Transform.m_Position),m_Length=math.distance(a.Transform.m_Position,b.Transform.m_Position)};
            return AreaCompanionNetwork.SameTypedData(expected,item,true,true);
        }
        internal static Projection TryProject(AreaCompanionNetwork before,AreaCompanionNetwork after,IEnumerable<Entity> deletedObjects,out string reason){
            reason=null;var deleted=deletedObjects.Distinct().ToArray();
            if(before==null||after==null||before.Owner!=after.Owner||deleted.Length!=1||!before.TryMap(before,true,out _)||!after.TryMap(after,true,out _)){reason="spawn closure: owner, single deletion or closed graph proof differs";return null;}
            var target=before.Anchors.SingleOrDefault(a=>a.Id==deleted[0]);if(target==null){reason="spawn closure: deleted object is not a unique original path anchor";return null;}
            var incident=before.Items.Where(i=>i.HasLane&&new[]{i.Lane.m_StartNode.GetOwnerIndex(),i.Lane.m_MiddleNode.GetOwnerIndex(),i.Lane.m_EndNode.GetOwnerIndex()}.Contains(target.Id.Index)).ToArray();
            if(incident.Length==0||incident.Any(i=>!Generated(i,before))){reason="spawn closure: incident lanes are absent or not supported generated inside connections";return null;}
            var removed=new HashSet<Entity>(incident.Select(i=>i.Id));var projected=Subgraph(before,removed);
            if(projected==null||projected.Anchors.Any(a=>a.Id==target.Id)||after.Anchors.Any(a=>a.Id==target.Id)){reason="spawn closure: deleted anchor remains referenced";return null;}
            var extra=after.Items.Length-projected.Items.Length;AreaCompanionNetwork.Item bridge=null;
            if(extra==1){
                var neighbours=incident.SelectMany(Endpoints).Where(i=>i!=target.Id.Index).Distinct().ToArray();
                if(incident.Length!=2||neighbours.Length!=2||projected.Items.Any(i=>i.HasLane&&SamePair(i,neighbours[0],neighbours[1]))){reason="spawn closure: replacement is not a single two-neighbour contraction";return null;}
                var peers=neighbours.Select(n=>before.Anchors.SingleOrDefault(a=>a.Id.Index==n)).ToArray();
                if(peers.Any(a=>a==null||a.Owner!=before.Owner||!ReferenceEquals(a.Prefab,target.Prefab))||target.Owner!=before.Owner||incident.Any(i=>!Straight(i,before))){reason="spawn closure: neighbour prefab/owner or original straight-connection proof differs";return null;}
                var template=incident[0];var other=incident[1];var same=other.DetachedCopy();same.Curve=template.Curve;
                if(!AreaCompanionNetwork.SameTypedData(template,same,true,true)||template.Connection.m_AccessRestriction!=other.Connection.m_AccessRestriction){reason="spawn closure: incident connection settings differ";return null;}
                var candidates=after.Items.Where(i=>Generated(i,after)&&SamePair(i,neighbours[0],neighbours[1])).ToArray();
                if(candidates.Length!=1){reason="spawn closure: replacement neighbour pair is missing or ambiguous";return null;}bridge=candidates[0];
                var expected=template.DetachedCopy();expected.Id=bridge.Id;expected.Lane=new Lane{m_StartNode=new PathNode(peers[0].Id,(ushort)0),m_MiddleNode=new PathNode(peers[0].Id,(ushort)3),m_EndNode=new PathNode(peers[1].Id,(ushort)0)};
                expected.Curve=new Curve{m_Bezier=NetUtils.StraightCurve(peers[0].Transform.m_Position,peers[1].Transform.m_Position),m_Length=math.distance(peers[0].Transform.m_Position,peers[1].Transform.m_Position)};
                var methods=before.RootLanes.Where(l=>removed.Contains(l.m_SubLane)).ToArray();
                if(methods.Length!=2||methods[0].m_PathMethods!=methods[1].m_PathMethods){reason="spawn closure: incident root membership methods differ";return null;}
                var proof=new AreaCompanionNetwork{Owner=before.Owner,Items=new[]{expected},Anchors=peers,RootLanes=new[]{new SubLane{m_SubLane=expected.Id,m_PathMethods=methods[0].m_PathMethods}}};
                var livePeers=after.Anchors.Where(a=>peers.Any(p=>p.Id==a.Id)).ToArray();
                var live=new AreaCompanionNetwork{Owner=after.Owner,Items=new[]{bridge},Anchors=livePeers,RootLanes=after.RootLanes.Where(l=>l.m_SubLane==bridge.Id).ToArray()};
                if(!proof.TryMap(live,true,out _,true,true,true)){reason="spawn closure: replacement geometry, metadata or root membership differs";return null;}
            }else if(extra!=0){reason="spawn closure: unexpected generated connection count before="+before.Items.Length+" retained="+projected.Items.Length+" after="+after.Items.Length;return null;}
            var liveRetained=bridge==null?after:Subgraph(after,new HashSet<Entity>{bridge.Id});
            if(liveRetained==null||!projected.TryMap(liveRetained,false,out var map,true,true,true)){reason="spawn closure: retained graph differs: "+(liveRetained==null?"unclosed graph":projected.ExplainMismatch(liveRetained,false,true,true,true));return null;}
            // Deletion never regenerates retained object placements or editable nets.
            // Only generated connections may change entity allocation/direction.
            if(projected.Anchors.Any(a=>map[a.Id]!=a.Id||!a.Transform.Equals(liveRetained.Anchors.Single(b=>b.Id==a.Id).Transform))||projected.Items.Any(i=>!Generated(i,projected)&&(map[i.Id]!=i.Id||!AreaCompanionNetwork.SameTypedData(i,liveRetained.Items.Single(a=>a.Id==map[i.Id]),false,true)))){reason="spawn closure: an editable companion or retained object identity/data changed";return null;}
            var items=after.Items.Select(i=>{var copy=i.DetachedCopy();var source=projected.Items.SingleOrDefault(e=>map[e.Id]==i.Id);if(source!=null&&source.HasSeed)copy.Seed=source.Seed;return copy;}).ToArray();
            return new Projection{Network=new AreaCompanionNetwork{Owner=after.Owner,Items=items,Anchors=after.Anchors,RootNets=after.RootNets,RootLanes=after.RootLanes},RetainedMap=map,Removed=removed};
        }
    }
}
