using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using Game;
using Game.Prefabs;
using Game.SceneFlow;
using Game.UI;
using Game.UI.Editor;
using Game.UI.Localization;
using HarmonyLib;
using Unity.Entities;
using UnityEngine;
using MW.EditorTools.Settings;

namespace MW.EditorTools.Modules.DeletionProtection
{
    public sealed partial class ComponentConfirmation : MonoBehaviour
    {
        private const string PatchId = DeletionPatchPolicy.ConfirmationOwner;
        private const string SupportedMvid = "e15551e9-3dea-4855-8dde-1da3cea7d7ef";
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static ComponentConfirmation active;
        private readonly ConfirmationGate gate = new ConfirmationGate();
        private readonly ConcurrentQueue<Answer> answers = new ConcurrentQueue<Answer>();
        private Harmony harmony;
        private MethodInfo remove;
        private MethodInfo collect;
        private FieldInfo parent;
        private FieldInfo callback;
        private FieldInfo dismissibleCallback;
        private Request pending;
        private Request bypass;
        private int mainThread;
        private bool showingOwnDialog;
        private bool installed;
        private EditorToolsSettings settings;
        internal bool ProtectionInstalled => installed;

        private sealed class Request
        {
            internal long Token;
            internal InspectorPanelSystem Inspector;
            internal World World;
            internal PrefabBase Prefab;
            internal ComponentBase Component;
            internal ComponentBase[] Components;
            internal Type[] Closure;
            internal object Selection;
            internal object Parent;
            internal Game.UI.Widgets.PagedList List;
            internal System.Collections.IList Storage;
            internal FieldInfo ListField;
            internal MW.EditorTools.History.PrefabSnapshot ListSnapshot;
        }

        private struct Answer { internal long Token; internal int Action; }

