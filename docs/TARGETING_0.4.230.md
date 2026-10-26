# Rendering prototype: gold hover, cyan selected, no added circles

User authorised implementing the rendering prototype while preserving companion data/references and accepted Undo. This version is an optional per-instance mesh-outline prototype; native gameplay appearance remains UNPROVEN.

## Behaviour

Hover requests gold and MW selection requests cyan. Selection wins when the same entity is hovered and selected. Existing surface/area boundaries and network/lane curves retain their separate colours. All object/marker/node circular overlay drawing is removed. Native gizmos may retain game colours and shapes: the prototype does not claim a recoloured silhouette for an element without an eligible render mesh. The targeting panel reports the prototype status separately from Undo eligibility. Current unknown native builds disable only the optional colour renderer, leaving native highlighting, other modules and Undo active.

## Exact verified contracts

VERIFIED-STATIC: Game.dll MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef and Colossal.Core.dll MVID b5ff7e4e-51b0-4c1f-a177-2afdfc22906b are required. Native Object/Net/Lane OutlineColors are InstancePropertyAttribute entries for _Outlines_Color, float4. The existing engine writes linear UnityEngine.Color through the native instance writer; MW follows that conversion.

Game.Common.SystemOrder.Initialize registers BatchDataSystem at Rendering (IL_1336..1338) and BatchUploadSystem at Cleanup (IL_0033..0035). OutlineColourSystem is registered through the public UpdateAfter<OutlineColourSystem,BatchDataSystem>(Rendering) contract. It requests read dependencies for native groups and write dependencies for instances, completes those dependencies before synchronous bounded writes, then registers its completed access. It does not dispose borrowed native render buffers. It adds no job fence while inactive with no colours to restore.

The inspected public contract provides NativeBatchGroups.IsValidGroup/GetGroupCount/GetGroupData, GroupData.GetPropertyIndex, NativeBatchInstances.GetInstanceCount/GetInstanceData/AsParallelInstanceWriter, GetPropertyValue(out T,...) and SetPropertyValue(T,...). The production Release build compiles against those actual installed APIs. Complete metadata/IL evidence is under docs/evidence/targeting-0.4.230.

## Preservation and failure isolation

Each write requires an exact current entity version plus mesh-group/index/tile identity match, valid group/instance bounds, an existing group-specific outline property, an MW-owned native highlight and a read-only MeshBatch buffer. Shared rendering settings/materials, prefab colours, transform properties, geometry, owner/companion topology and references are never changed. Native Error/Warning/Override targets and temporary/deleted targets are excluded. At most 1024 batches per target are visited. Duplicate render slots are suppressed; selected is processed before hovered.

Original colour and group mesh identity are retained only in a local renderer cache. Cleanup resolves current identity/property mapping again and restores only if the slot still contains the precise colour last written by MW; later native/foreign colour changes and reused slots are not overwritten. Cleanup is attempted on retirement, context/tool changes, module release and runtime failure. A failure retires only this optional renderer, logs the error and attempts restoration; persistent renderer failures require native refresh/reload. Exact matching behaviour when a foreign writer deliberately writes the identical colour remains intrinsically indistinguishable and is not claimed as ownership proof.

Existing temporary-highlight cleanup before Undo preflight/clipboard dispatch remains in force. Accepted native capture/reconstruction, companion storage, typed component policies and reference mapping are unchanged. No new Harmony patches or capture whitelists are added. Renderer startup failure is isolated from targeting/UI/Undo; the renderer is released before the targeting tool on disposal. All circles were removed from the MW targeting overlay, including selected circles; native gizmo geometry itself is not suppressed.

## Checks and native acceptance

Managed tests compile actual renderer source with controlled native-buffer/ECS doubles. They verify two colours simultaneously, selected precedence, same-group sibling isolation, all exact submeshes, restoration, duplicated/reused/version-changed slots, later foreign colours, native warnings, group read/instance write dependencies, no ECS writes by renderer, no idle fence and injected writer failure. Native GPU upload/frame ordering and visual appearance are not proven by those doubles.

Grouped native acceptance: in one disposable editable asset, hover one prop while a different prop stays selected and confirm gold/cyan silhouettes with no added circles; repeat with bike rack/foliage, surface/area and fence/lane geometry; note which effect/marker targets report the native-gizmo limit. Clear selection, change filter/asset and stop targeting, confirming colours return to normal. Then perform existing copy/replace/replacement Undo and accepted native prop/effect/marker move/deletion Undo, and save/reopen checks as appropriate. Do not discard unrelated unsaved work. No computer use or publishing performed.

Build/test/staging/installation results are recorded in docs/evidence/targeting-0.4.230; native acceptance pending.

VALIDATION VERIFIED (managed doubles): 22 actual-source renderer assertions, 69 targeting assertions, 28 lifecycle assertions, 45 replacement Undo and 86 viewport checks pass; 25 targeting UI checks plus tab fallback/header-focus checks pass. Release/UI builds pass; cached NU1900 advisory-feed warning persists. Focused whitespace checks pass. No native GPU appearance acceptance inferred.

INSTALL VERIFIED 2026-10-23: game absence checked with elevated process visibility. Installed assembly0.4.230.0 and all three copied hashes verified. Nine previous files backed up and hash verified under outputs/rollback/MWEditorTools-before-0.4.230-20261023-230832/MWEditorTools. Record outputs/installation-0.4.230.txt and docs/evidence/targeting-0.4.230/installed-hashes.csv. Native prototype acceptance pending.

LOG REVIEW VERIFIED-RUNTIME 2026-10-23, session23:10:54 through23:13:08: MW0.4.230 loaded; per-instance outline prototype initialized23:10:54.522; targeting activated23:11:31.523 on MW Mixed Test Components. No ERROR/CRITICAL/exception entries found in current-session logs or Player.log at this snapshot; no optional renderer disable/cleanup failure or Undo conflict reported. Existing unsupported-CSS warnings, Move It icon rectangle diagnostics and duplicate SR prefab warnings are present. At23:13:08 targeting pauses under the readiness gate (editing/save/dialog/focus); the combined reason does not establish which condition applied. No actual Undo operations or per-mesh colour-write counters are logged in this session. Visual gold/cyan/no-circle acceptance and successful GPU colour writes cannot be inferred from these logs. User visual outcome pending. Snapshots docs/evidence/targeting-0.4.230/native-test-2313. No code/install changes.

USER ACCEPTANCE VERIFIED-RUNTIME 2026-10-23: after reviewing the latest hover/selected outline test logs, the user confirmed 'All OK' in response to the explicit gold-hover/cyan-selected/no-circles check. Record that visual acceptance as passed for the tested0.4.230 targets. This does not independently establish every native marker/effect gizmo category, untested asset/LOD cases, or fresh Undo/save-reopen regression acceptance; no Undo actions were recorded in the reviewed session. No code/install changes.
