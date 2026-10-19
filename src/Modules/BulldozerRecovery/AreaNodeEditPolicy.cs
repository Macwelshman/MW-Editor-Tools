using System;
using System.Linq;
using Unity.Mathematics;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class AreaNodeEditPolicy {
        internal static bool Shape(StandaloneAreaData before,StandaloneAreaData after,bool bound) {
            if(before==null||after==null)return false;
            var a=before.Nodes;var b=after.Nodes;
            if(a.Length==b.Length)return AreaMovePolicy.Shape(before,after,!bound,bound);
            if(before.Flags!=after.Flags||Math.Abs(a.Length-b.Length)!=1||a.Length<3||b.Length<3)return false;
            var al=before.Local;var bl=after.Local;
            if(bound&&(al==null||bl==null))return false;
            if(!bound&&((al!=null&&al.Any(n=>n.m_ParentMesh!=-1))||(bl!=null&&bl.Any(n=>n.m_ParentMesh!=-1))))return false;
            if(!bound&&al!=null&&bl==null)return false;
            if(!bound&&al==null&&bl!=null&&(bl.Length!=b.Length||bl.Select((n,i)=>math.lengthsq(n.m_Position-b[i].m_Position)>=1e-8f).Any(v=>v)))return false;
            var longer=a.Length>b.Length?a:b;var shorter=a.Length>b.Length?b:a;
            var ll=a.Length>b.Length?al:bl;var sl=a.Length>b.Length?bl:al;
            if((ll!=null&&ll.Length!=longer.Length)||(sl!=null&&sl.Length!=shorter.Length))return false;
            int matches=0;
            for(int skip=0;skip<longer.Length;skip++) {
                // A newly inserted snapped node uses only the native attachment
                // transition already supported for ordinary moves.
                if(b.Length>a.Length&&ll!=null&&ll[skip].m_ParentMesh!=-1&&ll[skip].m_ParentMesh!=0)continue;
                bool equal=true;
                for(int i=0;i<shorter.Length;i++) {
                    int j=i<skip?i:i+1;
                    if(math.lengthsq(shorter[i].m_Position-longer[j].m_Position)>=1e-8f||math.abs(shorter[i].m_Elevation-longer[j].m_Elevation)>=1e-4f||
                        (sl!=null&&ll!=null&&sl[i].m_ParentMesh!=ll[j].m_ParentMesh)){equal=false;break;}
                    if(!bound&&sl==null&&ll!=null&&math.lengthsq(ll[j].m_Position-longer[j].m_Position)>=1e-8f){equal=false;break;}
                }
                if(equal&&++matches>1)return false;
            }
            return matches==1;
        }
        internal static string Action(StandaloneAreaData before,StandaloneAreaData after)=>after.Nodes.Length>before.Nodes.Length?"node addition":after.Nodes.Length<before.Nodes.Length?"node deletion":"move";
        internal static int AddedIndex(StandaloneAreaData before,StandaloneAreaData after,bool bound) {
            if(after.Nodes.Length!=before.Nodes.Length+1||!Shape(before,after,bound))return -1;
            var a=before.Nodes;var b=after.Nodes;var al=before.Local;var bl=after.Local;
            for(int skip=0;skip<b.Length;skip++)if((bl==null||bl[skip].m_ParentMesh==-1||bl[skip].m_ParentMesh==0)&&a.Select((n,i)=>{
                int j=i<skip?i:i+1;
                return math.lengthsq(n.m_Position-b[j].m_Position)<1e-8f&&math.abs(n.m_Elevation-b[j].m_Elevation)<1e-4f&&(al==null||bl==null||al[i].m_ParentMesh==bl[j].m_ParentMesh);
            }).All(v=>v))return skip;
            return -1;
        }
        internal static bool OnlyAddedNodeMoved(StandaloneAreaData before,StandaloneAreaData after,int index,bool bound) {
            if(!AreaMovePolicy.Shape(before,after,!bound,bound))return false;
            var a=before.Nodes;var b=after.Nodes;var al=before.Local;var bl=after.Local;
            if(index<0||index>=a.Length)return false;
            bool changed=false;
            for(int i=0;i<a.Length;i++) {
                bool same=math.lengthsq(a[i].m_Position-b[i].m_Position)<1e-8f&&math.abs(a[i].m_Elevation-b[i].m_Elevation)<1e-4f&&
                    (al==null||bl==null||al[i].m_ParentMesh==bl[i].m_ParentMesh);
                if(i!=index&&!same)return false;if(i==index&&!same)changed=true;
            }
            return changed;
        }
        internal static bool Expansion(Game.Areas.Expand[] before,Game.Areas.Expand[] after,bool topology) => !topology?AreaMovePolicy.ExpansionShape(before,after):
            before!=null&&after!=null&&before.Length<=4096&&after.Length<=4096&&before.All(e=>math.all(math.isfinite(e.m_Offset)))&&after.All(e=>math.all(math.isfinite(e.m_Offset)));
        internal static bool Commit(bool allowed,bool applied,bool edit,int state,bool removing)=>AreaMovePolicy.Commit(allowed,applied,edit,state)||(allowed&&applied&&edit&&removing&&state==3);
    }
}
