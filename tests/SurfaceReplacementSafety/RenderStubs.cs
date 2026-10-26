using System;using System.Collections.Generic;using Unity.Entities;
namespace Unity.Jobs { public struct JobHandle {public void Complete(){}} }
namespace Unity.Entities {public struct ComponentLookup<T>{internal EntityManager em;public T Get(Entity e)=>em.GetComponentData<T>(e);public void Put(Entity e,T value)=>em.SetComponentData(e,value);}}
namespace Unity.Collections {
 public struct NativeQueue<T>:IDisposable {internal List<T> values;public NativeQueue(Allocator a){values=new();}public NativeQueue<T> AsParallelWriter()=>this;public void Enqueue(T item)=>values.Add(item);public void Dispose(){} }
 public sealed class NativeList<T>:List<T>{public int Length=>Count;}
}
namespace Colossal.Collections {public struct NativeHeapBlock{public uint Begin;}}
namespace Game.Rendering {
 using Unity.Collections;
 public sealed class AreaBatchSystem {
  internal struct Meta {public Entity m_Entity;public int m_StartIndex;}
  internal struct NativeBatchData {public NativeList<Meta> m_AreaMetaData;}
  internal Unity.Jobs.JobHandle m_DataDependencies;
  internal NativeList<int> m_TriangleMetaData=new(){0,0,0};
  internal NativeList<NativeBatchData> m_NativeBatchData=new();
  internal int m_AreaBufferAllocator;
  internal bool FailRelease;
  public int GetBatchCount()=>m_NativeBatchData.Count;
  internal void Allocated(Entity e,int batch){while(m_NativeBatchData.Count<=batch)m_NativeBatchData.Add(new(){m_AreaMetaData=new()});m_NativeBatchData[batch].m_AreaMetaData.Add(new(){m_Entity=e});}
  private struct CullingAction {public Game.Areas.AreaSearchItem m_Item;}
  private struct AllocationAction {public Entity m_Entity;}
  private struct CullingActionJob {
   public ComponentLookup<Game.Areas.Batch> m_BatchData;
   public NativeList<int> m_TriangleMetaData;
   public NativeQueue<AllocationAction> m_AllocationActions;
   private void FailedCulling(CullingAction action){var b=m_BatchData.Get(action.m_Item.m_Area);if(b.m_AllocatedSize==0)return; if(b.m_VisibleCount>0){b.m_VisibleCount=0;m_BatchData.Put(action.m_Item.m_Area,b);m_AllocationActions.Enqueue(new(){m_Entity=action.m_Item.m_Area});}}
  }
  private struct BatchAllocationJob {
   public ComponentLookup<Game.Areas.Batch> m_BatchData;
   public NativeQueue<AllocationAction> m_AllocationActions;
   public NativeList<NativeBatchData> m_NativeBatchData;
   public int m_AreaBufferAllocator;
   private void Execute(){foreach(var a in m_AllocationActions.values){var b=m_BatchData.Get(a.m_Entity);var metas=m_NativeBatchData[b.m_BatchIndex].m_AreaMetaData;if(metas[b.m_MetaIndex].m_Entity!=a.m_Entity)throw new Exception("wrong material bucket");metas.RemoveAt(b.m_MetaIndex);b.m_AllocatedSize=0;b.m_BatchAllocation=default;m_BatchData.Put(a.m_Entity,b);m_AreaBufferAllocator++;}m_AllocationActions.values.Clear();}
  }
 }
}
