# Bike-rack first Undo stutter and intermittent failure — 0.4.220

Authorised: investigate first bike-rack Undo stutter and intermittent failure, preserve companion data/references and working Undo. No publishing or computer use.

## Evidence and findings

VERIFIED-RUNTIME baseline: 0.4.218 first rack move capture timed out on a retained FenceResidentialHigh01 typed lane; a later move reached ready but source/current-world rack disagreement blocked Undo and a subsequent prop capture. Exact omitted typed-field or raw-float cause was not established. 0.4.219 added diagnostics only; its next session completed three rack, one Pedestrian Access and one ordinary prop Undo, all source/graph/companions verified. User reports first rack Undo stutter. Logged dispatch-to-completion139ms first,120/157ms later do not measure frame stalls. See `docs/evidence/bike-rack-reload-0.4.218` and `docs/evidence/bike-rack-acceptance-0.4.219`. The intermittent failure remains unresolved; a passing diagnostic run does not prove a correction.

VERIFIED-STATIC performance issue: `StartBoundRestoration` called `NetworkRows<ObjectSubObjectInfo>` inside each root projection. `NetworkRows` materializes the full detached saved row array. The recorded mixed asset has60 roots, so this repeated an identical complete reconstruction60 times. Hoisted the materialization outside that projection: one array per preparation, with the same independent exact/native unique `ForNativeSource` resolution for every root. That resolver does not mutate rows. No source snapshot, live world/cache, companion or reference write was introduced. Preparation and rollback remain in the same order and exception boundaries. This benefits existing bound restoration paths using that preparation, without expanding eligibility.

UNPROVEN: whether this repeated reconstruction caused the reported stutter, and whether initial native rebuilding, JIT, garbage collection or another mod contributes. No native CPU/frame profiler data was available. The change removes demonstrable redundant work; no measured frame-rate improvement is claimed.

## Targeted observations

`RecoveryTiming` is pure diagnostic state with no game writes, validation cache or authority to approve Undo. `BikeRackMoveDiagnostics` ties it to the exact BikeRack01 move entry and bound restoration. No new hooks, systems, UI or file output; records go to the existing mod log. The all-prefab inventory remains off by default.

The observation summary reports:

- capture/validation tick, readiness, Undo preparation, before/rollback preparation, network capture, source-root alignment, retained-prop capture and restoration-tick counts, total/max elapsed method time;
- maximum repeated calls and accumulated elapsed time for a phase within one frame, to distinguish an expensive call from several checks in the same frame;
- time/frames from bound restoration preparation to completion; this excludes earlier shortcut queuing/readiness checks, whose existing timestamps and separate measurements remain available;
- maximum observed **previous** engine-frame duration, plus global generation0/1/2 GC-count changes since capture observation and since Undo preparation. These do not attribute all frame cost/GC to this mod;
- retained root count and at most three distinct early validation/readiness mismatches, each capped at6000 characters, in addition to the existing terminal timeout/blocked diagnostics. At most16 metric labels are retained. Detailed0.4.219 typed/source/world evidence remains enabled.

Nested timing regions are inclusive; their totals must not be added together as exclusive CPU cost. Stopwatch measures elapsed method time, not CPU sampling. Instrumentation has some overhead and first-call JIT can affect it. GC counts observe collections without forcing any collection; no forced native synchronization was added. No successful readiness decision is reused for execution. All existing source, owner, geometry, metadata, mapped-reference, ambiguity and three-frame completion checks remain.

Observation references are released on completion, cancellation of that observed move, replacement, history clearing, failure and disposal. Cancelling an unrelated move leaves the existing observation/Undo slot alone. Diagnostic logger/sampling failures do not change recovery decisions or swallow operation failures.

## Validation and next test

Managed: Phase3Safety1167 (13 timing aggregation/bound/failure checks added); BikeRackTimingSafety14 actual observer lifecycle/isolation tests; MoveUndoSafety41; BoundCompletionSafety77; SpawnAccessSafety55; EffectUndoSafety41; EffectRestoreSafety35; BoundFoliageSafety25; AreaMoveSafety97. Total1552 targeted checks. Release/UI builds pass; cached NU1900 advisory-feed warning remains. The prior online dependency audit was clean. Evidence: `docs/evidence/bike-rack-timing-0.4.220`.

Native acceptance/performance and intermittent-failure cause remain UNPROVEN. On a disposable mixed asset, perform first and repeated rack move/rotate Undo after loading, followed by Pedestrian Access and ordinary prop Undo. Repeat that sequence once after freshly reopening the unchanged asset so initial and warmed cases can be compared. Leave failed test edits unsaved. No need to repeat the already user-confirmed colour, priority or component persistence checks. Review timing and first/terminal mismatch evidence as one grouped run before deciding the next correction.

INSTALL VERIFIED 2026-10-23:0.4.220 installed after elevated Cities2 absence checks. All nine prior mod files backed up/hash-verified at outputs/rollback/MWEditorTools-before-0.4.220-20261023-164136/MWEditorTools. Installed DLL/MJS/CSS hashes match staging; assembly0.4.220.0 verified. Evidence docs/evidence/bike-rack-timing-0.4.220/installation.txt. Native timing/first/repeated acceptance pending; intermittent cause remains unresolved.

VERIFIED-RUNTIME ACCEPTED 2026-10-23:0.4.220 user reports all passed. Full log confirms11 completed Undos (4 racks,1 Pedestrian Access,3 props/foliage,1 effect,2 surface edits), no warnings/errors/timeouts. First rack restoration84.940ms across3 frames, preparation8.989ms, source alignment2.084ms, no GC during restoration. First capture-validation max62.796ms and readiness up to24.417ms/frame remain measured costs, not unique historical-stutter attribution. Earlier intermittent typed/source failure not reproduced. Evidence docs/evidence/bike-rack-acceptance-0.4.220. No new deletion/save-reopen claim.

USER-REPORTED RUNTIME ACCEPTANCE 2026-10-23: User confirms "Persistence on save is confirmed" following the passing0.4.220 grouped Undo sequence and0.4.221 diagnostic-only correction. Record persistence on save for the tested Undo results as accepted. Earlier colour/priority/component save/reopen acceptance remains applicable. This statement does not enumerate a new per-type test matrix or independently establish a fresh reopening log; no additional log/build/install review performed in this acceptance update.
