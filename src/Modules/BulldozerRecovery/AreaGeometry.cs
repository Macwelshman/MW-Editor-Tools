using System.Linq;
using Game.Areas;
using Game.Tools;
using Unity.Mathematics;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class AreaGeometry {
        private static string Scalar(float value)=>value.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
        private static string Position(float3 p)=>"("+Scalar(p.x)+","+Scalar(p.y)+","+Scalar(p.z)+")";
        internal static string Explain(Node[] a,LocalNodeCache[] al,Node[] b,LocalNodeCache[] bl,float3[] saved=null) {
            if(a==null||b==null||al==null||bl==null)return "missing node/cache array";
            var differences=new System.Collections.Generic.List<string>();
            differences.Add("nodes="+a.Length+"/"+b.Length+" caches="+al.Length+"/"+bl.Length);
            int limit=System.Math.Min(System.Math.Min(a.Length,b.Length),System.Math.Min(al.Length,bl.Length));
            for(int i=0;i<limit&&differences.Count<5;i++) {
                if(math.lengthsq(a[i].m_Position-b[i].m_Position)<1e-8f&&math.abs(a[i].m_Elevation-b[i].m_Elevation)<1e-4f&&
                    al[i].m_ParentMesh==bl[i].m_ParentMesh&&math.lengthsq(al[i].m_Position-bl[i].m_Position)<1e-8f)continue;
                differences.Add("node["+i+"] position="+Position(a[i].m_Position)+"/"+Position(b[i].m_Position)+
                    " elevation="+Scalar(a[i].m_Elevation)+"/"+Scalar(b[i].m_Elevation)+
                    " cache="+Position(al[i].m_Position)+"/"+Position(bl[i].m_Position)+
                    " mesh="+al[i].m_ParentMesh+"/"+bl[i].m_ParentMesh+
                    (saved!=null&&i<saved.Length?" saved="+Position(saved[i]):""));
            }
            return string.Join("; ",differences);
        }
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
