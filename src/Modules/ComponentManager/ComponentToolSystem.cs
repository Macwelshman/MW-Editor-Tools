using System;
using System.Globalization;
using Game.Prefabs;
using Colossal.UI.Binding;
using Game;
using Game.SceneFlow;
using Game.UI;
using MW.EditorTools.Modules.ComponentManager.Diagnostics;
using MW.EditorTools.Modules.ComponentManager.GameIntegration;
using MW.EditorTools.Modules.ComponentManager.Clipboard;
using MW.EditorTools.Modules.ComponentManager.Transfer;
using UnityEngine;

namespace MW.EditorTools.Modules.ComponentManager.Systems
{
    public sealed class ComponentToolSystem : UISystemBase
    {
        private ValueBinding<string> binding;
        private InspectorState state = new InspectorState();
        private string published;
        private string lastDiagnostic;
        private float nextRead;
        private bool visible = false;
        private readonly TransferClipboard clipboard = new TransferClipboard();
        private readonly TransferSession transfer = new TransferSession();
        private TransferMode mode = TransferMode.ComponentsAndSettings;
        private readonly SelectionRevision selectionRevision = new SelectionRevision();
        private string copyResult = "";
        private string componentSelectionToken = "";
        private PrefabBase reportAsset;
        private int[] selectedIndices = new int[0];
        private Func<bool> writeAllowed;
        internal void Initialize(Action beforeWrite,Func<bool> allowWrite){transfer.BeforeWrite=beforeWrite;writeAllowed=allowWrite;}

        public override GameMode gameMode { get { return GameMode.Editor; } }

        protected override void OnCreate()
        {
            base.OnCreate();
            AddBinding(binding = new ValueBinding<string>("mwComponentTool", "state", "{}"));
            AddBinding(new TriggerBinding<string>("mwComponentTool", "command", Command));
        }

        private void Command(string command)
        {
            if (!state.available) return;
            if(writeAllowed?.Invoke()!=true)return;
            var manager = GameManager.instance;
            if (manager == null || manager.gameMode != GameMode.Editor || manager.isGameLoading) { Clear(); return; }
            if (command == "open") visible = true;
            else if (command == "close") visible = false;
            else if (command == "clearClipboard") { clipboard.Clear(); transfer.InvalidatePreview(); copyResult = "Clipboard cleared."; }
            else if (command != null && command.StartsWith("mode|", StringComparison.Ordinal))
            {
                TransferMode requested;
                if (!Enum.TryParse(command.Substring(5), out requested) || !Enum.IsDefined(typeof(TransferMode), requested)) return;
                mode = requested; transfer.InvalidatePreview(); copyResult = "Mode: " + mode + ". Ready to paste into the destination.";
            }
            else if (command != null && (command.StartsWith("preview|", StringComparison.Ordinal) || command.StartsWith("apply|", StringComparison.Ordinal) || command.StartsWith("paste|", StringComparison.Ordinal) || command.StartsWith("restore|", StringComparison.Ordinal)))
            {
                try
                {
                    PrefabBase prefab;
                    object selection;
                    var fresh = EditorSelectionReader.Read(World, out prefab, out selection);
                    TrackSelection(fresh, prefab, selection);
                    var parts = command.Split('|');
                    if (parts.Length != 2 || !fresh.hasAsset || parts[1] != fresh.selectionToken)
                        copyResult = "Selection changed. Nothing applied or restored; select the destination again.";
                    else
                    {
                        var target = TransferTarget.Resolve(World, prefab, selection);
                        copyResult = parts[0] == "paste" ? transfer.Paste(target, clipboard, mode)
                            : parts[0] == "preview" ? transfer.Plan(target, clipboard, mode)
                            : parts[0] == "apply" ? transfer.Apply(clipboard, target) : transfer.Restore(target);
                    }
                    state = fresh;
                    MW.EditorTools.Mod.Log.Info(copyResult);
                }
                catch (Exception error)
                {
                    copyResult = "Transfer did not complete: " + error.Message;
                    MW.EditorTools.Mod.Log.Error(error, "Component transfer did not complete.");
                }
            }
            else if (command != null && (command.StartsWith("copy|", StringComparison.Ordinal) || command.StartsWith("select|", StringComparison.Ordinal)))
            {
                try
                {
                    PrefabBase prefab;
                    object selection;
                    var fresh = EditorSelectionReader.Read(World, out prefab, out selection);
                    TrackSelection(fresh, prefab, selection);
                    var parts = command.Split('|');
                    if (parts.Length != 3 || !fresh.hasAsset || parts[1] != fresh.selectionToken)
                        copyResult = "Selection changed. Select components again; clipboard preserved.";
                    else
                    {
                        var items = parts[2].Length == 0 ? new string[0] : parts[2].Split(',');
                        if (items.Length > fresh.components.Length) throw new ArgumentException("Invalid component selection.");
                        var indices = new int[items.Length];
                        for (int i = 0; i < items.Length; i++)
                            if (!int.TryParse(items[i], NumberStyles.None, CultureInfo.InvariantCulture, out indices[i]))
                                throw new ArgumentException("Select at least one component to copy.");
                        if (parts[0] == "select")
                        {
                            foreach (int index in indices)
                                if (index < 0 || index >= fresh.components.Length || !fresh.components[index].valid)
                                    throw new ArgumentException("Component list changed. Select components again.");
                            selectedIndices = indices;
                            componentSelectionToken = fresh.selectionToken;
                            copyResult = indices.Length + " component(s) selected.";
                            MW.EditorTools.Mod.Log.Info(copyResult);
                        }
                        else
                        {
                            copyResult = clipboard.Copy(prefab, indices);
                            transfer.InvalidatePreview();
                            MW.EditorTools.Mod.Log.Info(copyResult);
                        }
                    }
                    state = fresh;
                }
                catch (Exception error)
                {
                    copyResult = "Component selection/copy failed; clipboard preserved. " + error.Message;
                    MW.EditorTools.Mod.Log.Error(error, "Component selection/copy failed.");
                }
            }
            else return;
            state.visible = visible;
            Decorate();
            Publish();
        }

