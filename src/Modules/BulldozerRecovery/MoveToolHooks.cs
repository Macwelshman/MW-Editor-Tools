using System;
using System.Linq;
using Game.Tools;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private static void MoveAllowed(ObjectToolSystem __instance,bool __result) {
            var a=active;if(a!=null&&a.PatchesCompatible()&&ReferenceEquals(__instance.World,a.world)&&a.objectInApply)a.objectAccepted=__result;
        }
        private static bool BeforeMoveStart(ObjectToolSystem __instance,Entity __0) {
            var a=active;if(a==null||!ReferenceEquals(__instance.World,a.world)||!a.context.EditorAvailable)return true;
            if(!a.PatchesCompatible(true))return !a.Busy;
            if(a.Busy){a.Status="Wait for Undo to finish before moving an item.";return false;}
            if(a.TryBeginEffectMove(__0))return true;
            a.moveCandidate=null;a.moveCommitted=a.moveRestoring=false;
            a.movePreviewEntered=true;a.movePreviewTarget=__0;
            if(a.AutomaticCaptureAllowed?.Invoke()!=true||a.moveDisabled||a.context.DialogBusy)return true;
            try {
                var group=a.CaptureMoveGroup(__0);if(!a.MoveGroupSettled(group))throw new NotSupportedException("Wait for prop initialization before moving it.");
                var entry=new MoveEntry{Target=__0,Before=group,Marker=IsMarkerMoveGroup(group),References=GroupReferences(group).Distinct().ToDictionary(p=>p,Capture)};
                if(a.world.EntityManager.HasComponent<Game.Common.Owner>(__0))entry.BoundBefore=a.CaptureMoveOwner(group[0].Owner);
                if(entry.References.Values.Sum(p=>p.Weight)>250000||entry.References.Any(p=>!NestedReferenceMatches(p.Key,p.Value)))throw new NotSupportedException("Move references changed or exceed budget.");
                a.moveCandidate=entry;a.Status="Move Undo is capturing the original placement.";Mod.Log.Info(a.Status+" prefab="+group[0].Prefab.name);
            }catch(Exception error){a.Status="Move Undo unavailable: "+error.Message;Mod.Log.Info(a.Status);}
            return true;
        }
        private static bool BeforeObjectApply(ObjectToolSystem __instance,JobHandle __0,ref JobHandle __result,out bool __state) {
            __state=false;var a=active;if(a==null||!ReferenceEquals(__instance.World,a.world)||!a.context.EditorAvailable)return true;
            if(!a.PatchesCompatible(true)){__result=__0;return !a.Busy;}
            if(a.Busy){__result=__0;a.Status="Wait for the current Undo operation.";return false;}
            a.objectInApply=true;a.objectAccepted=false;a.objectMoveApplying=__instance.actualMode==ObjectToolSystem.Mode.Move&&a.MoveMatchesTool(__instance);__state=true;return true;
        }
        private static void AfterObjectApply(ObjectToolSystem __instance,bool __state) {
            var a=active;if(!__state||a==null||!ReferenceEquals(__instance.World,a.world))return;
            if(!a.PatchesCompatible())return;
            a.objectInApply=false;
            if(a.FinishEffectMove(__instance))return;
            var decision=PropMovePolicy.Commit(a.objectAccepted,__instance.applyMode==ApplyMode.Apply,a.objectMoveApplying,a.moveCandidate!=null);
            if(decision==PropMovePolicy.CommitAction.Ignore)return;
            if(decision==PropMovePolicy.CommitAction.Clear){a.ClearMovePrevious();a.ClearMove();a.Status="Previous Undo cleared by an unsupported placement or move.";return;}
            a.moveCommitted=true;a.moveFrame=Time.frameCount;a.moveStable=0;a.moveLastFrame=-1;a.Status="Waiting for move to finish…";
        }
    }
}
