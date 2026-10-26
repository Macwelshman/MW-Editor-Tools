using System;
using System.Linq;
using Game;
using Game.UI;
using Colossal.UI.Binding;
using UnityEngine;

namespace MW.EditorTools.Modules.ColourVariations
{
    public sealed class MWUISystem : UISystemBase
    {
        ValueBinding<string> state;
        string lastState;
        float nextStateTime;
        bool stateFault, wasReady;
        internal void Release(){Enabled=false;lastState=null;state?.Update("{\"available\":false,\"visible\":false}");}
        public override GameMode gameMode { get { return GameMode.Editor; } }
        protected override void OnCreate()
        {
            base.OnCreate();
            AddBinding(state = new ValueBinding<string>("mwEditorToolsColors", "state", "{}"));
            AddBinding(new TriggerBinding<string>("mwEditorToolsColors", "command", json => { if (PaletteWindow.Instance != null) PaletteWindow.Instance.Receive(json); }));
            Debug.Log("MW Colors 0.5.11: UI bindings created; waiting for editor readiness.");
        }
        protected override void OnUpdate()
        {
            base.OnUpdate();
            var window = PaletteWindow.Instance;
            bool ready = window != null && window.IsEditorReady;
            if (ready != wasReady)
            {
                wasReady = ready; nextStateTime = 0;
                Debug.Log("MW Colors 0.5.11: editor ready = " + ready);
            }
            if (ready && UnityEngine.Time.realtimeSinceStartup < nextStateTime) return;
            nextStateTime = UnityEngine.Time.realtimeSinceStartup + 0.1f;
            try
            {
                string json = ready ? window.UIState() : "{\"available\":false,\"visible\":false}";
                if (json != lastState) { state.Update(json); lastState = json; }
                stateFault = false;
            }
            catch (Exception ex)
            {
                if (!stateFault) Debug.LogWarning("MW Colors: UI state update failed: " + ex);
                stateFault = true;
                const string unavailable = "{\"available\":false,\"visible\":false}";
                if (lastState != unavailable) { state.Update(unavailable); lastState = unavailable; }
            }
        }
    }
    public sealed partial class PaletteWindow
    {
        public static PaletteWindow Instance;
        internal Func<string> BlockReason;
        internal Action BeforeWrite;
        bool visible;
        object observedSelection, observedWorld, observedPanel;
        int selectionRevision;
        void ObserveSelection()
        {
            var inspector = Inspector; var panel = Panels?.activePanel;
            var selected = inspector?.selectedObject;
            if (!ReferenceEquals(observedSelection, selected) || !ReferenceEquals(observedWorld, CurrentWorld) || !ReferenceEquals(observedPanel, panel))
            { observedSelection = selected; observedWorld = CurrentWorld; observedPanel = panel; selectionRevision++; }
        }
        [Serializable] class WindowPosition { public float x = .30f, y = .12f; }
        static readonly string PositionPath = System.IO.Path.Combine(Application.persistentDataPath, "MWEditorTools-Colours-window-position.json");
        readonly WindowPosition windowPosition = LoadWindowPosition();
        static bool ValidPosition(float value) { return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 1; }
        static WindowPosition LoadWindowPosition()
        {
            try
            {
                if (System.IO.File.Exists(PositionPath))
                {
                    var position = JsonUtility.FromJson<WindowPosition>(System.IO.File.ReadAllText(PositionPath));
                    if (position != null && ValidPosition(position.x) && ValidPosition(position.y)) return position;
                }
            }
            catch (Exception ex) { Debug.LogWarning("MW Colors: could not load window position: " + ex.Message); }
            return new WindowPosition();
        }
        void SaveWindowPosition(float x, float y)
        {
            if (!ValidPosition(x) || !ValidPosition(y)) return;
            if (windowPosition.x == x && windowPosition.y == y) return;
            windowPosition.x = x; windowPosition.y = y;
            try { System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PositionPath)); System.IO.File.WriteAllText(PositionPath, JsonUtility.ToJson(windowPosition)); }
            catch (Exception ex) { Debug.LogWarning("MW Colors: could not save window position: " + ex.Message); }
        }
        public bool IsEditorReady { get { return EditorReady; } }
        void UpdateWindowInput() { }
        void ReleaseWindowInput() { }
        void DisposeWindowStyle() { }
        [Serializable] public class CommandData { public string action, pairs, channel2; public bool append; public int index; public float x, y; }
        [Serializable] public class UIData
        {
            public bool available, visible, changed, hasTarget, hasColors, canPaste, canUndo, canApply, canReset, confirmPaste, append, readOnly;
            public string target, source, group, status, pairs, channel2, blockedReason;
            public string[] targets;
            public int selectionRevision, paletteRevision, targetIndex, groupIndex, groupCount, generated, total, page, pages;
            public string[] previewRows;
            public float windowX, windowY;
        }
        public string UIState()
        {
            if (!EditorReady) return "{\"available\":false,\"visible\":false}";
            if (!visible) return "{\"available\":true,\"visible\":false}";
            ObserveSelection();
            var s = new UIData { available = true, visible = true, status = status, pairs = pairs, channel2 = channel2,
                windowX = windowPosition.x, windowY = windowPosition.y,
                selectionRevision = selectionRevision, paletteRevision = paletteRevision, append = append, source = copiedFrom, canPaste = copiedProperties != null, canUndo = undo != null, canReset = undo != null && undoPaste && target != null && undoTarget != null && target.Mesh == undoTarget.Mesh && CurrentWorld == undoWorld,
                targets = targets.Select(t => t.Mesh == null ? "Unavailable mesh" : t.Mesh.name).ToArray(),
                targetIndex = targets.IndexOf(target), groupIndex = groupIndex, pages = 1, previewRows = new string[0] };
            if (!s.visible) return JsonUtility.ToJson(s);
            s.blockedReason=BlockReason?.Invoke();
            if(s.blockedReason!=null){s.canPaste=false;s.canUndo=false;}
            s.changed = CurrentWorld != capturedWorld || CurrentWorld == null || !CurrentWorld.IsCreated || Inspector == null || !ReferenceEquals(Inspector.selectedObject, capturedSelection);
            s.hasTarget = target != null && target.Mesh != null;
            s.hasColors = s.hasTarget && target.Colors != null;
            s.target = s.hasTarget ? target.Mesh.name : "No mesh selected";
            s.confirmPaste = s.hasTarget && ReferenceEquals(pendingPasteTarget, target);
            s.readOnly = s.hasTarget && (target.Mesh.isReadOnly || (target.Root != null && target.Root.isReadOnly));
            if(s.changed||s.readOnly||s.blockedReason!=null)s.canReset=false;
            s.canApply = s.blockedReason==null && !s.changed && staged != null && s.hasColors && !s.readOnly && target.Colors.active;
            if (s.hasColors)
            {
                s.group = Group() ?? "Ungrouped";
                s.groupCount = target.Colors.m_VariationGroups == null ? 0 : target.Colors.m_VariationGroups.Count;
                if (staged != null)
                {
                    s.generated = generated; s.total = staged.Count;
                }
            }
            return JsonUtility.ToJson(s);
        }
        static string[] PreviewRows(System.Collections.Generic.List<Game.Prefabs.ColorProperties.VariationSet> rows, int[] map, int start)
        {
            return rows.Skip(start).Take(PreviewPageSize).Select((r, i) =>
                (start + i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" +
                string.Join(" ", map.Select(slot => ToRgba(r.m_Colors[slot]).Hex()).ToArray())).ToArray();
        }
        public void Receive(string json)
        {
            if (!EditorReady) return;
            Run(delegate {
                var c = JsonUtility.FromJson<CommandData>(json);
                if(c==null)throw new ArgumentException("Missing colour command.");
                ColourWriteGate.Before(c.action,BlockReason,BeforeWrite);
                switch (c.action)
                {
                    case "open": OpenPanel(); break;
                    case "close": ClosePanel(); break;
                    case "position": SaveWindowPosition(c.x, c.y); break;
                    case "follow":
                        ObserveSelection();
                        if (c.index != selectionRevision) break;
                        Capture(); break;
                    case "capture": Capture(); break;
                    case "target":
                        if (c.index < 0 || c.index >= targets.Count) throw new ArgumentException("Mesh no longer available.");
                        Choose(targets[c.index]); break;
                    case "copy": CopyProperties(); break;
                    case "paste": PreparePaste(); PasteProperties(); break;
                    case "reset":
                        CheckSelection(false);
                        if(!undoPaste||undoTarget==null||target.Mesh!=undoTarget.Mesh)throw new InvalidOperationException("No pasted settings to reset on this mesh.");
                        Undo(); break;
                    case "confirm": PasteProperties(); break;
                    case "cancel": pendingPasteTarget = null; break;
                    case "load": LoadPalette(); break;
                    case "group":
                        CheckSelection(false, false);
                        if (target.Colors == null || target.Colors.m_VariationGroups == null || c.index < 0 || c.index >= target.Colors.m_VariationGroups.Count) throw new ArgumentException("Group no longer available.");
                        groupIndex = c.index; InvalidatePreview(); break;
                    case "preview": pairs = c.pairs ?? ""; channel2 = c.channel2 ?? ""; append = c.append; Preview();
                        Debug.Log("MW Colors: preview generated " + generated + " combinations; " + staged.Count + " staged rows."); break;
                    case "page": previewPage = Math.Max(0, c.index); break;
                    case "apply": Apply(); break;
                    case "undo": Undo(); break;
                    default: throw new ArgumentException("Unknown MW command.");
                }
                if(c.action=="apply" || c.action=="paste" || c.action=="confirm" || c.action=="undo" || c.action=="reset")
                    MW.EditorTools.Mod.Log.Info("Integrated Colours completed command: "+c.action);
            });
        }
    }
}


