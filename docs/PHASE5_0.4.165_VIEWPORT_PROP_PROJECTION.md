# 0.4.165 candidate - viewport prop source projection

## Report and diagnosis

VERIFIED-RUNTIME: the user's screenshots and 19 October 2026 log show a refused wall replacement. At 17:54:18, the highlighted destination has cache position (7.500013, 0, -5.800007), parent mesh -1, probability 100, group 0 and raw index 36. Source row 36 has the same reference/rotation/settings and position (7.500013, 0.04623413, -5.800007). Row 42 shows the same height discrepancy in the earlier refusal. The refusal occurs before mutation. Selected log lines are retained in `evidence/viewport-prop-0.4.164-refusal.txt`.

VERIFIED-STATIC: `BoundDescriptorIndex.Resolve` requires exact cache/source position equality. Using this Undo snapshot matcher for viewport paste rejects these observed source rows.

VERIFIED-STATIC: installed Game.dll SHA256 remains AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A, Game Version 0.0.0.0, MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. Reuse `evidence/detail.txt`: `Game.Tools.ApplyPrefabsSystem.ListObjectSubObjects` constructs inverse owner placement at IL_004b..006c, projects the live child transform at IL_0126..013b, and calls cache snapping at IL_0165..017c. `CheckCachedValue(float3&,float3)` uses cached position only when distance is strictly below 0.01 at IL_0007..0015. Its quaternion overload uses `Colossal.Mathematics.MathUtils.RotationAngle` and a strict 0.017453292 threshold at the same offsets. `evidence/area-move-source-projection-native.txt` records the associated WorldToLocal implementation.

HYPOTHESIS: terrain/ground realization caused the live/cache height divergence. Its precise origin is not established by the screenshots alone. The native source projection, rather than an arbitrary extra height tolerance, is the applicable matching contract.

## Change

Viewport prop rows and nested ancestor rows now use a separate `ViewportPropSource` policy: project live placement into the direct owner, apply the native position/rotation cache-snapping rules, and require a unique source row with the same prefab identity, parent mesh, probability, group and projected placement. Position/rotation comparison uses the existing 1e-4 numerical precision convention used for native effect source projection. Nonfinite placements, zero rotations, absent or duplicate matches refuse. Cache indices are not trusted as row identity.

Only row resolution changes. The existing replacement operation preserves destination row settings and changes its prefab reference. Ownership/path, editable asset, type, deferred-update and foliage guards remain. `BoundDescriptorIndex` and all Undo implementation files remain unchanged. No cache, transform, source row or asset is mutated during resolution.

## Validation

The new managed harness compiles the actual policy with installed Unity.Mathematics and Colossal.Mathematics libraries and game data doubles. It covers recorded wall positions, stale indices, cache-only decoys, ambiguous matches, translated/rotated owners, native cache snapping, exact-cache compatibility, settings/ref identity mismatches, invalid data and read-only resolution. Relevant regression suites and builds are run before local installation.

UNSUPPORTED/UNPROVEN: native game acceptance of this candidate is pending. On a disposable copy, copy the intended wall (verify the clipboard label), replace one matching destination and then test Replace all. Check that destination placement/settings remain unchanged and the refusal is gone. No computer use or automatic publication is part of this fix.

VERIFIED-STATIC completion: Release and production UI builds pass; six relevant managed suites pass 153 assertions (23 viewport projection, 14 hover/ownership, 18 replacement foliage, 25 standalone nested, 38 move Undo, 35 effect restore). Release build reports NU1900 for unavailable vulnerability metadata; the effect restore harness retains its unused test-field warning. Candidate packaged under outputs/test-package-0.4.165. Installed while Cities: Skylines II was closed, complete prior folder backed up and verified, four runtime hashes verified in evidence/phase5-0.4.165-installed-hashes.csv. Native acceptance remains pending.
