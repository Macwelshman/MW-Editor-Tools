using Game.Prefabs;
using MW.EditorTools.History;
using System.Linq;
using Unity.Mathematics;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class AreaNetDescriptorPolicy {
        // Bound deletion capture uses this. Complete live graph equivalence is
        // mandatory before recording: descriptor curves/indices are serialized
        // representations, not the authority for current network topology.
        internal static bool Matches(ObjectSubNetInfo expected,ObjectSubNetInfo actual) {
            if(expected==null||actual==null||!Finite(expected.m_BezierCurve)||!Finite(actual.m_BezierCurve))return false;
            var copy=(ObjectSubNetInfo)PrefabSnapshot.Take(expected).Materialize();
            copy.m_NodeIndex=actual.m_NodeIndex;
            copy.m_BezierCurve=actual.m_BezierCurve;
            if((copy.m_Upgrades?.Length??0)==0&&(actual.m_Upgrades?.Length??0)==0)copy.m_Upgrades=actual.m_Upgrades;
            return PrefabSnapshot.Take(copy).MatchesNativeGeometry(actual);
        }
        private static bool Finite(Colossal.Mathematics.Bezier4x3 curve)=>math.all(math.isfinite(curve.a))&&math.all(math.isfinite(curve.b))&&math.all(math.isfinite(curve.c))&&math.all(math.isfinite(curve.d));
        internal static bool MatchesSet(ObjectSubNetInfo[] expected,ObjectSubNetInfo[] actual) {
            if(expected==null||actual==null||expected.Length!=actual.Length)return false;
            var used=new bool[actual.Length];
            foreach(var row in expected){int found=Enumerable.Range(0,actual.Length).Where(i=>!used[i]&&Matches(row,actual[i])).DefaultIfEmpty(-1).First();if(found<0)return false;used[found]=true;}
            return true;
        }
    }
}
