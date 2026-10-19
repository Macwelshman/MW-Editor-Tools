using System.Linq;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class AreaMovePolicy {
        internal static string ShapeDifference(StandaloneAreaData before,StandaloneAreaData after,bool unbound=false) {
            if(before==null||after==null)return "missing geometry image";
            var changes=new System.Collections.Generic.List<string>();
            if(before.Flags!=after.Flags)changes.Add("flags="+before.Flags+" -> "+after.Flags);
            var a=before.Nodes;var b=after.Nodes;var al=before.Local;var bl=after.Local;
            if(a.Length!=b.Length)changes.Add("vertices="+a.Length+" -> "+b.Length);
            if((al==null)!=(bl==null))changes.Add("cache="+(al==null?"absent":al.Length.ToString())+" -> "+(bl==null?"absent":bl.Length.ToString()));
            if(al!=null&&bl!=null){
                if(al.Length!=bl.Length)changes.Add("cache length="+al.Length+" -> "+bl.Length);
                changes.AddRange(Enumerable.Range(0,System.Math.Min(al.Length,bl.Length)).Where(i=>al[i].m_ParentMesh!=bl[i].m_ParentMesh).Take(8).Select(i=>"node["+i+"] parentMesh="+al[i].m_ParentMesh+" -> "+bl[i].m_ParentMesh));
            }
            if(changes.Count==0)changes.Add(Shape(before,after,unbound)?"geometry shape unchanged":"cache positions or attachments unsupported");
            return string.Join("; ",changes);
        }
        // Bound native surface snapping can toggle the observed -1/0 attachment.
        // Full saved-source, ownership and companion checks remain mandatory.
        internal static bool Shape(StandaloneAreaData before,StandaloneAreaData after,bool unbound=false,bool boundSnap=false) {
            if(before==null||after==null||before.Flags!=after.Flags)return false;
            var a=before.Nodes;var b=after.Nodes;var al=before.Local;var bl=after.Local;
            if(a.Length!=b.Length)return false;
            if(al==null)return bl==null||(unbound&&bl.Select((n,i)=>n.m_ParentMesh==-1&&Unity.Mathematics.math.lengthsq(n.m_Position-b[i].m_Position)<1e-8f).All(v=>v));
            return bl!=null&&al.Length==bl.Length&&al.Select((n,i)=>n.m_ParentMesh==bl[i].m_ParentMesh||
                (boundSnap&&!unbound&&(n.m_ParentMesh==-1||n.m_ParentMesh==0)&&(bl[i].m_ParentMesh==-1||bl[i].m_ParentMesh==0))).All(v=>v);
        }
        // ApplyPrefabsSystem saves world nodes in owner space, using the cache
        // only when it is within the native 0.01 distance threshold.
        internal static Unity.Mathematics.float3[] SavedPositions(Game.Areas.Node[] nodes,Game.Tools.LocalNodeCache[] cache,Unity.Mathematics.float3 ownerPosition,Unity.Mathematics.quaternion ownerRotation) {
            if(nodes==null||cache==null||nodes.Length!=cache.Length)return null;
            var inverse=Unity.Mathematics.math.inverse(ownerRotation);
            return nodes.Select((n,i)=>{
                var p=Unity.Mathematics.math.mul(inverse,n.m_Position-ownerPosition);
                return Unity.Mathematics.math.distance(p,cache[i].m_Position)<.01f?cache[i].m_Position:p;
            }).ToArray();
        }
        internal static bool Sources(Game.Prefabs.ObjectSubAreaInfo[] rows,Game.Prefabs.AreaPrefab[] prefabs,Game.Tools.LocalNodeCache[][] local,Unity.Mathematics.float3[][] saved=null) {
            if(rows==null||prefabs==null||local==null||rows.Length!=prefabs.Length||rows.Length!=local.Length||local.Any(l=>l==null))return false;
            if(saved!=null&&(saved.Length!=local.Length||saved.Select((p,i)=>p==null||p.Length!=local[i].Length).Any(v=>v)))return false;
            var used=new bool[rows.Length];
            for(int i=0;i<prefabs.Length;i++){
                var candidates=Enumerable.Range(0,rows.Length).Where(r=>!used[r]&&AreaRemovalPolicy.ValidRow(rows[r])&&ReferenceEquals(rows[r].m_AreaPrefab,prefabs[i])&&rows[r].m_NodePositions.Length==local[i].Length&&
                    local[i].Select((n,j)=>Unity.Mathematics.math.lengthsq((saved==null?n.m_Position:saved[i][j])-rows[r].m_NodePositions[j])<1e-8f&&(rows[r].m_ParentMeshes==null?n.m_ParentMesh==-1:rows[r].m_ParentMeshes[j]==n.m_ParentMesh)).All(v=>v)).ToArray();
                if(candidates.Length!=1)return false;used[candidates[0]]=true;
            }
            return true;
        }
        internal static bool RegeneratedCache(Game.Tools.LocalNodeCache[] before,Game.Tools.LocalNodeCache[] actual,Unity.Mathematics.float3[] saved) {
            return before!=null&&actual!=null&&saved!=null&&before.Length==actual.Length&&before.Length==saved.Length&&
                before.Select((n,i)=>n.m_ParentMesh==actual[i].m_ParentMesh&&Unity.Mathematics.math.all(Unity.Mathematics.math.isfinite(actual[i].m_Position))&&
                    Unity.Mathematics.math.lengthsq(actual[i].m_Position-saved[i])<1e-8f).All(v=>v);
        }
        internal static bool ExpansionShape(Game.Areas.Expand[] before,Game.Areas.Expand[] after) {
            if(before==null||after==null||before.Length!=after.Length)return false;
            for(int i=0;i<before.Length;i++){var value=before[i];if(!Unity.Mathematics.math.all(Unity.Mathematics.math.isfinite(value.m_Offset))||!Unity.Mathematics.math.all(Unity.Mathematics.math.isfinite(after[i].m_Offset)))return false;value.m_Offset=after[i].m_Offset;if(!value.Equals(after[i]))return false;}
            return true;
        }
        internal static bool Commit(bool allowed,bool applied,bool edit,int state)=>allowed&&applied&&edit&&(state==1||state==2);
        internal static bool PreviewActive(bool sameTool,int state,bool recreate,bool inApply)=>sameTool&&(inApply||state!=0||recreate);
    }
}
