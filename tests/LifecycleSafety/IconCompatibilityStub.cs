namespace MW.EditorTools.Modules.Compatibility
{
    public class AssetIconCreatorCompatibility
    {
        public void Initialize() { if (Probe.IconFails) throw new System.NotSupportedException("adapter conflict"); }
        public void Release() { Probe.IconReleases++; }
    }
}
