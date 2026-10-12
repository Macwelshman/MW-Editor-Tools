using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using Colossal.IO.AssetDatabase;
using Colossal.UI.Binding;
using Game;
using Game.AssetPipeline;
using Game.Prefabs;
using Game.SceneFlow;
using Game.UI;
using Game.UI.Editor;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace MW.EditorTools.Modules.MultiSave
{
    public sealed class MultiSaveSystem : UISystemBase
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly FieldInfo HierarchyField = typeof(EditorHierarchyUISystem).GetField("m_Hierarchy", PrivateInstance);
        private static readonly MethodInfo SaveMethod = typeof(InspectorPanelSystem).GetMethod("OnSaveAssetCheckDLC", PrivateInstance);
        private static readonly MethodInfo CompleteMethod = typeof(SystemBase).GetMethod("CompleteDependency", PrivateInstance);
        private ValueBinding<string> binding;
        internal Func<string> BlockReason;
        internal bool IsBusy => state.busy;
        private EditorHierarchyUISystem hierarchy;
        private InspectorPanelSystem inspector;
        private PrefabSystem prefabs;
        private readonly State state = new State();
        private List<PrefabBase> queue;
        private int cursor;
        private bool waiting;
        private bool stopRequested;
        private int generation;
        private float nextRefresh;
        private float nextSave;
        private string published;
        private string lastScan;
        private static string PositionPath { get { return Path.Combine(Application.persistentDataPath, "MWEditorTools-MultiSave-window.json"); } }

        [Serializable]
        public sealed class WindowPosition
        {
            public float x = .36f, y = .2f;
        }

        [Serializable]
        public sealed class State
        {
            public bool available, visible, busy;
            public float windowX = .36f, windowY = .2f;
            public int eligible, locked, saved, failed, skipped, total, processed;
            public string current = "", status = "Ready.", report = "";
        }

        public override GameMode gameMode { get { return GameMode.Editor; } }
        protected override void OnCreate()
        {
            base.OnCreate();
            try
            {
                if (File.Exists(PositionPath)) SetPosition(JsonUtility.FromJson<WindowPosition>(File.ReadAllText(PositionPath)), false);
            }
            catch (Exception error) { Mod.Log.Warn("Could not read window position: " + error.Message); }
            AddBinding(binding = new ValueBinding<string>("mwEditorToolsMultiSave", "state", "{}"));
            AddBinding(new TriggerBinding<string>("mwEditorToolsMultiSave", "command", Command));
            AddBinding(new TriggerBinding<string>("mwEditorToolsMultiSave", "windowPosition", SavePosition));
        }

        private void SavePosition(string json)
        {
            try { SetPosition(JsonUtility.FromJson<WindowPosition>(json), true); }
            catch (Exception error) { Mod.Log.Warn("Could not save window position: " + error.Message); }
        }

        private void SetPosition(WindowPosition position, bool save)
        {
            if (position == null || float.IsNaN(position.x) || float.IsInfinity(position.x) ||
                float.IsNaN(position.y) || float.IsInfinity(position.y)) return;
            position.x = Mathf.Clamp01(position.x);
            position.y = Mathf.Clamp01(position.y);
            state.windowX = position.x;
            state.windowY = position.y;
            if (save)
            {
                File.WriteAllText(PositionPath, JsonUtility.ToJson(position));
                Publish();
            }
        }

        private bool InEditor()
        {
            var manager = GameManager.instance;
            if (manager == null || manager.gameMode != GameMode.Editor || manager.isGameLoading) return false;
            // The game owns these systems and their initialization order. Creating them
            // from this mod's OnCreate can run editor initialization during game startup.
            hierarchy = World.GetExistingSystemManaged<EditorHierarchyUISystem>();
            inspector = World.GetExistingSystemManaged<InspectorPanelSystem>();
            prefabs = World.GetExistingSystemManaged<PrefabSystem>();
            return hierarchy != null && inspector != null && prefabs != null && hierarchy.Enabled;
        }

        // The full hierarchy includes collapsed rows and precedes search/viewport filtering.
        // Use the editor's own list, rather than all prefabs in the game database.
        private List<PrefabBase> Snapshot(out int locked)
        {
            locked = 0;
            NativeList<EditorHierarchyUISystem.HierarchyItem> rows;
            if (!TryGetRows(out rows)) { ScanStatus("Waiting for the workspace list to be created."); return null; }
            var seen = new HashSet<PrefabBase>();
            var result = new List<PrefabBase>();
            locked = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                var id = rows[i].id;
                PrefabBase prefab = null;
                if (id.type == EditorHierarchyUISystem.ItemType.Object || id.type == EditorHierarchyUISystem.ItemType.Prefab)
                    prefabs.TryGetPrefab(id.prefab, out prefab);
                // Child mesh dependencies are saved recursively by PrefabAsset.Save.
                // Queue the parent row, as with the editor's individual Save action.
                if (prefab == null || !seen.Add(prefab)) continue;
                if (prefab.isReadOnly) locked++;
                else result.Add(prefab);
            }
            ScanStatus("Workspace scan: " + rows.Length + " rows, " + seen.Count + " resolved assets, " + result.Count + " unlocked, " + locked + " locked.");
            return result;
        }

        private void ScanStatus(string message)
        {
            if (message == lastScan) return;
            lastScan = message;
            Mod.Log.Info(message);
        }

        private bool TryGetRows(out NativeList<EditorHierarchyUISystem.HierarchyItem> rows)
        {
            rows = default(NativeList<EditorHierarchyUISystem.HierarchyItem>);
            if (HierarchyField == null || SaveMethod == null || CompleteMethod == null)
                throw new NotSupportedException("This game version's editor API is not supported.");
            // ECS Dependency can include fresh jobs every frame. Waiting for IsCompleted
            // before completing it can starve workspace reads indefinitely. Synchronize
            // before accessing NativeList, only while the panel/batch requests a read.
            CompleteMethod.Invoke(hierarchy, null);
            rows = (NativeList<EditorHierarchyUISystem.HierarchyItem>)HierarchyField.GetValue(hierarchy);
            return rows.IsCreated;
        }

        private void Command(string command)
        {
            if (!InEditor()) return;
            if (command == "open") { state.visible = true; nextRefresh = 0; }
            else if (command == "close") state.visible = false;
            else if (command == "stop" && state.busy)
            {
                stopRequested = true;
                state.status = waiting ? "Stopping after the current asset. Finish any editor dialog first." : "Stopping…";
            }
            else if (command == "saveAll" && !state.busy)
            {
                try
                {
                    int locked;
                    var block = BlockReason?.Invoke();
                    if (block != null) { state.status = block; Publish(); return; }
                    queue = Snapshot(out locked);
                    state.total = 0;
                    state.locked = locked;
                    state.saved = state.failed = state.skipped = state.processed = cursor = 0;
                    state.report = state.current = "";
                    state.visible = true;
                    state.busy = true;
                    waiting = stopRequested = false;
                    generation++;
                    nextSave = UnityEngine.Time.realtimeSinceStartup + .25f;
                    state.status = "Preparing unlocked assets…";
                    if (queue != null) PrepareQueue(locked);
                }
                catch (Exception error) { FailOperation(error); }
            }
            Publish();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            state.available = InEditor();
            if (!state.available)
            {
                if (state.busy) { state.status = "Save All stopped because the editor closed or started loading."; ResetQueue(); }
                Publish();
                return;
            }
            if (state.busy)
            {
                if (!waiting)
                {
                    if (stopRequested) Finish();
                    else if (queue == null)
                    {
                        try { int locked; queue = Snapshot(out locked); if (queue != null) PrepareQueue(locked); }
                        catch (Exception error) { FailOperation(error); }
                    }
                    else if (cursor >= queue.Count) Finish();
                    else if (UnityEngine.Time.realtimeSinceStartup >= nextSave) SaveNext();
                }
            }
            else if (state.visible && UnityEngine.Time.realtimeSinceStartup >= nextRefresh)
            {
                nextRefresh = UnityEngine.Time.realtimeSinceStartup + 1f;
                try
                {
                    int locked;
                    var snapshot = Snapshot(out locked);
                    if (snapshot == null)
                        state.status = "Waiting for the workspace list. Asset count has not refreshed yet.";
                    else
                    {
                        state.eligible = snapshot.Count;
                        state.locked = locked;
                        if (state.status == "Ready." || state.status.StartsWith("Waiting for the workspace list.") || state.status.StartsWith("Workspace scan:"))
                            state.status = lastScan;
                    }
                }
                catch (Exception error) { state.available = false; state.status = error.Message; }
            }
            Publish();
        }

        protected override void OnStopRunning()
        {
            if (state.busy) state.status = "Save All stopped when the editor became inactive.";
            ResetQueue();
            state.available = state.visible = false;
            Publish();
            base.OnStopRunning();
        }

        private void SaveNext()
        {
            var prefab = queue[cursor];
            bool advanced = false;
            state.current = prefab == null ? "Removed asset" : prefab.name;
            state.status = "Saving unlocked assets…";
            try
            {
                NativeList<EditorHierarchyUISystem.HierarchyItem> rows;
                if (!TryGetRows(out rows)) return;
                cursor++;
                advanced = true;
                nextSave = UnityEngine.Time.realtimeSinceStartup + .25f;
                if (prefab == null || prefab.isReadOnly || !Contains(rows, prefab))
                {
                    state.skipped++; state.processed++;
                    Report("Skipped: " + state.current + " (removed or locked).");
                    return;
                }
                if (prefab.asset == null)
                {
                    if (string.IsNullOrWhiteSpace(prefab.name)) throw new InvalidOperationException("Give this asset a name before saving.");
                    // The native first-save path. Never replace a different asset at that path.
                    var path = AssetImportPipeline.GetPath(null, prefab.name, prefab.name, DataType.Prefab, AssetDatabase.user, false);
                    PrefabAsset existing;
                    if (AssetDatabase.user.Exists(path, out existing)) throw new InvalidOperationException("A saved asset already uses this name. Rename it or save it individually.");
                }
                waiting = true;
                int run = generation;
                Mod.Log.Info("Starting save: " + state.current);
                SaveMethod.Invoke(inspector, new object[] { prefab, prefab.name, new Action<PrefabAsset>(asset =>
                {
                    if (run != generation) return;
                    waiting = false;
                    nextSave = UnityEngine.Time.realtimeSinceStartup + .25f;
                    state.processed++;
                    if (asset != null) { state.saved++; Report("Saved: " + prefab.name); }
                    else { state.failed++; Report("Failed: " + prefab.name); }
                    Publish();
                }) });
                if (waiting) state.status = "Waiting for the editor's dependency dialog for " + state.current + ".";
            }
            catch (Exception error)
            {
                if (!advanced) cursor++;
                waiting = false;
                state.failed++; state.processed++;
                var cause = error is TargetInvocationException && error.InnerException != null ? error.InnerException : error;
                Report("Failed: " + state.current + " — " + cause.Message);
                Mod.Log.Error(cause, "Could not save " + state.current);
            }
        }

        private bool Contains(NativeList<EditorHierarchyUISystem.HierarchyItem> rows, PrefabBase target)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                var id = rows[i].id;
                PrefabBase prefab;
                if ((id.type == EditorHierarchyUISystem.ItemType.Object || id.type == EditorHierarchyUISystem.ItemType.Prefab)
                    && prefabs.TryGetPrefab(id.prefab, out prefab) && prefab == target) return true;
            }
            return false;
        }

        private void PrepareQueue(int locked)
        {
            state.eligible = state.total = queue.Count;
            state.locked = locked;
            state.busy = queue.Count != 0;
            state.status = state.busy ? "Saving unlocked assets…" : "No unlocked assets to save.";
        }

        private void Finish()
        {
            state.status = (stopRequested ? "Stopped. " : "Finished. ") + state.saved + " saved, " + state.failed + " failed, " + state.skipped + " skipped.";
            if (state.locked > 0) state.status += " " + state.locked + " locked assets excluded.";
            if (stopRequested) state.status += " " + (state.total - state.processed) + " not attempted.";
            Mod.Log.Info(state.status);
            ResetQueue();
            nextRefresh = 0;
        }
        private void Report(string message)
        {
            Mod.Log.Info(message);
            // Bound the UI payload; the complete report remains in the mod log.
            state.report += message + "\n";
            if (state.report.Length > 16000) state.report = state.report.Substring(state.report.IndexOf('\n', state.report.Length - 12000) + 1);
        }
        private void FailOperation(Exception error)
        {
            ResetQueue(); state.status = "Save All could not start: " + error.Message;
            Mod.Log.Error(error, state.status);
        }
        private void ResetQueue() { queue = null; state.busy = waiting = stopRequested = false; state.current = ""; generation++; }
        private void Publish()
        {
            var json = JsonUtility.ToJson(state);
            if (json == published) return;
            published = json; binding.Update(json);
        }
        public void Release() { ResetQueue(); state.available = state.visible = false; Publish(); }
    }
}
