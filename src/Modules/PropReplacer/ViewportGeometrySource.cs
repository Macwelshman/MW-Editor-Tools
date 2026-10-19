using System;
using Game.Prefabs;
using Game.Tools;
using Unity.Mathematics;
using Colossal.Mathematics;

namespace MW.EditorTools.Modules.PropReplacer {
    // Read-only source geometry as serialized by ApplyPrefabsSystem. Never use
    // raw local caches as a substitute for the current live geometry.
    internal static class ViewportGeometrySource {
        internal static float3 Project(float3 world,Game.Objects.Transform owner,float3? cache=null) {
            if(!math.all(math.isfinite(world))||!math.all(math.isfinite(owner.m_Position))||!math.all(math.isfinite(owner.m_Rotation.value))||math.lengthsq(owner.m_Rotation.value)==0)
                throw new InvalidOperationException("Source geometry or owner placement is invalid.");
            var point=math.mul(math.inverse(owner.m_Rotation),world-owner.m_Position);
            if(cache.HasValue){
                if(!math.all(math.isfinite(cache.Value)))throw new InvalidOperationException("Cached source geometry is invalid.");
                if(math.distance(point,cache.Value)<.01f)point=cache.Value;
            }
            if(!math.all(math.isfinite(point)))throw new InvalidOperationException("Projected source geometry is invalid.");
            return point;
        }
        internal static float3[] Area(float3[] world,LocalNodeCache[] cache,Game.Objects.Transform owner) {
            if(world==null||world.Length<3||world.Length>4096||cache==null||cache.Length!=world.Length)
                throw new InvalidOperationException("Area nodes or their placement cache are incomplete.");
            var result=new float3[world.Length];
            for(int i=0;i<world.Length;i++)result[i]=Project(world[i],owner,cache[i].m_Position);
            return result;
        }
        internal static bool AreaMatches(ObjectSubAreaInfo row,PrefabBase prefab,float3[] positions,LocalNodeCache[] cache) {
            if(row==null||ReferenceEquals(prefab,null)||!ReferenceEquals(row.m_AreaPrefab,prefab)||positions==null||cache==null||cache.Length!=positions.Length||
                row.m_NodePositions==null||row.m_NodePositions.Length!=positions.Length||
                (row.m_ParentMeshes!=null&&row.m_ParentMeshes.Length!=positions.Length))return false;
            for(int i=0;i<positions.Length;i++)if(!Near(row.m_NodePositions[i],positions[i])||(row.m_ParentMeshes==null?-1:row.m_ParentMeshes[i])!=cache[i].m_ParentMesh)return false;
            return true;
        }
        internal static Bezier4x3 Curve(Bezier4x3 world,Bezier4x3? cache,Game.Objects.Transform owner)=>new Bezier4x3 {
            a=Project(world.a,owner,cache.HasValue?(float3?)cache.Value.a:null),
            b=Project(world.b,owner,cache.HasValue?(float3?)cache.Value.b:null),
            c=Project(world.c,owner,cache.HasValue?(float3?)cache.Value.c:null),
            d=Project(world.d,owner,cache.HasValue?(float3?)cache.Value.d:null)
        };
        internal static bool NetworkMatches(object row,PrefabBase prefab,Bezier4x3 curve,int2 meshes,bool sameEndpoint) {
            Bezier4x3 saved;int2 parent,nodes;PrefabBase identity;
            if(row is ObjectSubNetInfo net){saved=net.m_BezierCurve;parent=net.m_ParentMesh;nodes=net.m_NodeIndex;identity=net.m_NetPrefab;}
            else if(row is ObjectSubLaneInfo lane){saved=lane.m_BezierCurve;parent=lane.m_ParentMesh;nodes=lane.m_NodeIndex;identity=lane.m_LanePrefab;}
            else return false;
            return !ReferenceEquals(prefab,null)&&ReferenceEquals(identity,prefab)&&parent.Equals(meshes)&&nodes.x>=0&&nodes.y>=0&&
                ((nodes.x==nodes.y)==sameEndpoint)&&Near(saved.a,curve.a)&&Near(saved.b,curve.b)&&Near(saved.c,curve.c)&&Near(saved.d,curve.d);
        }
        private static bool Near(float3 a,float3 b)=>math.all(math.isfinite(a))&&math.all(math.isfinite(b))&&math.lengthsq(a-b)<1e-8f;
    }
}
