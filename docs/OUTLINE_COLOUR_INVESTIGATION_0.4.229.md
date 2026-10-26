# Investigation: gold hover and cyan selected outlines without circles

Scope: investigate only. No mod source, installed package, asset data, game files or third-party files changed. Current installed version remains 0.4.229. Only local inspection helpers and this evidence/report were written. No gameplay or computer-use test performed.

## Finding

VERIFIED-STATIC: a per-render-instance outline colour path exists. It is more capable than the empty Game.Tools.Highlighted tag. A dedicated local rendering override appears technically feasible for mesh objects, networks and lanes without changing prefab colours, shared materials, transforms, ownership, companions or references. This feasibility conclusion is a HYPOTHESIS until frame timing, GPU upload and native cleanup are validated in a prototype. It is not evidence that a working two-colour feature is already installed.

Installed Game.dll identity: Game, Version=0.0.0.0; MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. SHA256 recorded in docs/evidence/outline-investigation-0.4.229/game-hash.json. Colossal.Core, Version=0.0.0.0 supplies NativeBatchInstances and its writer API. Evidence files contain exact signatures/IL.

## Why both current native outlines are blue

VERIFIED-STATIC: Game.Rendering.BatchDataSystem/BatchDataJob.UpdateObjectData checks ObjectProperty.OutlineColors (7) at IL_0c09..0c13. It prioritises native Error, Warning and Override colours at IL_0c18..0c7f. Ordinary highlighted objects use RenderingSettingsData.m_HoveredColor at IL_0ccc..0cd6, convert it to linear colour at IL_0cda, then write it to one batch instance at IL_0ce1..0cf7. Therefore adding Highlighted to both hovered and selected objects does not distinguish their colours. Changing shared m_HoveredColor would affect other highlights and would not solve simultaneous independent colours.

VERIFIED-STATIC: NetProperty.OutlineColors is 13; LaneProperty.OutlineColors is 5. UpdateNetData and UpdateLaneData contain equivalent per-instance colour writes. Surface/area node outlines and curve overlays already receive separate gold/cyan colours in MW and do not need a global palette modification. Removing the remaining selected-object DrawRing path is straightforward, but by itself would leave both native object silhouettes blue.

## Available local renderer contract

VERIFIED-STATIC: BatchManagerSystem exposes GetNativeBatchGroups(bool, out JobHandle), GetNativeBatchInstances(bool, out JobHandle), and dependency registration through AddNativeBatchInstancesReader/Writer and AddNativeBatchGroupsReader/Writer. NativeBatchInstances exposes GetInstanceData(group, instance), AsParallelInstanceWriter and GetInstanceCount. Its ParallelInstanceWriter exposes GetPropertyValue<T> and SetPropertyValue<T>(value, group, property, instance). GroupData.GetPropertyIndex resolves the actual group-specific property slot. No fixed shader-buffer offset should be guessed.

VERIFIED-STATIC: InstanceData contains m_Entity, m_MeshGroup, m_MeshIndex and m_TileIndex; MeshBatch carries group/instance indices and matching mesh/tile identity fields. These provide an exact-entity-and-mesh verification path before a render property write. Numeric batch indices alone are insufficient because batches can be rebuilt/reused.

## Recommended prototype

A separately guarded rendering step should run after native batch-data updates and before upload, using the exposed reader/writer dependency contract. Re-read current mesh batches; validate entity version and mesh/tile identity, property presence and bounds; colour only exact MW targets. Selected wins if the same entity is also hovered. Preserve native error/warning/override precedence and pre-existing foreign highlight ownership. Never change shared rendering settings, materials, prefab colours, transform properties or instance topology.

Handle every mesh and current LOD belonging to the exact target. Retired/rebuilt targets must return to native colours through a validated cleanup/refresh path; never write a cached batch index after it has been reassigned. Current tag cleanup before preflight/clipboard and on context/filter/tool changes must remain. The renderer override should be optional and fail locally if its API/order/conflict guard fails; it must not disable accepted Undo or broaden typed companion/component schemas.

Remove object/marker/node circular overlays entirely once the outline route is accepted. Keep polygon boundaries and native curves with gold hover/cyan selection. Do not substitute a bounding box or wire mesh and advertise it as a silhouette.

## Markers and effects

VERIFIED-STATIC: EditorGizmoSystem.EditorSelectedGizmoJob.DrawGizmo uses a separate gizmo path. Its Highlighted test is at IL_004b..0056 and takes m_HoveredColor at IL_0059, IL_0060 and IL_0067. An override of mesh-instance OutlineColors alone cannot be advertised as covering every marker/effect gizmo. Some elements have no mesh silhouette at all. A separate exact-target gizmo path must be evaluated without changing global gizmo colours or suppressing unrelated native gizmos.

## Evidence limits and next acceptance

UNPROVEN: correct before-upload ordering, colour persistence across native rebuilds/LOD changes, safe cleanup under later colour changes by another mod, and marker/effect rendering. No implementation or native validation has been performed in this investigation.

The next experiment should prove one hovered prop gold and a different selected prop cyan simultaneously, with no circles; then check a multi-mesh bike rack, foliage, network/lane meshes, surface/area boundaries and both marker types/effects. Exercise filter/asset/tool exit, deleted/recycled entities, native error highlights and active editor mods. Finally group the existing accepted copy/replace, prop/effect/marker move/deletion Undo and save/reopen checks. The renderer must not change companion data or references, and failed rendering must leave Undo operational.
