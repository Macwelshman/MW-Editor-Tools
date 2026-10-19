namespace MW.EditorTools.Modules.PropReplacer {
    // The effect harness exercises its own pending refresh path; foliage has no
    // pending operation in this managed world.
    public sealed class PropReplacerSystem {
        public void BeforeNativeFoliageRendering() { }
    }
}
