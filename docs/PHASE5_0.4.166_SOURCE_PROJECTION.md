# 0.4.166 candidate - foliage, surface/area and network source matching

## Authorised scope

The user requested fixes for the related matching issues, beginning with foliage replacement and then surfaces, areas and networks, preserving accepted Undo and installing a new test candidate. No computer use or publishing.

## Changes

- Foliage: `PlanFoliageReplacement` maps settled, uniquely owned live targets with `ResolveViewportObjectIndex` / `ViewportPropSource`, replacing its call to the shared Undo cache-only matcher. Target mapping is computed once before choosing changed indices. It retains donor validation, exact reference/placement/settings matching, one target per changed descriptor, witness checks and candidate cleanup. Native entity poses and cached values are copied unchanged into the detached candidate; source rows keep their settings.
- Surfaces and areas: viewport matching projects the ordered live node positions into the direct owner and applies native per-node cache snapping. It checks source identity, node count/order and parent meshes, with one unique source row required. The source and live/cache arrays are never mutated by matching. Existing attached-owner requirements remain.
- Networks: viewport matching projects all four live Bezier control points and applies native snapping independently. Endpoint parent meshes and point/segment topology must agree with a unique source descriptor. Missing live curves cannot be replaced by a raw cache. Native lane containers retain their `NetLanePrefab` identity for copy and paste instead of using the marker prefab reference.
- Guarding: owners, nested parents and network endpoints still being updated are refused. Network target links must appear exactly once in the native owner's SubNet buffer. Connected junctions are not redirected to coincident point rows; the user is directed to the segment. Generated lanes without an attached native source segment/point remain unsupported.

The accepted replacement mutation still changes only the selected/matching prefab reference, preserving destination placement, topology and row settings. No new network move or junction-only recovery support is introduced.

## Undo preservation

VERIFIED-STATIC: there are no changes under `src/Modules/BulldozerRecovery` or `src/History`. In particular, `BoundDescriptorIndex`, single-action history, capture, restore, companion validation and cancellation rules are unchanged. The prior audit's possible strict Undo refusals are not addressed by relaxing recovery matching.

## Native contract and evidence

Reuse the installed Game Version 0.0.0.0, MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef, SHA256 AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A and recorded `docs/evidence/detail.txt`:

- `ApplyPrefabsSystem.ListObjectSubObjects`: inverse owner transform IL_004b..006c, local projection IL_0126..013b, cached position/rotation checks IL_0165..017c.
- `ListObjectSubAreas`: live node WorldToLocal IL_018f, cache snapping IL_01dc, parent mesh IL_01e1..01f8.
- `ListObjectSubNets`: four-point projection IL_01cf..026e, cache snapping IL_02a2/02ba/02d2/02ea; lane prefab from EditorContainer IL_037d..038f (segment) and IL_0564..0576 (point). Parent meshes are read from endpoint local caches through `GetParentMesh` IL_0000..0018. Topology is preserved as source data, never rewritten by row resolution.
- `CheckCachedValue(float3&,float3)`: strictly less than 0.01 distance, IL_0007..0015. Quaternion snapping uses strictly less than 0.017453292 via the installed `MathUtils.RotationAngle`.

Projected source comparisons retain the existing 1e-4 numerical precision convention, rejecting multiple matching rows rather than choosing by cache index or nearest distance. This is not a widened ground-height tolerance.

## Validation and acceptance

The actual viewport projection policies are exercised with installed Unity.Mathematics/Colossal.Mathematics and managed game data doubles. The actual network adapter is also compiled into the harness with managed entity data. Checks include reported height drift, translated/rotated owners, cache snapping boundaries, node order/count, prefab identities, parent meshes, endpoint topology, missing live data, duplicate owner links and source ambiguity. The full managed and interface regression runs are recorded under `docs/evidence/placement-0.4.166`.

UNSUPPORTED/UNPROVEN: these checks do not establish native rendering, deferred regeneration or persistence for this candidate. Grouped acceptance should use disposable copies: replace one/all foliage on a sloped asset with retained neighbours; replace a surface and an area after native node edits; replace an attached network segment/lane container; verify destination geometry/settings and retained neighbours, then save/reopen the disposable copy if the results are correct. Existing accepted Undo should remain usable on its supported actions. Do not save a visibly failed operation over an original asset.

The native-collection regression gate caught unsupported DynamicBuffer enumeration during development. The final adapters use indexed native buffer access. The network test buffer intentionally exposes only Length/index access so LINQ or foreach cannot be hidden by an ordinary managed List. The compiled assembly gate is rerun after that correction.

VERIFIED-STATIC final checks: Release build passes (existing NU1900 vulnerability-metadata warning), production interface build passes, all 24 managed suites and nine UI scripts pass. This includes 51 geometry/policy checks and 10 actual network adapter checks. The corrected production assembly passes the native-collection regression gate. Game.dll hash remains unchanged. Shared Undo/History source directories have no diff. Candidate package is outputs/test-package-0.4.166/MWEditorTools; native acceptance remains pending.

INSTALLED: 0.4.166 with Cities: Skylines II fully closed. Complete prior mod folder preserved and hash-verified under outputs/rollback/MWEditorTools-before-0.4.166-*. Seven copied files and installed assembly version verified; hashes are in evidence/phase5-0.4.166-installed-hashes.csv. No publishing or native game automation.
