# 0.4.18 multiple Sub-element deletion history

User authorised continuation to multiple-deletion history after validating 0.4.17 single-prop Undo/Redo/save-reopen. Same Phase 3 scope; Main/Everything, last-object and nested targets remain unsupported. No publishing or later phase work.

Arm Sub-element history test once on a saved disposable duplicate. Each accepted native action must remove exactly one prop with another remaining. Consecutive supported actions retain before/after prefab payloads and semantic graphs. Undo visits newest first; Redo oldest undone first. New deletion after Undo discards the Redo branch. Same asset and owning entity required. Other edits, unsupported operations, load/context/settings changes, explicit clear or arming again invalidate history. Configured history depth applies; retained snapshot weight capped at 500000 units, starting fresh history when next entry exceeds total budget. Each entry must independently meet existing 500000 limit.

Uses the existing TransactionHistory actual source. Commands choose Latest/NextRedo, validate current payload/graph, and retain a pending entry without advancing history. Cursor moves only when deferred native graph validation succeeds and pending entry identity still matches. Original 120-frame window and strict equality retained.

VERIFIED-STATIC: Release zero warnings/errors, production UI, Settings render; 413 assertions (25 bulldozer lifecycle/history,22 snapshots/history,18 protection,311 shortcuts/settings,11 lifecycle,26 Options). New history checks cover triple ordering, pending/failed validation, stale completion rejection, Redo branch, depth and invalidation. They do not simulate native multi-deletion regeneration.
VERIFIED-RUNTIME: 0.4.17 single-prop Undo/Redo/Undo logs and user save/reopen pass preserved. Multi-entry live acceptance remains UNPROVEN.

## Test

1. Open a fresh saved disposable duplicate containing Grill01/02/03 and other props. Select asset and click Arm Sub-element history test ONCE.
2. Bulldoze Grill01, Grill02, Grill03 individually in Sub-element mode. Wait for each recorded deletion. Undo count should reach 3.
3. Undo three times, waiting for each completion. Grills should return in reverse deletion order with original properties.
4. Redo three times. Grills should disappear in original deletion order.
5. Undo twice, then delete a different remaining prop. Confirm Redo clears and Undo restores that new deletion first.
6. Undo to the desired state, save the duplicate and reopen it. Check all props/transforms/probabilities/effects/networks/areas.
7. If any failure, reload saved duplicate before saving and report the status. Reply finished after testing.

Ordinary list recovery remains a separate history; arming clears list history and supported list edits invalidate bulldozer history.

VERIFIED-RUNTIME 0.4.18: preserved docs/evidence/bulldozer-0.4.18-runtime.log shows three consecutive deletions (Objects 8 to7 to6 to5), UndoCount1/2/3, three successful Undo completions with payload and semantic child graph verified. New deletion after all three Undo records one entry, clearing prior redo branch; then Undo and Redo of new entry succeed. No errors in captured log. Full three-entry Redo sequence and multi-entry save/reopen are not evidenced by this run and remain UNPROVEN. User said finished, not explicit full acceptance.



VERIFIED-RUNTIME 0.4.18 follow-up: docs/evidence/bulldozer-0.4.18-followup-runtime.log confirms three recorded deletions, then three successful Undo and three consecutive successful Redo completions at 22:27:22-29 log time, each exact payload/semantic graph validated. No errors in preserved log. User reports test finished. Save/reopen visual/data result is not established by this log; await user confirmation before marking that gate passed. Main/Everything and other unsupported scopes unchanged.



VERIFIED-RUNTIME (user-confirmed): User explicitly answered Yes when asked whether the 0.4.18 multiple-deletion duplicate was correct after saving and reopening. Multi-entry Sub-element Undo/Redo, new-deletion redo branching, and tested save/reopen gates pass. Installed 0.4.18 retained. Scope remains individually deleted props with other objects remaining on one armed asset/owner; Main/Everything/nested/last-child unsupported. No next phase or publishing authorised by acceptance.


