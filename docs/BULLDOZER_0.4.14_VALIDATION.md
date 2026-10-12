# 0.4.14 bounded experimental bulldozer Undo

The user explicitly approved this live experiment on 9 October 2026: one Sub-element prop on a disposable duplicate. This supersedes the earlier approval block; it does not authorise broader recovery or later phases.

1. Open a saved disposable duplicate containing several props. Select its owning asset.
2. Open MW Editor Tools, History & Recovery. Click Arm next Sub-element Undo test.
3. Choose Sub-element bulldozer and delete ONE grill. Keep other props present.
4. Wait for Recorded one Sub-element prop deletion and enabled Undo.
5. Click Undo. Confirm the grill returns once, with original position, rotation and probability. Check the other objects, effects, networks and areas remain correct.
6. Wait for completed Undo, then click Redo. Confirm only the intended grill disappears once.
7. Undo once more. Save the duplicate and reopen it; compare all related data.
8. Report the displayed status and any mismatch. If recovery fails or times out, reload the saved duplicate before saving.

Main, Everything, last-object deletion, nested child targets, buildings/networks/areas as targets, AssetStamp and ActivityLocation parents are excluded. There is one experimental entry, cleared by another edit, deletion, settings/context changes or load. Ordinary supported list recovery remains separate.

VERIFIED-STATIC: Release build zero warnings/errors; production UI build; 402 existing regression assertions and Settings rendering. These checks do not simulate native live restoration.
VERIFIED-RUNTIME (previous builds): native Sub-element capture, SaveInstance synchronization and source-preserving rebuild dispatch; objects 7 to 6 with related effects and net payload changes.
VERIFIED-STATIC: Game.dll 0.0.0.0, MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. ReplacePrefabJob.Execute source-instance exclusion IL_007e/0083 explains the 0.4.13 dry-run timeout. Recording uses matching original source dispatch. Restoration uses Inspector.UpdateParent-equivalent UpdatePrefab(asset, Entity.Null), IL_007c, followed by matching owner in ReplacePrefabSystem.Finalize.OnUpdate, job completion IL_04b9 and ECB playback IL_04c6.
UNPROVEN: successful live Undo/Redo, runtime rollback, effect/network scene regeneration and save/reopen. On failure the experiment disables itself; deferred scene rollback is not claimed. Whole supported prefab field images are restored; final validation checks payload equality plus child prefab/container identities, ownership and transforms. Separate network/area ECS graph validation remains outside this experiment.
