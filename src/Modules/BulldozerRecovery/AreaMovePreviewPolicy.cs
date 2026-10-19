using System.Linq;
using Game.Tools;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class AreaMovePreviewPolicy {
        internal static bool Matches(Temp[] outputs,Entity target,Entity[] retained) {
            if(target==Entity.Null||outputs==null||outputs.Length==0)return false;
            if(outputs.Count(t=>t.m_Original==target)!=1)return false;
            if(outputs.Select(t=>t.m_Original).Distinct().Count()!=outputs.Length)return false;
            return outputs.All(t=>t.m_Original==target
                ? (t.m_Flags&TempFlags.Delete)==0
                : t.m_Original!=Entity.Null&&t.m_Flags==0&&retained!=null&&retained.Contains(t.m_Original));
        }
    }
}
