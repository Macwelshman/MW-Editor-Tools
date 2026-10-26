using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine { static class Time { internal static int frameCount; internal static float unscaledDeltaTime=.016f; } }
namespace MW.EditorTools {
    static class Mod { internal static class Log {
        internal static readonly List<string> Messages=new List<string>();internal static bool Fail;
        internal static void Info(string text){if(Fail)throw new InvalidOperationException("logger unavailable");Messages.Add(text);}
    } }
}
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private sealed class Node {internal bool Bike;}
        private sealed class NetworkEntry {internal object[][] Props=new[]{new object[]{new object()}};}
        private sealed class MoveEntry {internal bool Marker;internal Node[] Before;internal NetworkEntry BoundBefore;internal int Target;}
        private static bool IsBikeRackMoveGroup(Node[] group)=>group!=null&&group.Length==1&&group[0].Bike;
        internal static int Run(){
            int count=0;void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;}
            var trace=new BulldozerTrace();var source=new NetworkEntry();var entry=new MoveEntry{Marker=true,Before=new[]{new Node{Bike=true}},BoundBefore=source,Target=20};
            var originalRows=source.Props;var ordinary=new MoveEntry{Before=new[]{new Node()},BoundBefore=new NetworkEntry(),Target=21};
            Check(trace.ObserveBikeMove(null,"readiness")==null&&trace.ObserveBikeMove(ordinary,"readiness")==null&&trace.bikeTiming==null,"ordinary and absent moves do not create observers");
            using(trace.ObserveBikeMove(entry,"capture")){}
            Check(ReferenceEquals(trace.observedBikeMove,entry)&&trace.bikeTiming!=null,"rack observer follows exact captured entry");
            var observer=trace.bikeTiming;
            using(trace.ObserveBikeMove(entry,"readiness")){}
            Check(ReferenceEquals(trace.bikeTiming,observer),"repeated readiness observes same cycle without recapturing it");
            Check(trace.ObserveBikeRestore(ordinary.BoundBefore,"prepare")==null,"unrelated restoration cannot enter rack trace");
            trace.CancelBikeObservation(ordinary);Check(ReferenceEquals(trace.bikeTiming,observer),"cancelled unrelated move preserves observation of existing Undo slot");
            trace.BeginBikeUndo(source);UnityEngine.Time.frameCount+=3;
            using(trace.ObserveBikeRestore(source,"restore-tick")){}
            var value=new object();Check(ReferenceEquals(trace.TimeBikeRestore(source,"work",()=>value),value),"observed operation preserves exact returned object identity");
            bool refused=false;try{trace.TimeBikeRestore<object>(source,"failure",()=>throw new InvalidOperationException("native refusal"));}catch(InvalidOperationException error){refused=error.Message=="native refusal";}
            Check(refused,"observation never swallows or substitutes native operation failure");
            foreach(var reason in new[]{"curve","curve","source","schema","extra"})trace.BikeMismatch(entry,reason);
            Check(Mod.Log.Messages.Count(m=>m.Contains("first validation mismatch"))==3,"failure evidence bounded and deduplicated");
            Check(ReferenceEquals(source.Props,originalRows)&&entry.Target==20,"measurement leaves companion source identities and target unchanged");
            trace.FinishBikeObservation("wrong owner",ordinary.BoundBefore);Check(ReferenceEquals(trace.bikeTiming,observer),"unrelated completion cannot consume observation");
            trace.FinishBikeObservation("completed",source);
            Check(trace.bikeTiming==null&&trace.observedBikeMove==null,"completed observation releases captured graph references");
            Check(Mod.Log.Messages.Any(m=>m.Contains("outcome=completed")&&m.Contains("undoFrames=3")&&m.Contains("roots=1")),"completion includes deferred frames and retained-root count");
            using(trace.ObserveBikeMove(entry,"readiness")){}
            Mod.Log.Fail=true;trace.FinishBikeObservation("cleared");Mod.Log.Fail=false;
            Check(trace.bikeTiming==null&&trace.observedBikeMove==null,"logger failure cannot prevent observer cleanup");
            using(trace.ObserveBikeMove(entry,"readiness")){}
            trace.CancelBikeObservation(entry);Check(trace.bikeTiming==null&&trace.observedBikeMove==null,"cancelled rack observation releases graph references");
            using(trace.ObserveBikeMove(entry,"readiness")){}
            var pending=new MoveEntry{Marker=true,Before=entry.Before,BoundBefore=new NetworkEntry(),Target=entry.Target};
            using(trace.ObserveBikeMove(pending,"capture-validation-tick")){}
            var pendingObserver=trace.bikeTiming;var serial=trace.bikeObservation;
            for(int i=0;i<4;i++){
                Check(trace.ObserveBikeMove(entry,"readiness")==null,"old Undo readiness cannot replace new move timing");
                using(trace.ObserveBikeMove(pending,"capture-validation-tick")){}
            }
            Check(ReferenceEquals(trace.observedBikeMove,pending)&&ReferenceEquals(trace.bikeTiming,pendingObserver)&&trace.bikeObservation==serial,"interleaved old/new queries keep one stable measurement cycle");
            Check(entry.Target==pending.Target&&ReferenceEquals(entry.BoundBefore,source),"diagnostic query isolation does not change previous Undo identity");
            trace.CancelBikeObservation(pending);
            return count;
        }
    }
}
class Program {static void Main()=>Console.WriteLine("PASS: "+MW.EditorTools.Modules.BulldozerRecovery.BulldozerTrace.Run()+" actual-source bike-rack timing lifecycle/isolation checks. No native performance claim.");}
