using System;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class AutomaticBoundCapture {
        internal static bool TryEnable(bool enabled, bool blocked, bool supportedMode, bool singleOwnedTarget,
            Func<bool> validate, Action enable, Action<string> refused=null) {
            if (!enabled || blocked || !supportedMode || !singleOwnedTarget) return false;
            bool valid;
            try { valid = validate(); } catch (NotSupportedException error) { refused?.Invoke(error.Message); return false; }
            if (!valid) return false;
            enable();
            return true;
        }
    }
}


