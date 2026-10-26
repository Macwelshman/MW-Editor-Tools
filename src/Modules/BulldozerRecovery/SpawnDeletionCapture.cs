using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private SpawnConnectionDeletion.Projection spawnDeletionProjection;
        private string spawnDeletionRefusal;
        private AreaCompanionNetwork spawnDeletionNetworkCheckpoint;
        private bool spawnDeletionProofReported;
        private void ClearSpawnDeletionCapture(){spawnDeletionProjection=null;spawnDeletionRefusal=null;spawnDeletionProofReported=false;spawnDeletionNetworkCheckpoint=null;}
        private AreaCompanionNetwork ProjectSpawnDeletion(AreaCompanionNetwork before,AreaCompanionNetwork after,IEnumerable<Entity> deleted){
            spawnDeletionProjection=null;spawnDeletionRefusal=null;
            var strict=OwnedConnectionPolicy.AfterObjectDeletion(before,after,deleted);if(strict!=null){if(spawnDeletionNetworkCheckpoint!=null)boundCompletion.Reset();spawnDeletionNetworkCheckpoint=null;return strict;}
            var projection=SpawnConnectionDeletion.TryProject(before,after,deleted,out spawnDeletionRefusal);if(projection==null)return null;
            if(!CompanionSourcesValid(before.Items.Select(i=>i.Id),after.Items.Select(i=>i.Id))){spawnDeletionRefusal="spawn closure: an original network entity survives outside the after graph";return null;}
            if(spawnDeletionNetworkCheckpoint==null||!spawnDeletionNetworkCheckpoint.TryMap(projection.Network,true,out var stable)||stable.Any(p=>p.Key!=p.Value))boundCompletion.Reset();
            spawnDeletionNetworkCheckpoint=projection.Network;
            spawnDeletionProjection=projection;
            if(!spawnDeletionProofReported){Mod.Log.Info("Native spawn deletion connection projection verified; removed="+projection.Removed.Count+" retained="+projection.RetainedMap.Count+" before="+before.Items.Length+" after="+after.Items.Length);spawnDeletionProofReported=true;}
            return projection.Network;
        }
        private NestedNode[][] AdaptSpawnDeletionConnections(NestedNode[][] before,NestedNode[][] actual,BoundCompanions current,Dictionary<Entity,Entity> map){
            var allowed=new HashSet<Entity>(current.Network.Items.Where(i=>i.HasLane).Select(i=>i.Id).Concat(current.AreaLanes.Values.SelectMany(g=>g.Items).Where(i=>i.HasLane).Select(i=>i.Id)));
            if(spawnDeletionProjection==null){var kept=new HashSet<Entity>(current.Network.Items.Where(i=>i.HasLane).Select(i=>i.Id));var removed=new HashSet<Entity>(boundBefore.Network.Items.Where(i=>i.HasLane&&!kept.Contains(i.Id)).Select(i=>i.Id));return AdaptConnectionImages(before,actual,removed,allowed,map);}
            var referenceMap=EntityReferenceMap.WithOwnerPrefab(EntityReferenceMap.Copy(spawnDeletionProjection.RetainedMap),current.Owner,boundBefore.OwnerPrefab,current.OwnerPrefab);
            if(!MapAreaLanes(boundBefore,current,referenceMap,false)||!AddForestMap(before,actual,referenceMap))return null;
            var result=new NestedNode[before.Length][];
            for(int g=0;g<before.Length;g++){result[g]=new NestedNode[before[g].Length];for(int n=0;n<before[g].Length;n++){
                var node=before[g][n];var live=actual[g][n];
                if(!node.SpawnAccess.NativeConnectionsMatch(live.SpawnAccess,spawnDeletionProjection.Removed,allowed,referenceMap))return null;
                if(node.SpawnAccess.Matches(live.SpawnAccess,map))result[g][n]=node;
                else{var copy=node.DetachedCopy();copy.SpawnAccess=live.SpawnAccess.DetachedCopy();result[g][n]=copy;}
            }}
            return result;
        }
    }
}
