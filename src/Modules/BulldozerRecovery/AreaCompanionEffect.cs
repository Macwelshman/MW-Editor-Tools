using System;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Mathematics;
using Unity.Entities;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed class AreaCompanionEffect {
        internal Entity Original;
        internal PrefabBase Prefab,Container;
        internal bool HasTransform,HasLocal,HasElevation;
        internal float3 Position,Scale;
        internal quaternion Rotation;
        internal float Intensity;
        internal int Group;
        internal LocalTransformCache Local;
        internal Game.Objects.Elevation Elevation;
        internal static LocalTransformCache DetachedLocal(LocalTransformCache value){value.m_PrefabSubIndex=0;return value;}
        private bool Finite=>math.all(math.isfinite(Position))&&math.all(math.isfinite(Rotation.value))&&math.all(math.isfinite(Scale))&&math.isfinite(Intensity)&&
            (!HasLocal||(math.all(math.isfinite(Local.m_Position))&&math.all(math.isfinite(Local.m_Rotation.value))))&&(!HasElevation||math.isfinite(Elevation.m_Elevation));
        internal static bool Same(AreaCompanionEffect a,AreaCompanionEffect b)=>a!=null&&b!=null&&a.Finite&&b.Finite&&
            ReferenceEquals(a.Prefab,b.Prefab)&&ReferenceEquals(a.Container,b.Container)&&a.HasTransform==b.HasTransform&&
            a.Position.Equals(b.Position)&&a.Rotation.Equals(b.Rotation)&&a.Scale.Equals(b.Scale)&&a.Intensity.Equals(b.Intensity)&&a.Group==b.Group&&
            a.HasLocal==b.HasLocal&&(!a.HasLocal||a.Local.Equals(b.Local))&&a.HasElevation==b.HasElevation&&(!a.HasElevation||a.Elevation.Equals(b.Elevation));
        internal static bool Matches(AreaCompanionEffect[] expected,AreaCompanionEffect[] actual) {
            if(expected==null||actual==null||expected.Length!=actual.Length)return false;
            var used=new bool[actual.Length];
            foreach(var item in expected){var matches=Enumerable.Range(0,actual.Length).Where(i=>!used[i]&&Same(item,actual[i])).ToArray();
                if(matches.Length!=1)return false;used[matches[0]]=true;}
            return true;
        }
    }
}
