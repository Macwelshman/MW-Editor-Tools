namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class NestedPayloadPolicy {
        // These attachments require recovery beyond the captured object tree.
        internal static bool Accept(int areas,int networks,int lanes,int effects,int animationCurves) =>
            areas==0 && networks==0 && lanes==0 && effects==0 && animationCurves==0;
    }
}
