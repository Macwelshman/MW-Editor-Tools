using System;
using System.Linq;
using Game.Prefabs;
using MW.EditorTools.History;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class AreaRemovalPolicy {
        // Exactly one descriptor may disappear. All retained geometry and references must match.
        internal static int RemovedIndex(ObjectSubAreaInfo[] before, ObjectSubAreaInfo[] after) {
            if(before==null||after==null||before.Length!=after.Length+1)return -1;
            int result=-1;
            for(int i=0;i<before.Length;i++) {
                var retained=before.Where((value,index)=>index!=i).ToArray();
                if(!PrefabSnapshot.Take(retained).Matches(after))continue;
                if(result!=-1)return -1;
                result=i;
            }
            return result;
        }
        internal static bool AncillaryOnly(int objects,int nets,int lanes,int effects)=>objects==0&&nets==0&&lanes==0&&effects==0;
    }
}
