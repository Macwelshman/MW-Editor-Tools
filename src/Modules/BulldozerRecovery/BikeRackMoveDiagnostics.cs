using System;
using UnityEngine;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private MoveEntry observedBikeMove;
        private RecoveryTiming bikeTiming;
        private int bikeObservation;

        private IDisposable ObserveBikeMove(MoveEntry entry,string phase){
            try{
                if(entry==null||!entry.Marker||!IsBikeRackMoveGroup(entry.Before))return null;
                // While a new move is being validated, the old shared Undo entry
                // can still be queried. It must not displace the current probe.
                if(phase=="readiness"&&observedBikeMove!=null&&!ReferenceEquals(observedBikeMove,entry))return null;
                if(!ReferenceEquals(observedBikeMove,entry)){
                    FinishBikeObservation("superseded");observedBikeMove=entry;bikeTiming=new RecoveryTiming();bikeObservation++;
                }
                return bikeTiming.Measure(phase,Time.unscaledDeltaTime*1000f,Time.frameCount);
            }catch{return null;} // Diagnostic failures never change recovery behavior.
        }
        private IDisposable ObserveBikeRestore(NetworkEntry entry,string phase){
            try{return bikeTiming!=null&&ReferenceEquals(observedBikeMove?.BoundBefore,entry)?bikeTiming.Measure(phase,Time.unscaledDeltaTime*1000f,Time.frameCount):null;}
            catch{return null;}
        }
        private T TimeBikeRestore<T>(NetworkEntry entry,string phase,Func<T> work){using(ObserveBikeRestore(entry,phase))return work();}
        private void BeginBikeUndo(NetworkEntry entry){try{if(ReferenceEquals(observedBikeMove?.BoundBefore,entry))bikeTiming?.BeginUndo(Time.frameCount);}catch{}}
        private void CancelBikeObservation(MoveEntry entry){if(ReferenceEquals(observedBikeMove,entry))FinishBikeObservation("cancelled or unchanged");}
        private void BikeMismatch(MoveEntry entry,string reason){
            try{if(ReferenceEquals(observedBikeMove,entry)&&bikeTiming?.ObserveMismatch(reason)==true)
                Mod.Log.Info("Bike rack observation #"+bikeObservation+" first validation mismatch frame="+Time.frameCount+" target="+entry.Target+"; "+(reason.Length>6000?reason.Substring(0,6000):reason));}
            catch{}
        }
        private void FinishBikeObservation(string outcome,NetworkEntry entry=null){
            if(entry!=null&&!ReferenceEquals(observedBikeMove?.BoundBefore,entry))return;
            try{if(bikeTiming!=null)Mod.Log.Info("Bike rack observation #"+bikeObservation+" outcome="+outcome+" target="+observedBikeMove.Target+" roots="+(observedBikeMove.BoundBefore?.Props.Length??0)+"; "+bikeTiming.Summary(Time.frameCount));}
            catch{}
            finally{bikeTiming=null;observedBikeMove=null;}
        }
    }
}
