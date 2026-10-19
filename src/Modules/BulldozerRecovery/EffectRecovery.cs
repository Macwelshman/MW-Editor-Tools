using System;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using MW.EditorTools.History;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private sealed class EffectEntry {
            internal bool Move;
            internal Entity Live;
            internal EffectImage Before,After;
            internal NetworkEntry BoundBefore=null,BoundAfter;
            internal IsolatedEffectOwner IsolatedBefore,IsolatedAfter;
            internal bool Bound=>BoundBefore!=null||IsolatedBefore!=null;
        }
        private readonly LastDeletion<EffectEntry> effectHistory=new LastDeletion<EffectEntry>();
        private EffectEntry effectPending;
        private bool effectMovePreview,effectCommitted,effectRestoring,effectInDelete,effectAccepted,effectDisabled;
        private int effectFrame,effectStable,effectLastFrame=-1;
        private string effectUndoBlockReason;
        private bool EffectUndoBlocked(string reason){if(reason!=effectUndoBlockReason){effectUndoBlockReason=reason;Mod.Log.Info("Effect Undo preflight blocked: "+reason);}return false;}
        private bool EffectBusy=>effectPending!=null&&(effectCommitted||effectRestoring||effectInDelete);
        private void ClearEffects(){ClearEffectRenderRefresh();effectHistory.Clear();effectPending=null;effectMovePreview=effectCommitted=effectRestoring=effectInDelete=false;effectStable=0;}
        private void CancelEffectPreview(){if(effectMovePreview&&!effectCommitted&&!effectRestoring){effectPending=null;effectMovePreview=false;movePreviewEntered=false;}}
        private void FailEffect(string reason){effectDisabled|=effectRestoring;if(effectCommitted||effectRestoring){ClearMovePrevious();ClearMove();}ClearEffects();Status=reason;Mod.Log.Warn(reason);}
        private EffectEntry NewEffectEntry(Entity id,bool move) {
            if(effectDisabled||AutomaticCaptureAllowed?.Invoke()!=true||context.DialogBusy)throw new NotSupportedException("Enable recovery and close dialogs; reload after a recovery error.");
            var e=new EffectEntry{Move=move,Live=id,Before=CaptureEffect(id)};Mod.Log.Info("Effect capture "+(move?"move":"deletion")+": "+EffectDetails(e.Before));
            if(e.Before.Owner!=Entity.Null){e.IsolatedBefore=CaptureIsolatedEffectOwner(e.Before.Owner);
                if(!IsolatedSources(e.IsolatedBefore))throw new NotSupportedException("Isolated effect source does not uniquely match native local placements.");}
            return e;
        }
        private bool TryBeginEffectMove(Entity id) {
            CancelEffectPreview();if(!IsEffectContainer(id))return false;
            moveCandidate=null;moveCommitted=moveRestoring=false;movePreviewEntered=true;movePreviewTarget=id;effectMovePreview=true;effectPending=null;
            try{effectPending=NewEffectEntry(id,true);Status="Capturing original effect placement.";Mod.Log.Info(Status+" effect="+effectPending.Before.Value.Container.name);}
            catch(Exception error){Status="Effect move Undo unavailable: "+error.Message;Mod.Log.Info(Status);}return true;
        }
        private bool FinishEffectMove(ObjectToolSystem tool) {
            if(!effectMovePreview)return false;
            if(!objectAccepted||tool.applyMode!=ApplyMode.Apply)return true;
            if(!objectMoveApplying||effectPending==null){ClearMovePrevious();ClearMove();ClearEffects();Status="Previous Undo cleared after unsupported effect move.";return true;}
            effectMovePreview=false;movePreviewEntered=false;effectCommitted=true;effectFrame=Time.frameCount;effectStable=0;effectLastFrame=-1;Status="Waiting for effect move to finish…";return true;
        }
        private bool BeginEffectDeletion(BulldozeToolSystem tool) {
            var points=(NativeList<ControlPoint>)controlPoints.GetValue(tool);if(!points.IsCreated)return false;
            var ids=points.AsArray().ToArray().Select(p=>p.m_OriginalEntity).Where(id=>id!=Entity.Null).Distinct().ToArray();
            if(!ids.Any(IsEffectContainer))return false;
            try {
                if(ids.Length!=1)throw new NotSupportedException("Delete one effect at a time.");
                var e=NewEffectEntry(ids[0],false);
                if(e.Bound?(tool.actualMode!=BulldozeToolSystem.Mode.SubElements&&tool.actualMode!=BulldozeToolSystem.Mode.Everything):(tool.actualMode!=BulldozeToolSystem.Mode.MainElements&&tool.actualMode!=BulldozeToolSystem.Mode.Everything))throw new NotSupportedException("Use the matching main/sub-element bulldozer mode.");
                effectPending=e;effectInDelete=true;effectAccepted=false;effectFrame=Time.frameCount;effectStable=0;effectLastFrame=-1;Status="Capturing effect deletion…";Mod.Log.Info(Status+" effect="+e.Before.Value.Container.name+" bound="+e.Bound);
            }catch(Exception error){FailEffect("Effect deletion Undo unavailable: "+error.Message);}return true;
        }
        private void EndEffectDeletion(){if(!effectInDelete)return;effectInDelete=false;if(effectAccepted)effectCommitted=true;else{effectPending=null;Status="Native effect deletion denied; no Undo entry.";}}
        private bool CanEffectUndo {
            get{try{var e=effectHistory.Latest;if(e==null)return false;
                if(effectDisabled||Busy||MovePreview||AreaMovePreview||context.DialogBusy||!context.EditorAvailable||AutomaticCaptureAllowed?.Invoke()!=true)return EffectUndoBlocked("context disabled="+effectDisabled+" busy="+Busy+" preview="+MovePreview+" areaPreview="+AreaMovePreview+" dialog="+context.DialogBusy+" editor="+context.EditorAvailable);
                if(!e.Before.Valid)return EffectUndoBlocked("before-image references differ: "+EffectReferenceDifference(e.Before));
                if(e.IsolatedBefore!=null&&!CanIsolatedEffectUndo(e))return false;
                if(e.BoundBefore!=null&&(!NetworkOwnerValid(e.BoundAfter)||!e.BoundAfter.Before.Matches(e.BoundAfter.Asset)||!NetworkCompanionsMatch(e.BoundAfter,true)||!e.BoundAfter.Network.TryMap(CaptureAreaNetwork(e.Before.Owner,false,true),true,out _)))return false;
                if(e.Move){if(!e.After.Valid)return EffectUndoBlocked("after-image references differ: "+EffectReferenceDifference(e.After));var current=CaptureEffect(e.Live);if(!SameEffect(e.After,current))return EffectUndoBlocked("moved target differs. Recorded: "+EffectDetails(e.After)+"; Current: "+EffectDetails(current));}
                else if(world.EntityManager.Exists(e.Before.Original))return EffectUndoBlocked("deleted original still exists");
                effectUndoBlockReason=null;return true;
            }catch(Exception error){return EffectUndoBlocked(error.GetType().Name+": "+error.Message);}}
        }
        private void UndoEffect() {
            if(!CanEffectUndo)return;var e=effectHistory.Latest;
            if(e.BoundBefore!=null){e.BoundBefore.EffectEntry=e;StartBoundRestoration(e.BoundBefore);return;}
            effectPending=e;effectRestoring=true;effectFrame=Time.frameCount;effectStable=0;effectLastFrame=-1;
            try{if(e.IsolatedBefore!=null)RestoreIsolatedEffect(e);
                else if(e.Move){if(!SameEffect(e.After,CaptureEffect(e.Live)))throw new NotSupportedException("Effect changed before Undo.");WriteEffectValues(e.Live,e.Before);}
                else CreateStandaloneEffect(e);if(e.Move)InvalidateEffectHover(e.Live,e.Before.Owner);Status="Restoring effect…";}
            catch(Exception error){if(!e.Move&&e.Live!=e.Before.Original&&world.EntityManager.Exists(e.Live))world.EntityManager.DestroyEntity(e.Live);Mod.Log.Warn("Effect Undo exception: "+error);FailEffect("Effect Undo failed; reload without saving: "+error.Message);}
        }
        private void TickEffects() {
            if(!context.EditorAvailable){ClearEffects();return;}
            if(effectMovePreview){if(!MovePreview)CancelEffectPreview();return;}
            if(effectPending==null||effectInDelete)return;var e=effectPending;bool valid=false;string reason="effect placement/source or companion state differs";
            try {
                if(!e.Before.Valid)throw new NotSupportedException("Effect references changed.");
                if(effectRestoring){valid=EffectRenderReady(e.Live)&&(e.IsolatedBefore!=null?IsolatedEffectRestored(e):EffectSettled(e.Live)&&SameEffect(e.Before,CaptureEffect(e.Live)));if(!valid&&e.IsolatedBefore!=null)reason=isolatedEffectReason;}
                else if(effectCommitted){
                    EffectImage actual=null;if(e.Move){actual=CaptureEffect(e.Live);if(!EffectEditPolicy.Move(e.Before.Value,actual.Value,e.Bound)||e.Before.Owner!=actual.Owner||e.Before.HasEnabledBuffer!=actual.HasEnabledBuffer||e.Before.HasSeed!=actual.HasSeed||e.Before.HasSurface!=actual.HasSurface||e.Before.HasDecoration!=actual.HasDecoration)throw new NotSupportedException("Effect move changed shape/ownership/parameters. Before: "+EffectDetails(e.Before)+"; After: "+EffectDetails(actual));}
                    else if(world.EntityManager.Exists(e.Before.Original))throw new NotSupportedException("Native effect deletion pending.");
                    var owner=e.BoundBefore!=null?CaptureMoveOwner(e.Before.Owner,true):null;
                    var isolated=e.IsolatedBefore!=null?CaptureIsolatedEffectOwner(e.Before.Owner):null;
                    if(e.BoundBefore!=null&&!EffectOwnerAfter(e,owner,actual))throw new NotSupportedException("Effect owner source, retained companions or references changed.");
                    if(isolated!=null&&!IsolatedAfter(e,isolated,actual))throw new NotSupportedException("Isolated effect source, child witnesses or retained effects changed.");
                    if(e.Move&&SameEffect(e.Before,actual)&&(!e.Bound||(isolated!=null?e.IsolatedBefore.Source.Matches(isolated.Asset):e.BoundBefore.Before.Matches(owner.Asset)))){effectPending=null;effectCommitted=false;Status="Effect unchanged; previous Undo retained.";return;}
                    if(e.Move&&(e.After==null||!SameEffect(e.After,actual))||(owner!=null&&(e.BoundAfter==null||!e.BoundAfter.Before.Matches(owner.Asset)))||(isolated!=null&&(e.IsolatedAfter==null||!e.IsolatedAfter.Source.Matches(isolated.Asset)||!IsolatedChildrenSame(e.IsolatedAfter,isolated)))){e.After=actual;e.BoundAfter=owner;e.IsolatedAfter=isolated;effectStable=0;}
                    valid=true;
                }
            }catch(NotSupportedException error){reason=error.Message;}
            catch(Exception error){FailEffect("Effect recovery failed; reload without saving: "+error.Message);return;}
            if(!valid)effectStable=0;else if(effectLastFrame!=Time.frameCount){effectLastFrame=Time.frameCount;effectStable++;}
            // Native edit application can update unrelated child poses after
            // the target and source first settle. Keep observing the complete
            // after-image; once ready, the exact witness guard remains strict.
            if(effectStable>=(e.IsolatedBefore!=null?30:3)){
                if(effectRestoring){if(!effectHistory.Undo(v=>ReferenceEquals(v,e))){FailEffect("Effect recovery slot changed; reload without saving.");return;}Status="Undid "+(e.IsolatedBefore!=null?"isolated bound ":"")+"effect "+(e.Move?"move":"deletion")+"; placement and values verified.";}
                else {ClearMovePrevious();ClearMove();if(e.BoundBefore!=null)PrepareEffectBoundEntry(e);effectHistory.Record(e);effectUndoBlockReason=null;Status="Effect "+(e.Move?"move":"deletion")+" ready to Undo.";}
                Mod.Log.Info(Status);effectPending=null;effectCommitted=effectRestoring=false;effectStable=0;
            }else if(Time.frameCount-effectFrame>120)FailEffect((effectRestoring?"Effect Undo did not validate; reload without saving: ":"Effect edit did not validate; no Undo entry: ")+reason);
        }
    }
}
