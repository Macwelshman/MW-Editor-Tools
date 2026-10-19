# 0.4.170 — last replacement Undo

The user explicitly authorised Undo for the last single or batch replacement on 2026-10-19. This extends the existing shared one-action Undo slot. No computer use or publishing.

## Behaviour

A successful replacement records its original list, exact completed list, changed underlying indices, original and replacement prefab identities, selected asset, root/component identities and supported source-list guards for the owning asset and nested ancestors. Ctrl+Z, the shared header Undo and inspector Undo dispatch to the same replacement action. A batch restores the full action in one step, retaining rows that already used the replacement prefab. Materialized snapshots preserve original row order, references, placement and settings. No Redo or earlier-action stack is added.

Replacements require enabled, available Undo recovery before any write. Existing same-prefab no-ops return before clearing the prior action. A newer replacement replaces the slot; existing move/deletion commits, unrelated writes, loads/context changes, settings changes and explicit Clear invalidate it through the established recovery dispatcher. Existing native recovery implementations, LastDeletion, PrefabSnapshot and deferred Ctrl+Z gates are unchanged.

Undo refuses while foliage is settling or after a native foliage validation failure. It requires exact current source snapshots, live references, unchanged selected asset, writable roots, component identities, activation and ownership. It pre-materializes before/after images before mutation. Restoring original foliage uses the existing candidate planner with the original prefab and only the captured changed indices, avoiding pre-existing matching rows. The established witness/render completion checks apply to regenerated foliage after Undo. Restoration failure restores the post-paste source; rollback failure blocks further replacement writes.

## Verification

VERIFIED-STATIC: Release and interface builds pass. All 26 managed suites and nine interface scripts pass, including 34 actual-source replacement snapshot, single/batch, reverse foliage plan, shared-slot and rollback checks. Existing 428 Phase 3 snapshot/history assertions and the production native-buffer safety gate pass. Lifecycle constructor-bypass tests pass with the new callback wiring. No changes to src/Modules/BulldozerRecovery, LastDeletion or PrefabSnapshot. Existing NU1900 vulnerability-metadata warning remains. Evidence: evidence/replacement-undo-0.4.170/.

UNSUPPORTED/UNPROVEN: in-game replacement Undo regeneration, appearance and persistence are pending native acceptance. Grouped disposable-asset check: single wall-post replacement then Undo; batch replacement then one Undo (retain a prop already using the replacement prefab); foliage replacement then Undo after regeneration; one previously accepted move/deletion then Undo to check shared-slot sequencing. Confirm placement/settings and save/reopen only a disposable asset. No computer use or publishing performed.

INSTALLED: 0.4.170 while Cities: Skylines II was fully closed. Complete prior mod folder backed up under outputs/rollback/MWEditorTools-before-0.4.170-20261019-190301. All 10 rollback files and seven installed file hashes verified; installed assembly version 0.4.170.0. Evidence: evidence/phase5-0.4.170-installed-hashes.csv.
