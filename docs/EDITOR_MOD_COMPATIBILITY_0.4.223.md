# Additional editor tools checked with MW0.4.223

Read-only review2026-10-23. Initial logs were stale; after the user launched the game, the fresh17:39 session confirms **Anarchy1.8.0, Move It0.5.16, Better Bulldozer1.3.19 and Tree Controller1.7.5** enabled alongside the previous playset. No mod, game file, setting, playset or installation was changed by this review. No computer use or publishing.

## Observed startup

VERIFIED-RUNTIME: `Modding.log` loads MW.EditorTools0.4.223.0 and all four tools. MW's component confirmation, list recovery, native deletion, object move and area move hooks attach. At17:39:52.168 the Asset Icon Creator1.4.1 compatibility adapter reports successful installation. The checked Modding/MW/UI/four-tool logs contain no ERROR, CRITICAL, exception or patch-conflict match at the recorded checkpoint. Move It logs Editor preload. This verifies startup co-loading and adapter attachment; no new move/deletion/thumbnail/save acceptance is inferred.

## Findings

| Tool | Direct patch comparison | Practical limit |
|---|---|---|
| Anarchy1.8.0 | VERIFIED-STATIC: seven declared targets, none a native MW Undo target or the Asset Icon Creator callback | Editor override prevention can add unsupported protection/transform components and enforce positions. Some affected props/markers can be refused for MW Undo. This is a data compatibility limit without a Harmony collision. |
| Move It0.5.16 | Hash matches the previously inspected binary; no Harmony reference/attributes in that inspection | Custom Move It actions are not the native move workflow MW captures. Do not treat its edits as covered by MW Undo or assume coordinated history. |
| Better Bulldozer1.3.19 | Hash unchanged; patch targets InitializeRaycast and GetRaycastResult differ from MW targets | Its custom sub-element operations are outside MW's advertised native-tool capture support. |
| Tree Controller1.7.5 | Hash unchanged; WindControl.SetGlobalProperties, ToolbarUISystem.Apply and ToolUISystem.SetBrushStrength differ from MW targets | Tree-state edits can invalidate a prior snapshot. Combined tree-edit/Undo behaviour remains untested. |

UI registration scan identifies Game/Editor overlays and game tool-options/toolbar/selected-info targets, not MW's editor Hierarchy or EditorSectionBase extension targets. That reduces a direct wrapper collision risk, but does not verify every overlay/focus/shortcut interaction. Existing Find It, Workflow Kit and thumbnail-adapter limitations remain documented in earlier reviews.

## Anarchy's meaningful interaction

VERIFIED-STATIC: `Anarchy, Version=1.8.0.0`, MVID `6a916474-e521-436f-987b-3e623f5cf195`, SHA256 `478B647F959D2051F1D7DBC5C22176AFF9EDA9D7453F4DCDF7E305C3DB56CFB9`.

Declared Harmony targets: NetToolSystem.InitializeRaycast; ToolbarUISystem.ActivatePrefabTool/Apply/OnUpdate; ToolUISystem.GetElevationRange; UniqueAssetTrackingSystem.IsPlacedUniqueAsset/OnCreate. Anarchy references Harmony2.2.2.0; the current game log loads2.4.2.0 and MW hooks succeed, so the reference alone is not a runtime version conflict.

`AnarchyPlopSystem.OnUpdate` checks editor mode at IL_0019..001e and `PreventOverrideInEditor` at IL_002f, then can add/set `Anarchy.Components.TransformRecord` (IL_0336/036a). Its selected system IL also refers to PreventOverride. `ResetTransformSystem.OnUpdate` checks editor mode at IL_0007..000c and that setting at IL_001d; the early-return branch IL_002a..002d skips its processing when editor prevention is disabled. When active, this system consumes recorded positions/rotations and can reapply transforms. Do not claim Anarchy is entirely inactive in the editor merely because some patches target InGame UI classes.

VERIFIED-STATIC: MW `MainPropCapture.CaptureMainProp` and `NestedGraphImage.CaptureNestedImage` reject unknown ECS component types. `PropComponentSchema.Known` does not include Anarchy's TransformRecord/PreventOverride components. Therefore an entity carrying such components cannot pass these typed prop/group capture routes. Existing ownership/reference and source/geometry checks must remain intact; simply allowing unknown Anarchy types would advertise recovery without preserving their data. No Anarchy compatibility adapter has been implemented or requested in this check.

HYPOTHESIS/UNPROVEN: editor protection, manual Anarchy component tools or interleaved elevation/tree edits could refuse or interfere with a particular supported move/deletion Undo. The user's saved Anarchy settings and live entity archetypes were not read, and no such native failure appears in this startup log. New patch-conflict monitoring cannot report every independent ECS data write; startup success is not full functional compatibility.

## Evidence and next useful check

`docs/evidence/editor-compatibility-0.4.223`: current log snapshots/error scan; exact Anarchy identity/attributes/call offsets in `assembly-overlap.json`; selected patch and registration IL in `selected-details.json`; editor transform/protection system IL in `selected-systems.json`; unchanged hashes for the three previously inspected tool assemblies in `reused-tool-hashes.json`; UI target/hash inventory. Cecil inspected assemblies without executing them.

One grouped disposable-asset acceptance is sufficient to establish the next evidence: normal native prop/marker/rack move and deletion Undo with this playset; check Anarchy protection settings if capture refuses; keep custom tool Undo separate; thumbnail Group/Priority retention and save/reopen. No native acceptance is claimed by the static scan or startup log alone.
