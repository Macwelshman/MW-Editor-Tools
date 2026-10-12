# 0.4.25 Main prop deletion history

VERIFIED-STATIC: Main recovery now retains multiple individually deleted standalone props. Each entry holds its typed image and its own recreated entity identity. Undo/Redo commit the history cursor only after deferred native initialization/property validation or confirmed deletion. New deletion clears the Redo branch. The configured history depth applies; each colour buffer remains capped at 256 entries. Unknown components, ownership and nonempty related buffers remain refused. Existing validated Sub-element recovery is retained.

VERIFIED-STATIC: Release build has zero warnings/errors; production UI and settings render pass. Six safety suites pass 420 assertions (25 bulldozer/history, 29 snapshots, 18 Phase 2, 311 Phase 4, 11 lifecycle, 26 options). Native factory and typed equality remain the 0.4.24 validated route. Main multi-entry runtime behavior is UNPROVEN pending this test. Native assembly identity and method evidence remain recorded in BULLDOZER_0.4.24_VALIDATION.md and evidence/main-applied-markers.txt.

## In-game test

Use a disposable editor session. Editor scene saving is not required or claimed supported.

1. Place three simple standalone props (Grill01, Grill02, Grill03), with distinct positions, outside asset ownership.
2. Click Arm Main prop history test once. In Main bulldozer mode, delete the props individually, waiting for each recorded status.
3. Undo three times, waiting for completion each time. Check each original position/appearance and exactly one copy.
4. Redo three times, waiting for each completion. All three should disappear again.
5. Undo twice. Delete a different simple standalone prop. Check Redo is cleared; Undo and Redo the new deletion.
6. Report finished and any visual mismatch/error. On error discard the disposable editor test state (reopen the asset/restart editor as needed).

Everything, linked/nested objects, last-child recovery and Main scene persistence remain unsupported/unproven. No later phase or publishing is included. After success automatically continue within authorised bulldozer scope.

INSTALLED: 0.4.25 with Cities2 closed; all seven copied SHA256 hashes verified (docs/evidence/bulldozer-0.4.25-installed-hashes.csv). Validated 0.4.24 preserved at outputs/rollback/MWEditorTools-before-0.4.25-20261009-230724/MWEditorTools. Next: user Main three-entry Undo/Redo and new-deletion branching test; then automatically continue authorised bulldozer investigation after success.
VERIFIED-RUNTIME 0.4.25: evidence/bulldozer-0.4.25-main-runtime.log confirms Grill01/02/03 individually recorded; three Undo then three Redo completions validate per-entry properties/new identities/deletion. Two further Undo then a new Grill03 deletion records Undo=2, dropping the two-entry Redo branch. No recovery errors. New-branch Undo/Redo and visual result are not independently evidenced by this log. User reports finished. Automatically continue Everything read-only investigation.
