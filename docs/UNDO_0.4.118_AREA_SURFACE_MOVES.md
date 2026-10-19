# Undo 0.4.118 — built-in area/surface moves

User explicitly authorised area and surface move Undo on 2026-10-16. Uses the same single Ctrl+Z last-action workflow as supported prop moves and deletion recovery. No computer use, publishing, Redo/history expansion, network moves or third-party tool support.

VERIFIED-RUNTIME baseline: user confirms 0.4.117 bound prop, nested prop and foliage move Undo passed. Preserved native log confirms bound move restoration with source/graph/companion validation, including foliage at 18:42:01.482. Evidence: docs/evidence/undo-0.4.117-prop-moves-runtime.log. Earlier standalone move, cancellation, foliage refresh and bound deletion passes remain accepted. Persistence remains untested. Area/surface moves were outside 0.4.117 scope.

## Implemented scope and guards

VERIFIED-STATIC: supported SurfacePrefab/SpacePrefab areas may relocate or reposition existing polygon vertices through AreaToolSystem. Supports standalone areas with a surviving original entity and areas directly bound to a writable owner; bound targets may be unambiguously remapped after native regeneration. Vertex counts, flags, source references, parent-mesh attachments and typed nongeometry metadata must remain consistent. Surface expansion offsets may follow moved geometry, with unchanged expansion topology/other values. Vertex addition/removal, generating new polygons, deleting/merging polygons, lots, specialized unsupported schemas, nested owners, multiple/new/deleted native preview outputs and standalone entity replacement are refused. Unsupported completed edits clear stale previous slots.

Capture occurs before setting recreate or beginning an existing-area node drag. Native previews and cancelled/unchanged supported moves preserve the previous completed slot. Completed moves are recorded only after native allow-apply, final Apply mode, Edit mode and appropriate native Create/Modify state agree; a single temporary area must reference the captured original without Delete flags. After-images must remain exact across three frames before replacing the prior action. Capture/query exceptions preserve vanilla editing, and an accepted edit without a valid capture clears stale Undo. Optional area hooks are conflict-checked separately; attachment failure removes only those hooks, preserving existing prop/deletion hooks.

Bound moves capture the original owner payload, full prop forest, all areas, effects, closed owner network graph, the target's own generated network graph and referenced prefab payloads. Recording requires unchanged retained native companions and source component shape/active state; saved area descriptors must uniquely match native local-node geometry/parent attachments. Only proved ancillary saved representations can normalize. The original area descriptor list remains the restore target. Undo reuses native parent regeneration and existing finite-cache/seed restoration and bounded network world-rounding gates. Complete restored target metadata, geometry, expansion, seed and owned generated network graph are checked in addition to all owner companions before consuming the slot.

Standalone Undo preflights the entire after-image, restores detached world-node/elevation and local-node buffers on the surviving area, restores captured expansion and seed, and marks native Updated/BatchesUpdated. A newly added unbound cache is accepted only when all nodes agree with world positions and have parent mesh -1; Undo restores its original absence. Native geometry/rendering and generated network updates must settle. Seed/cache restoration requires full typed topology/geometry preflight; finite differing network caches remain refused. Three final exact frames are required before slot consumption.

World/asset changes, read-only owners, saving/dialogs, pending native changes, stale references/after-state, unsupported shapes and concurrent recovery are guarded. Capture/restore expires at 120 frames. Failure after live mutation stops recovery and requires reload without saving; deferred native rollback remains UNPROVEN. A visible polygon return alone does not establish successful recovery.

## Native evidence

VERIFIED-STATIC assembly: Game, Version=0.0.0.0, MVID=e15551e9-3dea-4855-8dde-1da3cea7d7ef; SHA256 AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A, unchanged. No game files modified.

AreaToolSystem.OnUpdate(JobHandle) seeds Create state/control points from recreate's original Node buffer at IL_006e–012f. Apply(JobHandle, Boolean) selects Default/Create/Modify/Remove state at IL_0000–0008; existing-node modification begins at IL_0113–0145. Create completion requires native GetAllowApply and marks Apply at IL_0386 and IL_048d–0490. Modify completion checks native GetAllowApply at IL_0760 and sets Apply at IL_0775–0776 when the temporary area query is nonempty, otherwise Clear at IL_07a4–07a5. Tool mode Generate is excluded. Evidence: docs/evidence/area-move-tool-native.txt.

ApplyAreasSystem.HandleTempEntitiesJob.Update(Int32, Entity, Temp, DynamicBuffer<Node>, DynamicBuffer<LocalNodeCache>) writes the original entity's Node buffer at IL_0008–0041 and existing/new LocalNodeCache buffer at IL_0053–00b5. This establishes the original-buffer update route used by standalone restore; final engine scheduling/triangulation/rendering acceptance is still UNPROVEN. Evidence: docs/evidence/area-move-apply-native.txt.

## Validation and one grouped acceptance pass

VERIFIED-STATIC: Release build with zero warnings/errors, production UI and TypeScript; 630 focused recovery/lifecycle assertions (Phase3Safety 302, AreaMoveSafety 30, MoveUndoSafety 36, BoundFoliageSafety 25, BulldozerSafety 47, NestedRecoveryRunner 133, StandaloneNestedSafety 25, LastPropHistory 12, LifecycleSafety 20), plus 35 Options assertions and recovery/window/settings/Multi-Save/Colours UI regressions. New tests exercise actual area hooks, commit/denial/cancellation and dependency preservation, single-target gates, capture-failure fallback, actual detached geometry/cache/expansion/seed restoration, native update tags, partial-write failure propagation, topology/attachment/source rejection and finite expansion rules. Managed doubles do not establish Unity/native regeneration correctness.

UNPROVEN: native area/surface move acceptance, preview timing/output-query shape, generated geometry restoration and persistence. On the existing disposable mixed asset:

1. Move a bound area, then a bound surface, exiting the tool and pressing Ctrl+Z after each. Include an existing-vertex drag if that is the normal workflow. Check the original outline and all retained props/foliage/effects/networks. Require successful recovery completion in the log.
2. Repeat with standalone supported area/surface instances. Check outline, triangulation/rendering and generated companions where present.
3. Establish a prior Undo entry, begin an area move, cancel and exit the tool, then Ctrl+Z. Repeat with a supported unchanged move. The previous entry should remain available.
4. Save only a new disposable restored copy and reopen it. Check the recovered polygons, props and companions. Do not overwrite originals. Stop on a recovery error and reload without saving.

Do not repeat the already-accepted prop/deletion batch unless this focused pass exposes a regression. This stage cannot be marked runtime-complete before acceptance and persistence pass.
