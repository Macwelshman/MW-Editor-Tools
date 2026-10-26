# 0.4.220 runtime acceptance and timing — 2026-10-23

VERIFIED-RUNTIME: user reports all passed. Loaded0.4.220.0. Full log preserved locally (last write16:45:18,23931 bytes). Eleven completed Undos on MW Mixed Test Components:

- BikeRack01:16:43:32.506,16:43:38.741,16:43:44.312,16:44:34.364.
- Pedestrian Access Location:16:43:50.747.
- Grill01:16:43:56.797 and16:44:42.090.
- FlowerBushCity01:16:44:01.540.
- FireBigVFX isolated bound effect:16:44:09.032.
- Sand Surface01 node addition:16:44:58.763; surface move:16:45:08.838.

All complete with source/graph/companions or isolated effect placement/values verified. No WARN/ERROR, timeout, persistent typed mismatch or source-association refusal. Each first bike validation briefly reports pending/ambiguous companion data, then settles before readiness. Effect's busy preflight message occurs during its active restore, then succeeds. Whole-building move exclusion is expected unsupported scope.

Completed bike observations:

| Cycle | Restore wall ms | Restore frames | Preparation ms | Source-root alignment ms | Max capture-validation call ms | Max readiness cost per frame ms | GC during restoration (0/1/2) |
|---|---:|---:|---:|---:|---:|---:|---|
| First |84.940|3|8.989|2.084|62.796|24.417|0/0/0|
| Repeat |75.545|3|6.202|0.798|42.772|19.038|0/0/0|
| Repeat |85.334|3|6.054|0.759|41.324|17.821|1/1/1|
| Later repeat |70.101|3|6.422|0.811|42.632|15.036|0/0/0|

Intervals start at restoration preparation, excluding earlier readiness/shortcut queuing. First observed previous-engine-frame peak88.435ms, with the longest first capture-validation call62.796ms. These observations reveal remaining capture/readiness cost; they do not establish a unique cause of earlier visible stutter. No GC occurred within the first measured restoration window. Source alignment is now one materialisation for60 roots. No controlled before/after performance benchmark or verified causal resolution of the0.4.218 intermittent failure is claimed.

Diagnostic-only issue: cycles4–11 repeatedly supersede when pending new capture and readiness polling of the previous Undo entry alternate. Undo succeeds and remains unchanged.0.4.221 keeps the pending probe authoritative for measurement only, ignoring previous-entry readiness samples; no readiness decision/Undo data is changed.

Scope: this reported first/repeated rack and wider move sequence accepted; failure not reproduced. The log does not establish two separate fresh asset reopen cycles or deletion/save/reopen acceptance. Prior colour/priority/component persistence acceptance remains valid. No need to repeat this accepted sequence for the diagnostic-only bookkeeping correction.

USER-REPORTED RUNTIME ACCEPTANCE 2026-10-23: User confirms "Persistence on save is confirmed" following the passing0.4.220 grouped Undo sequence and0.4.221 diagnostic-only correction. Record persistence on save for the tested Undo results as accepted. Earlier colour/priority/component save/reopen acceptance remains applicable. This statement does not enumerate a new per-type test matrix or independently establish a fresh reopening log; no additional log/build/install review performed in this acceptance update.
