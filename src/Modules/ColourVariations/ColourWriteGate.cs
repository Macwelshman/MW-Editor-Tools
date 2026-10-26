using System;
namespace MW.EditorTools.Modules.ColourVariations {
    internal static class ColourWriteGate {
        internal static void Before(string action,Func<string> block,Action invalidate) {
            if(action!="apply" && action!="paste" && action!="confirm" && action!="undo" && action!="reset")return;
            var reason=block?.Invoke();
            if(reason!=null)throw new InvalidOperationException(reason);
            invalidate?.Invoke();
        }
    }
}
