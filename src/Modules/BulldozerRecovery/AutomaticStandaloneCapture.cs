using System;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class AutomaticStandaloneCapture {
        // Validate before switching recovery modes: rejected targets must not erase history.
        internal static bool TryEnable(bool enabled, bool blocked, bool supportedMode, Func<bool> capture, Action enable) {
            if(!enabled || blocked || !supportedMode)return false;
            bool valid;
            try { valid=capture(); } catch { return false; }
            if(!valid)return false;
            enable();return true;
        }
    }
}
