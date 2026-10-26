using System;
using System.Linq;
using Game.Prefabs;
using Unity.Entities;

namespace MW.EditorTools.Modules.PropReplacer {
 public sealed partial class PropReplacerSystem {
  private bool surfaceRendererAvailable;
  private SurfacePlan surfaceRenderPending;
  private int surfaceRenderTicks;
  private bool surfaceRenderSent;
  private bool surfaceRenderUndo;
  private float surfaceRenderDeadline;
  internal void SetSurfaceRenderer(bool value){surfaceRendererAvailable=value;if(!value)surfaceRenderPending=null;}
  private void QueueSurfaceRender(SurfacePlan plan){surfaceRenderPending=plan;surfaceRenderTicks=0;surfaceRenderSent=false;surfaceRenderUndo=false;surfaceRenderDeadline=UnityEngine.Time.realtimeSinceStartup+10f;}
  private void CheckSurfaceRenderDeadline(){
   if(surfaceRenderPending==null||UnityEngine.Time.realtimeSinceStartup<=surfaceRenderDeadline)return;
   surfaceRenderPending=null;surfaceFailed=true;status="Surface material refresh did not run or finish. Reload without saving.";
   MW.EditorTools.Mod.Log.Error(new TimeoutException(status),"Surface render callback/completion deadline exceeded; further replacements blocked.");
  }
  internal void FlushSurfaceRender(){
   var plan=surfaceRenderPending;
   if(plan==null)return;
   if(!Enabled||context?.EditorAvailable!=true){surfaceRenderPending=null;return;}
   try {
    var em=World.EntityManager;
    if(++surfaceRenderTicks>180)throw new InvalidOperationException("Surface material refresh did not complete. Reload without saving.");
    if(!em.Exists(plan.To)||!em.HasComponent<RenderedAreaData>(plan.To))throw new InvalidOperationException("Replacement surface render data is unavailable.");
    int expected=em.GetComponentData<RenderedAreaData>(plan.To).m_BatchIndex;
    if(expected<0)return;
    foreach(var id in plan.Live)if(!em.Exists(id)||em.HasComponent<Game.Common.Deleted>(id)||!em.HasComponent<PrefabRef>(id)||em.GetComponentData<PrefabRef>(id).m_Prefab!=plan.To||!em.HasComponent<Game.Areas.Batch>(id))throw new InvalidOperationException("Surface changed before its material refresh could complete.");
    if(!surfaceRenderSent){
     var previous=string.Join(",",plan.Live.Select(id=>em.GetComponentData<Game.Areas.Batch>(id).m_BatchIndex));
     foreach(var id in plan.Live)if(!em.HasComponent<Game.Common.Updated>(id))em.AddComponent<Game.Common.Updated>(id);
     surfaceRenderSent=true;
     MW.EditorTools.Mod.Log.Info("Surface material refresh submitted before native area batching: targets="+plan.Live.Length+" previous batches="+previous+" expected="+expected);
     return;
    }
    // Invisible surfaces will pick up the new prefab when native culling makes
    // them visible. Visible surfaces must actually use the replacement batch.
    if(plan.Live.All(id=>{var b=em.GetComponentData<Game.Areas.Batch>(id);return b.m_VisibleCount==0||b.m_BatchIndex==expected;})){
     surfaceRenderPending=null;
     status=surfaceRenderUndo?"Surface replacement undone. Original surface material, boundary and settings restored.":"Surface replacement complete. Destination boundaries, settings and companion references preserved. Use Undo to restore it.";
     MW.EditorTools.Mod.Log.Info("Surface material refresh completed: retained="+plan.Live.Length+" expected batch="+expected);
    }
   }catch(Exception error){
    surfaceRenderPending=null;surfaceFailed=true;status=error.Message;
    MW.EditorTools.Mod.Log.Error(error,"Surface material refresh failed; further replacements blocked. Reload without saving.");
   }
  }
 }
}
