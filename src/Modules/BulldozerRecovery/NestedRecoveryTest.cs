using System;
using Game.Prefabs;
using UnityEngine;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private PrefabBase nestedTestAsset;
        private int nestedTestStep, nestedTestFrame;
        private int nestedTestCount=1, nestedTestActions;
        private bool manualNestedHistory;
        internal bool NestedTestRunning=>nestedTestAsset!=null;
        internal bool NestedHistoryEnabled=>armed&&nestedArmed&&nestedForestArmed;
        internal void EnableNestedHistory(){
            if(NestedHistoryEnabled&&!NestedTestRunning){Mod.Log.Info("Nested prop Undo is already enabled; history preserved.");return;}
            if(!CanArm)return;
            nestedTestAsset=null;nestedTestStep=0;manualNestedHistory=true;
            if(!UseOrdinaryNestedGroup())return;
            if(BeginNestedTestPreparation())return;
            BeginArmedNestedTest();
        }
        internal void StartNestedRecoveryTest(int count=1){
            if(!CanArm)return;
            if(count<0||count>4){Status="Undo test stopped: unsupported deletion count.";return;}
            nestedTestActions=0;
            nestedTestAsset=null;nestedTestStep=0;manualNestedHistory=false;
            if((nestedFixture==null||ordinaryNested||!SelectedAssetUsesNestedGroup())&&!UseSavedNestedFixture()&&!UseOrdinaryNestedGroup())return;
            if(count==0){
                try{count=AdoptedRootCount();}catch(NotSupportedException error){Status="Undo test stopped: "+error.Message;Mod.Log.Info(Status);return;}
                if(count<1||count>4){Status="Undo test stopped: no bounded prop set is available.";Mod.Log.Info(Status);return;}
            }
            nestedTestCount=count;
            if(BeginNestedTestPreparation())return;
            BeginArmedNestedTest();
        }
        private void BeginArmedNestedTest(){
            ArmNestedForest();
            if(!nestedForestArmed)return;
            if(manualNestedHistory){Status="Prop Undo enabled for this asset (experimental). Everything-delete a supported prop or group, then use Undo or Redo. Use a disposable copy; do not save.";Mod.Log.Info(Status);return;}
            nestedTestAsset=armedAsset;nestedTestStep=1;nestedTestFrame=Time.frameCount;
            Status=nestedTestCount==1?"Test ready: Everything-delete one supported prop or group. Undo, Redo and the final Undo will run automatically. Do not save.":"Test ready: Everything-delete "+nestedTestCount+" supported props or groups once each. Then wait; "+nestedTestCount+" Undo, "+nestedTestCount+" Redo and "+nestedTestCount+" final Undo steps run automatically. Do not save.";
            Mod.Log.Info(Status);
        }
        private void StopNestedRecoveryTest(string reason){
            nestedTestAsset=null;nestedTestStep=0;
            Status="Undo test stopped: "+reason;
            Mod.Log.Info(Status);
        }
        private void TickNestedRecoveryTest(){
            if(nestedTestAsset==null)return;
            if(!context.EditorAvailable||disabled||(context.Asset!=null&&!ReferenceEquals(context.Asset,nestedTestAsset))){StopNestedRecoveryTest("the editor or asset changed.");return;}
            if(restoring!=null||session.Asset!=null||context.DialogBusy)return;
            if(nestedTestStep==1){
                if(!armed||!nestedForestArmed){StopNestedRecoveryTest("the deletion changed data outside the supported scope. No Undo was recorded. Restart without saving.");return;}
                if(records.UndoCount==0)return;
                if(records.UndoCount>nestedTestCount||records.RedoCount!=0||record?.ForestBefore==null||!ReferenceEquals(record.Asset,nestedTestAsset)){StopNestedRecoveryTest("unexpected history; no automated action taken.");return;}
                if(records.UndoCount<nestedTestCount){Status="Recorded "+records.UndoCount+" of "+nestedTestCount+" deletions. Everything-delete the remaining prop or group, then wait.";return;}
                nestedTestStep=2;
            }
            if(Time.frameCount-nestedTestFrame<15)return;
            if(nestedTestStep==2){
                if(records.UndoCount!=nestedTestCount-nestedTestActions||records.RedoCount!=nestedTestActions){StopNestedRecoveryTest("unexpected history during Undo.");return;}
                if(!CanUndo){StopNestedRecoveryTest("Undo is unavailable after deletion; restart without saving.");return;}
                Undo();nestedTestActions++;if(nestedTestActions==nestedTestCount){nestedTestStep=3;nestedTestActions=0;}
            }else if(nestedTestStep==3){
                if(records.UndoCount!=nestedTestActions||records.RedoCount!=nestedTestCount-nestedTestActions||!CanRedo){StopNestedRecoveryTest("Undo or Redo did not complete.");return;}
                Redo();nestedTestActions++;if(nestedTestActions==nestedTestCount){nestedTestStep=4;nestedTestActions=0;}
            }else if(nestedTestStep==4){
                if(records.UndoCount!=nestedTestCount-nestedTestActions||records.RedoCount!=nestedTestActions||!CanUndo){StopNestedRecoveryTest("Redo or the final Undo did not complete.");return;}
                Undo();nestedTestActions++;if(nestedTestActions==nestedTestCount)nestedTestStep=5;
            }else if(nestedTestStep==5){
                if(records.UndoCount!=0||records.RedoCount!=nestedTestCount||!CanRedo){StopNestedRecoveryTest("the final Undo did not complete.");return;}
                nestedTestAsset=null;nestedTestStep=0;
                Status="PASS: "+nestedTestCount+" deletion(s): Undo, Redo and Undo completed; recorded data and object ownership verified. Props restored. Do not save test edits.";
                Mod.Log.Info(Status);
            }
            nestedTestFrame=Time.frameCount;
        }
    }
}
