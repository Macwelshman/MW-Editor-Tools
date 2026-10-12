# 0.4.15 synchronous finalization correction

Repeat the bounded disposable-duplicate test in BULLDOZER_0.4.14_VALIDATION.md: select duplicate, Arm, delete ONE Sub-element grill leaving others, Undo, Redo, Undo. Check transforms, probabilities and related effects/networks/areas. If recovery fails, reload the saved duplicate before saving.

VERIFIED-RUNTIME: 0.4.14 captured and recorded the deletion, then timed out after requesting Undo. Preserved log: evidence/bulldozer-0.4.14-timeout-runtime.log. Successful regeneration in that attempt remains UNPROVEN.
VERIFIED-STATIC: Game 0.0.0.0 MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef, PrefabSystem.OnUpdate IL_0020 calls ReplacePrefabSystem.FinalizeReplaces before returning. FinalizeReplaces IL_0006 synchronously calls the nested Finalize system Update. Evidence: evidence/bulldozer-synchronous-finalization.txt.

0.4.14 incorrectly armed the finalization observer in the outer OnUpdate postfix, after finalization had already happened. 0.4.15 arms it on matching restoration ReplacePrefab dispatch, before the finalization call. The outer postfix does not restart a recovery already completed by the nested callback. Exact payload/ownership/transform checks and cursor-after-validation remain required. No timeout extension or validation bypass.

VERIFIED-STATIC: Release zero warnings/errors, production UI build, 14 bulldozer lifecycle and 22 snapshot/history checks pass. Live Undo/Redo and rollback remain UNPROVEN. Same user-approved single-prop scope; no new phase.

Installed with Cities2 closed; eight file hashes verified. Rollback: C:\Users\ian\Documents\Development\MW Editor Tools\outputs\rollback\MWEditorTools-before-0.4.15-20261009-215932
