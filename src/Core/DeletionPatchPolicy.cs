namespace MW.EditorTools
{
    internal static class DeletionPatchPolicy
    {
        internal const string ConfirmationOwner = "MW.EditorTools.ComponentConfirmation.0.2";
        internal const string RecoveryOwner = "MW.EditorTools.ListRecovery.0.3";
        internal static bool IsKnownOwner(string id) => id == ConfirmationOwner || id == RecoveryOwner;
    }
}
