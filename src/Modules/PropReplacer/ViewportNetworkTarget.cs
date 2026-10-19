using System;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;
using Unity.Mathematics;
using Colossal.Mathematics;

namespace MW.EditorTools.Modules.PropReplacer {
    public sealed partial class PropReplacerSystem {
        private Row[] ResolveViewportNetworkRows(Row[] rows,PrefabBase prefab,Entity id,Entity owner) {
            var em=World.EntityManager;
            if(!em.HasComponent<Game.Objects.Transform>(owner)||!em.HasBuffer<Game.Net.SubNet>(owner))
                throw new InvalidOperationException("The highlighted network has no unique attached source link.");
            var links=em.GetBuffer<Game.Net.SubNet>(owner,true);int linkCount=0;
            for(int i=0;i<links.Length;i++)if(links[i].m_SubNet==id)linkCount++;
            if(linkCount!=1)throw new InvalidOperationException("The highlighted network has no unique attached source link.");
            var ownerPose=em.GetComponentData<Game.Objects.Transform>(owner);
            Bezier4x3 world;Bezier4x3? cache=null;int2 meshes;bool sameEndpoint;
            if(em.HasComponent<Game.Net.Edge>(id)) {
                if(!em.HasComponent<Game.Net.Curve>(id))throw new NotSupportedException("The live network curve is unavailable.");
                var edge=em.GetComponentData<Game.Net.Edge>(id);
                RequireViewportNetworkNode(edge.m_Start);RequireViewportNetworkNode(edge.m_End);
                world=em.GetComponentData<Game.Net.Curve>(id).m_Bezier;
                if(em.HasComponent<LocalCurveCache>(id))cache=em.GetComponentData<LocalCurveCache>(id).m_Curve;
                meshes=new int2(ViewportParentMesh(edge.m_Start),ViewportParentMesh(edge.m_End));
                sameEndpoint=edge.m_Start==edge.m_End;
            } else if(em.HasComponent<Game.Net.Node>(id)) {
                RequireViewportNetworkNode(id);
                // Connected nodes normally have no independent native source row.
                // Do not redirect one to a coincident exported point descriptor.
                if(em.HasBuffer<Game.Net.ConnectedEdge>(id)) {
                    var connected=em.GetBuffer<Game.Net.ConnectedEdge>(id,true);
                    for(int i=0;i<connected.Length;i++) {
                        var edgeId=connected[i].m_Edge;
                        if(!em.Exists(edgeId)||!em.HasComponent<Game.Net.Edge>(edgeId)||!em.HasComponent<Game.Common.Owner>(edgeId)||em.GetComponentData<Game.Common.Owner>(edgeId).m_Owner!=owner)continue;
                        var edge=em.GetComponentData<Game.Net.Edge>(edgeId);
                        if(edge.m_Start==id||edge.m_End==id)throw new NotSupportedException("Hover the attached network segment rather than its connected junction.");
                    }
                }
                var point=em.GetComponentData<Game.Net.Node>(id).m_Position;
                world=new Bezier4x3{a=point,b=point,c=point,d=point};
                if(em.HasComponent<LocalTransformCache>(id)){var p=em.GetComponentData<LocalTransformCache>(id).m_Position;cache=new Bezier4x3{a=p,b=p,c=p,d=p};}
                meshes=new int2(ViewportParentMesh(id));sameEndpoint=true;
            } else throw new NotSupportedException("Hover an attached native network segment or point container.");
            var curve=ViewportGeometrySource.Curve(world,cache,ownerPose);
            return rows.Where(r=>ViewportGeometrySource.NetworkMatches(r.Value,prefab,curve,meshes,sameEndpoint)).ToArray();
        }
        private void RequireViewportNetworkNode(Entity id) {
            var em=World.EntityManager;
            if(!em.Exists(id)||!em.HasComponent<Game.Net.Node>(id)||em.HasComponent<Temp>(id)||em.HasComponent<Game.Common.Deleted>(id)||em.HasComponent<Game.Common.Created>(id)||em.HasComponent<Game.Common.Updated>(id))
                throw new InvalidOperationException("A network endpoint is unavailable or still updating.");
        }
        private int ViewportParentMesh(Entity id)=>World.EntityManager.HasComponent<LocalTransformCache>(id)?World.EntityManager.GetComponentData<LocalTransformCache>(id).m_ParentMesh:-1;
    }
}
