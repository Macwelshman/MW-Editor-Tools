using System;

namespace MW.EditorTools.History {
    // Shared controls own the slot; the replacement module owns data validation.
    internal sealed class ReplacementUndoAction {
        internal readonly string Label;
        private readonly Func<bool> ready,apply,contextValid;
        private bool consumed,applying;
        internal ReplacementUndoAction(string label,Func<bool> ready,Func<bool> apply,Func<bool> contextValid=null){Label=label;this.ready=ready;this.apply=apply;this.contextValid=contextValid??(()=>true);}
        internal bool ContextValid {get{try{return contextValid();}catch{return false;}}}
        internal bool CanUndo {get{try{return !consumed&&!applying&&ready();}catch{return false;}}}
        internal bool Undo(){if(!CanUndo)return false;applying=true;try{if(!apply())return false;consumed=true;return true;}finally{applying=false;}}
    }
}
