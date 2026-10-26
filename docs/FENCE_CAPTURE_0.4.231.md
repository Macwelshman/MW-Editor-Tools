# Network fence deletion capture validation — 0.4.231

User authorised fixing failed fence capture validation while preserving companion data/references and working Undo/outlines.

## Evidence and fix

VERIFIED-RUNTIME (0.4.230 logs): two FenceResidentialHigh01 captures failed with the generic network graph/source-descriptor mismatch, while the subsequent last Invisible Car Path deletion completed. The reviewed logs did not identify an individual mismatching retained lane field.

VERIFIED-STATIC: AreaCompanions captures ParkingLane, GarageLane, CarLane, PedestrianLane and SlaveLane flags and typed values, and AreaCompanionNetwork requires them in exact comparison. NetworkDeletionPolicy reconstructed retained items through an older partial field initializer that omitted all five typed lane groups. Therefore a fence deletion retaining another typed lane produced a comparison image that no longer represented the captured companions. This also made absent typed components in an after-image incorrectly resemble the incomplete comparison image.

VERIFIED-RUNTIME (managed reproduction): new actual-source regression fails before the fix on an otherwise unchanged retained parking lane. See docs/evidence/fence-capture-0.4.231/reproduction-before-fix.txt. The same regression passes after the fix. Its relation to both native test failures is a supported diagnosis, with native confirmation still pending.

NetworkDeletionPolicy now uses Item.DetachedCopy for each retained item and replaces only the three relationship arrays that legitimately lose links to proved deleted items: Connections, SubNets and SubLanes. Every captured typed field, presence flag, reference, prefab identity, seed, geometry and local cache remains available to the existing exact comparator. No tolerance, supported schema, remapping rule, deletion allowance, topology test or restoration algorithm is relaxed. Detached comparison copies do not mutate captured or live data.

Diagnostics now distinguish a still-existing target, retained deletion-graph mismatch, source removal/ancillary mismatch, source-row/live-graph mismatch and unstable after-graph. Only timeout diagnostics request full graph explanation; ordinary frame checks retain the short path. Recording still requires matching source/graph/companions and three stable frames. Existing companion reconstruction/reference repair remains unchanged.

## Preservation and checks

Renderer/targeting sources are unchanged from the visually accepted 0.4.230; before/after SHA256 verification is recorded under docs/evidence/fence-capture-0.4.231. No new rendering/UI behaviour or Harmony patches. Package version/banner only changes to 0.4.231.

New regression cases cover unchanged retained parking/garage/car/pedestrian/slave lanes, repeated comparison, subsequent final-edge deletion, typed settings/schema mutation refusal, access-reference mutation refusal, immutable capture links and refusal diagnostics. Existing identity, ownership, seed, topology, unrelated-removal and ambiguous-target refusals remain tested.

VERIFIED-RUNTIME (managed checks): Phase3 1200, bound completion77, spawn/access55, move44, effect41, area move100 and outline renderer22 assertions pass. Release and UI production builds pass; cached NU1900 advisory-feed warning remains. Native scheduling, save persistence and the actual fence retest are not inferred from these tests.

Grouped native acceptance: on the disposable mixed test asset, delete/Undo each FenceResidentialHigh01 segment while the other fence and car-path/lane companions remain. Repeat deletion/Undo of the same fence, then perform the accepted car-path/lane and prop/marker move/deletion Undo checks. Confirm gold-hover/cyan-selected outlines remain correct and native save/reopen preserves recovered companions/references. Do not discard unrelated unsaved user work.

Installation/staging details are recorded in docs/evidence/fence-capture-0.4.231/install-status.txt. No publishing or computer use performed.

INSTALL VERIFIED 2026-10-23: game absence checked; assembly0.4.231.0 and all three copied package hashes verified. Nine previous files backed up and hash-verified at outputs/rollback/MWEditorTools-before-0.4.231-20261023-233008/MWEditorTools. Record outputs/installation-0.4.231.txt. Native fence acceptance pending.

NATIVE FOLLOW-UP VERIFIED-RUNTIME 2026-10-23: both FenceResidentialHigh01 deletion captures now reach ready-to-Undo (23:33:49.468 and23:33:52.777). First CanNetworkUndo initially sees a transient Updated component on Pedestrian Access Location, then remains blocked on retained companion graph/typed values; shortcut at23:33:50.347 is refused ready=False. Therefore first Undo did not execute. Second Undo starts23:33:53.286, then its retained reference root-network mapping rejects regenerated source-free child lane53130:1 ->53026:333. Logged child curve interior Y values differ:511.9463 vs511.946777 and511.945068 vs511.944824; endpoints/horizontal controls and length appear equal in printed values; seeds differ before repair. Parent mapping succeeds in diagnostics, but complete parent/source/cache proof must be established before adapting child comparisons. Do not simply widen tolerance or ignore marker companion mismatch. Restoration times out23:33:55.204; guarded fallback restores the recorded deleted-fence state/companions at23:33:55.272, leaving new prop Undo available. No current UI exceptions. Capture regression fixed, complete fence Undo acceptance FAILED. Evidence docs/evidence/fence-capture-0.4.231/native-test-2333. No new code/install changes in this diagnostic follow-up.
