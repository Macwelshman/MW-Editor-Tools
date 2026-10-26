using UnityEngine;

namespace MW.EditorTools.Modules.BulldozerRecovery
{
    internal sealed partial class BulldozerTrace
    {
        private readonly PatchConflictMonitor patchMonitor = new PatchConflictMonitor(Id, DeletionPatchPolicy.RecoveryOwner);
        private bool patchConflictReported;

        private bool PatchesCompatible(bool force = false)
        {
            if (patchMonitor.Check(Time.frameCount, force)) return true;
            Status = "Undo unavailable: editor patch conflict. Restart after resolving the conflicting mod.";
            if (Busy) Status += " Recovery was in progress; reload without saving.";
            if (!patchConflictReported)
            {
                patchConflictReported = true;
                Mod.Log.Warn(Status + " " + patchMonitor.Conflict);
            }
            // Keep captured companion data, references and any in-flight state.
            // Busy continues to block MW saves/edits after an interrupted recovery.
            return false;
        }
    }
}
