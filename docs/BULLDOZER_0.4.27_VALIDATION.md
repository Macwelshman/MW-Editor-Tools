# 0.4.27 Everything native payload diagnostic

VERIFIED-RUNTIME: User confirms Grill01 returns after fully restarting CS2 without saving Test. Same-session reopening retained unsaved modifications. Future disposable tests require a full restart to discard changes; do not promise that reloading alone resets them. 0.4.26 Everything follow-up trace verifies owned Grill01 deletion (Test child count21->20, remaining Grill02/03 present) and standalone Grill01 deletion. Evidence: evidence/bulldozer-0.4.26-everything-followup-runtime.log.

VERIFIED-STATIC: 0.4.27 adds read-only owning-prefab before-image and matched SaveInstance before/after payload comparison for one Everything target, plus detached typed image validation for a simple standalone target after deletion. Owner lookup is cycle checked and capped at32; retained before payload weight capped250000; six actions and64 entity inventory unchanged. No history entries, recovery writes or Everything Undo enabled. Existing Main/Sub-element recovery retained.

Native evidence reused: Game.dll SHA256 AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A; Game0.0.0.0 MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. ApplyPrefabsSystem.OnUpdate SaveInstance IL_0021, object/net/area reconstruction IL_00aa/00b7/00c4, UpdatePrefab IL_00f1 (evidence/detail.txt). Exact Everything payload deltas remain UNPROVEN until this trace runs.

Release zero warnings/errors, production UI, settings render and54 relevant lifecycle/snapshot assertions pass.

## Test

1. On a disposable Test duplicate, select Everything. Do not arm recovery.
2. Delete asset-bound Grill01, leaving Grill02/03; wait a few seconds.
3. Place then delete a standalone Grill01 with Everything; wait a few seconds.
4. Reply finished. No Undo for these deletions. Do not save the test changes. Fully exit/restart CS2 when discarding the unsaved test edits.

Everything recovery, last child/nested/network/area reconstruction and Main persistence remain unsupported/unproven. This version gathers evidence only.
INSTALLED 0.4.27: seven hashes verified with CS2 closed. Rollback: C:\Users\ian\Documents\Development\MW Editor Tools\outputs\rollback\MWEditorTools-before-0.4.27-20261009-232451\MWEditorTools. Read-only Everything owner payload at matched SaveInstance and detached standalone capture; no Everything recovery enabled. Release/UI/settings and54 relevant assertions pass. User confirmed unsaved Grill01 returns after full CS2 restart; same-session reopening does NOT discard unsaved changes. Next: user Everything delete owned Grill01 then standalone Grill01, wait checkpoints; inspect log and automatically continue authorised bulldozer scope. See docs/BULLDOZER_0.4.27_VALIDATION.md.
VERIFIED-RUNTIME0.4.27 owned Everything payload PASS: evidence/bulldozer-0.4.27-cursor-runtime.log shows bound Grill01 then bound Grill02 accepted, SaveInstance matched both; ObjectSubObjects8->7->6, componentShapeTrue, no removed components. First deletion changes EffectSource.m_Effects,ObjectSubObjects.m_SubObjects,ObjectSubNets.m_SubNets; second objects only. No diagnostic exceptions logged. Standalone attempt not captured; user reports disabled cursor after bound deletion. Cause UNPROVEN; native GetAllowApply is OriginalDeletedSystem.GetOriginalDeletedResult(0)==0 (Game0.0.0.0 MVIDe15551e9-3dea-4855-8dde-1da3cea7d7ef, IL0001/0007/000d, evidence/inspection.txt). Diagnostic postfix does not modify result. Next: Escape/reselect Everything and retry actual standalone Grill01; capture failure cause if persists. No new build/install yet.
