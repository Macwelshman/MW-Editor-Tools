# Area and surface replacement — 0.4.242

Implemented; native acceptance pending. Not a published release.

## Evidence

VERIFIED-RUNTIME (0.4.241): the user reports Surface and Area Replace failed, and a native crash when changing an area type in the vanilla component list with Replace active. Logs record a prior retained surface/material update, then an inspector edit at 12:18:46. The native crash has an empty managed stack. Its exact native caller and cause remain UNPROVEN. See evidence/surface-crash-0.4.241-20261025.

VERIFIED-STATIC: installed Game.dll, Game 0.0.0.0, MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef:

- AreaBatchSystem/CullingActionJob.PassedCulling IL_005a writes the new Batch.m_BatchIndex before BatchAllocationJob.Execute. FailedCulling queues allocation release.
- BatchAllocationJob.Execute IL_0033 reads Batch.m_BatchIndex to choose a material bucket; IL_0160 releases the allocation and IL_0184 removes metadata with swap-back repair. Changing a retained live PrefabRef without first releasing its old allocation permits a new bucket identity to coexist with old allocation metadata. This is a verified hazardous transition, not proof of this crash's caller.
- InspectorPanelSystem.OnValueChanged IL_0050 calls UpdateParent(bool); UpdateParent IL_007c queues PrefabSystem.UpdatePrefab.
- ToolRaycastSystem.GetRaycastResult IL_0007 calls RaycastSystem.GetResult, whose IL_0001 calls CompleteRaycast.
- SpacePrefab.GetArchetypeComponents IL_0008 adds Areas.Space; IL_0014 adds Areas.Geometry.

Exact method/field evidence and inspection scripts are in evidence/area-replacement-0.4.242. No game files changed.

## Implementation

AreaRenderRelease completes renderer and ECS work, validates material/meta ownership and allocation bounds, then invokes native FailedCulling and BatchAllocationJob.Execute with a private queue BEFORE changing source/live references. Native routines handle heap release, visible-triangle cleanup and metadata repair. No search-tree writes or direct Batch allocation-field resets. Partial native release failure blocks further replacements and requires reload. Preflight refusal preserves source and previous Undo. Rollback queues the original material again.

The retained-identity source/compiled/live transaction now supports compatible Surface-to-Surface and Space-to-Space areas, single/batch and reverse Undo. Archetype component sets and geometry type/flags must match. Areas without material data do not need the surface renderer. Every live owner and unique source boundary must verify. Destination settings, node/elevation data, local caches, ownership and companion links are retained. Cross-kind conversions, nested owners and differing layouts remain unsupported.

TargetingEditGuard independently prefixes UpdateParent(bool). Active targeting completes the native raycast, restores owned mesh outline colours, clears its selection/highlights and returns to native Select before the rebuild is queued. Vanilla edits proceed. The UI explains why replacing stopped; Start replacing explicitly reacquires targets after the edit. Cleanup failure refuses that refresh with a diagnostic. Existing deletion/move Undo capture/restore code is unchanged. Outline drawing and colours are unchanged; synchronous restoration is exposed for this transition. The guard is isolated from Undo patches and disposed during module cleanup.

## Validation and acceptance

Actual-source managed tests use ECS/renderer doubles and do not establish native stability. They cover allocated old-material release before switching, reverse Undo release, foreign metadata and pending allocation refusal, rollback material requeue, Area single/batch/Undo without a renderer, geometry/settings/companion retention, and targeting cleanup before inspector refresh. Native field/method contracts and area-render phase are checked separately. Release/UI builds pass with the existing NU1900 advisory-feed warning; this is not a completed vulnerability audit.

Grouped native acceptance on a disposable mixed asset remains required:

1. Surface: copy a visibly different surface, replace one, confirm actual material, Undo; repeat and Replace all.
2. Area: copy a different compatible Area, replace one and Undo; repeat/batch.
3. With Replace active, change area type through vanilla Object Info. Replacing must stop, the native edit must complete without crashing, and Start replacing must reacquire targets. Repeat after a surface replacement.
4. Compare boundaries, elevations, destination settings, ownership and companion references. Save/reopen disposable copies only.
5. Include existing prop/effect/marker/fence/lane deletion/move Undo and gold/cyan outlines in the same acceptance group.

No new native acceptance result is claimed.

Validation complete: 77 area/surface adapter checks, 92 targeting checks, 45 replacement Undo checks, 1204 Phase 3 checks, 22 outline checks, 34 retry checks, 76 projection and 10 network adapter checks, 28 lifecycle checks, 33 UI checks. Installed automatically with Cities2 absent; 9 rollback files and all 3 copied hashes verified. See outputs/installation-0.4.242.txt and evidence/area-replacement-0.4.242/installed-hashes.csv. Native acceptance remains pending.
