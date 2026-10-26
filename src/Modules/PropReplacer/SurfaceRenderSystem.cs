using Game;

namespace MW.EditorTools.Modules.PropReplacer {
 // Isolated from recovery patches; submit the refresh at its native consumer.
 public sealed partial class SurfaceRenderSystem : GameSystemBase {
  private PropReplacerSystem replacer;
  internal void Initialize(PropReplacerSystem value){replacer=value;Enabled=true;value.SetSurfaceRenderer(true);}
  internal void Release(){replacer?.SetSurfaceRenderer(false);replacer=null;Enabled=false;}
  protected override void OnUpdate(){replacer?.FlushSurfaceRender();}
 }
}
