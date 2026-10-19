# Copy/replace placement audit - 19 October 2026

Scope: inspect related source-row resolution risks after the wall cache-height fix in candidate 0.4.165. This audit changes no production code or installed files. No computer use or publishing.

## Findings

### 1. Foliage replacement retains the old cache-only matcher

VERIFIED-STATIC, priority 2: `src/Modules/PropReplacer/ReplacementFoliage.cs:72` calls `BoundDescriptorIndex.Resolve` inside `PlanFoliageReplacement`. This is reached when the replacement is vegetation and the existing same-prefab neighbour protection requires a separately initialized candidate. Both browser-list and viewport replacements use this plan; batch replacement can encounter the same condition across several targets/instances.

The old matcher requires source and local cache positions/rotations to be exactly equal. Consequently, the viewport matcher can now resolve a valid destination, but the foliage plan can still reject it. Its filtering also resolves other same-original-prefab children before checking their source index, so an inconsistent sibling can abort the plan even if the chosen destination itself is exact.

VERIFIED-STATIC reproduction: a temporary harness under `outputs/placement-audit` compiles the actual `BoundDescriptorIndex` and `ViewportPropSource` files with installed mathematics libraries. With source height 0.04623413 and cache height 0, the new viewport resolver returns row 0 and the old matcher throws. The 23 current viewport-policy checks also pass. This is managed data reproduction, not native foliage acceptance.

Recommended correction: use the native projected source identity for settled foliage target planning, retaining one exact owner link, unique source mapping, donor protection, full retained-identity checks and deferred cleanup. Do not globally weaken the Undo matcher.

### 2. Viewport surface/area matching uses cached nodes only

VERIFIED-STATIC, priority 2: `ViewportTarget.cs:98-101` matches every source node directly against `LocalNodeCache.m_Position` using exact equality. The native editor instead projects live `Game.Areas.Node` positions relative to the owner, then snaps to the cache only when sufficiently close. Therefore a legitimate saved row whose live projected nodes differ from cached nodes can be refused by viewport paste.

Native evidence: `Game.Tools.ApplyPrefabsSystem.ListObjectSubAreas`, `docs/evidence/detail.txt`: WorldToLocal call at IL_018f and CheckCachedValue at IL_01dc; parent mesh preservation at IL_01e1..01f8. Use the same unique whole-polygon mapping, node-count/order and parent-mesh requirements when correcting this path. This is not a claim about unsupported polygon creation/merged edits.

UNSUPPORTED/UNPROVEN: a new in-game surface/area refusal of this exact kind has not been observed in this audit.

### 3. Viewport network/lane matching uses cached curves only

VERIFIED-STATIC, priority 2: `ViewportTarget.cs:103-106` compares the source Bezier curve directly with `LocalCurveCache.m_Curve`. Native network source generation projects the live curve into the owner and applies CheckCachedValue separately to all four control points. Live/cache drift or retained floating-point differences can therefore cause the same category of refusal.

Native evidence: `Game.Tools.ApplyPrefabsSystem.ListObjectSubNets`, `detail.txt`: live control-point projection at IL_01cf..026e and cache checks at IL_02a2/02ba/02d2/02ea. Correct source matching must retain ownership, unique identity, curve topology and parent-mesh checks rather than accept a near curve indiscriminately.

Additional lane identity concern, HYPOTHESIS: native lane source rows can take their NetLanePrefab from `EditorContainer.m_Prefab` (IL_0555..0576), whereas copy and destination resolution special-case container identity only for effects. The live lane-marker representation needs a focused native fixture before claiming this path works for all lane containers. No blanket support expansion is justified by this audit.

UNSUPPORTED/UNPROVEN: no new in-game network/lane reproduction is claimed.

### 4. Undo retains related strict refusals

VERIFIED-STATIC: `NestedGraphImage.cs:57` and several companion/restore validators still use `BoundDescriptorIndex`. The managed counterexample above applies to their descriptor predicate too. This can prevent capture/validation when live/cache/source values have diverged, including on mixed assets containing such props. These guards protect recovery correctness; a simple tolerance increase or shared global replacement would not establish safe Undo.

UNSUPPORTED/UNPROVEN: this audit does not establish a new native Undo regression or prove that every call site reaches the mismatched data. Any Undo correction needs capture/restore and retained-companion validation, separate from source-row selection alone.

## Paths already covered

VERIFIED-STATIC: the new 0.4.165 prop resolver covers both destination props and their nested ancestor source rows. `EffectEditPolicy.Descriptor` already projects live effect placement and applies position/rotation cache snapping, so it does not contain the same cache-only assumption. Ordinary browser-list row selection resolves the actual selected source row and avoids viewport geometry matching, although the foliage plan above remains shared.

The first supplied screenshot's clipboard names the whole building. `CopyHover` permits a directly highlighted root object to be copied; this is distinct from the height mismatch. The screenshots do not prove an incorrect native hit, so no inferred wall target is substituted for the actual highlighted object.

## Evidence limits

Reused native assembly evidence: Game Version 0.0.0.0, MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef, SHA256 AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A, verified unchanged during the 0.4.165 investigation. `CheckCachedValue(float3&,float3)` at IL_0007..0015 uses a strict 0.01 distance threshold. The existing recorded IL is reused; no broad decompilation was performed.

Priority: correct the foliage planner first, then viewport area and network source projection. Keep the accepted single-action Undo invariants intact. These findings establish inconsistent matching contracts and a concrete managed refusal, not new native acceptance or a general data-loss finding.
