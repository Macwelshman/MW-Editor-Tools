# Version 0.4.9 — press-to-record shortcuts

VERIFIED-STATIC: Options uses the game's native keyboard binding recorder. Click Undo or Redo and press the complete combination, such as Ctrl+Alt+T; Apply changes activates and saves it. Separate letter fields and modifier switches removed. Supported main keys remain A–Z; empty/unsupported entries retain the previous shortcut. History text input and reset remain independent.

VERIFIED-STATIC: Game.dll MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef, Game.Settings.ModSetting.InitializeKeyBindings and RegisterKeyBindings supply native ProxyBinding widgets. Native modifier metadata retained from default templates (CreateBinding closure IL_0010/0022/002a). Evidence: docs/evidence/phase4-keybinding-*.txt and phase4-create-binding.txt. InputRebindingUISystem active/pending recording fields gate recovery during recording/conflict dialogs. Registered actions stay disabled; existing guarded recovery polling retained. Constructor-bypass lifecycle fix retained.

VERIFIED-AUTOMATED: 311 Phase 4 safety, 26 actual-source Options/codec and 11 constructor-bypass lifecycle assertions pass. Native game input/persistence mocked in unit checks. Settings rendering passes. C# Release build has zero warnings/errors. UI build/type checks pass.

UNPROVEN: native recorder and game runtime until user testing.

Test list (use a duplicate asset):
1. Restart CS2; confirm all mod icons and MW window work.
2. Options → MW Editor Tools: click Undo shortcut, hold Ctrl+Alt and press T. Expect Ctrl+Alt+T shown without choosing modifiers separately. Record Redo as Ctrl+Shift+R. Click Apply changes.
3. Confirm History shows these combinations. Test a supported deletion, Undo with Ctrl+Alt+T, Redo with Ctrl+Shift+R. Missing/extra modifier must do nothing; held keys must not repeat.
4. While recording a shortcut, typing in an Options field, or handling a binding conflict dialog, recovery history must not change. Cancel recording and confirm previous binding remains.
5. Close/reopen Options and restart CS2: committed shortcuts persist. Test identical Undo/Redo combinations: neither action runs; restore distinct shortcuts afterwards.
6. History limit: select all, Backspace and type 37 one digit at a time; Apply changes. Reset window position and saved opening position still work.
7. Restore Ctrl+Z Undo / Ctrl+Shift+Z Redo and history 20, Apply changes. Run existing Phase 4 regression list.

