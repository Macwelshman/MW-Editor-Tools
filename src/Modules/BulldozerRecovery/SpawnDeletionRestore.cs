using System;
using System.Linq;
using System.Collections.Generic;
using Game.Prefabs;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private bool CompleteSpawnDeletion(NetworkEntry entry)=>entry.BoundRecord!=null&&records.Undo(record=>ReferenceEquals(record,entry.BoundRecord));
        private bool TryRestoreSpawnDeletion(Record record,bool undo){
            if(!undo||record.Area||record.BoundBefore==null||record.BoundAfter==null||record.ForestBefore==null||record.Nested==null||!record.Nested.Any(n=>n.SpawnAccess.Present))return false;
            if(!OwnerMatches(record)||!record.After.Matches(record.Asset)||!BoundRecordCompanionsMatch(record,false))
                throw new NotSupportedException("Spawn deletion recovery after-state or owner changed before dispatch.");
            var expected=record.BoundBefore;var refs=new Dictionary<PrefabBase,Payload>();
            foreach(var pair in expected.References.Concat(record.NestedReferences)){
                if(!NestedReferenceMatches(pair.Key,pair.Value))throw new NotSupportedException("Spawn deletion recovery referenced prefab changed.");
                if(!refs.ContainsKey(pair.Key))refs.Add(pair.Key,pair.Value);
            }
            var entry=new NetworkEntry{BoundRecord=record,Asset=record.Asset,Owner=record.Owner,OwnerPrefab=expected.OwnerPrefab,
                Target=record.Nested[0].Original,TargetPrefab=record.Nested[0].Prefab,Before=record.Before,After=record.After,
                Network=expected.Network,AfterNetwork=record.BoundAfter.Network,Props=record.ForestBefore,AfterProps=record.ForestAfter,
                Areas=expected.Areas,Effects=expected.Effects,Transform=expected.Transform,CompanionMap=expected,References=refs};
            StartBoundRestoration(entry);return true;
        }
    }
}
