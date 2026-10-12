# Installed API findings

All VERIFIED entries below are VERIFIED-STATIC: metadata and IL inspected read-only, not live editor tests. Scope: installed Steam build 25127643, screenshot game 1.6.2f1. Game.dll SHA-256 `AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A`; MVID `e15551e9-3dea-4855-8dde-1da3cea7d7ef`; assembly version 0.0.0.0. Evidence files accompany this report.

## Component header — VERIFIED

`Game.UI.Editor.InspectorPanelSystem.RefreshSections()` constructs `EditorSection`, captures `ComponentBase`, and assigns `<RefreshSections>b__6` to `EditorSection.onDelete` at IL_0379. The captured delegate calls the private instance method `RemoveComponent(Game.Prefabs.ComponentBase)` at IL_0011.

`RemoveComponent` obtains `component.prefab`, calls `CollectRequiringComponents(Type, PrefabBase)` at IL_000e, then loops over the returned types and calls static `Game.Tools.ApplyPrefabsSystem.RemoveComponent(PrefabBase, Type)` at IL_001e. The dependency collector starts with the selected type and repeatedly examines `ComponentRequirementAttribute`; its IL uses `All` over requirements. Do not substitute a guessed dependency rule.

`ApplyPrefabsSystem.RemoveComponent(PrefabBase, Type)` gets the exact component, returns if Unity-null, calls `PrefabBase.Remove(Type)` at IL_0014, then `UnityEngine.Object.DestroyImmediate(Object, true)` at IL_001b. `PrefabBase.Remove(Type)` removes the matching entry and marks `isDirty`. Retaining a C# reference is not a recovery snapshot after Unity destruction. No confirmation or history capture was seen in this handler chain.

## Object / Area / Net / Effect lists

VERIFIED generic inspector path: `EditorGenerator.TryBuildList` builds `PagedList`; `TryBuildListAdapter` chooses `ArrayAdapter` for arrays and `ListAdapter` for other list types. `PagedList.DeleteElement(Int32)` delegates to `IListAdapter.DeleteElement`. `ListAdapterBase<T>.DeleteElement` first calls `ResolveIndex`, then `DeleteElementInternal`, then invalidates the filter. Snapshot indices must refer to underlying storage, not the visible filtered row.

`ArrayAdapter.DeleteElementInternal(Int32)` allocates an array one element shorter, copies the two ranges and calls accessor `SetTypedValue` at IL_0046. `ListAdapter.DeleteElementInternal(Int32)` calls `IList.RemoveAt` at IL_000c. Separately, `Game.UI.Editor.ListField.RemoveItem(Int32)` only invokes `onItemRemoved`; it is not proof that these four inspector lists use that class.

VERIFIED storage: `ObjectSubObjects.m_SubObjects` is `ObjectSubObjectInfo[]`; `ObjectSubAreas.m_SubAreas` is `ObjectSubAreaInfo[]`; `ObjectSubNets.m_SubNets` is `ObjectSubNetInfo[]`; `EffectSource.m_Effects` is `List<EffectSource.EffectSettings>`.

HYPOTHESIS: the four visible lists named in the brief correspond to those members and use the generic adapter path. Field metadata plus generic code support this candidate mapping, but live widget/accessor ownership and each click's update chain have not been traced. Do not claim all list deletions are intercepted yet.

VERIFIED related synchronisation: `ApplyPrefabsSystem.OnUpdate` consumes `SaveInstance`, calls `UpdateObjectSubObjects`, `UpdateObjectSubNets`, `UpdateObjectSubAreas`, updates the prefab and marks its asset dirty. These routines reconstruct prefab collections from entities and may remove empty components. A history system must account for both directions of prefab/entity synchronisation; restoring only a list can be overwritten or lose generated data.

## Bulldozer and entity destruction — VERIFIED stages

`BulldozeToolSystem.UpdateDefinitions(JobHandle)` schedules `CreateDefinitionsJob` using `ToolOutputBarrier.CreateCommandBuffer` and registers its producer dependency. Job data includes editor mode, ownership-related data and SubObject/SubNet/SubArea lookups. `Apply(JobHandle)` checks `GetAllowApply`, sets applyMode and destroys definitions through the output barrier. This is not a synchronous single EntityManager deletion call.

