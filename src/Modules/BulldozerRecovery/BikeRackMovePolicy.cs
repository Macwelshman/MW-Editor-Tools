using System;
using System.Linq;
using System.Collections.Generic;
using Game.Objects;
using Game.Pathfind;
using Unity.Entities;
using Unity.Mathematics;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class BikeRackMovePolicy {
        internal static bool Supports(string name,bool bound,int count,bool spawn,bool takeoff,bool access,bool route,bool lanes=false)=>BikeRackMoveEligibility.Supports(name,bound,count,spawn,takeoff,access,route,lanes);
        private static AreaCompanionNetwork TransformGraph(AreaCompanionNetwork graph,Transform before,Transform after,HashSet<Entity> selected=null){
            float3 Point(float3 p)=>math.mul(after.m_Rotation,math.mul(math.inverse(before.m_Rotation),p-before.m_Position))+after.m_Position;
            var items=graph.Items.Select(i=>{if(selected!=null&&!selected.Contains(i.Id))return i;var copy=i.DetachedCopy();
                if(i.HasCurve){var c=i.Curve.m_Bezier;c.a=Point(c.a);c.b=Point(c.b);c.c=Point(c.c);c.d=Point(c.d);copy.Curve.m_Bezier=c;}
                if(i.HasNode){copy.Node.m_Position=Point(i.Node.m_Position);copy.Node.m_Rotation=math.mul(after.m_Rotation,math.mul(math.inverse(before.m_Rotation),i.Node.m_Rotation));}return copy;}).ToArray();
            return new AreaCompanionNetwork{Owner=graph.Owner,Items=items,RootNets=graph.RootNets,RootLanes=graph.RootLanes,Anchors=graph.Anchors};
        }
        private static bool Self(AreaCompanionNetwork.Item i,AreaCompanionNetwork graph,Entity target)=>i.Owner==graph.Owner&&i.HasLane&&i.HasCurve&&i.HasConnection&&i.HasSecondary&&!i.HasLocal&&!i.HasNode&&!i.HasEdge&&!i.HasContainer&&!i.HasSeed&&
            (i.Connection.m_Flags&Game.Net.ConnectionLaneFlags.Inside)!=0&&(i.Connection.m_Flags&Game.Net.ConnectionLaneFlags.Outside)==0&&
            i.Lane.m_StartNode.Equals(new PathNode(target,(ushort)0))&&i.Lane.m_MiddleNode.Equals(new PathNode(target,(ushort)3))&&i.Lane.m_EndNode.Equals(new PathNode(target,(ushort)0))&&
            !graph.RootNets.Contains(i.Id)&&i.Connections.Length==0&&i.SubNets.Length==0&&i.SubLanes.Length==0;
        internal static MarkerMoveNetwork.Proof Validate(AreaCompanionNetwork before,AreaCompanionNetwork after,AreaCompanionNetwork ownedBefore,AreaCompanionNetwork ownedAfter,Entity target,Transform oldPose,Transform newPose,Transform owner,out string reason){
            reason=null;var changed=new HashSet<Entity>();
            if(before==null||after==null||before.Owner!=after.Owner){reason="bike rack root owner differs";return null;}
            var normalized=before.MarkerHeightWitness(after,owner);
            AreaCompanionNetwork.Item[] Incident(AreaCompanionNetwork g)=>g.Items.Where(i=>i.HasLane&&new[]{i.Lane.m_StartNode.GetOwnerIndex(),i.Lane.m_MiddleNode.GetOwnerIndex(),i.Lane.m_EndNode.GetOwnerIndex()}.Contains(target.Index)).ToArray();
            var old=Incident(before);var live=Incident(after);
            if(old.Length!=live.Length||old.Length>1||old.Any(i=>!Self(i,before,target))||live.Any(i=>!Self(i,after,target))){reason="bike rack has unsupported non-self root connection layout; before="+old.Length+" after="+live.Length;return null;}
            var expected=TransformGraph(before,oldPose,newPose,new HashSet<Entity>(old.Select(i=>i.Id)));
            if(old.Length>0){
                if(old.Any(i=>math.distance(i.Curve.m_Bezier.a,oldPose.m_Position)>=.0001f||math.distance(i.Curve.m_Bezier.b,oldPose.m_Position)>=.0001f||math.distance(i.Curve.m_Bezier.c,oldPose.m_Position)>=.0001f||math.distance(i.Curve.m_Bezier.d,oldPose.m_Position)>=.0001f||math.abs(i.Curve.m_Length)>=.0001f)){reason="bike rack self connection is not anchored at the rack";return null;}
                var a=before.Anchors.Where(p=>p.Id==target).ToArray();var b=after.Anchors.Where(p=>p.Id==target).ToArray();
                if(a.Length!=1||b.Length!=1||!a[0].Transform.Equals(oldPose)||!b[0].Transform.Equals(newPose)||a[0].Owner!=b[0].Owner||!ReferenceEquals(a[0].Prefab,b[0].Prefab)){reason="bike rack path anchor differs";return null;}
                expected.Anchors=before.Anchors.Select(p=>p.Id==target?b[0]:p).ToArray();
            }
            if(!expected.TryMap(normalized,true,out var map,true,true)){reason="bike rack root graph differs: "+expected.ExplainMismatch(normalized,true,true,true);return null;}
            foreach(var i in old)changed.Add(i.Id);
            if((ownedBefore==null)!=(ownedAfter==null)){reason="bike rack owned lane schema differs";return null;}
            if(ownedBefore!=null){
                if(ownedBefore.Owner!=target||ownedAfter.Owner!=target||ownedBefore.Anchors.Length!=0||ownedAfter.Anchors.Length!=0||ownedBefore.RootNets.Length!=0||ownedAfter.RootNets.Length!=0||ownedBefore.Items.Any(i=>!i.HasLane||i.HasNode||i.HasEdge||i.Owner!=target)){reason="bike rack owned lanes are not a closed direct lane set";return null;}
                var moved=TransformGraph(ownedBefore,oldPose,newPose);
                if(!moved.TryMap(ownedAfter,true,out var lanes,true,true)){reason="bike rack owned lane geometry/settings/references differ: "+moved.ExplainMismatch(ownedAfter,true,true,true);return null;}
                foreach(var pair in lanes)if(!EntityReferenceMap.Add(map,pair.Key,pair.Value)){reason="bike rack owned lane reference mapping conflicts";return null;}
                foreach(var i in ownedBefore.Items)changed.Add(i.Id);
            }
            return new MarkerMoveNetwork.Proof{References=map,ChangedLanes=changed};
        }
    }
}
