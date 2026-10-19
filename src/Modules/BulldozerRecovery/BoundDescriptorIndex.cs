using System;
using Game.Prefabs;
using Game.Tools;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class BoundDescriptorIndex {
        internal static LocalTransformCache ForSource(ObjectSubObjectInfo[] rows,PrefabBase prefab,LocalTransformCache local) {
            local.m_PrefabSubIndex=Resolve(rows,prefab,local);
            return local;
        }
        // Saved assets can have stale cache indices. Resolve detached root snapshots
        // by complete placement data; never write the native cache to prepare Undo.
        internal static int Resolve(ObjectSubObjectInfo[] rows, PrefabBase prefab, LocalTransformCache local) {
            int found=-1;
            if(rows!=null) for(int i=0;i<rows.Length;i++) {
                var row=rows[i];
                if(row==null || !ReferenceEquals(row.m_Object,prefab) || !row.m_Position.Equals(local.m_Position) ||
                    !row.m_Rotation.Equals(local.m_Rotation) || row.m_ParentMesh!=local.m_ParentMesh ||
                    row.m_Probability!=local.m_Probability || row.m_GroupIndex!=local.m_GroupIndex) continue;
                if(found!=-1) throw new NotSupportedException("Prop placement matches multiple descriptors.");
                found=i;
            }
            if(found<0) throw new NotSupportedException("Prop placement does not match an asset descriptor. "+Describe(rows,prefab,local));
            return found;
        }
        private static string Describe(ObjectSubObjectInfo[] rows,PrefabBase prefab,LocalTransformCache local) {
            var message="cache position="+local.m_Position+" rotation="+local.m_Rotation+" parentMesh="+local.m_ParentMesh+" probability="+local.m_Probability+" group="+local.m_GroupIndex+" index="+local.m_PrefabSubIndex;
            int count=0;
            if(rows!=null)for(int i=0;i<rows.Length&&count<4;i++){
                var row=rows[i];if(row==null||!ReferenceEquals(row.m_Object,prefab))continue;count++;
                message+="; row["+i+"] position="+row.m_Position+" rotation="+row.m_Rotation+" parentMesh="+row.m_ParentMesh+" probability="+row.m_Probability+" group="+row.m_GroupIndex;
            }
            return message;
        }
    }
}
