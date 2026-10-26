# 0.4.241 native crash — 25 October 2026

VERIFIED-RUNTIME: user reports Surface Replace failed, followed by a crash. Native acceptance remains failed.

Preserved MWEditorTools.log, Player.log and UI.log before another launch. These are the 12:15 session, distinct from the overwritten earlier test. Game process was absent during capture.

Timeline from MWEditorTools.log:
- 12:17:25.895 copied Tiles Surface 02 from entity 53050:101.
- 12:17:30.090 retained surface source row 6 / one live entity was replaced with Tiles Surface 02.
- 12:17:30.109 native material refresh submitted, previous batch 25, expected 24.
- 12:17:30.136 material refresh completed.
- 12:17:30.140 Undo readiness passed retained identities, boundary/source/reference checks. No Undo dispatch recorded.
- 12:17:36.247 copied FenceResidentialHigh01. No fence replacement recorded.
- Subsequent hover/selection events continue through 12:18:33.705.
- 12:18:46.418 an inspector non-deletion edit cleared history and invalidated replacement Undo. This is a generic inspector widget commit; the log does not identify the widget or changed field. FindIt logged a partial prefab reindex at 12:18:46.437.

Player.log ends in Native Crash Reporting: UNKNOWN while executing native code. Managed stack is empty. No native caller, entity, or causal connection to surface replacement is available. No current crash files found in Temp/Colossal Order, Temp/UnityCBMCrashes, or Local/CrashDumps.

UNPROVEN: why the visible replacement failed; whether the surface transaction caused a delayed crash; what inspector edit immediately preceded the crash. A material batch match is not visual correctness or stability acceptance. Do not attribute this crash to the older NativeQuadTree.Update failure, which is absent here. No code or safety checks changed based on this incomplete evidence.

Next smallest evidence: identify the user's action immediately before the crash, particularly an Object Info field edit or native paste. Do not request more surface replacement testing on this version until that distinction is understood.

## User clarification

The user confirms the action immediately before the crash was changing an area in the component list. This explains the inspector commit and partial prefab reindex at 12:18:46; it was not a logged Replacer operation.

VERIFIED-STATIC: SurfaceReplacement.ApplySurfaceReplacement pre-synchronizes compiled SubArea references and retained live PrefabRef values before Refresh(root), intentionally suppressing native area recreation. Later native component-list edits are allowed to take the ordinary area recreation path. The material completion check proves only batch assignment/visibility, not future native rebuild safety.

HYPOTHESIS: retained surface state after replacement is incompatible with a subsequent ordinary native area rebuild. This is now a specific interaction to investigate, not a proven native crash cause. Neither the source read nor the empty managed stack identifies the failing native method. Do not change companion references, geometry, or unrelated Undo based on that hypothesis alone.

## Further clarification — active Replace mode

User explicitly reports Surface Replace failed and Area Replace failed, and confirms Replace mode was enabled when the vanilla component-list area type was changed. The earlier assumption that the surface transaction alone caused the later crash is not established. Treat active targeting plus a vanilla area-type rebuild as the reported crash scenario.

VERIFIED-STATIC: TargetHighlights.AddNativeHighlight skips entities with an Areas.Node buffer. Areas are drawn from their node boundary into OverlayRenderSystem; this path does not change area prefab/owner/geometry data. Native mesh outline painting requires an owned native Highlighted component and MeshBatch data. This narrows the investigation but does not certify native overlay/raycast interaction during rebuild. Existing per-frame Stable checks reject Created/Updated/Deleted/Temp, and entity versions guard stale selection identities; these checks do not prove correct native scheduling through a rebuild.

Area replacement is intentionally refused for non-SurfacePrefab clipboard entries in PasteBlockReason and PlanSurfaceReplacement. Its reported failure must not be described as a successful supported action. Surface native acceptance is likewise failed despite reference/material log completion. Need isolate active target/raycast lifecycle across vanilla inspector changes and the visual surface material result; no verified crash caller yet. No code fix claimed.
