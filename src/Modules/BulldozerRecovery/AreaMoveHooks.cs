using System;
using System.Linq;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private static void AreaMoveAllowed(ToolBaseSystem __instance,bool __result) {
            var a=active;if(a!=null&&__instance is AreaToolSystem&&ReferenceEquals(__instance.World,a.world)&&a.areaMoveInApply)a.areaMoveApplyAllowed=__result;
        }
        private static bool BeforeAreaRecreate(AreaToolSystem __instance,Entity __0) {
            var a=active;if(a==null||!ReferenceEquals(__instance.World,a.world)||!a.context.EditorAvailable)return true;
            if(__0==Entity.Null){if(!a.areaMoveInApply&&!a.areaMoveCommitted&&!a.areaMoveRestoring)a.CancelAreaMovePreview();return true;}
            if(a.Busy){a.Status="Wait for Undo to finish before moving an area.";return false;}
            a.BeginAreaMove(__0);return true;
        }
        private static bool BeforeAreaMoveApply(AreaToolSystem __instance,JobHandle __0,ref JobHandle __result,out bool __state) {
            __state=false;var a=active;if(a==null||!ReferenceEquals(__instance.World,a.world)||!a.context.EditorAvailable)return true;
            if(a.Busy){__result=__0;a.Status="Wait for Undo to finish before editing an area.";return false;}
            a.areaMoveHookFailed=false;
            try {
            a.areaMoveApplyState=Convert.ToInt32(a.areaMoveStateField.GetValue(__instance));
            var points=(NativeList<ControlPoint>)a.areaMovePointsField.GetValue(__instance);
            var targets=points.IsCreated?points.AsArray().ToArray().Select(p=>p.m_OriginalEntity).Where(id=>id!=Entity.Null).Distinct().ToArray():Array.Empty<Entity>();
            if(__instance.actualMode==AreaToolSystem.Mode.Edit&&__instance.recreate==Entity.Null&&a.areaMoveApplyState==0&&targets.Length==1&&(!a.areaMovePreview||a.areaMoveCandidate==null||a.areaMoveCandidate.Target!=targets[0])&&!a.world.EntityManager.HasComponent<Temp>(targets[0]))a.BeginAreaMove(targets[0]);
            a.areaMoveApplyTarget=a.areaMoveCandidate!=null&&(__instance.recreate==a.areaMoveCandidate.Target||(targets.Length==1&&targets[0]==a.areaMoveCandidate.Target));
            // Native snapping can replace the cursor's original entity. The
            // completed preview, rather than the release cursor, proves identity.
            if(a.areaMoveCandidate!=null&&(a.areaMoveApplyState==1||a.areaMoveApplyState==2||a.areaMoveApplyState==3))a.areaMoveApplyTarget=a.AreaMovePreviewTargets(__instance,a.areaMoveCandidate.Target);
            a.areaMoveApplyAllowed=false;a.areaMoveInApply=true;__state=true;return true;
            }catch(Exception error){a.areaMoveHookFailed=true;a.areaMoveInApply=true;__state=true;Mod.Log.Warn("Area move capture unavailable; native edit preserved: "+error.Message);return true;}
        }
        private static void AfterAreaMoveApply(AreaToolSystem __instance,bool __state) {
            FinishAreaNodeApply(__instance,__state,false);
        }
        private static void FinishAreaNodeApply(AreaToolSystem __instance,bool observed,bool removing) {
            var a=active;if(!observed||a==null||!ReferenceEquals(__instance.World,a.world))return;
            a.areaMoveInApply=false;
            if(__instance.applyMode!=ApplyMode.Apply)return;
            if(a.areaMoveHookFailed){a.ClearMovePrevious();a.ClearMove();a.CancelAreaMovePreview();a.Status="Previous Undo cleared after unavailable area capture.";return;}
            if(!a.areaMoveApplyAllowed)return;
            bool commit=AreaNodeEditPolicy.Commit(a.areaMoveApplyAllowed,true,__instance.actualMode==AreaToolSystem.Mode.Edit,a.areaMoveApplyState,removing);
            if(!commit||!a.areaMoveApplyTarget||a.areaMoveCandidate==null){a.ClearMovePrevious();a.ClearMove();a.CancelAreaMovePreview();a.Status="Previous Undo cleared by an unsupported area edit.";return;}
            a.areaMoveCommitted=true;a.areaMoveFrame=UnityEngine.Time.frameCount;a.areaMoveStable=0;a.areaMoveLastFrame=-1;a.Status="Waiting for area move to finish…";
        }
        private static bool BeforeAreaMoveCancel(AreaToolSystem __instance,JobHandle __0,ref JobHandle __result,out bool __state) {
            __state=false;var a=active;if(a==null||!ReferenceEquals(__instance.World,a.world)||!a.context.EditorAvailable)return true;
            if(a.Busy){__result=__0;return false;}
            if(a.areaMoveInApply)return true;
            // Native right-click starts/removes nodes through Cancel, while
            // cancelling a Create/Modify preview must preserve the prior slot.
            try {
                int state=Convert.ToInt32(a.areaMoveStateField.GetValue(__instance));
                return state==0||state==3?BeforeAreaMoveApply(__instance,__0,ref __result,out __state):true;
            }catch(Exception error){a.areaMoveHookFailed=true;a.areaMoveInApply=true;__state=true;Mod.Log.Warn("Area node deletion capture unavailable; native edit preserved: "+error.Message);return true;}
        }
        private static void AfterAreaMoveCancel(AreaToolSystem __instance,bool __state=false) {
            FinishAreaNodeApply(__instance,__state,true);
            var a=active;if(a==null||!ReferenceEquals(__instance.World,a.world)||a.areaMoveInApply||a.areaMoveCommitted||a.areaMoveRestoring)return;
            try{if(Convert.ToInt32(a.areaMoveStateField.GetValue(__instance))==0)a.CancelAreaMovePreview();}
            catch(Exception error){a.CancelAreaMovePreview();Mod.Log.Warn("Area node preview cleanup unavailable; native edit preserved: "+error.Message);}
        }
    }
}
