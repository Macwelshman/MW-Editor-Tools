# 0.4.143 — bound effect owner-marker rebuild

VERIFIED-RUNTIME: `docs/evidence/undo-0.4.142-effect-marker-runtime.log` records the bound effect move and strict Undo completion at 14:50:55.437, but contains no `Effect hover marker queued` message. The user confirms the actual effect returns while the marker remains stale. Therefore the active default tool's cached hover entity was not the effect entity, and 0.4.142 correctly made no cache write.

VERIFIED-STATIC: the inspected native `DefaultToolSystem.Update(JobHandle)` reuses temporary marker definitions while the raycast entity equals `m_LastRaycastEntity`; changing that cache causes `UpdateDefinitions` to destroy and rebuild definitions from current entity/subobject data. Evidence remains in `docs/evidence/effect-default-hover-native.txt`. Bound effect capture records the exact direct owner and isolated restoration proves that owner identity and its subobject membership before Undo.

Correction: after successful effect move restoration, invalidate `DefaultToolSystem.m_LastRaycastEntity` only when it exactly equals either the restored effect or that effect's captured direct owner. The next native update rebuilds the hovered parent asset's temporary definitions, including the restored effect marker. A different hovered entity is preserved. Standalone moves retain their effect-only match because their owner is `Entity.Null`.

Deletion recovery, effect/source restoration, selection and selected index, cursor position, highlighted components, tool mode, renderer allocation data, parent/subobject data and all safety checks are unchanged. The runtime log now includes both restored target and matched hover entity when invalidation occurs.

VERIFIED-STATIC: Release build succeeds with zero warnings/errors. EffectRestoreSafety passes 35 actual-source assertions, covering exact effect and exact owner invalidation plus preservation of another hovered entity. EffectUndoSafety passes 41 lifecycle assertions. Native marker refresh remains UNPROVEN.

Acceptance: in a fresh disposable harbour copy, move one existing main-building effect and press Ctrl+Z while leaving the cursor still over the asset. Confirm the effect and marker both return promptly. Deletion need not be repeated. No original asset overwrite, computer use or publishing.

CURRENT 2026-10-18: 0.4.143 installed after the guarded installer verified Cities2 absent. Five installed hashes and 151 rollback files verified. Rollback: `outputs/rollback/MWEditorTools-before-0.4.143-20261018-145246/MWEditorTools`; hash evidence: `docs/evidence/recovery-0.4.143-installed-hashes.csv`.

ACCEPTED-RUNTIME 2026-10-18: user confirms the effect and marker both return correctly with the cursor kept still. Exact-owner hover invalidation is accepted for the tested bound harbour effect move. Existing bound effect deletion Undo was already accepted on 0.4.141. Remaining grouped effect gate is a disposable single-effect save/reopen check for last-source recreation and persistence.

ACCEPTED-RUNTIME 2026-10-18: user confirms save/reopen persistence passes. The direct effect move/deletion stage, including last-source recreation and persistence, is complete on installed 0.4.143.
