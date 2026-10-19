using System;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private void RestoreAreaMoveCaches(NetworkEntry entry) {
            if(AreasMatch(entry.Areas,entry.Owner))return;
            if(!entry.Before.Matches(entry.Asset)||!NetworkNonAreaCompanionsMatch(entry,true))throw new NotSupportedException("Area cache preflight: owner source or retained props/effects differ.");
            var network=CaptureAreaNetwork(entry.Owner,true);
            if(!NetworkSettled(network)||!entry.Network.TryMap(network,false,out var map,false,true)||
                !CompanionSourcesValid(entry.RestoreFrom.Items.Select(i=>i.Id),network.Items.Select(i=>i.Id))||
                entry.Network.Items.Any(i=>i.HasLocal&&!AreaCompanionNetwork.CanRestoreLocal(i.Local,network.Items.Single(n=>n.Id==map[i.Id]).Local)))
                throw new NotSupportedException("Area cache preflight: owner network topology or typed values differ.");
            var expected=entry.Areas;var actual=CaptureAreas(entry.Owner);
            if(expected.Length!=actual.Length||!CompanionSourcesValid(expected.Select(a=>a.Original),actual.Select(a=>a.Original)))throw new NotSupportedException("Area cache preflight: area count or surviving identities differ.");
            var saved=expected.Select(a=>AreaMovePolicy.SavedPositions(a.Nodes,a.Local,entry.Transform.m_Position,entry.Transform.m_Rotation)).ToArray();
            var rows=NetworkRows<ObjectSubAreaInfo>(entry.Before,"ObjectSubAreas.m_SubAreas");
            if(!AreaMovePolicy.Sources(rows,expected.Select(a=>a.Prefab as AreaPrefab).ToArray(),expected.Select(a=>a.Local).ToArray(),saved))throw new NotSupportedException("Area cache preflight: before-image nodes differ from saved source.");
            var used=new System.Collections.Generic.HashSet<Unity.Entities.Entity>();
            var writes=new System.Collections.Generic.List<Tuple<Unity.Entities.Entity,LocalNodeCache[]>>();
            for(int i=0;i<expected.Length;i++) {
                var old=expected[i];
                var matches=actual.Where(a=>!used.Contains(a.Original)&&AreaEqual(old,a.WithLocal(old.Local))&&
                    (AreaEqual(old,a)||AreaMovePolicy.RegeneratedCache(old.Local,a.Local,saved[i]))).ToArray();
                if(matches.Length!=1)throw new NotSupportedException("Area cache preflight: geometry/metadata, attachment or regenerated cache differs for "+old.Prefab.name+" matches="+matches.Length+" candidates="+
                    string.Join(" | ",actual.Where(a=>ReferenceEquals(a.Prefab,old.Prefab)).Take(3).Select(a=>a.Original+" geometryWithoutCache="+AreaEqual(old,a.WithLocal(old.Local))+" regeneratedCache="+AreaMovePolicy.RegeneratedCache(old.Local,a.Local,saved[i])+" expectedMetadata=["+old.Metadata+"] actualMetadata=["+a.Metadata+"] "+AreaGeometry.Explain(old.Nodes,old.Local,a.Nodes,a.Local,saved[i]))));
                var live=matches[0];used.Add(live.Original);
                if(!AreaMoveSettled(live.Original))throw new NotSupportedException("Area cache preflight: native area update pending.");
                if(!AreaEqual(old,live))writes.Add(Tuple.Create(live.Original,old.Local));
            }
            // The edited area's generated graph must also be closed and mapped
            // before restoring any cache. Seeds/invalid derived curves use the
            // existing final restoration path after this preflight.
            var move=entry.AreaMoveEntry;
            var target=actual.Single(a=>AreaEqual(entry.Areas.Single(e=>e.Original==move.Target),a.WithLocal(move.Before.Data.Local)));
            var targetImage=CaptureStandaloneArea(target.Original,true,true);
            targetImage.Data=new StandaloneAreaData(targetImage.Data.Flags,targetImage.Data.Nodes,move.Before.Data.Local);
            targetImage.Seed=move.Before.Seed;
            if(!StandaloneAreaMatches(move.Before,targetImage))throw new NotSupportedException("Area cache preflight: edited area metadata or expansion differs.");
            var own=CaptureAreaNetwork(target.Original,true);
            if(!NetworkSettled(own)||!move.Before.Network.TryMap(own,false,out var ownMap,false,true)||
                !CompanionSourcesValid(move.After.Network.Items.Select(i=>i.Id),own.Items.Select(i=>i.Id))||
                move.Before.Network.Items.Any(i=>i.HasLocal&&!AreaCompanionNetwork.CanRestoreLocal(i.Local,own.Items.Single(n=>n.Id==ownMap[i.Id]).Local)))
                throw new NotSupportedException("Area cache preflight: edited area generated companions differ.");
            foreach(var write in writes) {
                var buffer=world.EntityManager.GetBuffer<LocalNodeCache>(write.Item1);
                for(int i=0;i<write.Item2.Length;i++)buffer[i]=write.Item2[i];
            }
            if(writes.Count!=0)Mod.Log.Info("Bound area original local caches restored after complete source/geometry/companion preflight: "+writes.Count);
        }
    }
}
