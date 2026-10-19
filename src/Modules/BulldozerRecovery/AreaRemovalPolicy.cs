using System;
using System.Linq;
using Game.Prefabs;
using MW.EditorTools.History;
using Unity.Mathematics;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class AreaRemovalPolicy {
        // Used only with a captured native target and mandatory stable retained
        // AreaImage checks. SaveInstance may rewrite surface polygon descriptors.
        internal static bool ReferencesAfterRemoval(ObjectSubAreaInfo[] before,ObjectSubAreaInfo[] after,AreaPrefab target) {
            if(before==null||after==null||before.Length!=after.Length+1||ReferenceEquals(target,null))return false;
            if(before.Concat(after).Any(row=>!ValidRow(row)))return false;
            int removed=Array.FindIndex(before,row=>ReferenceEquals(row.m_AreaPrefab,target));
            if(removed<0)return false;
            var remaining=before.Where((row,i)=>i!=removed).Select(row=>row.m_AreaPrefab).ToList();
            foreach(var row in after){int index=remaining.FindIndex(p=>ReferenceEquals(p,row.m_AreaPrefab));if(index<0)return false;remaining.RemoveAt(index);}
            return remaining.Count==0;
        }
        internal static bool ValidRow(ObjectSubAreaInfo row)=>row!=null&&!ReferenceEquals(row.m_AreaPrefab,null)&&row.m_NodePositions!=null&&
            row.m_NodePositions.Length>=3&&row.m_NodePositions.Length<=4096&&row.m_NodePositions.All(p=>math.all(math.isfinite(p)))&&
            (row.m_ParentMeshes==null||row.m_ParentMeshes.Length==row.m_NodePositions.Length);
        private static bool SameDescriptor(ObjectSubAreaInfo a,ObjectSubAreaInfo b)=>a!=null&&b!=null&&ReferenceEquals(a.m_AreaPrefab,b.m_AreaPrefab)&&
            (a.m_ParentMeshes==null?b.m_ParentMeshes==null:b.m_ParentMeshes!=null&&a.m_ParentMeshes.SequenceEqual(b.m_ParentMeshes))&&
            a.m_NodePositions!=null&&b.m_NodePositions!=null&&a.m_NodePositions.Length==b.m_NodePositions.Length&&
            a.m_NodePositions.Select((p,i)=>math.all(math.isfinite(p))&&math.all(math.isfinite(b.m_NodePositions[i]))&&math.lengthsq(p-b.m_NodePositions[i])<1e-8f).All(v=>v);
        // Exactly one descriptor may disappear. All retained geometry and references must match.
        internal static int RemovedIndex(ObjectSubAreaInfo[] before, ObjectSubAreaInfo[] after) {
            if(before==null||after==null||before.Length!=after.Length+1)return -1;
            int result=-1;
            for(int i=0;i<before.Length;i++) {
                var retained=before.Where((value,index)=>index!=i).ToArray();
                if(!DescriptorPermutation.Matches(retained,after,SameDescriptor))continue;
                if(result!=-1)return -1;
                result=i;
            }
            return result;
        }
    }
}
