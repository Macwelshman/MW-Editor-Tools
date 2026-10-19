using System;
using System.Linq;
using System.Text;
using Game.Prefabs;
using Game.Tools;
using Unity.Mathematics;

namespace MW.EditorTools.Modules.PropReplacer {
    internal static class ViewportPropSource {
        // ApplyPrefabsSystem.ListObjectSubObjects projects the live pose into its
        // owner, then snaps to the cache only within 1 cm / 1 degree. The cache
        // alone can therefore differ from the saved descriptor on uneven ground.
        internal static int Resolve(ObjectSubObjectInfo[] rows, PrefabBase prefab, LocalTransformCache cache,
            Game.Objects.Transform pose, Game.Objects.Transform owner,bool[] groundSources=null) {
            var projected=Project(cache,pose,owner);
            var position=projected.m_Position;var rotation=projected.m_Rotation;
            // Native OnGround creation discards the authored local height. Only
            // use that contract when the caller corroborates the compiled source
            // entry, and the native index agrees with one unique horizontal row.
            if(rows!=null&&groundSources!=null&&groundSources.Length==rows.Length&&cache.m_PrefabSubIndex>=0&&cache.m_PrefabSubIndex<rows.Length&&groundSources[cache.m_PrefabSubIndex]&&
                cache.m_ParentMesh==-1&&cache.m_Position.y==0&&math.lengthsq(math.mul(owner.m_Rotation,new float3(0,1,0))-new float3(0,1,0))<1e-8f&&
                math.lengthsq(cache.m_Position.xz-position.xz)<1e-8f) {
                int grounded=-1;
                for(int i=0;i<rows.Length;i++) {
                    var row=rows[i];
                    if(row==null||!ReferenceEquals(row.m_Object,prefab)||row.m_ParentMesh!=cache.m_ParentMesh||row.m_Probability!=cache.m_Probability||row.m_GroupIndex!=cache.m_GroupIndex||
                        !math.all(math.isfinite(row.m_Position))||!math.all(math.isfinite(row.m_Rotation.value))||
                        !(math.lengthsq(row.m_Position.xz-position.xz)<1e-8f)||!(math.lengthsq(row.m_Rotation.value-rotation.value)<1e-8f))continue;
                    if(grounded!=-1)throw new InvalidOperationException("Ground-aligned placement matches multiple source rows; replacement refused.");
                    grounded=i;
                }
                if(grounded!=cache.m_PrefabSubIndex)throw new InvalidOperationException("Ground-aligned placement and native source index disagree; replacement refused.");
                return grounded;
            }
            int found=-1;
            if (rows!=null) for (int i=0;i<rows.Length;i++) {
                var row=rows[i];
                if (row==null || !ReferenceEquals(row.m_Object,prefab) ||
                    row.m_ParentMesh!=cache.m_ParentMesh || row.m_Probability!=cache.m_Probability || row.m_GroupIndex!=cache.m_GroupIndex ||
                    !math.all(math.isfinite(row.m_Position)) || !math.all(math.isfinite(row.m_Rotation.value)) ||
                    !(math.lengthsq(row.m_Position-position)<1e-8f) || !(math.lengthsq(row.m_Rotation.value-rotation.value)<1e-8f)) continue;
                if (found!=-1) throw new InvalidOperationException("The highlighted placement matches multiple source rows; replacement refused.");
                found=i;
            }
            if (found<0) throw new InvalidOperationException("The highlighted placement does not match its source row. Diagnostic details were saved to the mod log.");
            return found;
        }
        private static Game.Objects.Transform Project(LocalTransformCache cache,Game.Objects.Transform pose,Game.Objects.Transform owner) {
            if (!Finite(pose) || !Finite(owner) || !math.all(math.isfinite(cache.m_Position)) ||
                !math.all(math.isfinite(cache.m_Rotation.value)) || math.lengthsq(cache.m_Rotation.value)==0)
                throw new InvalidOperationException("The highlighted object has invalid placement data.");
            var inverse=math.inverse(owner.m_Rotation);
            var position=math.mul(inverse,pose.m_Position-owner.m_Position);
            var rotation=math.mul(inverse,pose.m_Rotation);
            if (math.distance(position,cache.m_Position)<.01f) position=cache.m_Position;
            if (Colossal.Mathematics.MathUtils.RotationAngle(rotation,cache.m_Rotation)<.017453292f) rotation=cache.m_Rotation;
            if (!math.all(math.isfinite(position)) || !math.all(math.isfinite(rotation.value)))
                throw new InvalidOperationException("The highlighted object's owner has invalid placement data.");
            return new Game.Objects.Transform{m_Position=position,m_Rotation=rotation};
        }
        internal static string Describe(ObjectSubObjectInfo[] rows,PrefabBase prefab,LocalTransformCache cache,Game.Objects.Transform pose,Game.Objects.Transform owner) {
            var text=new StringBuilder("livePosition="+pose.m_Position+" liveRotation="+pose.m_Rotation+" ownerPosition="+owner.m_Position+" ownerRotation="+owner.m_Rotation+
                " cachePosition="+cache.m_Position+" cacheRotation="+cache.m_Rotation+" cacheMesh="+cache.m_ParentMesh+" cacheProbability="+cache.m_Probability+" cacheGroup="+cache.m_GroupIndex+" cacheIndex="+cache.m_PrefabSubIndex);
            Game.Objects.Transform projected;
            try{projected=Project(cache,pose,owner);}catch(Exception error){return text.Append(" projectionError="+error.Message).ToString();}
            text.Append(" projectedPosition="+projected.m_Position+" projectedRotation="+projected.m_Rotation);
            var candidates=rows==null?new int[0]:Enumerable.Range(0,rows.Length).Where(i=>rows[i]!=null&&ReferenceEquals(rows[i].m_Object,prefab)).ToArray();
            text.Append(" matchingPrefabRows="+candidates.Length);
            // Bound the log while including the geometrically closest candidates,
            // rather than only the first few occurrences of a repeated wall.
            foreach(var i in candidates.OrderBy(i=>math.lengthsq(rows[i].m_Position-projected.m_Position)).Take(8)) {
                var row=rows[i];
                text.Append("; row["+i+"] position="+row.m_Position+" rotation="+row.m_Rotation+" mesh="+row.m_ParentMesh+" probability="+row.m_Probability+" group="+row.m_GroupIndex+
                    " positionDelta="+(row.m_Position-projected.m_Position)+" rotationDeltaSq="+math.lengthsq(row.m_Rotation.value-projected.m_Rotation.value)+
                    " oppositeRotationDeltaSq="+math.lengthsq(row.m_Rotation.value+projected.m_Rotation.value)+" cachePositionDelta="+(row.m_Position-cache.m_Position));
            }
            return text.ToString();
        }
        private static bool Finite(Game.Objects.Transform pose)=>math.all(math.isfinite(pose.m_Position)) &&
            math.all(math.isfinite(pose.m_Rotation.value)) && math.lengthsq(pose.m_Rotation.value)>0;
        private static bool MatchesCompiled(ObjectSubObjectInfo row,SubObject native,bool samePrefab)=>row!=null&&samePrefab&&row.m_ParentMesh==native.m_ParentIndex&&
            math.all(math.isfinite(row.m_Position))&&math.all(math.isfinite(row.m_Rotation.value))&&row.m_Position.Equals(native.m_Position)&&row.m_Rotation.Equals(native.m_Rotation)&&
            row.m_GroupIndex==native.m_GroupIndex&&row.m_Probability==native.m_Probability;
        internal static bool ProvesGround(ObjectSubObjectInfo row,SubObject native,bool samePrefab)=>MatchesCompiled(row,native,samePrefab)&&row.m_ParentMesh==-1&&native.m_ParentIndex==-1&&
            (native.m_Flags&(SubObjectFlags.OnGround|SubObjectFlags.AnchorTop|SubObjectFlags.AnchorCenter|SubObjectFlags.OnAttachedParent))==SubObjectFlags.OnGround;
        internal static bool[] GroundSources(ObjectSubObjectInfo[] rows,PrefabBase prefab,SubObject[] native,Unity.Entities.Entity identity) {
            if(rows==null||native==null)return null;
            var result=new bool[rows.Length];
            for(int i=0;i<rows.Length;i++) {
                if(rows[i]==null||!ReferenceEquals(rows[i].m_Object,prefab))continue;
                int matches=0;bool ground=false;
                for(int j=0;j<native.Length;j++)if(MatchesCompiled(rows[i],native[j],native[j].m_Prefab==identity)) {
                    matches++;ground=ProvesGround(rows[i],native[j],true);
                }
                result[i]=matches==1&&ground;
            }
            return result;
        }
    }
}
