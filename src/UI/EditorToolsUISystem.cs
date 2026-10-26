using System;
using Colossal.UI.Binding;
using Game;
using Game.UI;
using MW.EditorTools.Modules.DeletionProtection;
using MW.EditorTools.Services;
using MW.EditorTools.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
namespace MW.EditorTools.UI
{
    public sealed class EditorToolsUISystem : UISystemBase
    {
        private EditorToolsSettings options;
        private EditorContextService context;
        private ListRecovery recovery;
        private ComponentConfirmation confirmation;
        private ValueBinding<string> binding;
        private bool visible, typing = true;
        private float heartbeat = -100;
        private string published;
        private bool frontendReported;
        private bool? lastAvailable;
        private int positionRevision;
        private readonly BrowserCategoryShortcuts browserShortcuts=new BrowserCategoryShortcuts();
        [Serializable] public sealed class State {
            public bool available, visible, canUndo, canRedo, protectedComponents, installedRecovery, canArmBulldozerTest, canArmMainTest, nestedHistoryEnabled, mainHistoryEnabled;
            public string asset, status, bulldozerDiagnostic, undoTarget, redoTarget;
            public int undo, redo;
            public int positionRevision;
            public string[] recent;
            public MW.EditorTools.Modules.Targeting.TargetingState targeting;
        }
        [Serializable] public sealed class Change { public string name, value; }
        [Serializable] public sealed class Position { public float x, y; public int revision; }
        public override GameMode gameMode => GameMode.Editor;
        protected override void OnCreate() {
            base.OnCreate();
            AddBinding(binding = new ValueBinding<string>("mwEditorTools", "state", "{}"));
            AddBinding(new TriggerBinding<string>("mwEditorTools", "command", Command));
            AddBinding(new TriggerBinding<string>("mwEditorTools", "setting", Setting));
            AddBinding(new TriggerBinding<string>("mwEditorTools", "windowPosition", PositionChanged));
            AddBinding(new TriggerBinding<string>("mwEditorTools", "frontendDiagnostic", value => {
                if (value != null && value.Length <= 1024) Mod.Log.Info("MW Editor Tools: frontend diagnostic " + value);
            }));
            AddBinding(new TriggerBinding<bool>("mwEditorTools", "typing", value => {
                typing = value; heartbeat = UnityEngine.Time.realtimeSinceStartup;
                if (!frontendReported) { frontendReported = true; Mod.Log.Info("MW Editor Tools: frontend mounted; focus heartbeat received."); }
            }));
            Mod.Log.Info("MW Editor Tools: shared UI bindings created.");
        }
        internal void Initialize(EditorToolsSettings settings, EditorContextService selection, ListRecovery history, ComponentConfirmation protection) {
            options = settings; context = selection; recovery = history; confirmation = protection; Enabled = true;
        }
        internal void Release() { browserShortcuts.Cancel(); Enabled = false; options = null; recovery = null; context = null; confirmation = null; }
        internal void ResetWindowPosition() {
            if (options == null) return;
            options.windowX = .4f; options.windowY = .18f; options.Save(); positionRevision++; Publish();
        }
        private void Command(string command) {
            if (options == null || !context.EditorAvailable) return;
            if(command!=null&&command.StartsWith("target",StringComparison.Ordinal)) {
                World.GetExistingSystemManaged<MW.EditorTools.Modules.Targeting.EditorTargetingTool>()?.Command(command);
                Publish();return;
            }
            if (command == "open" || command == "close") Mod.Log.Info("MW Editor Tools: window command " + command);
            switch(command) {
                case "open": visible = true; break;
                case "close": visible = false; break;
                case "armMainTest": recovery?.ArmMainTest(); break;
                case "startNestedRecoveryTest": recovery?.StartNestedRecoveryTest(); break;
                case "startNestedBatchTest": recovery?.StartNestedRecoveryTest(2); break;
                case "startFullPropTest": recovery?.StartNestedRecoveryTest(0); break;
                case "enableNestedHistory": recovery?.EnableNestedHistory(); break;
                case "inspectNestedFixture": recovery?.InspectNestedFixture(); break;
                case "createAreaFixture": recovery?.CreateAreaFixture(); break;
                case "createNestedFixture": recovery?.CreateNestedFixture(); break;
                case "armNestedForestTest": recovery?.ArmNestedForestTest(); break; case "armNestedGroupTest": recovery?.ArmNestedGroupTest(); break; case "armLastPropTest": recovery?.ArmLastPropTest(); break;
                case "armBulldozerTest": recovery?.ArmBulldozerTest(); break;
                case "undo": if (context.EditorAvailable) recovery?.Undo(); break;
                case "redo": if (context.EditorAvailable) recovery?.Redo(); break;
                case "clear": recovery?.ClearHistory(); break;
            }
            Publish();
        }
        private void PositionChanged(string json) {
            if(options == null) return;
            try { var position = JsonUtility.FromJson<Position>(json); if(position == null) return;
                if (position.revision != positionRevision) return;
                options.windowX = position.x; options.windowY = position.y; options.Normalize(); options.Save(); Publish();
            } catch(Exception error) { Mod.Log.Warn("Window position rejected: " + error.Message); }
        }
        private void Setting(string json) {
            if(options == null || !context.EditorAvailable || context.DialogBusy) return;
            try {
                var change = JsonUtility.FromJson<Change>(json); if(change == null) return;
                bool flag; int count; bool clear = false;
                switch(change.name) {
                    case "confirmation": if(!bool.TryParse(change.value, out flag)) return; options.confirmation = flag; break;
                    case "recovery": if(!bool.TryParse(change.value, out flag)) return; options.recovery = flag; clear = true; break;
                    case "shortcuts": if(!bool.TryParse(change.value, out flag)) return; options.shortcuts = flag; break;
                    case "inspectorControls": if(!bool.TryParse(change.value, out flag)) return; options.inspectorControls = flag; break;
                    case "historyDepth": if(!int.TryParse(change.value, out count)) return; options.historyDepth = count; clear = true; break;
                    case "undoKey": options.undoKey = change.value; break;
                    case "redoKey": options.redoKey = change.value; break;
                    default: return;
                }
                options.Normalize(); options.Save(); if(clear) recovery?.ApplySettings(); Publish();
            } catch(Exception error) { Mod.Log.Warn("Setting rejected: " + error.Message); }
        }
        protected override void OnUpdate() {
            base.OnUpdate();
            if(options == null) return;
            browserShortcuts.Update(World,UnityEngine.Time.frameCount,BrowserDispatchAllowed());
            var keyboard = Keyboard.current;
            if(keyboard != null) {
                var undo = keyboard[(Key)Enum.Parse(typeof(Key), options.undoKey)];

                var action = ShortcutPolicy.Resolve(options.shortcuts, context.EditorAvailable,
                    typing || UnityEngine.Time.realtimeSinceStartup - heartbeat > .75f || !Application.isFocused,
                    context.DialogBusy, false, keyboard.ctrlKey.isPressed, keyboard.shiftKey.isPressed,
                    keyboard.altKey.isPressed, keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed,
                    undo.wasPressedThisFrame, false, options.undoModifiers, options.redoModifiers);
                int modifiers=(keyboard.ctrlKey.isPressed?1:0)|(keyboard.shiftKey.isPressed?2:0)|(keyboard.altKey.isPressed?4:0)|((keyboard.leftMetaKey.isPressed||keyboard.rightMetaKey.isPressed)?8:0);
                var screen=World.GetExistingSystemManaged<Game.UI.Editor.EditorScreenUISystem>();
                if(options.hideUIShortcut && modifiers==options.hideUIModifiers &&
                    keyboard[(Key)Enum.Parse(typeof(Key),options.hideUIKey,true)].wasPressedThisFrame && !options.HideUIConflict() &&
                    context.EditorAvailable && Application.isFocused && recovery?.OperationBusy!=true &&
                    World.GetExistingSystemManaged<MW.EditorTools.Modules.MultiSave.MultiSaveSystem>()?.IsBusy!=true && screen!=null) {
                    // Native free-camera UI can stop browser heartbeats. Restore must not depend on them.
                    if(screen.activeScreen==Game.UI.Editor.EditorScreenUISystem.EditorScreen.FreeCamera)
                        screen.SetScreen(Game.UI.Editor.EditorScreenUISystem.EditorScreen.Main);
                    else if(screen.activeScreen==Game.UI.Editor.EditorScreenUISystem.EditorScreen.Main &&
                        !context.DialogBusy && !typing && UnityEngine.Time.realtimeSinceStartup-heartbeat<=.75f)
                        screen.SetScreen(Game.UI.Editor.EditorScreenUISystem.EditorScreen.FreeCamera);
                }
                if(undo.wasPressedThisFrame&&modifiers==options.undoModifiers&&recovery?.UndoCount>0) {
                    bool ready=recovery.CanUndo;
                    if(action<0||!ready||options.BrowserConflict(options.undoKey,modifiers))Mod.Log.Info("Undo shortcut blocked: ready="+ready+" typing="+typing+" heartbeatFresh="+(UnityEngine.Time.realtimeSinceStartup-heartbeat<=.75f)+" applicationFocused="+Application.isFocused+" dialog="+context.DialogBusy+" shortcuts="+options.shortcuts+" browserConflict="+options.BrowserConflict(options.undoKey,modifiers));
                }
                if(action>=0 && !options.BrowserConflict(action==0?options.undoKey:options.redoKey,modifiers)) recovery?.RequestShortcut(action,ShortcutDispatchAllowed);
                if(BrowserDispatchAllowed()) for(int i=0;i<EditorToolsSettings.BrowserCategories.Length;i++) {
                    var key=options.BrowserKey(i);
                    if(modifiers==options.BrowserModifiers(i)&&keyboard[(Key)Enum.Parse(typeof(Key),key)].wasPressedThisFrame && !options.BrowserConflict(key,modifiers,i))
                        browserShortcuts.Request(EditorToolsSettings.BrowserCategories[i],UnityEngine.Time.frameCount);
                }
            }
            Publish();
        }
        private bool ShortcutDispatchAllowed()=>options?.shortcuts==true && context?.EditorAvailable==true && !context.DialogBusy && !typing &&
            UnityEngine.Time.realtimeSinceStartup-heartbeat<=.75f && Application.isFocused;
        internal bool ComponentCommandAllowed=>context?.EditorAvailable==true&&!context.DialogBusy&&
            UnityEngine.Time.realtimeSinceStartup-heartbeat<=.75f&&Application.isFocused&&recovery?.OperationBusy!=true&&
            World.GetExistingSystemManaged<MW.EditorTools.Modules.MultiSave.MultiSaveSystem>()?.IsBusy!=true;
        internal bool ClipboardShortcutAllowed=>ComponentCommandAllowed&&!typing;
        private bool BrowserDispatchAllowed()=>options?.browserShortcuts==true && context?.EditorAvailable==true && !context.DialogBusy && !typing &&
            UnityEngine.Time.realtimeSinceStartup-heartbeat<=.75f && Application.isFocused && recovery?.OperationBusy!=true &&
            World.GetExistingSystemManaged<MW.EditorTools.Modules.MultiSave.MultiSaveSystem>()?.IsBusy!=true;
        private void Publish() {
            var available = context.EditorAvailable;
            if (lastAvailable != available) {
                lastAvailable = available;
                Mod.Log.Info("MW Editor Tools: shared UI available = " + available + "; inspector present = " + (context.Inspector != null) + "; inspector enabled = " + (context.Inspector != null && context.Inspector.Enabled));
            }
            var state = new State {
                available = available, visible = visible, asset = recovery?.ActiveAssetName ?? "No asset selected", canArmBulldozerTest = recovery?.CanArmBulldozerTest==true, canArmMainTest = recovery?.CanArmMainTest==true,
                bulldozerDiagnostic = recovery?.BulldozerDiagnostic ?? "Bulldozer diagnostic unavailable", nestedHistoryEnabled = recovery?.NestedHistoryEnabled==true, mainHistoryEnabled = recovery?.MainHistoryEnabled==true,
                status = recovery != null ? recovery.Status : "List recovery unavailable", undo = recovery?.UndoCount ?? 0, redo = recovery?.RedoCount ?? 0,
                undoTarget = recovery?.UndoTarget, redoTarget = recovery?.RedoTarget,
                canUndo = context.EditorAvailable && recovery != null && recovery.CanUndo, canRedo = context.EditorAvailable && recovery != null && recovery.CanRedo,
                protectedComponents = confirmation != null && confirmation.ProtectionInstalled, installedRecovery = recovery != null && recovery.RecoveryInstalled,
                recent = recovery != null ? recovery.Recent : new string[0], positionRevision = positionRevision,
                targeting=World.GetExistingSystemManaged<MW.EditorTools.Modules.Targeting.EditorTargetingTool>()?.GetState()
            };
            var serializedState = Newtonsoft.Json.JsonConvert.SerializeObject(state);
            var json = serializedState.Substring(0, serializedState.Length - 1) + ",\"settings\":" + options.ToJson() + "}";
            if(json != published) { published = json; binding.Update(json); }
        }
    }
}







