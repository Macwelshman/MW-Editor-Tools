using System;
using MW.EditorTools.History;

namespace MW.EditorTools.Modules.DeletionProtection {
    public sealed partial class ListRecovery {
        private ReplacementUndoAction replacementUndo;
        private ReplacementUndoAction shortcutReplacement;
        private bool ReplacementRecoveryReady => enabledFeature && settings?.recovery==true && !Saving && !OperationBusy && context?.EditorAvailable==true && !context.DialogBusy;
        internal bool CanRecordReplacementUndo => CanAct && !OperationBusy;
        internal void RecordReplacementUndo(ReplacementUndoAction action) {
            if(!CanRecordReplacementUndo)throw new InvalidOperationException("Replacement Undo is unavailable. Enable recovery and close pending editor operations.");
            bulldozerTrace?.Invalidate();Clear("Replacement ready to Undo");
            asset=context.Asset;replacementUndo=action??throw new ArgumentNullException(nameof(action));
            status=action.Label+" ready to Undo. Ctrl+Z restores the whole action.";
            Mod.Log.Info("Replacement Undo recorded: "+action.Label+" inspector="+(context.Asset?.name??"none"));
        }
        private void CheckReplacementContext() {
            if(replacementUndo!=null&&!replacementUndo.ContextValid)Clear("Replacement Undo cleared because the editor or owning asset context changed");
        }
        private bool ShortcutContextValid => shortcutReplacement!=null?ReferenceEquals(shortcutReplacement,replacementUndo)&&shortcutReplacement.ContextValid:ReferenceEquals(context.Asset,shortcutAsset);
        private void UndoReplacement() {
            var entry=replacementUndo;
            applying=true;
            try{if(entry.Undo()){if(ReferenceEquals(entry,replacementUndo))replacementUndo=null;status="Undid "+entry.Label;Mod.Log.Info(status);}}
            catch(Exception error){Clear("Replacement Undo failed; check the asset before saving.");Mod.Log.Error(error,"Replacement Undo failed; source rollback attempted.");}
            finally{applying=false;}
        }
    }
}
