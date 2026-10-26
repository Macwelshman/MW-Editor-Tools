using System;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;
using Unity.Mathematics;
using Colossal.Mathematics;

namespace MW.EditorTools.Modules.PropReplacer {
    public sealed partial class PropReplacerSystem {
        private Row[] ResolveViewportNetworkRows(Row[] rows,PrefabBase prefab,Entity id,Entity owner,bool allowReverse=false) {
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
            var reversed=new Bezier4x3{a=curve.d,b=curve.c,c=curve.b,d=curve.a};
            var reversedMeshes=new int2(meshes.y,meshes.x);
            var exact=rows.Where(r=>ViewportGeometrySource.NetworkMatches(r.Value,prefab,curve,meshes,sameEndpoint)||
                allowReverse&&ViewportGeometrySource.NetworkMatches(r.Value,prefab,reversed,reversedMeshes,sameEndpoint)).ToArray();
            return exact.Length>0||!allowReverse||!cache.HasValue?exact:CompiledNetworkRows(rows,prefab,id,owner,curve,cache.Value,meshes,sameEndpoint);
        }
        private Row[] CompiledNetworkRows(Row[] rows,PrefabBase prefab,Entity id,Entity owner,Bezier4x3 live,Bezier4x3 cache,int2 meshes,bool sameEndpoint) {
            var em=World.EntityManager;
            if(rows.Length>4096||!em.HasComponent<PrefabRef>(owner)||!em.HasComponent<PrefabRef>(id))return new Row[0];
            var root=em.GetComponentData<PrefabRef>(owner).m_Prefab;
            if(!em.Exists(root)||em.HasComponent<Game.Common.Updated>(root)||em.HasComponent<Game.Common.Created>(root)||em.HasComponent<Game.Common.Deleted>(root)||em.HasComponent<Temp>(root))return new Row[0];
            var identity=em.GetComponentData<PrefabRef>(id).m_Prefab;
            var reverseCache=new Bezier4x3{a=cache.d,b=cache.c,c=cache.b,d=cache.a};
            var found=new System.Collections.Generic.List<Row>();
            foreach(var row in rows) {
                Bezier4x3 sourceCurve,proofCurve;int2 sourceNodes,sourceMeshes,proofNodes,proofMeshes;Entity proofPrefab,expected=identity;
                if(row.Index<0)continue;
                if(row.Value is ObjectSubNetInfo source&&em.HasBuffer<Game.Prefabs.SubNet>(root)) {
                    var compiled=em.GetBuffer<Game.Prefabs.SubNet>(root,true);if(compiled.Length>4096||row.Index>=compiled.Length)continue;
                    var proof=compiled[row.Index];sourceCurve=source.m_BezierCurve;sourceNodes=source.m_NodeIndex;sourceMeshes=source.m_ParentMesh;
                    proofCurve=proof.m_Curve;proofNodes=proof.m_NodeIndex;proofMeshes=proof.m_ParentMesh;proofPrefab=proof.m_Prefab;
                    if(!ReferenceEquals(source.m_NetPrefab,prefab))continue;
                }else if(row.Value is ObjectSubLaneInfo lane&&em.HasBuffer<Game.Prefabs.SubLane>(root)) {
                    var compiled=em.GetBuffer<Game.Prefabs.SubLane>(root,true);if(compiled.Length>4096||row.Index>=compiled.Length)continue;
                    var proof=compiled[row.Index];sourceCurve=lane.m_BezierCurve;sourceNodes=lane.m_NodeIndex;sourceMeshes=lane.m_ParentMesh;
                    proofCurve=proof.m_Curve;proofNodes=proof.m_NodeIndex;proofMeshes=proof.m_ParentMesh;proofPrefab=proof.m_Prefab;
                    if(em.HasComponent<Game.Tools.EditorContainer>(id))expected=em.GetComponentData<Game.Tools.EditorContainer>(id).m_Prefab;
                    if(!ReferenceEquals(lane.m_LanePrefab,prefab))continue;
                }else continue;
                // Cache is accepted only with exact compiled source proof. Live
                // regenerated control handles/heights need not serialize identically.
                if(proofPrefab!=expected||!proofNodes.Equals(sourceNodes)||!proofMeshes.Equals(sourceMeshes)||!NetworkCurveEqual(proofCurve,sourceCurve))continue;
                if(!NetworkReadOnlyCacheEqual(proofCurve,cache)&&!NetworkReadOnlyCacheEqual(proofCurve,reverseCache))continue;
                // -1 is the native descriptor default for an independent endpoint,
                // not an invalid source or proof that both endpoints are one node.
                if(sourceNodes.x< -1||sourceNodes.y< -1||sourceNodes.x>=0&&sourceNodes.y>=0&&((sourceNodes.x==sourceNodes.y)!=sameEndpoint))continue;
                bool forward=NetworkEndpointXZ(live.a,proofCurve.a)&&NetworkEndpointXZ(live.d,proofCurve.d);
                bool reverse=NetworkEndpointXZ(live.a,proofCurve.d)&&NetworkEndpointXZ(live.d,proofCurve.a);
                if(forward&&sourceMeshes.Equals(meshes)||reverse&&sourceMeshes.Equals(new int2(meshes.y,meshes.x)))found.Add(row);
            }
            return found.ToArray();
        }
        // Presentation only, after editable/compiled source equality is proved.
        // Permit sub-millimetre rounding in interior heights; endpoints and XZ
        // retain their strict checks. Never used for replacement or Undo.
        private static bool NetworkReadOnlyCacheEqual(Bezier4x3 source,Bezier4x3 cached) {
            bool Endpoint(float3 a,float3 b)=>math.all(math.isfinite(a))&&math.all(math.isfinite(b))&&math.lengthsq(a-b)<1e-8f;
            bool Interior(float3 a,float3 b)=>math.all(math.isfinite(a))&&math.all(math.isfinite(b))&&math.lengthsq(a.xz-b.xz)<1e-8f&&math.abs(a.y-b.y)<=.0005f;
            return Endpoint(source.a,cached.a)&&Endpoint(source.d,cached.d)&&Interior(source.b,cached.b)&&Interior(source.c,cached.c);
        }
        private static bool NetworkEndpointXZ(float3 a,float3 b)=>math.all(math.isfinite(a))&&math.all(math.isfinite(b))&&math.lengthsq(a.xz-b.xz)<1e-6f;
        private static bool NetworkCurveEqual(Bezier4x3 a,Bezier4x3 b)=>math.lengthsq(a.a-b.a)<1e-8f&&math.lengthsq(a.b-b.b)<1e-8f&&math.lengthsq(a.c-b.c)<1e-8f&&math.lengthsq(a.d-b.d)<1e-8f;
        private void RequireViewportNetworkNode(Entity id) {
            var em=World.EntityManager;
            if(!em.Exists(id)||!em.HasComponent<Game.Net.Node>(id)||em.HasComponent<Temp>(id)||em.HasComponent<Game.Common.Deleted>(id)||em.HasComponent<Game.Common.Created>(id)||em.HasComponent<Game.Common.Updated>(id))
                throw new InvalidOperationException("A network endpoint is unavailable or still updating.");
        }
        private int ViewportParentMesh(Entity id)=>World.EntityManager.HasComponent<LocalTransformCache>(id)?World.EntityManager.GetComponentData<LocalTransformCache>(id).m_ParentMesh:-1;
    }
}
