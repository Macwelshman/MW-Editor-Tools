using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class OwnedConnectionPolicy {
        internal static AreaCompanionNetwork AfterObjectDeletion(AreaCompanionNetwork before,AreaCompanionNetwork after,IEnumerable<Entity> deletedObjects) {
            if(before==null||after==null||before.Owner!=after.Owner||!before.TryMap(before,true,out _))return null;
            if(before.TryMap(after,false,out _))return before;
            var deleted=new HashSet<Entity>(deletedObjects);
            var anchors=before.Anchors.ToDictionary(a=>a.Id);
            var live=new HashSet<Entity>(after.Items.Select(i=>i.Id));
            var removed=new HashSet<Entity>(before.Items.Where(i=>!live.Contains(i.Id)).Select(i=>i.Id));
            if(removed.Count==0||after.Items.Any(i=>!before.Items.Any(b=>b.Id==i.Id)))return null;
            foreach(var item in before.Items.Where(i=>removed.Contains(i.Id))){
                if(!item.HasLane||item.HasNode||item.HasEdge||item.HasLocal||item.HasContainer||item.Owner!=before.Owner||before.RootNets.Contains(item.Id))return null;
                var indices=new[]{item.Lane.m_StartNode.GetOwnerIndex(),item.Lane.m_MiddleNode.GetOwnerIndex(),item.Lane.m_EndNode.GetOwnerIndex()};
                if(!indices.Any(index=>anchors.Values.Any(a=>a.Id.Index==index&&deleted.Contains(a.Id))))return null;
            }
            var kept=before.Items.Where(i=>!removed.Contains(i.Id)).Select(i=>{
                var copy=i.DetachedCopy();copy.Connections=i.Connections.Where(id=>!removed.Contains(id)).ToArray();
                copy.SubNets=i.SubNets.Where(id=>!removed.Contains(id)).ToArray();copy.SubLanes=i.SubLanes.Where(l=>!removed.Contains(l.m_SubLane)).ToArray();return copy;
            }).ToArray();
            var known=new HashSet<int>(kept.Select(i=>i.Id.Index)){before.Owner.Index};var needed=new HashSet<Entity>();
            foreach(var index in kept.Where(i=>i.HasLane).SelectMany(i=>new[]{i.Lane.m_StartNode.GetOwnerIndex(),i.Lane.m_MiddleNode.GetOwnerIndex(),i.Lane.m_EndNode.GetOwnerIndex()}).Where(index=>index!=0&&!known.Contains(index))){
                var matches=before.Anchors.Where(a=>a.Id.Index==index).ToArray();if(matches.Length!=1)return null;
                var anchor=matches[0];var chain=new HashSet<Entity>();
                while(anchor.Id!=before.Owner){if(deleted.Contains(anchor.Id)||!chain.Add(anchor.Id))return null;needed.Add(anchor.Id);
                    if(anchor.Owner==before.Owner)break;if(!anchors.TryGetValue(anchor.Owner,out anchor))return null;}
            }
            var projected=new AreaCompanionNetwork{Owner=before.Owner,Items=kept,Anchors=before.Anchors.Where(a=>needed.Contains(a.Id)).ToArray(),RootNets=before.RootNets,RootLanes=before.RootLanes.Where(l=>!removed.Contains(l.m_SubLane)).ToArray()};
            return projected.TryMap(after,false,out var map)&&map.All(p=>p.Key==p.Value)?projected:null;
        }
        private static bool SameAreaPath(Game.Pathfind.PathNode a,Game.Pathfind.PathNode b,Entity owner,Entity actualOwner){
            if(a.GetOwnerIndex()==0)return a.Equals(b);
            if(a.GetOwnerIndex()!=owner.Index||b.GetOwnerIndex()!=actualOwner.Index)return false;
            a.SetOwner(Entity.Null);b.SetOwner(Entity.Null);return a.Equals(b);
        }
        // Area source/metadata must already prove the target edit. Only generated
        // lanes with unique unchanged indexed owner paths may change derived curves.
        internal static bool MapEditedArea(AreaCompanionNetwork before,AreaCompanionNetwork after,out Dictionary<Entity,Entity> map){
            map=null;if(before==null||after==null)return false;
            if(before.TryMap(after,true,out map))return true;
            if(before.Items.Length!=after.Items.Length||before.RootNets.Length!=0||after.RootNets.Length!=0||before.Anchors.Length!=0||after.Anchors.Length!=0)return false;
            var adjusted=new List<AreaCompanionNetwork.Item>();
            foreach(var item in before.Items){
                if(!item.HasLane||item.HasEdge||item.HasNode||item.HasLocal||item.HasContainer||item.Owner!=before.Owner)return false;
                var matches=after.Items.Where(i=>i.HasLane&&!i.HasEdge&&!i.HasNode&&!i.HasLocal&&!i.HasContainer&&i.Owner==after.Owner&&
                    ReferenceEquals(item.Prefab,i.Prefab)&&ReferenceEquals(item.Container,i.Container)&&item.HasCurve==i.HasCurve&&item.HasSeed==i.HasSeed&&(!item.HasSeed||item.Seed.Equals(i.Seed))&&
                    SameAreaPath(item.Lane.m_StartNode,i.Lane.m_StartNode,before.Owner,after.Owner)&&
                    SameAreaPath(item.Lane.m_MiddleNode,i.Lane.m_MiddleNode,before.Owner,after.Owner)&&
                    SameAreaPath(item.Lane.m_EndNode,i.Lane.m_EndNode,before.Owner,after.Owner)).ToArray();
                if(matches.Length!=1)return false;
                var copy=item.DetachedCopy();copy.Curve=matches[0].Curve;adjusted.Add(copy);
            }
            var proof=new AreaCompanionNetwork{Owner=before.Owner,Items=adjusted.ToArray(),RootNets=before.RootNets,RootLanes=before.RootLanes};
            return proof.TryMap(after,true,out map);
        }
    }
}
