using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using MW.EditorTools.Modules.BulldozerRecovery;
using MW.EditorTools.History;

namespace MW.EditorTools.Modules.PropReplacer {
 public sealed partial class PropReplacerSystem {
  private sealed class FoliageWitness {internal Entity Id,Owner,Prefab;internal Game.Objects.Transform Pose;}
  private FoliageWitness[] foliageWitnesses=new FoliageWitness[0];private Entity[] foliageCreated=new Entity[0];private int foliageCheckFrame,foliageStable;private bool foliageFailed,foliageRefreshSent;
  private List<FoliageCandidate> foliagePlan;private Row foliageRow;private PrefabSnapshot foliageBefore,foliageAfter;
  private void WatchFoliageCandidates(List<FoliageCandidate> plan,Row row,PrefabSnapshot before){foliageWitnesses=plan.Where(c=>c.Witnesses!=null).SelectMany(c=>c.Witnesses).GroupBy(w=>w.Id).Select(g=>g.First()).ToArray();foliageCreated=plan.Select(c=>c.Created).ToArray();foliagePlan=plan;foliageRow=row;foliageBefore=before;foliageAfter=PrefabSnapshot.Take(row.ListField.GetValue(row.Owner));foliageCheckFrame=UnityEngine.Time.frameCount;foliageStable=0;foliageRefreshSent=false;}
  internal void BeforeNativeFoliageRendering(){
   if(!Enabled||context?.EditorAvailable!=true||foliageWitnesses.Length==0||foliageRefreshSent||foliagePlan==null)return;
   var em=World.EntityManager;
   if(foliagePlan.Any(c=>!em.Exists(c.Created)||em.HasComponent<Game.Common.Created>(c.Created)||em.HasComponent<Game.Common.Deleted>(c.Created)||!em.HasComponent<Game.Rendering.CullingInfo>(c.Created)))return;
   try{foreach(var c in foliagePlan){CandidatePut(c.Created,new Game.Common.Updated());CandidatePut(c.Created,new Game.Common.BatchesUpdated());}foliageRefreshSent=true;MW.EditorTools.Mod.Log.Info("Replacement foliage refresh submitted at native pre-culling consumer.");}
   catch(Exception error){MW.EditorTools.Mod.Log.Error(error,"Replacement foliage render refresh failed.");}
  }
  private void CheckFoliageWitnesses(){
   if(foliageWitnesses.Length==0)return;var em=World.EntityManager;
   if(UnityEngine.Time.frameCount==foliageCheckFrame)return;
   if(context?.EditorAvailable!=true){foliageWitnesses=new FoliageWitness[0];return;}
   bool exact=foliageWitnesses.All(w=>em.Exists(w.Id)&&!em.HasComponent<Game.Common.Deleted>(w.Id)&&em.HasComponent<PrefabRef>(w.Id)&&em.GetComponentData<PrefabRef>(w.Id).m_Prefab==w.Prefab&&em.HasComponent<Game.Objects.Transform>(w.Id)&&em.GetComponentData<Game.Objects.Transform>(w.Id).Equals(w.Pose)&&em.HasComponent<Game.Common.Owner>(w.Id)&&em.GetComponentData<Game.Common.Owner>(w.Id).m_Owner==w.Owner&&OwnerLinks(w.Owner,w.Id)==1);
   bool settled=exact&&foliageCreated.All(id=>em.Exists(id)&&!em.HasComponent<Game.Common.Created>(id)&&!em.HasComponent<Game.Common.Updated>(id)&&!em.HasComponent<Game.Common.Deleted>(id))&&foliageWitnesses.All(w=>!em.HasComponent<Game.Common.Updated>(w.Owner)&&!em.HasComponent<Game.Common.Created>(w.Owner)&&!em.HasComponent<Game.Common.Updated>(w.Id));
   if(settled&&foliageRefreshSent){var tool=World.GetExistingSystemManaged<ToolSystem>()?.activeTool as DefaultToolSystem;if(tool!=null){var hover=(Entity)hoverEntity.GetValue(tool);if(foliagePlan.Any(c=>hover==c.Owner||hover==c.Target||hover==c.Created))hoverEntity.SetValue(tool,Entity.Null);}}
   bool destinations=settled&&foliageRefreshSent&&foliagePlan.All(c=>em.HasComponent<PrefabRef>(c.Created)&&em.GetComponentData<PrefabRef>(c.Created).m_Prefab==c.Prefab&&em.HasComponent<Game.Objects.Transform>(c.Created)&&em.GetComponentData<Game.Objects.Transform>(c.Created).Equals(c.Pose)&&OwnerLinks(c.Owner,c.Created)==1&&(!c.ExpectMesh||(em.HasBuffer<Game.Rendering.MeshBatch>(c.Created)&&em.GetBuffer<Game.Rendering.MeshBatch>(c.Created,true).Length>0))&&(!c.ExpectBounds||(em.HasComponent<Game.Rendering.CullingInfo>(c.Created)&&em.GetComponentData<Game.Rendering.CullingInfo>(c.Created).m_Radius>0)));
   foliageStable=destinations?foliageStable+1:0;
   if(foliageStable>=3){MW.EditorTools.Mod.Log.Info("Replacement foliage completed: donors retained="+foliageWitnesses.Length+" rendered destinations="+foliagePlan.Count);foliageWitnesses=new FoliageWitness[0];}
   else if(!exact||UnityEngine.Time.frameCount-foliageCheckFrame>180){
    foreach(var c in foliagePlan){using(var types=em.Exists(c.Created)?em.GetComponentTypes(c.Created,Allocator.Temp):default(NativeArray<ComponentType>))MW.EditorTools.Mod.Log.Info("Replacement foliage destination failure: entity="+c.Created+" exists="+em.Exists(c.Created)+" mesh="+(em.Exists(c.Created)&&em.HasBuffer<Game.Rendering.MeshBatch>(c.Created)?em.GetBuffer<Game.Rendering.MeshBatch>(c.Created,true).Length:0)+" radius="+(em.Exists(c.Created)&&em.HasComponent<Game.Rendering.CullingInfo>(c.Created)?em.GetComponentData<Game.Rendering.CullingInfo>(c.Created).m_Radius:0)+" hidden="+(em.Exists(c.Created)&&em.HasComponent<Hidden>(c.Created))+" types="+(types.IsCreated?string.Join(",",types.Select(t=>t.GetManagedType()?.FullName)):"none"));}
    foliageFailed=true;
    try{if(foliageRow.Root!=null&&foliageAfter.Matches(foliageRow.ListField.GetValue(foliageRow.Owner))){RemoveFoliageCandidates(foliagePlan);foliageRow.ListField.SetValue(foliageRow.Owner,foliageBefore.Materialize());Refresh(foliageRow.Root,foliageRow.Ancestors);MW.EditorTools.Mod.Log.Warn("Foliage replacement failed validation; previous source list restored and regeneration queued.");}}
    catch(Exception error){MW.EditorTools.Mod.Log.Error(error,"Foliage failure rollback incomplete.");}
    status="Foliage replacement did not finish. Previous source rollback attempted; restart the disposable session without saving.";MW.EditorTools.Mod.Log.Warn(status);foliageWitnesses=new FoliageWitness[0];
   }
  }
  private List<FoliageCandidate> PlanFoliageReplacement(Row row,bool batch,PrefabBase oldPrefab) {
   var plan=new List<FoliageCandidate>();if(row.Value.GetType()!=typeof(ObjectSubObjectInfo))return plan;
   var em=World.EntityManager;var registry=World.GetExistingSystemManaged<PrefabSystem>();Entity prefab;
   if(!registry.TryGetEntity(clipboard.Replacement,out prefab)||!em.Exists(prefab)||!em.HasComponent<ObjectData>(prefab))return plan;
   var archetype=em.GetComponentData<ObjectData>(prefab).m_Archetype;
   bool vegetation=false;
   using(var types=archetype.GetComponentTypes(Allocator.Temp))vegetation=types.Any(t=>t.GetManagedType()==typeof(Game.Objects.Plant)||t.GetManagedType()==typeof(Game.Objects.Tree));
   if(!vegetation)return plan;
   // New candidates are initialized from the real prefab archetype. Declared
   // companions belong to that prefab; runtime child entities/handles are never
   // copied from a donor. Validate supported source lists before any live write.
   int sourceWeight=0;
   for(int i=0;i<RecoveryListSchema.Components.Length;i++)foreach(var component in clipboard.Replacement.components.Where(c=>c!=null&&c.GetType()==RecoveryListSchema.Components[i])){
    if(!ReferenceEquals(component.prefab,clipboard.Replacement))throw new InvalidOperationException("Foliage companion source ownership is invalid.");
    var image=PrefabSnapshot.Take(component.GetType().GetField(RecoveryListSchema.Members[i]).GetValue(component));sourceWeight+=image.Weight;
    if(!image.ReferencesValid||sourceWeight>250000)throw new NotSupportedException("Foliage companion source is unavailable or exceeds the snapshot budget.");
   }
   var rows=((ObjectSubObjects)row.Owner).m_SubObjects;
   var indices=Enumerable.Range(0,rows.Length).Where(i=>rows[i]!=null&&ReferenceEquals(rows[i].m_Object,oldPrefab)&&(batch||i==row.Index)).ToArray();
   using(var query=em.CreateEntityQuery(ComponentType.ReadOnly<PrefabRef>(),ComponentType.ReadOnly<Game.Objects.SubObject>()))
   using(var owners=query.ToEntityArray(Allocator.Temp))foreach(var owner in owners){
    if(em.HasComponent<Game.Common.Deleted>(owner)||em.HasComponent<Temp>(owner)||!ReferenceEquals(registry.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(owner)),row.Root))continue;
    var children=em.GetBuffer<Game.Objects.SubObject>(owner,true).ToNativeArray(Allocator.Temp);
    Entity[] ids;try{ids=children.Select(c=>c.m_SubObject).ToArray();}finally{children.Dispose();}
    var donors=ids.Where(id=>em.Exists(id)&&em.HasComponent<PrefabRef>(id)&&em.GetComponentData<PrefabRef>(id).m_Prefab==prefab).ToArray();
    if(donors.Length==0)continue; // No same-prefab neighbour can be borrowed.
    foreach(var donor in donors)ValidateFoliageDonor(donor,owner);
    var template=donors[0];
    using(var types=archetype.GetComponentTypes(Allocator.Temp))foreach(var type in types){var name=type.GetManagedType()?.FullName??"unknown";if(!PropComponentSchema.Known.Contains(name)&&name!="Game.Objects.Plant"&&name!="Game.Objects.Tree"&&name!="Game.Simulation.UpdateFrame"&&name!="Game.Common.Created"&&name!="Game.Common.Updated"&&name!="Game.Common.Applied")throw new NotSupportedException("Unsupported foliage candidate archetype: "+name);}
    foreach(var index in indices){
     var targets=ids.Where(id=>em.Exists(id)&&em.HasComponent<PrefabRef>(id)&&ReferenceEquals(registry.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(id)),oldPrefab)&&em.HasComponent<LocalTransformCache>(id)&&BoundDescriptorIndex.Resolve(rows,oldPrefab,em.GetComponentData<LocalTransformCache>(id))==index).ToArray();
     if(targets.Length!=1)throw new InvalidOperationException("Foliage replacement requires one exact live target per changed descriptor.");
     var target=targets[0];if(em.HasComponent<Temp>(target)||em.HasComponent<Game.Common.Deleted>(target)||em.HasComponent<Game.Common.Created>(target)||em.HasComponent<Game.Common.Updated>(target)||!em.HasComponent<Game.Objects.Transform>(target)||!em.HasComponent<Game.Common.PseudoRandomSeed>(target)||!em.HasComponent<Game.Common.Owner>(target)||em.GetComponentData<Game.Common.Owner>(target).m_Owner!=owner)throw new InvalidOperationException("Foliage destination is not settled.");
     var local=em.GetComponentData<LocalTransformCache>(target);local.m_PrefabSubIndex=index;
     var c=new FoliageCandidate{Owner=owner,Prefab=prefab,Archetype=archetype,Local=local,Pose=em.GetComponentData<Game.Objects.Transform>(target),Seed=em.GetComponentData<Game.Common.PseudoRandomSeed>(target),Surface=em.HasComponent<Game.Objects.Surface>(target),Elevation=em.HasComponent<Game.Objects.Elevation>(target),Plant=em.HasComponent<Game.Objects.Plant>(template),Tree=em.HasComponent<Game.Objects.Tree>(template),Color=em.HasComponent<Game.Objects.Color>(template),Decoration=em.HasComponent<Game.Objects.Decoration>(template)};
     if(c.Surface)c.SurfaceValue=em.GetComponentData<Game.Objects.Surface>(target);if(c.Elevation)c.ElevationValue=em.GetComponentData<Game.Objects.Elevation>(target);
     if(!Unity.Mathematics.math.all(Unity.Mathematics.math.isfinite(c.Pose.m_Position))||!Unity.Mathematics.math.all(Unity.Mathematics.math.isfinite(c.Pose.m_Rotation.value))||!Unity.Mathematics.math.all(Unity.Mathematics.math.isfinite(local.m_Position))||!Unity.Mathematics.math.all(Unity.Mathematics.math.isfinite(local.m_Rotation.value)))throw new InvalidOperationException("Nonfinite foliage destination pose.");
     c.Witnesses=donors.Select(d=>new FoliageWitness{Id=d,Owner=owner,Prefab=prefab,Pose=em.GetComponentData<Game.Objects.Transform>(d)}).ToArray();
     c.Target=target;
     c.ExpectMesh=em.HasBuffer<Game.Rendering.MeshBatch>(template)&&em.GetBuffer<Game.Rendering.MeshBatch>(template,true).Length>0;c.ExpectBounds=em.HasComponent<Game.Rendering.CullingInfo>(template)&&em.GetComponentData<Game.Rendering.CullingInfo>(template).m_Radius>0;
     if(c.Plant)c.PlantValue=em.GetComponentData<Game.Objects.Plant>(template);if(c.Tree)c.TreeValue=em.GetComponentData<Game.Objects.Tree>(template);if(c.Color)c.ColorValue=em.GetComponentData<Game.Objects.Color>(template);
     if(c.Decoration)c.DecorationEnabled=em.IsComponentEnabled<Game.Objects.Decoration>(template);
     if(em.HasBuffer<Game.Rendering.MeshColor>(template))using(var values=em.GetBuffer<Game.Rendering.MeshColor>(template,true).ToNativeArray(Allocator.Temp))c.Colors=values.ToArray();
     if(em.HasBuffer<Game.Rendering.CustomMeshColor>(template))using(var values=em.GetBuffer<Game.Rendering.CustomMeshColor>(template,true).ToNativeArray(Allocator.Temp))c.Custom=values.ToArray();
     plan.Add(c);if(plan.Count>256)throw new NotSupportedException("Foliage replacement candidate budget exceeded.");
    }
   }
   return plan;
  }
  private void ValidateFoliageDonor(Entity id,Entity owner){
   var em=World.EntityManager;
   if(em.HasComponent<Temp>(id)||em.HasComponent<Game.Common.Deleted>(id)||em.HasComponent<Game.Common.Created>(id)||em.HasComponent<Game.Common.Updated>(id)||!em.HasComponent<Game.Common.Owner>(id)||em.GetComponentData<Game.Common.Owner>(id).m_Owner!=owner||(!em.HasComponent<Game.Objects.Plant>(id)&&!em.HasComponent<Game.Objects.Tree>(id)))throw new InvalidOperationException("Foliage neighbour is not settled.");
   if((em.HasBuffer<Game.Objects.SubObject>(id)&&em.GetBuffer<Game.Objects.SubObject>(id,true).Length>256)||(em.HasBuffer<Game.Effects.EnabledEffect>(id)&&em.GetBuffer<Game.Effects.EnabledEffect>(id,true).Length>256)||(em.HasBuffer<Game.Net.SubNet>(id)&&em.GetBuffer<Game.Net.SubNet>(id,true).Length>256)||(em.HasBuffer<Game.Areas.SubArea>(id)&&em.GetBuffer<Game.Areas.SubArea>(id,true).Length>256))throw new NotSupportedException("Foliage companion witness budget exceeded.");
   if((em.HasBuffer<Game.Rendering.MeshColor>(id)&&em.GetBuffer<Game.Rendering.MeshColor>(id,true).Length>256)||(em.HasBuffer<Game.Rendering.CustomMeshColor>(id)&&em.GetBuffer<Game.Rendering.CustomMeshColor>(id,true).Length>256))throw new NotSupportedException("Foliage colour budget exceeded.");
  }
 }
}

