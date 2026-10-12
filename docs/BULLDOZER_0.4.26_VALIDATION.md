# 0.4.26 Everything read-only diagnostic

VERIFIED-STATIC: existing native Apply/GetAllowApply observers now inventory Everything-mode control-point targets, their owner chains and SubObject relationships. Inventory is capped at 64 unique entities and six actions. Logs component types, prefab, ownership, transform, child count and entity existence before/after Apply and at 1/5/30/120-frame checkpoints. Acceptance comes from the existing native GetAllowApply postfix. No Everything recovery images, history entries or ECS writes are added. Main/Sub-element recovery behavior remains unchanged; switching to unsupported Everything clears armed recovery history as before.

VERIFIED-STATIC: Game.dll SHA256 AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A unchanged; reuse Game0.0.0.0 MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef evidence. BulldozeToolSystem.Apply calls GetAllowApply IL_0001 (evidence/inspection.txt); native ownership/control-point fields were already verified in previous diagnostics. Actual Everything target/deletion behavior remains UNPROVEN pending runtime trace.

Release build zero warnings/errors, production UI build, settings render and 54 relevant lifecycle/snapshot/history assertions pass. This does not simulate native Everything deletion.

## Test

1. Use a disposable Test asset duplicate/editor session. Do not arm either recovery mode.
2. Select Everything bulldozer mode; delete one asset-bound grill. Wait a few seconds.
3. Place one simple standalone grill, then delete it with Everything. Wait a few seconds.
4. Reply finished, noting what disappeared. Do not attempt Undo for these Everything deletions. No editor scene saving is required. Discard disposable test state afterwards.

Unsupported: Everything recovery, broad target/graph completeness, nested/network/area reconstruction and Main persistence. This trace is evidence collection only, not a recovery promise.
INSTALLED: 0.4.26 Everything read-only diagnostic with Cities2 closed; seven installed hashes verified. Rollback: C:\Users\ian\Documents\Development\MW Editor Tools\outputs\rollback\MWEditorTools-before-0.4.26-20261009-231344\MWEditorTools. Release/UI/settings render and 54 relevant assertions pass. Next user test: Everything delete one asset-bound grill, then one standalone grill in disposable state; no arming or Undo; inspect runtime log and continue automatically within bulldozer scope.
VERIFIED-RUNTIME 0.4.26 Everything: followup-runtime.log captures owned Grill01 Entity50542:11 deletion, Test owner50525:1273 remains with21->20 children and Grill02/03 unchanged; standalone Grill01 Entity50520:2043 also deleted. No recovery attempted. User reports Grill01 remains missing after reopening Test WITHOUT restarting CS2. HYPOTHESIS: in-memory modified prefab is reused; disk persistence is UNPROVEN. Next required check: fully exit without saving, restart CS2 and reopen Test. Do not assume same-session reload discards edits; pause destructive tests on this asset pending result. No 0.4.27 source changes/package prepared.
