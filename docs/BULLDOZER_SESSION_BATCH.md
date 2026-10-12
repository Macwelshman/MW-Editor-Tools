# One-session check: 0.4.65 mixed-asset history

Use the current restored MW Mixed Prop Undo Test working copy after its successful automatic check, or a fresh native copy of the saved baseline. Do not reload a modified baseline in the same session as a way to discard edits. Do not save test edits. No computer use or new installation is needed.

The automatic two-carport cycle and lane payload preservation already passed. Do not repeat that check. This batch checks manual history transitions with the newly included lane snapshots. Earlier versions passed related history gates; these are focused regressions of the expanded payload, not new universal support claims.

1. Click **Enable nested prop Undo**. Wait until the panel says **Nested prop Undo enabled** before deleting anything.
2. Everything-delete one carport. Wait about ten seconds, then click **Undo**. Check it returns once, in its original position. This checks delayed manual Undo.
3. Everything-delete the other carport. Check **Redo** is unavailable: the new deletion must replace the earlier redo branch. Click **Undo** and check both carports are back. This checks redo branch replacement with lane-bearing snapshots.
4. Select a different **asset**, then return to the mixed test asset. Check both **Undo** and **Redo** remain unavailable. This checks history invalidation across assets.

Report all three results together: delayed Undo / redo branch / asset switch. Leave CS2 open so the log can be inspected. If an error or Stopped message appears, stop this batch and report the message; do not continue editing or save.

UNPROVEN: these manual transitions on 0.4.65 until the batch is completed. No network/lane/area/effect deletion recovery is requested. Existing automatic sequence remains VERIFIED-RUNTIME.

VERIFIED-RUNTIME 0.4.65 manual session batch: user confirmed all requested checks. Log independently shows mixed-asset deletion at 12:11:43.544, delayed Undo started 12:11:57.867 and completed with payload/graph/nested ownership verification 12:11:57.920; alternate deletion recorded 12:12:12.719 and Undo verified 12:12:22.948 (two groups/eight nodes restored). Nonempty lane payload captured; only object list changed. Redo unavailability after alternate deletion and both history buttons remaining unavailable after other-asset selection/return are user-reported runtime confirmation, not independently logged button-state assertions. Evidence docs/evidence/bulldozer-0.4.65-manual-batch-runtime.log. Batch accepted; do not repeat. No saves or computer use requested.
