using System;
using Game.Prefabs;
using Game.Tools;
using Unity.Mathematics;
using System.Linq;
using MW.EditorTools.History;

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
        // ApplyPrefabsSystem.ListObjectSubObjects serializes world-derived local
        // position unless it lies within 1 cm of the cache. Do not change the cache
        // or accept an arbitrary height tolerance: reproduce that exact projection.
        internal static float3 NativePosition(Game.Objects.Transform owner,Game.Objects.Transform world,LocalTransformCache local) {
            if(!math.all(math.isfinite(owner.m_Position))||!math.all(math.isfinite(world.m_Position))||
                !math.all(math.isfinite(owner.m_Rotation.value))||!math.all(math.isfinite(world.m_Rotation.value))||
                !math.all(math.isfinite(local.m_Position)))throw new NotSupportedException("Nonfinite companion placement witness.");
            var position=math.mul(math.inverse(owner.m_Rotation),world.m_Position-owner.m_Position);
            if(!math.all(math.isfinite(position))||math.abs(position.x-local.m_Position.x)>=0.0001f||math.abs(position.z-local.m_Position.z)>=0.0001f)
                throw new NotSupportedException("Companion world/cache horizontal placement differs.");
            return math.distance(position,local.m_Position)<0.01f?local.m_Position:position;
        }
        internal static quaternion NativeRotation(Game.Objects.Transform owner,Game.Objects.Transform world,LocalTransformCache local){
            bool Unit(quaternion q)=>math.all(math.isfinite(q.value))&&math.abs(math.lengthsq(q.value)-1)<1e-5f;
            if(!Unit(owner.m_Rotation)||!Unit(world.m_Rotation)||!Unit(local.m_Rotation))throw new NotSupportedException("Invalid companion rotation witness.");
            // ListObjectSubObjects IL_0129/017c; CheckCachedValue(quaternion)
            // keeps the entire cached quaternion only below exactly one degree.
            var rotation=math.mul(math.inverse(owner.m_Rotation),world.m_Rotation);
            return Colossal.Mathematics.MathUtils.RotationAngle(rotation,local.m_Rotation)<0.017453292f?local.m_Rotation:rotation;
        }
        private static bool ExactRow(ObjectSubObjectInfo row,PrefabBase prefab,LocalTransformCache local)=>row!=null&&ReferenceEquals(row.m_Object,prefab)&&row.m_Position.Equals(local.m_Position)&&row.m_Rotation.Equals(local.m_Rotation)&&row.m_ParentMesh==local.m_ParentMesh&&row.m_Probability==local.m_Probability&&row.m_GroupIndex==local.m_GroupIndex;
        private static bool NearGround(Game.Objects.Transform owner,Game.Objects.Transform world,LocalTransformCache local){
            // This is a zero-height serialization case, not a general positional
            // tolerance. NativePosition validates finite witnesses and horizontal pose.
            NativePosition(owner,world,local);
            var projected=math.mul(math.inverse(owner.m_Rotation),world.m_Position-owner.m_Position);
            return local.m_ParentMesh==-1&&math.abs(local.m_Position.y)<0.0001f&&math.abs(projected.y)<0.0001f;
        }
        internal static int ResolveNative(ObjectSubObjectInfo[] rows,PrefabBase prefab,LocalTransformCache local,Game.Objects.Transform owner,Game.Objects.Transform world) {
            // An existing exact association, including any ambiguity, takes priority.
            if(rows!=null&&rows.Any(row=>ExactRow(row,prefab,local)))return Resolve(rows,prefab,local);
            var projected=local;projected.m_Position=NativePosition(owner,world,local);
            if(rows!=null&&rows.Any(row=>ExactRow(row,prefab,projected)))return Resolve(rows,prefab,projected);
            projected.m_Rotation=NativeRotation(owner,world,local);
            if(rows!=null&&rows.Any(row=>ExactRow(row,prefab,projected)))return Resolve(rows,prefab,projected);
            if(NearGround(owner,world,local)){
                var grounded=local;grounded.m_Position.y=0;
                if(rows!=null&&rows.Any(row=>ExactRow(row,prefab,grounded)))return Resolve(rows,prefab,grounded);
                grounded.m_Rotation=projected.m_Rotation;
                if(rows!=null&&rows.Any(row=>ExactRow(row,prefab,grounded)))return Resolve(rows,prefab,grounded);
            }
            try{return Resolve(rows,prefab,projected);}catch(NotSupportedException error){throw new NotSupportedException(error.Message+"; native position="+Position(projected.m_Position)+" native rotation="+Rotation(projected.m_Rotation)+" world-local rotation="+Rotation(math.mul(math.inverse(owner.m_Rotation),world.m_Rotation))+" owner="+Position(owner.m_Position)+"/"+Rotation(owner.m_Rotation)+" world="+Position(world.m_Position)+"/"+Rotation(world.m_Rotation),error);}
        }
        internal static LocalTransformCache ForNativeSource(ObjectSubObjectInfo[] rows,PrefabBase prefab,LocalTransformCache local,Game.Objects.Transform owner,Game.Objects.Transform world) {
            int index=ResolveNative(rows,prefab,local,owner,world);
            // Only detached expectations adopt the uniquely proved saved pose.
            // Original snapshots, world placement and attachments stay intact.
            local.m_PrefabSubIndex=index;local.m_Position=rows[index].m_Position;local.m_Rotation=rows[index].m_Rotation;return local;
        }
        internal static bool NativeRows(ObjectSubObjectInfo[] before,ObjectSubObjectInfo[] after,PrefabBase[] prefabs,LocalTransformCache[] locals,Game.Objects.Transform[] worlds,Game.Objects.Transform owner) {
            if(before==null||after==null||before.Length!=prefabs.Length||locals.Length!=prefabs.Length||worlds.Length!=prefabs.Length)return false;
            if(DescriptorPermutation.Matches(before,after))return true;
            var normalized=(ObjectSubObjectInfo[])PrefabSnapshot.Take(before).Materialize();var used=new System.Collections.Generic.HashSet<int>();
            for(int i=0;i<prefabs.Length;i++){
                int row=ResolveNative(before,prefabs[i],locals[i],owner,worlds[i]);if(!used.Add(row))return false;
                if(!after.Any(a=>PrefabSnapshot.Take(before[row]).Matches(a))){
                    normalized[row].m_Position=NativePosition(owner,worlds[i],locals[i]);
                    normalized[row].m_Rotation=NativeRotation(owner,worlds[i],locals[i]);
                    if(NearGround(owner,worlds[i],locals[i])){
                        var zero=(ObjectSubObjectInfo)PrefabSnapshot.Take(normalized[row]).Materialize();zero.m_Position=locals[i].m_Position;zero.m_Position.y=0;
                        if(after.Any(a=>PrefabSnapshot.Take(zero).Matches(a)))normalized[row]=zero;
                    }
                }
            }
            return DescriptorPermutation.Matches(before,after,(a,b)=>{
                int index=Array.IndexOf(before,a);
                return PrefabSnapshot.Take(a).Matches(b)||PrefabSnapshot.Take(normalized[index]).Matches(b);
            });
        }
        private static string Describe(ObjectSubObjectInfo[] rows,PrefabBase prefab,LocalTransformCache local) {
            var message="cache position="+Position(local.m_Position)+" rotation="+Rotation(local.m_Rotation)+" parentMesh="+local.m_ParentMesh+" probability="+local.m_Probability+" group="+local.m_GroupIndex+" index="+local.m_PrefabSubIndex;
            int count=0;
            if(rows!=null)for(int i=0;i<rows.Length&&count<4;i++){
                var row=rows[i];if(row==null||!ReferenceEquals(row.m_Object,prefab))continue;count++;
                message+="; row["+i+"] position="+Position(row.m_Position)+" rotation="+Rotation(row.m_Rotation)+" parentMesh="+row.m_ParentMesh+" probability="+row.m_Probability+" group="+row.m_GroupIndex;
            }
            return message;
        }
        private static string Position(float3 p)=>FormattableString.Invariant($"({p.x:R},{p.y:R},{p.z:R})");
        private static string Rotation(quaternion q)=>FormattableString.Invariant($"({q.value.x:R},{q.value.y:R},{q.value.z:R},{q.value.w:R})");
    }
}
