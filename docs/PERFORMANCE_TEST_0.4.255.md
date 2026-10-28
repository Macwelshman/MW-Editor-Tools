# performance-test support — 0.4.255

The user authorized implementing beta compatibility on 28 October 2026. This updates the initial [0.4.254 diagnosis](PERFORMANCE_TEST_COMPATIBILITY_20261028.md). Mode: IMPLEMENT/REVIEW. Game assemblies were read only; no computer use or publishing.

## Verified API evidence

VERIFIED-STATIC: Steam `performance-test`, build 26269843. Exact Game, Colossal.Core, Colossal.Collections, Unity.Collections and Unity.Entities identities/hashes are recorded in [assembly-identities.json](evidence/performance-test-20261028/assembly-identities.json). Game is Version=0.0.0.0, MVID e8325128-556a-460d-a950-497d90b3627c. The earlier stable Game MVID remains e15551e9-3dea-4855-8dde-1da3cea7d7ef.

[inspect-api.ps1](evidence/performance-test-20261028/inspect-api.ps1) reads selected installed APIs with Colossal.Mono.Cecil and reuses recorded stable evidence. [stable-comparison.json](evidence/performance-test-20261028/stable-comparison.json) records 39 exact full-body matches among 63 selected methods. An unmatched or absent baseline is not evidence of an API change; two older records contain only snippets, including duplicated lines in the local-transform-cache record. No full-body equality is claimed for those snippets.

The [selected IL](evidence/performance-test-20261028/beta-selected-il.txt) and [field contracts](evidence/performance-test-20261028/beta-fields.json) include these Game methods:

- InspectorPanelSystem.RemoveComponent, CollectRequiringComponents, UpdateParent, RefreshSections, OnValueChanged and selection callbacks: exact recorded IL. ArrayAdapter/ListAdapter.DeleteElementInternal and PagedList.Clear: exact recorded IL. The dialog callback fields retain Action<int> and Action<int,bool> types.
- BulldozeToolSystem.Apply/GetAllowApply, ObjectToolSystem.StartMoving/Apply/GetAllowApply and AreaToolSystem.Apply/Cancel: exact recorded IL. AreaToolSystem.set_recreate stores the Entity backing field at IL_0002. Control-point, move-target, area-state and temporary-query fields retain the types consumed by recovery.
- ApplyPrefabsSystem.OnUpdate, PrefabSystem.OnUpdate/UpdatePrefab/UpdatePrefabs, ReplacePrefabSystem.ReplacePrefab/Finalize.OnUpdate and PreCullingSystem.OnUpdate: exact recorded IL. Update/save queries, update map and update-instance queue match the recovery contracts.
- ToolRaycastSystem.OnUpdate/GetRaycastResult: exact recorded IL. RaycastResult, ControlPoint and native ownership/placement data remain available to the compiled targeting path.
- AreaBatchSystem.CullingActionJob.FailedCulling and BatchAllocationJob.Execute: exact recorded IL. The existing native allocation-job field/queue/release and scheduling assertions pass on the beta; area batching remains in PreCulling (27).
- BatchManagerSystem.GetNativeBatchGroups/GetNativeBatchInstances return native read/write job fences (IL_0005/IL_000b; CombineDependencies IL_0010). GroupData.GetPropertyIndex still maps a property into its native slot. MeshBatch and InstanceData identity fields used by outline ownership remain present.

VERIFIED-STATIC: Colossal.Core NativeBatchInstances.ParallelInstanceWriter.GetPropertyValue/SetPropertyValue exactly match stable recorded IL. The underlying UnsafeBatches property operations were inspected: GPU allocation/byte stride, native update mask and copy behavior remain consistent with the existing per-instance approach. NativeHeapAllocator.Release delegates its own block to UnsafeHeapAllocator.Release (IL_000c). NativeQueue enqueue/dequeue/writer forwarding was inspected. Unity.Entities DynamicBuffer.GetEnumerator exactly matches recorded stable IL, including its native-array enumerator behavior; existing isolated recovery checks remain in place.