`ApplyObjectsSystem/HandleTempEntitiesJob.Delete(Int32, Entity, Temp)`, and equivalents in `ApplyAreasSystem` and `ApplyNetSystem`, enqueue `Game.Common.Deleted` on original/temporary entities. Object deletion also has ownership/update handling. `Game.Common.CleanUpSystem.OnUpdate` completes deleted/updated dependencies and calls `EntityManager.DestroyEntity(NativeArray<Entity>)` at IL_002a.

These stages are verified individually; complete scheduling order and every editor-specific propagation branch remain unproven. Capturing at cleanup is too late to assume the original relationships are intact. Removing a Deleted marker is not an undo implementation.

## Selection, creation and persistence

VERIFIED metadata/IL: `InspectorPanelSystem.selectedObject`, `SelectEntity(Entity)`, `SelectPrefab(PrefabBase)`, `SelectObject(Object,String)`; `EditorToolUISystem.SelectEntity(Entity)` and `ToolSystem.actionMode`. Do not create editor systems during startup merely to query them.

`PrefabBase.Create(Type,String)`, `Clone(String)`, `AddComponent(Type)` and `AddComponentFrom(ComponentBase)` exist. Clone/copy completeness and entity-reference remapping are unproven. `PrefabSystem.UpdatePrefab(PrefabBase,Entity)`, `AddOrUpdatePrefab(PrefabBase)` and serialization methods exist; entity instantiation and atomic graph reconstruction were not fully traced.

VERIFIED save entry points: `InspectorPanelSystem.ShowSaveAssetPanel(Action<AssetData>)`, private `OnSaveAssetCheckDLC(PrefabBase,String,Action<PrefabAsset>)`, private `SaveAsset(...)`; `PrefabSystem` save/serialization and `PrefabBase.OnBeforeSerialize/OnAfterDeserialize`. Odin serializer and asset database assemblies are installed. None establishes a complete transaction snapshot or successful restore after reopening. LoadAssetPanel exists; a complete asset load/remap round trip is still a test gate.

## Native undo and confirmation

No general editor deletion undo service was identified. Recursive type searches for Undo/Redo/History/Transaction/Snapshot in Game.dll found unrelated history/snapshot types; method-name scans in Game.dll, Colossal.Core.dll, Colossal.UI.dll and Unity.Entities.dll found no general Undo/Redo commands. `ObjectToolSystem` does contain an input action called "Undo Net Control Point"; that does not establish deletion recovery. Search is bounded and cannot prove native undo is absent everywhere, including bundled UI or unnamed facilities.

VERIFIED: `Game.UI.AppBindings.ShowConfirmationDialog(ConfirmationDialog, Action<Int32>)` and `ShowConfirmationDialogAndWait` exist. Inspector save DLC handling constructs a native ConfirmationDialog and invokes ShowConfirmationDialog. This supports native dialog reuse without editing installed files, subject to runtime lifecycle/thread/callback validation.

## Interception options

- Header handler interception: best candidate before dependency removal. Private managed method is verified; Harmony patchability is not. No Harmony reference appears in Game.dll's references and no Harmony DLL was found in the inspected Managed inventory. Obtain/verify a mod-owned compatible dependency before choosing it.
- Wrapping `EditorSection.onDelete`: public setter exists and preserves the original delegate in principle. Stable access to the generated section tree and refresh lifetime is not proven; generated closure names are brittle patch targets.
- Array/List adapters: managed methods are inspectable. A global patch would affect unrelated lists. Restrict by verified owning asset/member/accessor and resolve storage indices. Generic patch sharing and lifecycle need runtime tests.
- ECS observer before commit: candidate for bulldozer capture. Requires verified system ordering and dependency completion. Burst jobs are not a safe default Harmony target.
- Global EntityManager/DestroyImmediate interception: reject as first approach because scope is broad, cascades may already have occurred, and native/Burst paths can bypass assumptions.

No stable cancellable native pre-delete extension event was identified in the inspected path. This is a bounded finding, not proof none exists.
