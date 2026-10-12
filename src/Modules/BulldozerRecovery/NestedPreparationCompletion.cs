namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private void EndNestedTestPreparation(string message,bool successful=false){
            bool nativeUpdateQueued=preparingAsset!=null;
            preparingAsset=null;preparingPayload=preparingStablePayload=null;preparingGraph=null;preparingForest=null;preparingReferences=null;preparingEmptyLanes=null;
            preparingSyncPending=preparingSynced=preparingSourceMatched=false;preparingStable=0;
            if(!successful&&nativeUpdateQueued){
                // Pending native work cannot be cancelled or rolled back here.
                disabled=true;
                nestedTestAsset=null;nestedTestStep=0;manualNestedHistory=false;
                Invalidate();
            }
            Status=message;Mod.Log.Info(message);
        }
    }
}
