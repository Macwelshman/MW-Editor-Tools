using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private BoundCompanions CaptureAreaReferenceView(BoundCompanions expected,Entity owner,bool allowInvalidLocal){
            var actual=CaptureBoundCompanions(owner,true,allowInvalidLocal);
            var entry=networkPending;
            if(!networkRestoring||entry==null||!entry.AreaMove||!ReferenceEquals(expected,entry.CompanionMap))return actual;
            if(!NetworkOwnerValid(entry)||!entry.Before.Matches(entry.Asset)||expected.Areas.Length!=actual.Areas.Length)
                throw new NotSupportedException("Area reference preflight: source, owner or area count changed.");
            var rows=NetworkRows<ObjectSubAreaInfo>(entry.Before,"ObjectSubAreas.m_SubAreas");
            var positions=expected.Areas.Select(a=>AreaMovePolicy.SavedPositions(a.Nodes,a.Local,entry.Transform.m_Position,entry.Transform.m_Rotation)).ToArray();
            if(!AreaMovePolicy.Sources(rows,expected.Areas.Select(a=>a.Prefab as AreaPrefab).ToArray(),expected.Areas.Select(a=>a.Local).ToArray(),positions))
                throw new NotSupportedException("Area reference preflight: target source differs from captured world geometry.");
            var used=new HashSet<Entity>();var normalized=new List<AreaImage>();
            foreach(var old in expected.Areas){
                var saved=AreaMovePolicy.SavedPositions(old.Nodes,old.Local,entry.Transform.m_Position,entry.Transform.m_Rotation);
                var candidates=actual.Areas.Where(a=>!used.Contains(a.Original)&&AreaEqual(old,a.WithLocal(old.Local))&&
                    (AreaEqual(old,a)||AreaMovePolicy.RegeneratedCache(old.Local,a.Local,saved))).ToArray();
                if(candidates.Length!=1)throw new NotSupportedException("Area reference preflight: restored world geometry, attachments or saved-cache projection differs.");
                used.Add(candidates[0].Original);normalized.Add(candidates[0].WithLocal(old.Local));
            }
            // Read-only proof view; live buffers are still untouched. The existing
            // complete cache preflight runs before their restoration.
            actual.Areas=normalized.ToArray();return actual;
        }
        private bool AreaReferenceMap(BoundCompanions expected,Entity owner,Dictionary<Entity,Entity> map,bool native,bool strictLocal){
            var actual=CaptureAreaReferenceView(expected,owner,native);
            if(expected.Areas.Length!=actual.Areas.Length)return false;
            var used=new HashSet<Entity>();
            foreach(var area in expected.Areas){var matches=actual.Areas.Where(a=>!used.Contains(a.Original)&&AreaEqual(area,a)).ToArray();
                if(matches.Length!=1)return false;used.Add(matches[0].Original);}
            return MapAreaLanes(expected,actual,map,false,native,strictLocal);
        }
    }
}
