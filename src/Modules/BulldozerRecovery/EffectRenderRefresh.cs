using Game.Rendering;
using Game.Tools;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private Entity effectRenderPending;
        private void QueueEffectRenderRefresh(Entity id){effectRenderPending=id;}
        private void ClearEffectRenderRefresh(){effectRenderPending=Entity.Null;}
        private bool EffectRenderReady(Entity id)=>effectRenderPending!=id;
        private void InvalidateEffectHover(Entity id,Entity owner) {
            var tool=world.GetExistingSystemManaged<ToolSystem>()?.activeTool as DefaultToolSystem;
            if(tool==null)return;
            var hovered=(Entity)defaultRaycastEntity.GetValue(tool);
            if(hovered!=id&&hovered!=owner)return;
            defaultRaycastEntity.SetValue(tool,Entity.Null);
            Mod.Log.Info("Effect hover marker queued for native rebuild: target="+id+" hover="+hovered);
        }
        private static void BeforeEffectRendering(PreCullingSystem __instance) {
            __instance.World.GetExistingSystemManaged<MW.EditorTools.Modules.PropReplacer.PropReplacerSystem>()?.BeforeNativeFoliageRendering();
            var a=active;
            if(a==null||!ReferenceEquals(__instance.World,a.world)||a.effectRenderPending==Entity.Null)return;
            Guard(a.FlushEffectRenderRefresh);
        }
        private void FlushEffectRenderRefresh() {
            var id=effectRenderPending;var em=world.EntityManager;
            if(!context.EditorAvailable||!em.Exists(id)||em.HasComponent<Game.Common.Deleted>(id)){ClearEffectRenderRefresh();return;}
            // Fresh deletion restores must pass native object initialization first.
            if(em.HasComponent<Game.Common.Created>(id)||!em.HasComponent<CullingInfo>(id))return;
            // Send these at their native render consumer, after intervening
            // object updates/cleanup, once per restored target. Never hold the
            // tags on the entity or copy renderer allocation data.
            PutMain(id,new Game.Common.Updated());
            PutMain(id,new Game.Common.BatchesUpdated());
            PutMain(id,new Game.Common.EffectsUpdated());
            ClearEffectRenderRefresh();
            Mod.Log.Info("Effect render refresh submitted before native culling: "+id);
        }
    }
}
