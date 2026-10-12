using System.Linq;
using Game.Areas;
using Game.Tools;
using Unity.Mathematics;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class AreaGeometry {
        internal static bool Matches(Node[] a, LocalNodeCache[] al, Node[] b, LocalNodeCache[] bl) {
            if(a==null||al==null||b==null||bl==null||a.Length!=b.Length||al.Length!=bl.Length||a.Length!=al.Length)return false;
            return a.Select((n,i)=>math.all(math.isfinite(n.m_Position))&&math.all(math.isfinite(b[i].m_Position))&&
                math.isfinite(n.m_Elevation)&&math.isfinite(b[i].m_Elevation)&&
                math.lengthsq(n.m_Position-b[i].m_Position)<1e-8f&&math.abs(n.m_Elevation-b[i].m_Elevation)<1e-4f).All(v=>v)&&
                al.Select((n,i)=>math.all(math.isfinite(n.m_Position))&&math.all(math.isfinite(bl[i].m_Position))&&
                n.m_ParentMesh==bl[i].m_ParentMesh&&math.lengthsq(n.m_Position-bl[i].m_Position)<1e-8f).All(v=>v);
        }
    }
}
