using System;
using System.Linq;
using Game.Prefabs;
using Unity.Mathematics;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class NetworkDescriptorPolicy {
        internal static bool LaneMatches(ObjectSubLaneInfo expected,ObjectSubLaneInfo actual) {
            // Native ObjectSubLaneInfo has exactly these four public fields.
            // Complete live curves and path topology remain mandatory authority.
            return expected!=null&&actual!=null&&expected.GetType()==typeof(ObjectSubLaneInfo)&&actual.GetType()==typeof(ObjectSubLaneInfo)&&
                !ReferenceEquals(expected.m_LanePrefab,null)&&ReferenceEquals(expected.m_LanePrefab,actual.m_LanePrefab)&&
                expected.m_ParentMesh.Equals(actual.m_ParentMesh)&&Finite(expected.m_BezierCurve)&&Finite(actual.m_BezierCurve);
        }
        internal static bool Set<T>(T[] expected,T[] actual,Func<T,T,bool> equals) {
            if(expected==null||actual==null||expected.Length!=actual.Length)return false;
            var used=new bool[actual.Length];
            foreach(var row in expected){int found=Enumerable.Range(0,actual.Length).Where(i=>!used[i]&&equals(row,actual[i])).DefaultIfEmpty(-1).First();if(found<0)return false;used[found]=true;}
            return true;
        }
        internal static bool Removed<T>(T[] expected,T[] actual,Func<T,bool> target,Func<T,T,bool> equals) {
            if(expected==null||actual==null||expected.Length!=actual.Length+1)return false;
            return Enumerable.Range(0,expected.Length).Any(index=>target(expected[index])&&Set(expected.Where((row,i)=>i!=index).ToArray(),actual,equals));
        }
        private static bool Finite(Colossal.Mathematics.Bezier4x3 curve)=>math.all(math.isfinite(curve.a))&&math.all(math.isfinite(curve.b))&&math.all(math.isfinite(curve.c))&&math.all(math.isfinite(curve.d));
    }
}
