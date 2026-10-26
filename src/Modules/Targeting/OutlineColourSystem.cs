using System;
using System.Collections.Generic;
using Game;
using Game.Common;
using Game.Rendering;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Groups = Colossal.Rendering.NativeBatchGroups<Game.Rendering.CullingData,Game.Rendering.GroupData,Game.Rendering.BatchData,Game.Rendering.InstanceData>;
using Instances = Colossal.Rendering.NativeBatchInstances<Game.Rendering.CullingData,Game.Rendering.GroupData,Game.Rendering.BatchData,Game.Rendering.InstanceData>;

namespace MW.EditorTools.Modules.Targeting
{
    // Optional prototype: changes only the native per-instance outline colour property.
    public sealed partial class OutlineColourSystem : GameSystemBase
    {
        private EditorTargetingTool targeting;
        private readonly List<Paint> painted = new List<Paint>();
        private readonly HashSet<long> paintedSlots = new HashSet<long>();
        private struct Paint {
            internal Entity Entity, Mesh;
            internal MeshBatch Batch;
            internal int Property;
            internal Color Before, Applied;
        }
        internal void Initialize(EditorTargetingTool tool)
        {
            if (typeof(BatchManagerSystem).Module.ModuleVersionId.ToString() != "e15551e9-3dea-4855-8dde-1da3cea7d7ef" ||
                typeof(Instances).Module.ModuleVersionId.ToString() != "b5ff7e4e-51b0-4c1f-a177-2afdfc22906b")
                throw new NotSupportedException("Unverified per-instance outline API.");
            targeting = tool; Enabled = true;
            Mod.Log.Info("Per-instance outline prototype initialized; native rendering acceptance pending.");
        }
        protected override void OnUpdate()
        {
            try { Render(false); }
            catch (Exception error) {
                try { Render(true); } catch (Exception cleanup) { Mod.Log.Error(cleanup,"Outline prototype cleanup failed; native renderer refresh required."); }
                targeting?.SetOutlineStatus("Custom outline colours unavailable; native highlighting remains active.");
                Enabled = false;
                Mod.Log.Error(error,"Outline colour prototype disabled; editing and Undo remain active.");
            }
        }
        internal void RestoreForInspectorEdit() { if(World!=null&&World.IsCreated)Render(true); }
        internal void Release()
        {
            try { if (World != null && World.IsCreated) Render(true); }
            finally { targeting = null; Enabled = false; }
        }
        private static bool Same(Color a, Color b) => a.r==b.r && a.g==b.g && a.b==b.b && a.a==b.a;
        private bool Slot(Groups groups, Instances instances, Entity id, MeshBatch batch, int property, out int slot, out Entity mesh)
        {
            slot=-1; mesh=Entity.Null;
            if (!EntityManager.Exists(id) || batch.m_GroupIndex<0 || batch.m_GroupIndex>=groups.GetGroupCount() ||
                !groups.IsValidGroup(batch.m_GroupIndex) || batch.m_InstanceIndex<0 || batch.m_InstanceIndex>=instances.GetInstanceCount(batch.m_GroupIndex)) return false;
            var identity=instances.GetInstanceData(batch.m_GroupIndex,batch.m_InstanceIndex);
            if(identity.m_Entity!=id || identity.m_MeshGroup!=batch.m_MeshGroup || identity.m_MeshIndex!=batch.m_MeshIndex || identity.m_TileIndex!=batch.m_TileIndex) return false;
            var group=groups.GetGroupData(batch.m_GroupIndex); mesh=group.m_Mesh;
            return group.GetPropertyIndex(property,out slot) && slot>=0;
        }
        private void Render(bool restoreOnly)
        {
            Entity hovered=Entity.Null, selected=Entity.Null;
            if(!restoreOnly && targeting!=null) targeting.GetOutlineTargets(out hovered,out selected);
            if(painted.Count==0 && (restoreOnly || (hovered==Entity.Null && selected==Entity.Null))) return;
            if(painted.Count==0 && !HasMesh(hovered) && !HasMesh(selected)) {
                targeting?.SetOutlineStatus("This target has no eligible mesh outline; native gizmo colour is unchanged in this prototype.");
                return;
            }
            var manager=World.GetExistingSystemManaged<BatchManagerSystem>();
            if(manager==null) return;
            var groups=manager.GetNativeBatchGroups(true,out var groupJobs);
            var instances=manager.GetNativeBatchInstances(false,out var instanceJobs);
            JobHandle.CombineDependencies(Dependency,JobHandle.CombineDependencies(groupJobs,instanceJobs)).Complete();
            if(!groups.IsCreated || !instances.IsCreated) { painted.Clear(); return; }
            try {
                var writer=instances.AsParallelInstanceWriter();
                foreach(var old in painted) {
                    if(!Slot(groups,instances,old.Entity,old.Batch,old.Property,out var slot,out var mesh) || mesh!=old.Mesh) continue;
                    Color current=default;
                    // Never restore over a native/foreign change, or a reused instance slot.
                    if(writer.GetPropertyValue(out current,old.Batch.m_GroupIndex,slot,old.Batch.m_InstanceIndex) && Same(current,old.Applied))
                        writer.SetPropertyValue(old.Before,old.Batch.m_GroupIndex,slot,old.Batch.m_InstanceIndex);
                }
                painted.Clear(); paintedSlots.Clear();
                if(restoreOnly || targeting==null) return;
                int count=0;
                if(selected!=Entity.Null) count+=PaintTarget(selected,new Color(.1f,.9f,1f,1f),groups,instances);
                if(hovered!=Entity.Null && hovered!=selected) count+=PaintTarget(hovered,new Color(1f,.8f,.15f,1f),groups,instances);
                targeting.SetOutlineStatus(hovered==Entity.Null && selected==Entity.Null ? "" : count>0 ?
                    "Outline prototype: gold hover, cyan selected. Native gizmos may retain the game's colour." :
                    "This target has no eligible mesh outline; native gizmo colour is unchanged in this prototype.");
            } finally {
                manager.AddNativeBatchGroupsReader(default(JobHandle));
                manager.AddNativeBatchInstancesWriter(default(JobHandle));
            }
        }
        private bool HasMesh(Entity id) => id!=Entity.Null && targeting!=null && targeting.OwnsNativeHighlight(id) && EntityManager.HasBuffer<MeshBatch>(id);
        private int PaintTarget(Entity id, Color colour, Groups groups, Instances instances)
        {
            if(!targeting.OwnsNativeHighlight(id) || !EntityManager.HasBuffer<MeshBatch>(id) ||
                EntityManager.HasComponent<Game.Tools.Error>(id) || EntityManager.HasComponent<Game.Tools.Warning>(id) || EntityManager.HasComponent<Game.Tools.Override>(id) ||
                EntityManager.HasComponent<Deleted>(id) || EntityManager.HasComponent<Game.Tools.Temp>(id)) return 0;
            var batches=EntityManager.GetBuffer<MeshBatch>(id,true);
            // Bound work and restoration storage for malformed/extreme render data.
            if(batches.Length>1024) return 0;
            int property=EntityManager.HasComponent<Game.Net.Lane>(id)?(int)LaneProperty.OutlineColors:
                EntityManager.HasComponent<Game.Net.Edge>(id)||EntityManager.HasComponent<Game.Net.Node>(id)?(int)NetProperty.OutlineColors:(int)ObjectProperty.OutlineColors;
            var writer=instances.AsParallelInstanceWriter(); int count=0;
            foreach(var batch in batches) {
                if(!Slot(groups,instances,id,batch,property,out var slot,out var mesh)) continue;
                long key=((long)(uint)batch.m_GroupIndex<<32)|(uint)batch.m_InstanceIndex;
                if(!paintedSlots.Add(key)) continue;
                Color before=default;
                if(!writer.GetPropertyValue(out before,batch.m_GroupIndex,slot,batch.m_InstanceIndex)) continue;
                var applied=colour.linear;
                // Track before mutation so a later failure can restore this exact render slot.
                painted.Add(new Paint{Entity=id,Mesh=mesh,Batch=batch,Property=property,Before=before,Applied=applied});
                writer.SetPropertyValue(applied,batch.m_GroupIndex,slot,batch.m_InstanceIndex); count++;
            }
            return count;
        }
    }
}