VERIFIED-STATIC: BuildLocalizationFields and IsLocalizationReadOnly match recorded IL; component heading captures continue through the unchanged native RefreshSections callback. The hierarchy field remains NativeList<EditorHierarchyUISystem.HierarchyItem>. The optional Asset Icon Creator adapter retains its separate mod-version/MVID, Harmony, callback-token and original-IL checks.

## Implementation and regression boundaries

[VerifiedGameBuild](../src/Core/VerifiedGameBuild.cs) centralizes exact supported profiles. Recovery/targeting pair Game with Unity.Entities; outlines pair Game with Colossal.Core; area allocation pairs Game with both collection assemblies. Mixed profiles and unknown identities are refused. Existing private-member lookups, runtime ownership checks, conflict detection, snapshot validation and completion gates remain active.

VERIFIED-STATIC: Comparing the installed 0.4.254 mod with the 0.4.255 beta build found 3,195 exact unchanged method bodies; expected guard/logging changes and compiler-generated closure renumbering account for the remaining changes. This checks the compilation effects of the beta references as well as source scope; it is not runtime validation. See [mod-il-comparison.json](evidence/performance-test-20261028/mod-il-comparison.json).

Release and production UI builds pass (NU1900: unavailable NuGet vulnerability service; no fresh dependency advisory clearance). All 36 managed suites and 18 UI scripts pass. The new profile suite covers 48 stable/beta/unknown/mixed-dependency combinations. The beta editor contract check passes 67 assertions against hashes, recorded stable bodies, field types and renderer contracts; the existing area render and scheduling checks pass. One test project initially lacked the new linked helper; its project was corrected and the confirmation suite rerun successfully. Logs and consolidated results are in [the evidence folder](evidence/performance-test-20261028).

## Grouped native acceptance — pending

VERIFIED-RUNTIME: the user's 28 October 19:11 editor load initialized confirmation, list/native deletion recovery, prop/area move hooks, targeting and per-instance outlines without MW errors or unavailable-hook warnings. See [the startup log check](evidence/performance-test-20261028/editor-load-0.4.255/README.md). This verifies startup attachment only; action and persistence acceptance below remain pending. Separate missing-asset/null-prefab/CSS warnings and a Mono assertion were observed without an established MW compatibility cause.

UNSUPPORTED/UNPROVEN: native editor acceptance of 0.4.255 on the beta. Static/managed results do not certify game timing, renderer behavior or persistence. Use disposable asset copies and check these together:

1. Load the editor: no unverified-build errors; component confirmation, native deletion/move Undo, targeting and outline initialization succeed. Check for warnings about unavailable move/bulldozer hooks, not only ERROR lines.
2. Cancel component removal and list Clear: verify unchanged data. Confirm them on a disposable copy and check supported list Undo.
3. Delete and Undo a supported standalone/bound prop or foliage element; move and Undo it. Include supported area/surface/node and effect cases, and directly bound network deletion where used. Verify one last-action behavior and cancelled/no-op preservation.
4. Hover/select/filter targets, then edit Object Info. Verify targeting pauses, owned outlines clear and normal native editing/Undo continues. Check surface replacement/Undo if used.
5. Save/reopen the disposable copy and inspect relationships, geometry, settings and references. Keep the existing unsupported-operation limits.

VERIFIED-STATIC: 0.4.255 was installed into the existing local MWEditorTools mod folder after Cities2 absence checks. All nine existing files were backed up and hash-verified under outputs/rollback/MWEditorTools-before-0.4.255-20261028-190008/MWEditorTools. The three runtime files and release notes match the staged SHA256 hashes; the installed DLL is version 0.4.255.0. Harmony already matched the build dependency and was retained. See [installed-hashes.csv](evidence/performance-test-20261028/installed-hashes.csv) and outputs/installation-0.4.255.txt. No native test or publication is implied by installation.
