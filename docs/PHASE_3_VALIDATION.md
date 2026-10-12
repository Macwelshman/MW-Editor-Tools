# Phase 3 — prefab-list recovery test build

Status: Object, Area, Net and Effect prefab-list recovery has user-reported in-game validation on the tested installed build, including multiple-step recovery and save/reopen persistence. Build and managed tests also pass. This is validation of the tested scope, not a general release or broad compatibility guarantee. Phase 2 component confirmation remains part of the build.

## Verified static findings

Read-only installed Game.dll MVID remains e15551e9-3dea-4855-8dde-1da3cea7d7ef. See evidence/phase3-inspection.txt.

- ArrayAdapter/ListAdapter.DeleteElementInternal receives the underlying index after ListAdapterBase.ResolveIndex. This avoids interpreting a filtered or paginated row index as storage position.
- List adapters expose their accessor. Runtime matching requires the accessor's actual storage object to be reference-identical to one of the selected prefab's specific ObjectSubObjects.m_SubObjects, ObjectSubAreas.m_SubAreas, ObjectSubNets.m_SubNets or EffectSource.m_Effects fields. Other lists use vanilla behavior.
- InspectorPanelSystem.OnValueChanged(IWidget) invokes UpdateParent(true). UpdateParent(false) updates the current parent prefab through PrefabSystem.UpdatePrefab and marks its asset dirty, without moving selected subobjects. This is the proposed regeneration route for restored prefab data. Final deferred ECS graph integrity is unproven.
- EffectSource additionally stores animation curves; ObjectSubNets stores inversion settings. Snapshots include all public declared fields of these supported components plus active state, not just the deleted row.
- Native EditorSection, ButtonRow and Button APIs support small inspector controls. The inspector children property is protected; the exact inspected property is accessed through guarded reflection.

## Transaction design

Before native row deletion, capture supported component payloads, component identities/order, owning prefab and expected list contents after removing the resolved row. The original adapter mutation proceeds. Record history only when the inspector's native value-change event identifies that same PagedList/adapter and expected deletion result. Unmapped/mismatched events never get an undo claim. Pending capture expires after two frames if no matching event arrives.

Each transaction keeps immutable before/after payloads. Snapshot schemas include positions, rotations, exact probabilities, groups/parent meshes; area node arrays; network curves/node indices/upgrades/inversion; effects and animation properties/curve keys/wrap modes. External prefab identities are retained in the same live world and must still be valid. Mutable arrays, lists and DTOs are recreated for every restore. Raw Entity references, arbitrary object graphs and opaque fields are refused.

History has a 20-operation limit, per-snapshot 100,000-node limit and cumulative 500,000-node budget. These are structural limits, not measured byte limits. A new deletion clears redo. Asset load/switch/world change, other inspector edits, component-header requests and bulldozer apply clear history conservatively. History is session-only and does not survive reopen.

Undo/redo refuses if the current supported payload, component identity/order, writable state, owning asset or external references no longer match. All target payloads are materialized before writes. Restore calls the native parent update and refreshes inspector sections. Prefab verification is immediate; it is not proof that deferred ECS jobs finished successfully. On failure, rollback of prefab payload and a native update are attempted; further recovery writes are disabled. A failure log requires inspection/reload before saving. Atomic ECS rollback is not claimed.

## Automated validation

Release build: zero warnings/errors. Phase 3: 22 managed DTO/history assertions, using actual installed prefab data types outside Unity. Phase 2 regression: 18 gate/Harmony/policy assertions. Tests cover exact probability, transforms, nested geometry arrays, node indices, effect settings, fresh independent restores, null/empty collections, rejected entity/arbitrary payloads, snapshot limits, history depth, failure cursor handling, undo/redo order and branch clearing.

Native prefab identity liveness, AnimationCurve native calls, actual Unity Harmony interception, native controls and ECS regeneration are not exercised by these tests. The first DTO run demonstrated Unity native calls cannot execute outside Unity; they were isolated behind non-inlined native helpers. Managed tests do not substitute for in-game validation.

## In-game acceptance — required before support claims

User-reported results: the initial Object deletion → Undo → Redo passes. The user subsequently confirmed all requested follow-up checks pass: settings/placement, multiple-step recovery, and Area/Net/Effect deletion/Undo/Redo with save/reopen after Undo. No independent runtime trace or entity-count audit was collected. Conflict, fault-injection and edge-case coverage beyond those requested checks is not inferred.

Use disposable duplicate assets. Start with Object deletion before the other types.

1. Confirm the log reports list recovery loaded and the inspector shows MW recovery controls. Retest Phase 2 cancellation/confirmation.
2. Delete one Object list row. The panel must report one recorded deletion. Undo must restore its prefab, position, rotation, probability, group and parent mesh exactly, including the placed preview. Redo must remove the same element once.
3. Delete several rows, then undo/redo several steps. Test filtered/paginated lists and first/middle/last/only rows. Verify restored arrays/lists and entity counts, with no duplicates or orphans.
4. Repeat separately for Area/surface (all geometry and parent meshes), Net (connectivity, nodes, lanes, curves, upgrades and inversion) and Effect (transform, scale, intensity, animation reference and curve settings). Do not call a category supported until all its checks pass.
5. After Undo, save and reopen. Confirm the restored content, settings and relationships persist. History should be empty after reopening. Repeat after Redo.
6. Make an unrelated inspector edit or change/load assets. History should clear, and an old Undo must never modify a different asset. Delete after Undo to verify redo branching.
7. Check read-only assets, missing/invalid references, oversized payloads and conflicting patches. Unsupported capture should block the mapped deletion with an explanatory log; unmapped lists retain vanilla behavior.
8. Confirm no unprotected operation is described as recoverable. Header deletion and bulldozer use clear list history; neither gets undo in this build.

Record each category separately with asset type, row count, field comparisons, preview/entity counts, save/reopen result and logs. If no history appears, provide the log: the runtime ownership/event guard may have rejected the path. A successful visual restore alone is insufficient.

## Bulldozer assessment

Bulldozer undo remains disabled. Phase 1 verified deferred deletion jobs, owner handling and cleanup. Capturing a prefab list at Apply is not sufficient proof that the full entity graph and associated edits can be restored; no validated precommit graph snapshot, remapping or safe scheduling boundary has been established. This build only clears existing list history on bulldozer apply. Whole-component recovery and automatic checkpoint/reload are also deferred.

## Known limitations

Recovery is restricted to the inspected build, exact owning fields and same live prefab/component identities. If native regeneration removes/replaces a component or normalizes payload data, undo is refused; no missing component is automatically recreated. Native inspector refresh can conservatively invalidate history. Settings are not configurable yet; keyboard shortcuts and unified window belong to Phase 4. Other mods' silent changes to unrelated asset data are not comprehensively fingerprinted. A test build is supplied, not automatically installed over the validated Phase 2 build.
