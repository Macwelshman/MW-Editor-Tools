# 0.4.93 automatic bound prop capture

VERIFIED-RUNTIME (user, 0.4.92): selecting the owning MW Mixed Undo Test asset, enabling prop Undo, Everything-deleting Grill02 and Undo/Redo/Undo works. This validates the manually enabled prop route; it does not validate whole-building or network/lane/effect deletion.

IMPLEMENTED: a supported single bound prop or nested group in Everything mode now attempts automatic read-only adoption before native deletion, when recovery is enabled and its writable owning asset is selected. Reuses the existing typed graph/schema/reference capture, bounded forest history, last-component rebinding and native synchronization/finalization checks. Existing enabled history is retained. Standalone and area paths remain separate. Main/Sub-elements modes, buildings, area/network targets, multiple targets, busy/dialog/save/disabled contexts cannot activate this new route.

Automatic activation never invokes BeginNestedTestPreparation, adds SaveInstance, changes descriptors or saves files. It requires existing root cache indices, prefab references, local positions and rotations to match descriptors. A stale cache refuses capture; the existing manual Enable prop Undo preparation control remains available and is described in the panel. Native regeneration behavior is UNPROVEN until user testing. This is the first implementation batch, not completion of all-types Undo.

VERIFIED-STATIC: Release zero warnings/errors; 312 relevant managed assertions (snapshot/history 95, bulldozer policies 38, nested runner 132, last-component rebinding 12, preparation completion 35); production UI build, TypeScript and 30 recovery UI assertions pass. Seven new automatic-bound policy assertions cover gating, refusal and activation ordering. Game.dll identity/evidence unchanged from 0.4.92. No computer use.

## New automatic behavior test

1. Restart with this build. Load a disposable copy of the saved mixed fixture and select the owning asset in Workspace. Enable recovery in Options if off.
2. Do **not** click Enable prop Undo. Use bulldozer **Everything** mode to delete one Grill02 prop, keeping the building.
3. Wait for a recorded prop deletion. Click Undo, Redo, Undo; verify placement and settings.
4. Delete another supported prop; verify multiple-step history and redo branch clearing after a new deletion.
5. If the panel reports a placement-cache mismatch, report that text. The fallback is Enable prop Undo, wait for Prop Undo enabled, then repeat. A fallback pass is not an automatic-route pass.
6. Do not save over the baseline. Whole-building, network/lane/effect targets and mixed-area recovery remain unfinished. The prior inspector lane/Clear and area-only test batch remains in RECOVERY_0.4.92_VALIDATION.md.
