using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private Dictionary<Entity,AreaCompanionNetwork> CaptureAreaLaneImages(AreaImage[] areas,bool allowInvalidLocal=false){
            var em=world.EntityManager;var result=new Dictionary<Entity,AreaCompanionNetwork>();
            foreach(var area in areas)if(em.HasBuffer<Game.Net.SubLane>(area.Original)&&em.GetBuffer<Game.Net.SubLane>(area.Original,true).Length>0)
                result.Add(area.Original,CaptureAreaNetwork(area.Original,allowInvalidLocal,objectAnchors:false));
            return result;
        }
        private bool MapAreaLanes(BoundCompanions expected,BoundCompanions actual,Dictionary<Entity,Entity> map,bool seeds,bool nativeRestoration=false,bool strictLocal=true){
            if(expected.AreaLanes.Count!=actual.AreaLanes.Count)return false;
            foreach(var pair in expected.AreaLanes){
                var source=expected.Areas.Single(a=>a.Original==pair.Key);
                var matches=actual.Areas.Where(a=>AreaEqual(source,a)).ToArray();
                if(matches.Length!=1||!actual.AreaLanes.TryGetValue(matches[0].Original,out var graph)||!pair.Value.TryMap(graph,seeds,out var lanes,strictLocal,nativeRestoration))return false;
                if(nativeRestoration&&!NetworkSettled(graph))return false;
                if(nativeRestoration&&pair.Value.Items.Any(i=>i.HasLocal&&!AreaCompanionNetwork.CanRestoreLocal(i.Local,graph.Items.Single(a=>a.Id==lanes[i.Id]).Local)))return false;
                if(!CompanionSourcesValid(pair.Value.Items.Select(i=>i.Id),graph.Items.Select(i=>i.Id)))return false;
                foreach(var lane in lanes)if(!EntityReferenceMap.Add(map,lane.Key,lane.Value))return false;
            }
            return true;
        }
        private NestedNode[][] AdaptDeletedAreaConnections(NestedNode[][] before,NestedNode[][] actual,BoundCompanions current,Dictionary<Entity,Entity> map){
            if(!areaCapture||!boundBefore.AreaLanes.TryGetValue(deletedArea,out var deleted))return before;
            var removed=new HashSet<Entity>(deleted.Items.Where(i=>i.HasLane).Select(i=>i.Id));
            var retained=new HashSet<Entity>(current.AreaLanes.Values.SelectMany(g=>g.Items).Where(i=>i.HasLane).Select(i=>i.Id));
            return AdaptConnectionImages(before,actual,removed,retained,map);
        }
        private static NestedNode[][] AdaptConnectionImages(NestedNode[][] before,NestedNode[][] actual,HashSet<Entity> removed,HashSet<Entity> retained,Dictionary<Entity,Entity> map){
            return before.Select((group,g)=>group.Select((node,n)=>{
                if(node.SpawnAccess.Matches(actual[g][n].SpawnAccess,map))return node;
                var image=node.SpawnAccess.AfterAreaDeletion(actual[g][n].SpawnAccess,removed,retained,map);
                if(image==null)return node;
                var copy=node.DetachedCopy();copy.SpawnAccess=image;return copy;
            }).ToArray()).ToArray();
        }
    }
}
