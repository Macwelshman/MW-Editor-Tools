# Viewport source-row mismatch diagnostics — 0.4.159

VERIFIED-RUNTIME: user accepted foliage single/batch replacement on 0.4.158. Effects and props with effects fail with source-row/type mismatch. Recorded attempts copy LightpolePark04 as ObjectSubObjectInfo and reject destination entities 50975/50972 before writes. Grill01 object replacement completes. No direct effect-copy attempt is present in that log.

0.4.159 records the copied prefab/type, exact hit prefab/type, EditorContainer prefab/type, owner root, component names and all source-row reference names when matching fails. The user-facing refusal now names the copied and target prefabs. Matching and mutation behavior is unchanged; this is a diagnostic build, not a claimed correction.

VERIFIED-STATIC: Release and production UI builds passed. Release emitted NU1900 for unavailable vulnerability metadata. Runtime cause remains UNPROVEN.

Grouped reproduction: one failed effect-to-effect attempt and one failed prop-with-effects attempt, noting source and target names. Stop without saving the disposable asset. Use the resulting identity diagnostics to select the correction; do not relax source ownership or permit cross-type replacement.

## Acceptance update — 2026-10-19
VERIFIED-RUNTIME: user reports passed. Log confirms viewport FireBigVFX effect copy, single effect replacement at source index 9, and batch replacement of three matching effects at index 10. No target mismatch is recorded in this session. Props-with-effects pass is user-reported; this log contains no corresponding prop replacement attempt. 0.4.159 changed diagnostics only, so the earlier mismatch cause remains UNPROVEN; do not attribute acceptance to a targeting correction. Foliage single/batch remains accepted on 0.4.158.


VERIFIED-RUNTIME (user acceptance, 2026-10-19): persistence passed after saving and reopening the disposable copy. This closes the requested persistence acceptance for the tested Phase 5 element replacements. Earlier intermittent type/source mismatch cause remains UNPROVEN; no targeting correction was introduced in 0.4.159.

