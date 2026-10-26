using System;
using System.Reflection;
using Game.Rendering;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace MW.EditorTools.Modules.PropReplacer {
 public sealed partial class PropReplacerSystem {
  // Run the verified native release routines with a private queue. Never transplant
  // a live allocation into another material batch or edit its allocator metadata.
  private void ReleaseAreaRender(SurfacePlan plan) {
   if(!plan.Rendered)return;
   VerifyAreaRenderApi();
   var manager=World.GetExistingSystemManaged<AreaBatchSystem>();
   if(manager==null)throw new NotSupportedException("Area renderer is unavailable.");
   var type=typeof(AreaBatchSystem);
   VerifySurfaceApi(World.GetExistingSystemManaged<PrefabSystem>(),context.Asset);
   const BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
   var releaseType=type.GetNestedType("CullingActionJob",BindingFlags.Public|BindingFlags.NonPublic);
   var allocationType=type.GetNestedType("BatchAllocationJob",BindingFlags.Public|BindingFlags.NonPublic);
   var actionType=type.GetNestedType("CullingAction",BindingFlags.Public|BindingFlags.NonPublic);
   var allocationAction=type.GetNestedType("AllocationAction",BindingFlags.Public|BindingFlags.NonPublic);
   if(releaseType==null||allocationType==null||actionType==null||allocationAction==null)throw new NotSupportedException("Area allocation routines unavailable.");
   var release=Activator.CreateInstance(releaseType);var allocate=Activator.CreateInstance(allocationType);
   var failed=releaseType.GetMethod("FailedCulling",flags);var execute=allocationType.GetMethod("Execute",flags);
   var queueType=typeof(NativeQueue<>).MakeGenericType(allocationAction);
   var queue=typeof(PropReplacerSystem).GetMethod(nameof(NewAreaQueue),BindingFlags.Static|BindingFlags.NonPublic).MakeGenericMethod(allocationAction).Invoke(null,null);
   try {
    var dependencies=type.GetField("m_DataDependencies",flags)??throw new MissingFieldException(type.Name,"m_DataDependencies");
    ((JobHandle)dependencies.GetValue(manager)).Complete();
    EntityManager.CompleteAllTrackedJobs();
    var lookup=GetComponentLookup<Game.Areas.Batch>(false);
    releaseType.GetField("m_BatchData",flags).SetValue(release,lookup);
    releaseType.GetField("m_TriangleMetaData",flags).SetValue(release,type.GetField("m_TriangleMetaData",flags).GetValue(manager));
    releaseType.GetField("m_AllocationActions",flags).SetValue(release,queueType.GetMethod("AsParallelWriter").Invoke(queue,null));
    foreach(var field in allocationType.GetFields(flags)) {
     if(field.Name=="m_BatchData")field.SetValue(allocate,lookup);
     else if(field.Name=="m_AllocationActions")field.SetValue(allocate,queue);
     else field.SetValue(allocate,(type.GetField(field.Name,flags)??throw new MissingFieldException(type.Name,field.Name)).GetValue(manager));
    }
    if(failed==null||execute==null)throw new MissingMethodException("Native area allocation methods unavailable.");
    foreach(var id in plan.Live) {
     var batch=EntityManager.GetComponentData<Game.Areas.Batch>(id);
     if(batch.m_AllocatedSize==0)continue;
     if(batch.m_BatchIndex<0||batch.m_BatchIndex>=manager.GetBatchCount())throw new InvalidOperationException("Area material allocation is unavailable.");
     ValidateAreaAllocation(type,manager,batch,id,flags);
     // Native FailedCulling only queues a release for visible allocations.
     // Refuse inconsistent hidden allocations rather than resetting bookkeeping.
     if(batch.m_VisibleCount<=0||batch.m_VisibleCount>batch.m_AllocatedSize)throw new InvalidOperationException("Area allocation has not settled. Move the camera to the area and try again.");
    }
    bool changed=false;
    try {
    foreach(var id in plan.Live) {
     changed=true;
     var action=Activator.CreateInstance(actionType);
     actionType.GetField("m_Item",flags).SetValue(action,new Game.Areas.AreaSearchItem(id,-1));
     failed.Invoke(release,new[]{action});
    }
    try {execute.Invoke(allocate,null);}
    finally {type.GetField("m_AreaBufferAllocator",flags).SetValue(manager,allocationType.GetField("m_AreaBufferAllocator",flags).GetValue(allocate));}
    foreach(var id in plan.Live)if(EntityManager.GetComponentData<Game.Areas.Batch>(id).m_AllocatedSize!=0)throw new InvalidOperationException("Native area allocation release did not complete.");
    }catch{if(changed)surfaceFailed=true;throw;}
    MW.EditorTools.Mod.Log.Info("Area render allocation released under original material: retained="+plan.Live.Length);
   } finally {((IDisposable)queue).Dispose();}
  }
  private static object NewAreaQueue<T>() where T:unmanaged => new NativeQueue<T>(Allocator.TempJob);
  private static object Item(object list,int index) => list.GetType().GetMethod("get_Item").Invoke(list,new object[]{index});
  private static int Length(object list) => (int)list.GetType().GetProperty("Length").GetValue(list);
  private static void ValidateAreaAllocation(Type type,object manager,Game.Areas.Batch batch,Entity id,BindingFlags flags) {
   var batches=type.GetField("m_NativeBatchData",flags).GetValue(manager);
   if(batch.m_BatchIndex>=Length(batches))throw new InvalidOperationException("Area batch index is unavailable.");
   var data=Item(batches,batch.m_BatchIndex);
   var entries=data.GetType().GetField("m_AreaMetaData",flags).GetValue(data);
   if(batch.m_MetaIndex<0||batch.m_MetaIndex>=Length(entries))throw new InvalidOperationException("Area allocation metadata is unavailable.");
   var entry=Item(entries,batch.m_MetaIndex);
   if(!id.Equals(entry.GetType().GetField("m_Entity",flags).GetValue(entry)))throw new InvalidOperationException("Area allocation metadata belongs to another entity.");
   var triangles=type.GetField("m_TriangleMetaData",flags).GetValue(manager);
   long begin=batch.m_BatchAllocation.Begin;
   if(begin!=(int)entry.GetType().GetField("m_StartIndex",flags).GetValue(entry))throw new InvalidOperationException("Area allocation start differs from its material metadata.");
   if(batch.m_AllocatedSize<0||begin+batch.m_AllocatedSize>Length(triangles))throw new InvalidOperationException("Area triangle allocation is out of range.");
  }
 }
}
