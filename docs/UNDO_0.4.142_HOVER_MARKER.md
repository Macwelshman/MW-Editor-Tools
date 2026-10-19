# 0.4.142 — effect move hover-marker rebuild

VERIFIED-RUNTIME: `docs/evidence/undo-0.4.141-effects-runtime.log` records a bound move refresh submitted before native culling at 14:40:00.164 and strict restoration completion at 14:40:00.584. The user confirms the actual effect returns correctly, but its editor marker remains at the moved location until the cursor leaves the asset. Bound deletion refresh submitted at 14:40:17.200 and strict restoration completed at 14:40:17.622; the user accepts deletion Undo.

VERIFIED-STATIC: installed `Game.dll` version 0.0.0.0, MVID `e15551e9-3dea-4855-8dde-1da3cea7d7ef`, SHA256 `AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A` remains the inspected assembly.

- `Game.Tools.DefaultToolSystem.Update(JobHandle)` IL_0022–0049 returns without rebuilding definitions when the current raycast entity equals `m_LastRaycastEntity` and the raycast has not changed.
- When it differs, IL_004a–0076 updates `m_LastRaycastEntity` and calls `UpdateDefinitions`.
- `UpdateDefinitions` IL_0000–0013 destroys the previous temporary definitions; IL_0024–02f4 schedules creation using the entity’s current transform and editor-container data.
- `DefaultToolSystem.OnStartRunning` IL_000c clears `m_LastRaycastEntity`. This is the native invalidation behavior reproduced when cursor focus leaves or the tool restarts.

Evidence is preserved in `docs/evidence/effect-default-hover-native.txt`. The editor gizmo and highlight paths are separately recorded in `effect-marker-refresh-native.txt` and `effect-hover-marker-native.txt`.

Correction: after a successful effect **move** restoration, if the active tool is `DefaultToolSystem` and its cached raycast entity is exactly the restored effect, set only `m_LastRaycastEntity` to `Entity.Null`. The next native tool update rebuilds its temporary marker from the restored transform even when the cursor has not moved. A cache entry for another entity is untouched. Deletion behavior, target data, tool selection, selected entity/index, cursor position, `Highlighted`, control points, native render allocations and all recovery guards are unchanged.

VERIFIED-STATIC: Release build succeeds with zero warnings/errors. EffectRestoreSafety passes 34 actual-source assertions, including matching-cache invalidation and preservation of a different hovered entity. EffectUndoSafety remains 41 assertions; the 0.4.141 deletion settlement and rendering correction is unchanged. Managed tests do not prove the native marker rebuild.

UNPROVEN: prompt native marker refresh in 0.4.142. Acceptance is one fresh disposable harbour copy: move an existing main-building effect, Ctrl+Z, and keep the cursor still over the asset. Confirm both actual effect and marker return promptly to the original location. Deletion need not be repeated unless move testing exposes a regression. Persistence remains pending. No original asset overwrite, computer use or publishing.

CURRENT 2026-10-18: 0.4.142 INSTALLED after the guarded installer verified Cities2 absent. Five installed hashes and 150 rollback files verified. Rollback: `outputs/rollback/MWEditorTools-before-0.4.142-20261018-144921/MWEditorTools`; hash evidence: `docs/evidence/recovery-0.4.142-installed-hashes.csv`.