        private void TrackSelection(InspectorState fresh, PrefabBase prefab, object selection)
        {
            if (!fresh.hasAsset) { prefab = null; selection = null; }
            if (!ReferenceEquals(reportAsset, prefab))
            {
                transfer.ClearReport();
                copyResult = "";
                reportAsset = prefab;
            }
            var components = prefab == null || prefab.components == null ? new ComponentBase[0] : prefab.components.ToArray();
            fresh.selectionToken = selectionRevision.Observe(selection, prefab, components);
            if (componentSelectionToken != fresh.selectionToken)
            {
                transfer.InvalidatePreview();
                selectedIndices = new int[0];
                componentSelectionToken = fresh.selectionToken;
            }
        }

        private void Decorate()
        {
            state.clipboardSummary = clipboard.Summary;
            state.copyResult = copyResult;
            state.selectedIndices = selectedIndices;
            state.transferMode = mode.ToString();
            state.previewSummary = transfer.Summary;
            state.canApply = transfer.CanApply;
            state.canRestore = transfer.CanRestore;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            var manager = GameManager.instance;
            if (manager == null || manager.gameMode != GameMode.Editor || manager.isGameLoading)
            {
                Clear();
                return;
            }
            if (UnityEngine.Time.realtimeSinceStartup < nextRead) return;
            nextRead = UnityEngine.Time.realtimeSinceStartup + 0.25f;
            try
            {
                PrefabBase prefab;
                object selection;
                state = EditorSelectionReader.Read(World, out prefab, out selection);
                TrackSelection(state, prefab, selection);
                string diagnostic = state.hasAsset
                    ? state.assetName + " | " + state.prefabType + " | " + InspectorStateJson.Serialize(state)
                    : state.status + " | " + state.selectionKind;
                if (diagnostic != lastDiagnostic)
                {
                    MW.EditorTools.Mod.Log.Info(state.hasAsset
                        ? "Selected prefab: " + state.assetName + " (" + state.prefabType + "); found " + state.components.Length + " components."
                        : state.status + " Selection: " + state.selectionKind);
                    lastDiagnostic = diagnostic;
                }
                state.visible = visible;
            }
            catch (Exception error)
            {
                state = new InspectorState { available = true, visible = visible,
                    status = "Unable to inspect this selection. See MWComponentTool logs for details." };
                string diagnostic = error.GetType().FullName + ": " + error.Message;
                if (diagnostic != lastDiagnostic)
                {
                    MW.EditorTools.Mod.Log.Error(error, "Selection inspection failed.");
                    lastDiagnostic = diagnostic;
                }
            }
            Decorate();
            Publish();
        }

        private void Publish()
        {
            string json = InspectorStateJson.Serialize(state);
            if (json == published) return;
            binding.Update(json);
            published = json;
        }

        private void Clear()
        {
            state = new InspectorState();
            visible = false;
            nextRead = 0;
            lastDiagnostic = null;
            clipboard.Clear();
            transfer.Clear();
            copyResult = "";
            selectedIndices = new int[0];
            componentSelectionToken = "";
            reportAsset = null;
            selectionRevision.Clear();
            Publish();
        }

        protected override void OnStopRunning()
        {
            Clear();
            base.OnStopRunning();
        }

        public void Release() { Clear();transfer.BeforeWrite=null;writeAllowed=null; }
    }
}

