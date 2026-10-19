# 0.4.168 candidate - compiled ground source mapping

## Evidence

VERIFIED-RUNTIME: 0.4.167 log at 18:24:23 identifies Stucco wall source row 43 in SR ResidentialLow S03_L5 2x3. Live and owner Y are both 511.9453; projected/cache local Y is 0; the saved row Y is -0.02758789. Horizontal position (5.5, -11.49998), rotation, prefab identity, parent mesh -1, probability 100 and group 0 agree. Cache index is 43. The repeated failure is a source/live height disagreement, not a quaternion-sign or horizontal-position mismatch. Evidence: `evidence/viewport-prop-0.4.167-refusal.txt`.

VERIFIED-STATIC: recorded installed Game Version 0.0.0.0, MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef, SHA256 AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A. Narrow read-only inspection of `Game.Objects.SubObjectSystem.UpdateSubObjectsJob.CreateSubObject` confirms the compiled OnGround flag is 256. In its oldSubObject overload, IL_027d..02a8 resets world Y to owner Y minus owner elevation and local Y to negative owner elevation for unanchored OnGround placements; IL_02ad..02c4 sets zero elevation and OnGround. IL_06c3..0718 can remove the live Elevation component on this same compiled flag, so component presence alone cannot prove that contract. Cache is populated from localTransformData at IL_0e09..0e58. Evidence: `evidence/viewport-ground-native.txt` and `evidence/viewport-ground-types.txt`.

HYPOTHESIS until native acceptance: this native OnGround normalization is the cause of the logged wall discrepancy. The new code requires live compiled-prefab corroboration before applying that contract; it does not assume every height difference is ground normalization.

## Guarded mapping

The read-only resolver obtains the owner's compiled SubObject buffer. For each same-prefab authored row, exactly one compiled entry must agree on full authored position/rotation, native prefab entity, parent index, group and probability. It must have OnGround with no top/centre anchor or attached-parent grounding and parent -1. Buffer ordering/count is not assumed to equal the authored prop list; duplicate compiled representations refuse proof.

Only with that proof may zero-height cached placement under an upright owner use ground-normalized source identity. It requires unchanged horizontal cache/live position, exact prefab/rotation/settings, one unique horizontal source row and agreement with the native source index. Stacked rows with identical horizontal placement refuse, even when one row happens to equal the current cache height. Stale index, non-ground, mesh attachment, nonzero cache height, tilted owner and unproved compiled data retain strict matching/refusal.

The chosen authored row's Y and all other placement/settings remain untouched; replacement still changes only its prefab reference. This applies to ordinary viewport objects, nested ancestor mapping and foliage planning through the shared read-only resolver. Surfaces/networks and accepted Undo/History implementations are unchanged. Detailed mismatch logging remains and now includes compiled proof status.

## Verification

VERIFIED-STATIC: the exact logged wall values reproduce the old refusal and resolve to row 43 with the compiled OnGround proof. Regression checks also cover unchanged source/cache heights, stacked ambiguity, stale indices, mismatched compiled source data, wrong native prefab, duplicate ground/non-ground compiled entries, anchored/attached objects, tilted owners and unsupported cache elevations.

Release and production interface builds pass. All 24 managed suites and nine interface scripts pass, including 76 policy/diagnostic checks and 10 actual-source network adapter checks. The compiled native-buffer gate passes. No diff exists under BulldozerRecovery or History. Existing NU1900 vulnerability-metadata warning remains.

UNSUPPORTED/UNPROVEN: in-game acceptance of the correction is pending. Retry the same wall replacement on a disposable copy and check destination placement and source-row settings. No computer use or publishing.

INSTALLED: 0.4.168 while Cities: Skylines II was closed. Full prior mod folder backed up and verified under outputs/rollback/MWEditorTools-before-0.4.168-*. Seven installed file hashes and assembly version verified; evidence/phase5-0.4.168-installed-hashes.csv. Unproved ground cases also log the nearest compiled entries and flags for follow-up. Native acceptance remains pending.
