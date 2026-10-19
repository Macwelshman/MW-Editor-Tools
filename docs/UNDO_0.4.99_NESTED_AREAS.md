# Undo 0.4.99 — nested props and mixed-asset area/surface recovery

## Implemented behavior (VERIFIED-STATIC)

- Supported bound nested prop groups use automatic capture in Sub-elements or Everything mode. Native SaveInstance may reorder retained object descriptors: compare a unique permutation of complete descriptors and resolve each retained root's new index by its complete placement. Actual unrelated edits, missing rows, changed probability/mesh/position and ambiguous duplicate rows still refuse recovery. Clear previous automatic arming before each new preflight so a refusal cannot reuse stale adoption state.
- Supported standalone static props with nested children now capture their entire typed tree plus referenced prefab snapshots. Main-elements or Everything deletion records only after all original descendants disappear. Undo creates a fresh root from its native archetype and ensures an empty SubObject buffer exists for native regeneration; no descendant IDs are resurrected. Native SubObjectSystem creates descendants. Restore transforms, seeds, mesh colours and vegetation values only after the complete native tree, ownership, placement caches, unchanged surface/custom colour values, and component presence match. Require three consecutive exact validation frames before consuming the last-deletion slot. Any transient mismatch resets the validation count.
- Bound areas and surfaces preserve Plant and Tree companions as typed data, including pollution, growth and tree state. Plant values must be finite. Live root placement probabilities between 0 and 100 can be retained exactly; nested child links still require deterministic placement. Companion forests, effects, network graphs, geometry and references retain their existing checks. Area descriptor deletion also accepts a unique full-value permutation of retained polygons.

## Evidence

VERIFIED-RUNTIME: `docs/evidence/undo-0.4.98-runtime.log` records native Carport03 bound nested deletion and successful exact recovery at 00:11:36–00:11:37 on 2026-10-14, then bound Grill03 recovery at 00:11:40–00:11:41. Earlier retained object rows were reordered and rejected. Area capture was refused for Game.Objects.Plant; standalone nested prop capture was refused for related objects.

VERIFIED-STATIC: installed Game, Version=0.0.0.0, MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. Game.dll SHA256 AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A (unchanged inspected build). Native Plant contains float m_Pollution; Tree contains TreeState m_State and byte m_Growth. Narrow extract: `docs/evidence/undo-0.4.99-native-nested.txt`.

Game.Objects.SubObjectSystem.OnCreate IL_0074/IL_007e requires SubObject; IL_008c/IL_00a2 includes Updated. UpdateSubObjectsJob.CreateSubObject overload in the extract uses native ObjectData archetype at IL_0c11 and adds child LocalTransformCache at IL_0e58 when cacheTransform is true. This supports the implementation path; it does not prove native timing or successful 0.4.99 regeneration.

## Validation and boundaries

VERIFIED-STATIC: Release build zero warnings/errors; production UI and TypeScript pass; six UI scripts pass. 862 managed assertions across 13 suites pass, including 23 against actual standalone nested restoration source with managed ECS doubles, seven descriptor-permutation regressions and the compiled native collection enumeration audit. These are not native ECS integration tests.

UNPROVEN: 0.4.99 standalone nested regeneration and mixed-asset area/surface restoration in game. Bound nested recovery has the 0.4.98 runtime evidence above; new permutation and vegetation paths require native acceptance.

Unsupported: standalone area/surface entities, lot areas, nested area ownership, props containing attached areas/networks/effects/activity data, uncertain/ambiguous descriptor mappings, unsupported component schemas, and graphs outside existing budgets. A bound nested group is restored as a unit; this does not authorise changing a read-only referenced prefab to recover an individual child edit. No redo/history feature added. Original assets and game assemblies were not edited.

Use Ctrl+Z after one supported deletion. Bound groups/areas/surfaces: Sub-elements or Everything; standalone nested groups: Main-elements or Everything. No manual arming or open inspector needed.

VERIFIED-STATIC installation: 0.4.99 installed after Cities2 exit checks; five package hashes and all rollback file hashes verified (see docs/evidence/recovery-0.4.99-installed-hashes.csv). Native acceptance pending.

