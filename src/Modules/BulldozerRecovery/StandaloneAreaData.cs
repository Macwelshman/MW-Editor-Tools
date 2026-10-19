using System;
using System.Linq;
using Game.Areas;
using Game.Tools;
using Unity.Mathematics;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed class StandaloneAreaData {
        private readonly Node[] nodes;
        private readonly LocalNodeCache[] local;
        internal readonly AreaFlags Flags;
        internal Node[] Nodes=>nodes.ToArray();
        internal LocalNodeCache[] Local=>local?.ToArray();
        internal StandaloneAreaData(AreaFlags flags,Node[] values,LocalNodeCache[] cache) {
            if(values==null||values.Length<3||values.Length>4096||values.Any(n=>!math.all(math.isfinite(n.m_Position))||!math.isfinite(n.m_Elevation))||
                (cache!=null&&(cache.Length!=values.Length||cache.Any(n=>!math.all(math.isfinite(n.m_Position))))))throw new NotSupportedException("Unsupported standalone area geometry.");
            Flags=flags;nodes=values.ToArray();local=cache?.ToArray();
        }
        internal bool Matches(StandaloneAreaData other)=>other!=null&&Flags==other.Flags&&
            ((local==null)==(other.local==null))&&AreaGeometry.Matches(nodes,ComparableLocal(),other.nodes,other.ComparableLocal());
        private LocalNodeCache[] ComparableLocal()=>local??nodes.Select(n=>new LocalNodeCache{m_Position=n.m_Position,m_ParentMesh=-1}).ToArray();
    }
}
