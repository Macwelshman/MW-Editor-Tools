using System;
namespace MW.EditorTools {static class Mod {internal static Logger Log=new Logger();internal sealed class Logger {internal void Info(string value){}}}}
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private object preparingAsset,preparingPayload,preparingStablePayload,preparingGraph,preparingForest,preparingReferences,preparingEmptyLanes,nestedTestAsset;
        private bool preparingSyncPending,preparingSynced,preparingSourceMatched,disabled,manualNestedHistory;
        private int preparingStable,nestedTestStep,invalidations;
        private string Status;
        private void Invalidate(){invalidations++;Status="History cleared";}
        private static int checks;
        private static void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
        private static BulldozerTrace Queued()=>new BulldozerTrace {
            preparingAsset=new object(),preparingPayload=new object(),preparingStablePayload=new object(),preparingGraph=new object(),
            preparingForest=new object(),preparingReferences=new object(),preparingEmptyLanes=new object(),nestedTestAsset=new object(),
            preparingSyncPending=true,preparingSynced=true,preparingSourceMatched=true,manualNestedHistory=true,preparingStable=2,nestedTestStep=1};
        public static void Main(){
            foreach(var message in new[]{"Preparation timed out","Preparation callback failed","Deletion interrupted preparation","Asset changed","Payload validation failed"}) {
                var trace=Queued();trace.EndNestedTestPreparation(message);
                Check(trace.disabled&&trace.invalidations==1,"Failed queued preparation must disable and invalidate");
                Check(trace.nestedTestAsset==null&&trace.nestedTestStep==0&&!trace.manualNestedHistory,"Failed preparation must stop automatic and manual modes");
                Check(trace.preparingAsset==null&&trace.preparingPayload==null&&trace.preparingStablePayload==null&&trace.preparingGraph==null&&trace.preparingForest==null&&trace.preparingReferences==null&&trace.preparingEmptyLanes==null,"Preparation snapshots released");
                Check(!trace.preparingSyncPending&&!trace.preparingSynced&&!trace.preparingSourceMatched&&trace.preparingStable==0,"Preparation fences reset");
                Check(trace.Status==message,"Failure reason preserved after invalidation");
                trace.EndNestedTestPreparation("Later callback");
                Check(trace.disabled&&trace.invalidations==1,"Later callbacks must not unlock the failure");
            }
            var success=Queued();success.EndNestedTestPreparation("Prepared",true);
            Check(!success.disabled&&success.invalidations==0,"Successful preparation remains eligible for arming");
            Check(success.nestedTestAsset!=null&&success.manualNestedHistory&&success.nestedTestStep==1,"Successful preparation retains requested mode");
            Check(success.preparingAsset==null&&success.preparingEmptyLanes==null&&!success.preparingSynced,"Successful preparation releases temporary capture");
            var preflight=new BulldozerTrace();preflight.EndNestedTestPreparation("Refused before native update");
            Check(!preflight.disabled&&preflight.invalidations==0,"Read-only preflight refusal must remain retryable");
            Check(preflight.Status=="Refused before native update","Preflight reason retained");
            Console.WriteLine("PASS: "+checks+" actual-source preparation completion assertions. Native fault rollback is not simulated.");
        }
    }
}
