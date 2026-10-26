using System;
using Game.Tools;
using Unity.Jobs;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private bool applyBlockedReported;
        private static bool BeginApply(BulldozeToolSystem __instance,JobHandle __0,ref JobHandle __result,out bool __state) {
            __state=false;
            var a=active;
            if(a==null||!a.context.EditorAvailable||!ReferenceEquals(__instance.World,a.world))return true;
            if(!a.PatchesCompatible(true)){__result=__0;return !a.Busy;}
            if(a.Busy) {
                __result=__0;
                a.Status="Recovery is finishing; wait before another deletion.";
                if(!a.applyBlockedReported){a.applyBlockedReported=true;Mod.Log.Info("Bulldozer Apply deferred while recovery is pending; native deletion skipped.");}
                return false;
            }
            a.applyBlockedReported=false;
            __state=true;
            ObserveApply(__instance);
            return true;
        }
    }
}
