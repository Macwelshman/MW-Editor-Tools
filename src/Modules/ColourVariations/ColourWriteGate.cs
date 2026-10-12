using System;
namespace MW.EditorTools.Modules.ColourVariations {
    internal static class ColourWriteGate {
        internal static void Before(string action,Func<string> block,Action invalidate) {
            if(action!="apply" && action!="confirm" && action!="undo")return;
            var reason=block?.Invoke();
            if(reason!=null)throw new InvalidOperationException(reason);
            invalidate?.Invoke();
        }
    }
}
