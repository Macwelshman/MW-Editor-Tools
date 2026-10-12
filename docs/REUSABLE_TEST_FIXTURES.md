# Reusable bulldozer Undo fixtures

VERIFIED-STATIC: The saved MW Nested Persistence Test asset and its nested prefab dependencies exist in the CS2 ImportedData/MW Nested Persistence Test folder. A complete hash-verified backup is preserved at outputs/reusable-fixtures/MW Nested Persistence Test; manifest: docs/evidence/reusable-nested-fixture-hashes.csv.

VERIFIED-RUNTIME (previous user confirmation, 0.4.52): the saved controlled three-level Grill01 fixture survived a full CS2 restart with all three props present, without recreating the fixture. Recovery evidence is recorded in NESTED_PERSISTENCE_BASELINE.md.

Use this saved asset as the controlled nested-test baseline. Load a fresh working copy for each run. Do not recreate its props, overwrite the baseline, or save deletion-test results over it. Preserve all dependency files together. A same-session reload may retain modified prefab state; use a fresh native copy or restart when discarding a run.

VERIFIED-RUNTIME (0.4.64/0.4.65): the user subsequently saved **MW Carport Undo Test** and **MW Mixed Prop Undo Test**. Both support the two-Carport03 automatic check; the mixed fixture also contains grills, a road network, a fence lane, areas and an effect. 0.4.65 logs capture one nonempty lane descriptor and verify payload preservation through Undo2/Redo2/Undo2. Their actual placements were accepted by the bounded ownership/descriptor checks; the earlier suggested coordinates are not required. See BULLDOZER_0.4.64_VALIDATION.md and BULLDOZER_0.4.65_VALIDATION.md. Do not recreate either fixture. Extras are preservation targets, not supported bulldozer Undo targets.

Batch compatible manual checks in the same session after successful restoration. See BULLDOZER_SESSION_BATCH.md. Restart for a new installed build or a recovery failure; same-session reload alone may retain modified prefab state.

Future automated checks should validate the chosen fixture's actual schema, references and expected group counts before making changes; its name alone is not evidence that it is suitable.

VERIFIED-RUNTIME 0.4.69: MW Mixed Nested Undo Test accepted with Carport01 and Carport03, two groups/six nodes, automatic two-deletion cycle passed. User completed the setup/test instructions including creating the new baseline; independent disk-file hash/persistence verification of this new fixture remains unproven. Reuse this named baseline for mixed-type coverage rather than recreating it.

MW Simple Prop Undo Test: three-grill disposable baseline used successfully for 0.4.71 simple-only adoption and two-deletion history. Retain for the full-set/final-prop batch; never save diagnostic deletion edits over the baseline.
