# Foliage destination validation — 0.4.157

VERIFIED-RUNTIME: the user reports that 0.4.156 paste removes the visible target. Its retained-donor checks passed, but did not establish that the replacement rendered. The cause remains HYPOTHESIS.

0.4.157 requests native Updated/BatchesUpdated on freshly initialized candidates and clears a matching default-tool hover cache. It checks replacement prefab, destination pose, unique owner link and mesh/bounds presence where the donor had them. Donor preservation alone no longer reports completion. Failure logs candidate components, mesh count, radius and Hidden state, blocks further replacement, and attempts source rollback only if the source still matches the operation's after-image. Native asynchronous rollback is UNPROVEN; discard the disposable session on failure.

VERIFIED-STATIC: Release and production UI builds passed. Managed checks: 18 replacement foliage creation/cleanup, 14 hover/ownership, 25 bound foliage, 428 snapshot/history, and shared Undo UI regression passed. Release emitted NU1900 because package vulnerability metadata was unavailable. These checks do not prove native rendering or the hypothesized correction.

Acceptance: in a disposable asset, copy FlowerBushCity01/03 and replace one target, then replace all. Require visible replacements at the target positions, unchanged copied source and correct counts. Check browser and viewport paths. Do not save over the original. If a target disappears, stop and retain the log for diagnosis.
