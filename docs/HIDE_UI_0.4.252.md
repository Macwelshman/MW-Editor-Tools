# Configurable native hide UI - 0.4.252

Authorised by the user on 2026-10-26. Options adds an enabled-by-default Ctrl+H binding and enable switch. Apply changes saves the binding; Reset Defaults restores Ctrl+H. Legacy settings gain this default. Letter keys A-Z and existing modifier recording are supported.

VERIFIED-STATIC: installed Game.dll SHA256 AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A. Game.UI.Editor.EditorScreenUISystem.OnCreate IL_002c through IL_004c binds editor.setActiveScreen to SetScreen; SetScreen IL_0007 updates the active screen binding. The mod calls the public native SetScreen with Main/FreeCamera. Installed Content/Game/UI/index.js editor-ui.tsx renders FreeCameraScreen for freeCamera; free-camera-screen.tsx applies native hide-ui styling and registers Back/Hide UI to close. The normal game Hide UI action also switches main/freeCamera. Game files are read only.

Requires editor, focused game and no save/recovery work. Hiding additionally requires fresh UI heartbeat, no typing and no dialog/key recording. Restoration does not depend on UI heartbeat or stale typing state; native Escape also restores. Pause/Options screens are not replaced. Conflicting enabled MW Undo/browser bindings are suppressed in both directions; clipboard combinations Ctrl+C/Ctrl+V/Ctrl+Shift+V are refused for hiding. Native/third-party binding conflicts remain user-configured as for existing shortcuts.

UNSUPPORTED/UNPROVEN: native acceptance pending. Grouped check: hide and restore with tools open/closed; Escape restores; custom binding/disabled preference persist after restart; typing, Options recording, dialogs and saving do not hide UI; Undo/browser/clipboard work with distinct bindings. No computer use or publishing.

VERIFIED-STATIC validation: Release build and production UI build passed. OptionsSafety 47, Phase4Safety 351 and BrowserShortcutSafety 15 assertions passed (413 total). git diff --check passed. NU1900 remains: the NuGet vulnerability feed was unavailable. Game assembly identity: Game, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null.

INSTALLED: 0.4.252 with Cities2 absent before and during copying. Nine prior files were backed up and hash-verified under outputs/rollback/MWEditorTools-before-0.4.252-20261026-182902/MWEditorTools. All three staged/installed payload hashes and assembly version 0.4.252.0 match. See evidence/hide-ui-0.4.252/installed-hashes.csv. Native acceptance remains pending.
