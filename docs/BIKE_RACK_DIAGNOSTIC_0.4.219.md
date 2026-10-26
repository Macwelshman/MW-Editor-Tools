# Bike-rack move Undo diagnostic — 0.4.219

VERIFIED-RUNTIME: user reports bike-rack move Undo fails during the combined persistence checks. Preserved 0.4.218 log and analysis under `docs/evidence/bike-rack-reload-0.4.218`. First capture times out on a FenceResidentialHigh01 typed lane comparison. Second move reaches ready, then a retained rack source/current-world discrepancy blocks Undo and a subsequent Grill01 capture. No Undo completion in that session. Earlier user colour/priority/component save/reopen acceptance remains valid.

VERIFIED-STATIC/MANAGED: a fixture reproducing all printed fence curve values is accepted by the existing native rounding rules and by BikeRackMovePolicy.Validate. Therefore the first rejection cannot be attributed to those printed heights alone. Existing text omits garage/car/pedestrian/slave values and prints shortened float values. The rack's later source mismatch reproduces refusal: saved Y-0.01330566 and quaternion(-0.008925325,0.9999602,0,0) versus current/cache local Y0 and quaternion(0,1,0,0). No current-world projection proves that saved row. Treat a grounding/cache transition as HYPOTHESIS until exact witnesses identify it. The 0.4.218 cleanup did not alter restoration/matching rules; causality from the cleanup is UNPROVEN.

0.4.219 changes diagnostic output only:

- Survivor mismatch names the actual failing typed predicates plus used/mapped reservation state.
- Curves/lengths print invariant round-trip float values. Garage/car/pedestrian/slave scalar fields and schema are included, alongside existing path/owner/seed/parking data.
- Source association refusal prints round-trip cached/source/native poses and complete guarded owner/current-world transforms.
- Startup logs the actual loaded assembly version.

No source or cache correction, curve write, new hook or validation relaxation. Original companion/reference/source/native completion safeguards remain unchanged. The new diagnostic methods are not inputs to recovery decisions. The all-prefab inventory remains opt-in.

Validation: Phase3Safety1154 (10 new diagnostic/reproduction checks), MoveUndoSafety41; Release and UI builds pass. Existing cached NU1900 build warning remains; the separate online dependency audit passed in the preceding review. LifecycleSafety20 passes, including actual Mod constructor bypass and cleanup fault injection. Native failure cause/fix remains UNPROVEN.

Next smallest grouped disposable test after installation: fresh-load BikeRack01 move/Undo, repeat the rack move/Undo, then Pedestrian Access Location and an ordinary prop. Keep the failed test asset unsaved so its baseline remains available. Exact log results determine the next correction; no new broad persistence test requested until Undo works again. No publishing or computer use.

INSTALL VERIFIED 2026-10-23: installed0.4.219 after elevated Cities2 absence checks. Previous nine files backed up/hash-verified at outputs/rollback/MWEditorTools-before-0.4.219-20261023-160145/MWEditorTools; installed DLL/MJS/CSS hashes match staging. See docs/evidence/bike-rack-diagnostic-0.4.219/installation.txt. Diagnostic build only; cause/correction awaits exact native mismatch evidence.

VERIFIED-RUNTIME 2026-10-23:0.4.219 grouped move sequence passed: three BikeRack01, one Pedestrian Access and one FenceIndustrialPieceLow02 prop Undo, all source/graph/companions verified, no warning/error/mismatch/timeout. User reports first rack Undo stuttery. Logged dispatch-to-completion139ms first versus120/157ms later rack; these timestamps do not profile frame stalls.0.4.218 failure not reproduced; underlying intermittent cause remains unresolved because0.4.219 changes diagnostics only. No new deletion or save/reopen result. Evidence docs/evidence/bike-rack-acceptance-0.4.219. No code/install/publishing changes in review.
