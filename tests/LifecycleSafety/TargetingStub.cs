namespace MW.EditorTools.Modules.Targeting
{
    public sealed class EditorTargetingTool
    {
        public void Initialize(MW.EditorTools.Services.EditorContextService context, MW.EditorTools.Modules.DeletionProtection.ListRecovery recovery, System.Func<bool> allowed)
        { if (Probe.TargetingFails) throw new System.NotSupportedException("targeting unavailable"); }
        public System.Action RestoreOutlineBeforeEdit;
        public void Release() { Probe.TargetingReleases++; }
        public void SetOutlineStatus(string message) {}
    }
}

namespace MW.EditorTools.Modules.Targeting { public sealed class OutlineColourSystem { public void Initialize(EditorTargetingTool tool) {if(Probe.OutlineFails)throw new System.NotSupportedException("outline API unavailable");} public void RestoreForInspectorEdit() {} public void Release() {Probe.OutlineReleases++;} } }
namespace Game.Rendering { public sealed class BatchDataSystem {} }

namespace MW.EditorTools.Modules.Targeting { internal sealed class TargetingEditGuard:System.IDisposable { internal TargetingEditGuard(EditorTargetingTool tool){}public void Dispose(){} } }
