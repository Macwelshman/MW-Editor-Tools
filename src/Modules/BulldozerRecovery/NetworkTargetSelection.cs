using System;
using System.Linq;
using Unity.Entities;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class NetworkTargetSelection {
        // Native bulldozer paths represent a segment by consecutive endpoint
        // nodes. Two control-point identities need not mean two deletions.
        internal static Entity Resolve(Entity[] points,AreaCompanionNetwork graph) {
            if(points==null||graph==null)return Entity.Null;
            var ids=points.Where(id=>id!=Entity.Null).Distinct().ToArray();
            if(ids.Length==0||ids.Length>3)return Entity.Null;
            var items=ids.Select(id=>graph.Items.SingleOrDefault(i=>i.Id==id)).ToArray();
            if(items.Any(i=>i==null||i.Owner!=graph.Owner))return Entity.Null;
            var edges=items.Where(i=>i.HasEdge&&graph.RootNets.Contains(i.Id)).ToArray();
            if(edges.Length==1&&items.All(i=>i.Id==edges[0].Id||(i.HasNode&&(i.Id==edges[0].Edge.m_Start||i.Id==edges[0].Edge.m_End))))return edges[0].Id;
            if(edges.Length!=0||ids.Length!=2||items.Any(i=>!i.HasNode))return Entity.Null;
            var found=graph.Items.Where(i=>i.Owner==graph.Owner&&i.HasEdge&&graph.RootNets.Contains(i.Id)&&
                ((i.Edge.m_Start==ids[0]&&i.Edge.m_End==ids[1])||(i.Edge.m_Start==ids[1]&&i.Edge.m_End==ids[0]))).ToArray();
            return found.Length==1?found[0].Id:Entity.Null;
        }
    }
}
