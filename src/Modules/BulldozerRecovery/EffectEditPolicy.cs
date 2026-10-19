using System;
using System.Linq;
using Game.Prefabs;
using Unity.Mathematics;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class EffectEditPolicy {
        internal static bool GroundDetach(AreaCompanionEffect a,AreaCompanionEffect b)=>a!=null&&b!=null&&a.HasLocal&&b.HasLocal&&a.HasElevation&&b.HasElevation&&
            a.Local.m_ParentMesh==0&&b.Local.m_ParentMesh==-1&&a.Elevation.m_Flags==0&&b.Elevation.m_Flags==Game.Objects.ElevationFlags.OnGround&&b.Elevation.m_Elevation==0;
        internal static bool Move(AreaCompanionEffect a,AreaCompanionEffect b,bool bound=false)=>a!=null&&b!=null&&!ReferenceEquals(a.Prefab,null)&&!ReferenceEquals(a.Container,null)&&AreaCompanionEffect.Same(a,a)&&AreaCompanionEffect.Same(b,b)&&ReferenceEquals(a.Prefab,b.Prefab)&&ReferenceEquals(a.Container,b.Container)&&
            a.HasTransform&&b.HasTransform&&a.HasLocal==b.HasLocal&&a.HasElevation==b.HasElevation&&a.Scale.Equals(b.Scale)&&a.Intensity.Equals(b.Intensity)&&a.Group==b.Group&&
            (!a.HasLocal||((a.Local.m_ParentMesh==b.Local.m_ParentMesh||(bound&&GroundDetach(a,b)))&&a.Local.m_Probability==b.Local.m_Probability&&a.Local.m_GroupIndex==b.Local.m_GroupIndex))&&
            (!a.HasElevation||a.Elevation.m_Flags==b.Elevation.m_Flags||(bound&&GroundDetach(a,b)));
        internal static bool Descriptor(EffectSource.EffectSettings row,AreaCompanionEffect e,Game.Objects.Transform owner,bool cached=false) {
            if(row==null||e==null||ReferenceEquals(row.m_Effect,null)||!AreaCompanionEffect.Same(e,e)||!e.HasTransform||!ReferenceEquals(row.m_Effect,e.Container)||!row.m_Scale.Equals(e.Scale)||row.m_Intensity!=e.Intensity||row.m_AnimationIndex!=e.Group)return false;
            var inverse=math.inverse(owner.m_Rotation);var p=math.mul(inverse,e.Position-owner.m_Position);var r=math.mul(inverse,e.Rotation);
            if(e.HasLocal){if(math.distance(p,e.Local.m_Position)<.01f)p=e.Local.m_Position;
                var angle=2f*math.acos(math.clamp(math.abs(math.dot(r.value,e.Local.m_Rotation.value)),0f,1f));if(angle<.017453292f)r=e.Local.m_Rotation;}
            // Only the captured target of a proved bound 0 -> -1 ground move may
            // use the native saved cache before the final ground projection.
            if(cached){if(!e.HasLocal)return false;p=e.Local.m_Position;r=e.Local.m_Rotation;}
            return math.all(math.isfinite(row.m_PositionOffset))&&math.all(math.isfinite(row.m_Rotation.value))&&math.lengthsq(p-row.m_PositionOffset)<1e-8f&&
                math.lengthsq(r.value-row.m_Rotation.value)<1e-8f&&row.m_ParentMesh==(e.HasLocal?e.Local.m_ParentMesh:-1);
        }
        internal static bool Sources(EffectSource.EffectSettings[] rows,AreaCompanionEffect[] effects,Game.Objects.Transform owner,Unity.Entities.Entity cachedTarget=default) {
            if(rows==null||effects==null||rows.Length!=effects.Length)return false;
            var used=new bool[rows.Length];foreach(var effect in effects){var matches=Enumerable.Range(0,rows.Length).Where(i=>!used[i]&&(Descriptor(rows[i],effect,owner)||(cachedTarget!=Unity.Entities.Entity.Null&&effect.Original==cachedTarget&&Descriptor(rows[i],effect,owner,true)))).ToArray();if(matches.Length!=1)return false;used[matches[0]]=true;}return true;
        }
        internal static bool Retained(AreaCompanionEffect[] before,AreaCompanionEffect[] after,Unity.Entities.Entity target)=>AreaCompanionEffect.Matches(before.Where(e=>e.Original!=target).ToArray(),after);
    }
}
