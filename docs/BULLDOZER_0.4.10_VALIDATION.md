# Version 0.4.10 — bulldozer capture diagnostic

Scope: read-only diagnostic for the user-prioritised bulldozer Undo work. Existing component confirmation, list recovery, persistent UI and recorded shortcuts retained. Bulldozer Undo is UNSUPPORTED in this version. Native deletions proceed unchanged; observer does not create history entries or perform restoration writes.

VERIFIED-RUNTIME (user report): all version 0.4.9 checks pass. Prop Replacer paused.

VERIFIED-STATIC: installed Game.dll hash remains AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A; identity Game 0.0.0.0, MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. Evidence: bulldozer-sync-api.txt and bulldozer-regeneration-api.txt.
- ToolBaseSystem.GetAllowApply returns native validation outcome (IL_0021/0029); diagnostic observes the original result inside BulldozeToolSystem.Apply without reevaluating validation.
- ApplyPrefabsSystem.OnUpdate consumes SaveInstance, reconstructs collections and queues UpdatePrefab. Match the native query's PrefabRef to the same captured owning prefab before observing the after image.
- PrefabSystem.UpdatePrefab only enqueues m_UpdateMap (IL_0008). UpdatePrefabs consumes the map and calls ReplacePrefabSystem.ReplacePrefab (IL_0154), then clears map (IL_01cd). This is dispatch, not final scene completion.
- ReplacePrefabSystem.OnUpdate completes a handle IL_04c6 and plays command buffers IL_0522/053f, with additional Finalize stages. Complete graph regeneration ordering is still UNPROVEN.

The observer logs a maximum of twelve native Apply attempts per launch: before snapshot counts, native accepted/denied result, matching SaveInstance before/after counts, changed supported fields, removed components and queued-rebuild dispatch. Pending observations expire at 120 frames and clear on asset/context change, overlapping actions or errors. Other worlds and unrelated native updates cannot match. Immutable supported payload snapshots retained only during a bounded observation; no whole entity graph snapshot or resurrection claim. Broadly modified/unhandled data will require a wider capture schema before recovery support.

Diagnostic patches use their own Harmony owner; incompatible patches disable the diagnostic without disabling validated recovery. Every observer callback contains its exceptions and preserves native return values/exceptions. Cleanup is guarded independently. No game assemblies modified.

VERIFIED-STATIC checks: C# Release zero warnings/errors; pinned UI build/type checks and Settings render pass. Automated checks: 14 observation lifecycle, 22 Phase 3 snapshot/history, 18 Phase 2 safety, 311 Phase 4, 11 constructor-bypass lifecycle and 26 Options assertions pass. These do not exercise Unity job scheduling, native patch dispatch, entity integrity or runtime timing.

Required in-game trace (disposable duplicate asset):
1. Restart CS2 and confirm other mods and MW Editor Tools still work. History & Recovery should show the bulldozer diagnostic ready message.
2. Save a duplicate asset with at least two placed props. Use the bulldozer to delete one prop; wait two seconds. Check the diagnostic message and report it. This version cannot undo that bulldozer deletion.
3. Delete the remaining prop; wait two seconds. This captures whether native code removes the entire ObjectSubObjects component. Do not save over the original asset.
4. Reload the duplicate, switch into and out of bulldozer mode without deleting anything, then perform one deletion. The logs distinguish previews/denied operations from actual native synchronization.
5. Confirm supported list deletion Undo/Redo and recorded keyboard shortcuts still work.

Tell Codex when finished so it can inspect MWEditorTools.log. Expected evidence chain: before apply → native accepted → matching SaveInstance → changed payload/component result → prefab rebuild dispatched. Missing stages are findings to investigate, not successful recovery. Runtime capture timing and final deferred restoration remain UNPROVEN.
