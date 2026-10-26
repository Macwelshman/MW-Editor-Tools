# Newly added editor mods — compatibility check with 0.4.222

Read-only review,2026-10-23. No runtime code changes, other-mod execution by the inspection tools, playset edits, installation, publishing or computer use.

## Loaded combination

VERIFIED-RUNTIME: latest `Modding.log` identifies the new additions as **Asset Icon Creator1.4.1** (`105288_9`, assembly1.4.1.0) and **StarQ's Workflow Kit3.1.0** (`92671_32`, assembly3.1.0.0). Both load alongside MW.EditorTools0.4.222.0, Find It2.0, Asset Icon Library4.1.1, Unified Icon Library1.0.14 and MWEditorEnvironment0.1.3. The loaded Harmony identity is2.4.2.0. MW's confirmation, list, object move, area move and native Undo hooks attach; shared editor UI opens/closes successfully in the log. No error, exception or patch-conflict refusal appears in the checked Modding/MW/UI/Asset Icon Creator logs. UI contains unsupported-style warnings without attributed source; those are not evidence of an Undo conflict.

This verifies co-loading and editor UI availability, not new native Undo/save acceptance. No deletion or move Undo has been performed in the reviewed session log. Previous compatibility evidence is reused only for unchanged assembly hashes, recorded in `reused-assembly-hashes.json`.

## Findings

| Mod | Direct hook/binding result | Asset editing interaction |
|---|---|---|
| Asset Icon Creator1.4.1 | VERIFIED-STATIC: no Harmony reference/patch attributes or inspected binding-name collision. Its UI appends an overlay to Editor/Game; it does not replace MW's hierarchy/component wrappers. | **UI Object replacement risk**, detailed below. Icon generation/automatic setting is outside MW Undo. |
| StarQ's Workflow Kit3.1.0 | VERIFIED-STATIC: Harmony patch targets are `EditorAssetCategorySystem.AddOverrides` and `PrefabBase.MarkCurrentPrefabObsolete`, neither a watched native MW Undo target. No MW binding-name literal collision identified. | Bulk prefab modification, renaming and saving operate independently of MW's queues/interlocks/history. Complete MW recovery before using these actions; do not treat them as MW-recoverable edits. |

### Asset Icon Creator and Group/Priority

VERIFIED-STATIC: `AssetIconCreator, Version=1.4.1.0`, SHA256 `A77E25A6EE15891D7457444014AE0DA02B57A2482076A60EF1C87BC5B7A47004`.

`ScreenshotUtility/<>c__DisplayClass33_0.<ProcessScreenshot>b__0` checks `Setting.AutoSetIcon` at IL_0063 and skips the write branch when false. When true, it obtains the existing UI Object at IL_0075, removes it at IL_0082, adds a UI Object at IL_008d and overwrites local3 with the newly added component at IL_0092. The Priority read IL_009b and Group read IL_00ad then use that **new** local3, not a retained old component. The icon is assigned at IL_00b9; `PrefabSystem.UpdatePrefab` is called at IL_00d9.

HYPOTHESIS supported by this exact IL: automatically assigning an icon can reset existing Group/Priority and other old UI Object settings to new-component defaults. A before/after native icon-generation test has not been performed, and no reset is demonstrated in the current logs. This behaviour does not require MW to be installed and is not a Harmony collision.

VERIFIED-STATIC: `Setting..ctor(IMod)` IL_0001..0002 initialises `_autoSetIcon` true. The English setting label in `LocaleEN.ReadEntries` IL_0114 is **Automatically set up UI Object component**. This is the code default, not confirmation of the user's saved setting. Disabling that option avoids this inspected automatic replacement branch; icon generation without automatic assignment remains a separate operation. The user can then assign the icon through the existing editor workflow while preserving Group/Priority. No setting was changed during this review.

MW's priority Apply/Restore deliberately verifies the exact UI Object reference and fingerprint (`PriorityBatch.Apply/Restore`). Replacing that component after preview/application can therefore invalidate the preview or prevent Restore. That refusal protects against overwriting a later external edit; it cannot repair values already reset by another mod.

### Workflow Kit and external writes

VERIFIED-STATIC: `StarQWorkflowKit, Version=3.1.0.0`, SHA256 `AB5E79F130CA4153E6DC143465FC79AAD508AAEEE761025D0B5282FAAEFE80DC`.

`WorkflowSystem.SaveAndContinue` adds the asset at IL_0090, saves at IL_009d and calls `PrefabSystem.UpdatePrefab` at IL_00b8. `AddSpawableObject` and `AddAssetPack` also call UpdatePrefab (IL_0089 / IL_0082). The `MarkCurrentPrefabObsolete` prefix maintains ObsoleteIdentifiers and returns false rather than executing the native original. These are meaningful external asset changes, although their Harmony targets do not overlap MW's watched methods.

HYPOTHESIS/UNPROVEN: interleaving those actions with an in-flight MW recovery or another save queue could invalidate snapshots or save intermediate state. No shared cross-mod busy/history contract was identified. No such failure is present in the current logs. MW's late patch monitor detects foreign owners on its watched methods; it cannot announce every independent asset write as a patch conflict. Typed data/source/reference validation remains necessary.

Workflow Kit references Harmony2.2.2.0 but current logs load Harmony2.4.2.0 and MW hooks install. Its bundled Harmony file hash differs from MW's file; a differing file hash or reference version alone is not evidence of an active version conflict.

## Evidence and limits

`docs/evidence/editor-compatibility-0.4.222/assembly-overlap.json` records exact identities, hashes, MVIDs, attributes and selected API call offsets for the two new assemblies. `selected-details.json` preserves narrowly selected patch/write/save method IL and relevant binding/input contracts. `AssetIconCreator.mjs.txt` preserves the inspected UI registration. Logs, final error scan and unchanged-assembly hashes are saved alongside them. Cecil reads metadata; the inspected mods were not loaded/executed by these tools.

No direct compatibility blocker was identified in these installed versions. **Do not describe this as complete compatibility acceptance**, particularly automatic icon assignment, bulk edits, independent saves or another tool's Undo. No changes to MW, either third-party mod, settings or installed packages were required for this check.
