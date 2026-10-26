using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class SpawnAccessImage {
        internal SpawnAccessImage DetachedCopy(){var copy=(SpawnAccessImage)MemberwiseClone();copy.RegistrationTypes=RegistrationTypes.ToArray();copy.References=References.Select(r=>r.DetachedCopy()).ToArray();return copy;}
        // Validate in the original reference namespace, then compare in the live
        // namespace. Reused lane IDs must never be remapped a second time.
        internal bool NativeConnectionsMatch(SpawnAccessImage after,HashSet<Entity> removed,HashSet<Entity> allowed,Dictionary<Entity,Entity> map){
            if(after==null)return false;var copy=(SpawnAccessImage)MemberwiseClone();var replacements=new HashSet<Entity>();
            bool Link(Entity original,float position,Entity target,float targetPosition,out Entity lane,out float value){
                lane=EntityReferenceMap.Resolve(original,map);value=MappedPosition(original,position,map);
                if(original==Entity.Null&&target==Entity.Null&&position.Equals(targetPosition))return true;
                if(!removed.Contains(original)){if(lane!=target||!PositionEqual(value,targetPosition,EntityReferenceMap.IsReversed(map,original)))return false;value=targetPosition;return true;}
                if((target!=Entity.Null&&!allowed.Contains(target))||!Finite(targetPosition)||targetPosition<0||targetPosition>1)return false;
                lane=target;value=targetPosition;if(target!=Entity.Null)replacements.Add(target);return true;
            }
            if(HasSpawn){
                copy.Spawn.m_AccessRestriction=EntityReferenceMap.Resolve(Spawn.m_AccessRestriction,map);
                if(!Link(Spawn.m_ConnectedLane1,Spawn.m_CurvePosition1,after.Spawn.m_ConnectedLane1,after.Spawn.m_CurvePosition1,out copy.Spawn.m_ConnectedLane1,out copy.Spawn.m_CurvePosition1)||
                   !Link(Spawn.m_ConnectedLane2,Spawn.m_CurvePosition2,after.Spawn.m_ConnectedLane2,after.Spawn.m_CurvePosition2,out copy.Spawn.m_ConnectedLane2,out copy.Spawn.m_CurvePosition2))return false;
            }
            if(HasTakeoff)copy.Takeoff.m_AccessRestriction=EntityReferenceMap.Resolve(Takeoff.m_AccessRestriction,map);
            if(HasAccess&&!Link(Access.m_Lane,Access.m_CurvePos,after.Access.m_Lane,after.Access.m_CurvePos,out copy.Access.m_Lane,out copy.Access.m_CurvePos))return false;
            if(HasRoute&&(!Link(Route.m_StartLane,Route.m_StartCurvePos,after.Route.m_StartLane,after.Route.m_StartCurvePos,out copy.Route.m_StartLane,out copy.Route.m_StartCurvePos)||
                          !Link(Route.m_EndLane,Route.m_EndCurvePos,after.Route.m_EndLane,after.Route.m_EndCurvePos,out copy.Route.m_EndLane,out copy.Route.m_EndCurvePos)))return false;
            var unchanged=References.Where(r=>!removed.Contains(r.Id)).ToArray();
            foreach(var reference in unchanged){var matches=after.References.Where(r=>r.Id==EntityReferenceMap.Resolve(reference.Id,map)).ToArray();if(matches.Length!=1||!reference.Matches(matches[0],map))return false;}
            if(after.References.Any(r=>!unchanged.Any(e=>EntityReferenceMap.Resolve(e.Id,map)==r.Id)&&(!replacements.Contains(r.Id)||!r.HasLane||!allowed.Contains(r.Id))))return false;
            copy.References=after.References;
            return copy.Matches(after);
        }
    }
}
