using System;
using System.Reflection;
using Game;
using Game.Prefabs;
using Game.SceneFlow;
using Game.UI;
using Game.UI.Editor;
using Unity.Entities;

namespace MW.EditorTools.Services
{
    internal sealed class EditorContextService
    {
        private readonly World world;
        private readonly FieldInfo parent = typeof(InspectorPanelSystem).GetField("m_ParentObject", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly FieldInfo callback = typeof(AppBindings).GetField("m_ConfirmationDialogCallback", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly FieldInfo dismissible = typeof(AppBindings).GetField("m_DismissibleConfirmationDialogCallback", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly FieldInfo recording = typeof(Game.UI.Menu.InputRebindingUISystem).GetField("m_ActiveRebinding", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly FieldInfo pendingRecording = typeof(Game.UI.Menu.InputRebindingUISystem).GetField("m_PendingRebinding", BindingFlags.NonPublic | BindingFlags.Instance);
        internal EditorContextService(World world)
        {
            this.world = world;
            if (parent == null || callback == null || dismissible == null || recording == null || pendingRecording == null) throw new NotSupportedException("Editor context API unavailable.");
        }
        internal InspectorPanelSystem Inspector => world != null && world.IsCreated ? world.GetExistingSystemManaged<InspectorPanelSystem>() : null;
        internal bool EditorAvailable
        {
            get
            {
                var gm = GameManager.instance;
                return gm != null && gm.gameMode == GameMode.Editor && !gm.isGameLoading && world != null && world.IsCreated;
            }
        }
        internal bool Available => EditorAvailable && Inspector != null && Inspector.Enabled;
        internal PrefabBase Asset => Available ? parent.GetValue(Inspector) as PrefabBase : null;
        internal bool DialogBusy
        {
            get
            {
                var ui = GameManager.instance?.userInterface?.appBindings;
                var rebinding = world != null && world.IsCreated ? world.GetExistingSystemManaged<Game.UI.Menu.InputRebindingUISystem>() : null;
                return ui == null || !ui.ready || callback.GetValue(ui) != null || dismissible.GetValue(ui) != null ||
                    (rebinding != null && (recording.GetValue(rebinding) != null || pendingRecording.GetValue(rebinding) != null));
            }
        }
    }
}