        public void Initialize(EditorToolsSettings options)
        {
            settings = options;
            mainThread = Thread.CurrentThread.ManagedThreadId;
            if (active != null) throw new InvalidOperationException("Another confirmation instance is active.");
            if (typeof(Harmony).Assembly.GetName().Version != new System.Version(2, 4, 2, 0))
                throw new NotSupportedException("A different Harmony version is loaded; protection was not installed.");
            if (typeof(InspectorPanelSystem).Module.ModuleVersionId.ToString() != SupportedMvid)
                throw new NotSupportedException("Unverified Game.dll build. Protection was not installed.");

            remove = typeof(InspectorPanelSystem).GetMethod("RemoveComponent", PrivateInstance,
                null, new[] { typeof(ComponentBase) }, null);
            collect = typeof(InspectorPanelSystem).GetMethod("CollectRequiringComponents",
                BindingFlags.NonPublic | BindingFlags.Static, null,
                new[] { typeof(Type), typeof(PrefabBase) }, null);
            parent = typeof(InspectorPanelSystem).GetField("m_ParentObject", PrivateInstance);
            callback = typeof(AppBindings).GetField("m_ConfirmationDialogCallback", PrivateInstance);
            dismissibleCallback = typeof(AppBindings).GetField("m_DismissibleConfirmationDialogCallback", PrivateInstance);
            if (remove == null || remove.ReturnType != typeof(void) || collect == null ||
                collect.ReturnType != typeof(Type[]) || parent == null || callback == null || dismissibleCallback == null)
                throw new NotSupportedException("Required editor signatures are unavailable.");
            if (Harmony.GetPatchInfo(remove)?.Owners.Any() == true)
                throw new NotSupportedException("Another patch owns the deletion handler; compatibility is unverified.");

            harmony = new Harmony(PatchId);
            active = this;
            try
            {
                harmony.Patch(remove, prefix: Patch(nameof(BeforeRemove)));
                InstallClearProtection();
                foreach (var method in typeof(InspectorPanelSystem).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.Name == "SelectObject" || m.Name == "SelectPrefab" || m.Name == "SelectEntity"))
                    harmony.Patch(method, prefix: Patch(nameof(InvalidateContext)));
                foreach (var method in typeof(GameManager).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.Name == "Load" || m.Name == "set_gameMode"))
                    harmony.Patch(method, prefix: Patch(nameof(InvalidateContext)));
                foreach (var method in typeof(AppBindings).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.Name == "ShowConfirmationDialog"))
                    harmony.Patch(method, prefix: Patch(nameof(BeforeDialog)));
                installed = true;
                Mod.Log.Info("MW Editor Tools: component-header and list Clear confirmation installed; recovery eligibility is checked separately.");
            }
            catch { Release(); throw; }
        }

        private static HarmonyMethod Patch(string name) => new HarmonyMethod(
            typeof(ComponentConfirmation).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static));

        private static void InvalidateContext() { active?.Invalidate(); }
        private static void BeforeDialog()
        {
            if (active != null && !active.showingOwnDialog) active.Invalidate();
        }

        private static bool BeforeRemove(InspectorPanelSystem __instance, ComponentBase __0)
        {
            var owner = active;
            if (owner == null) return true; // No active feature: retain vanilla behavior.
            try { return owner.Intercept(__instance, __0); }
            catch (Exception error)
            {
                owner.Invalidate();
                Mod.Log.Error(error, "Component deletion blocked because confirmation failed.");
                return false;
            }
        }

        private bool Intercept(InspectorPanelSystem inspector, ComponentBase component)
        {
            if (settings != null && !settings.confirmation) { Invalidate(); return true; }
            if (bypass != null && ReferenceEquals(bypass.Inspector, inspector) &&
                ReferenceEquals(bypass.Component, component))
            {
                bypass = null; // Consume before entering the original; nested calls cannot reuse it.
                return true;
            }
            if (!Ready(inspector) || Thread.CurrentThread.ManagedThreadId != mainThread)
            {
                Mod.Log.Warn("Component deletion blocked: editor context is unavailable.");
                return false;
            }
            if (gate.Busy) return false;
            if (component == null || component.prefab == null || component.prefab.isReadOnly)
                return false;
            if (Harmony.GetPatchInfo(remove)?.Owners.Any(id => !KnownOwner(id)) == true)
                throw new NotSupportedException("Deletion handler patch conflict detected.");

            var ui = GameManager.instance.userInterface?.appBindings;
            if (ui == null || !ui.ready || callback.GetValue(ui) != null || dismissibleCallback.GetValue(ui) != null)
            {
                Mod.Log.Warn("Component deletion blocked: another dialog is active or the UI is unavailable.");
                return false;
            }
            var prefab = component.prefab;
            if (!prefab.components.Any(c => ReferenceEquals(c, component))) return false;
            var selection = inspector.selectedObject;
            var parentObject = parent.GetValue(inspector);
            if (!ReferenceEquals(selection, prefab) && !ReferenceEquals(parentObject, prefab) &&
                !(selection is ComponentBase selected && ReferenceEquals(selected.prefab, prefab)))
                throw new InvalidOperationException("Component does not belong to the current inspector context.");
            var closure = Closure(component, prefab);
            if (!closure.Contains(component.GetType())) throw new InvalidOperationException("Invalid dependency closure.");
            pending = new Request
            {
                Token = gate.Begin(Time.realtimeSinceStartup), Inspector = inspector, World = inspector.World,
                Prefab = prefab, Component = component, Components = prefab.components.ToArray(),
                Closure = closure, Selection = selection, Parent = parentObject
            };
            var token = pending.Token;
            showingOwnDialog = true;
            try
            {
                ui.ShowConfirmationDialog(new ConfirmationDialog(
                    LocalizedString.Value("MW Editor Tools"),
                    LocalizedString.Value("Delete this component from " + prefab.name + "?\n\nComponents removed:\n" +
                        string.Join("\n", closure.Select(t => t.Name)) + "\n\nThis cannot be undone by MW Editor Tools."),
                    LocalizedString.Value("Delete"), LocalizedString.Value("Cancel"), new LocalizedString[0]),
                    action => answers.Enqueue(new Answer { Token = token, Action = action }));
            }
            finally { showingOwnDialog = false; }
            return false;
        }

        private Type[] Closure(ComponentBase component, PrefabBase prefab) =>
            ((Type[])collect.Invoke(null, new object[] { component.GetType(), prefab }))
                .OrderBy(t => t.AssemblyQualifiedName, StringComparer.Ordinal).ToArray();

        private static bool Ready(InspectorPanelSystem inspector)
        {
            var manager = GameManager.instance;
            return manager != null && manager.gameMode == GameMode.Editor && !manager.isGameLoading &&
                inspector != null && inspector.World != null && inspector.World.IsCreated && inspector.Enabled;
        }

        private bool Valid(Request request)
        {
            if (request.List != null) return ValidClear(request);
            return settings != null && settings.confirmation && installed && Ready(request.Inspector) && ReferenceEquals(request.Inspector.World, request.World) &&
                request.Component != null && request.Prefab != null && !request.Prefab.isReadOnly &&
                ReferenceEquals(request.Component.prefab, request.Prefab) &&
                ReferenceEquals(request.Inspector.selectedObject, request.Selection) &&
                ReferenceEquals(parent.GetValue(request.Inspector), request.Parent) &&
                request.Prefab.components.SequenceEqual(request.Components, ReferenceComparer.Instance) &&
                Closure(request.Component, request.Prefab).SequenceEqual(request.Closure) &&
                Harmony.GetPatchInfo(remove)?.Owners.All(KnownOwner) == true;
        }

        private sealed class ReferenceComparer : System.Collections.Generic.IEqualityComparer<ComponentBase>
        {
            internal static readonly ReferenceComparer Instance = new ReferenceComparer();
            public bool Equals(ComponentBase x, ComponentBase y) => ReferenceEquals(x, y);
            public int GetHashCode(ComponentBase obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }

        // Phase 3's own invalidation prefix is compatible; third-party owners remain refused.
        private static bool KnownOwner(string id) => DeletionPatchPolicy.IsKnownOwner(id);

        private void Update()
        {
            blockedClearChange = null;
            try
            {
                if (pending != null && (gate.Expired(Time.realtimeSinceStartup) || !Valid(pending))) Invalidate();
                while (answers.TryDequeue(out var answer))
                {
                    var request = pending;
                    if (!gate.Consume(answer.Token, answer.Action == 0, Time.realtimeSinceStartup))
                    {
                        if (!gate.Busy) pending = null;
                        continue;
                    }
                    pending = null;
                    if (request == null || !Valid(request)) continue;
                    bypass = request;
                    try { if(request.List!=null)ApplyClear(request);else remove.Invoke(request.Inspector, new object[] { request.Component }); }
                    finally { bypass = null; }
                    Mod.Log.Info(request.List!=null?"Confirmed list Clear delegated to the vanilla handler.":"Confirmed component removal delegated to the vanilla handler.");
                }
            }
            catch (Exception error)
            {
                Invalidate();
                Mod.Log.Error(error, "Component confirmation aborted. Vanilla removal may have failed; inspect the asset before saving.");
            }
        }

        private void Invalidate() { gate.Invalidate(); pending = null; }

        public void Release()
        {
            installed = false;
            Invalidate();
            bypass = null;
            // Pending native dialog callbacks remain harmless token-only enqueues.
            clearInspector = null;
            blockedClearChange = null;
            harmony?.UnpatchAll(PatchId);
            if (ReferenceEquals(active, this)) active = null;
            while (answers.TryDequeue(out _)) { }
        }

        private void OnDestroy() { Release(); }
    }
}

