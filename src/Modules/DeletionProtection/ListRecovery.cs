using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Game;
using Game.Prefabs;
using Game.Reflection;
using Game.SceneFlow;
using Game.Tools;
using Game.UI;
using Game.UI.Editor;
using Game.UI.Localization;
using Game.UI.Widgets;
using HarmonyLib;
using MW.EditorTools.History;
using Unity.Entities;
using UnityEngine;
using MW.EditorTools.Settings;
using MW.EditorTools.Services;

namespace MW.EditorTools.Modules.DeletionProtection
{
    public sealed partial class ListRecovery : MonoBehaviour
    {
        private const string Id = DeletionPatchPolicy.RecoveryOwner;
        private static ListRecovery active;
        private static readonly Type[] Supported = RecoveryListSchema.Components;
        private static readonly string[] Members = RecoveryListSchema.Members;
        private readonly LastDeletion<Entry> history = new LastDeletion<Entry>();
        private World world;
        private Harmony harmony;
        private FieldInfo parentField, sectionObjects, dialogCallback, dismissibleCallback;
        private MethodInfo updateParent, refresh;
        private PropertyInfo panelChildren;
        private PrefabBase asset;
        private Pending pending;
        private bool applying, enabledFeature;
        private int thread;
        private int weight;
        private EditorToolsSettings settings;
        private EditorContextService context;
        private MW.EditorTools.Modules.BulldozerRecovery.BulldozerTrace bulldozerTrace;
        private string StartTestBlockReason=>!enabledFeature?"Recovery module unavailable.":settings?.recovery!=true?"Enable deletion recovery in Options first.":bulldozerTrace==null?"Bulldozer recovery module unavailable.":bulldozerTrace.StartTestBlockReason;
        public string BulldozerDiagnostic => !enabledFeature ? "Recovery module unavailable." : settings?.recovery!=true ? "Enable deletion recovery in Options." : bulldozerTrace?.Status ?? "Bulldozer recovery unavailable.";
        public bool RecoveryInstalled => enabledFeature;
        private Func<bool> saveBusy;
        private bool Saving => saveBusy?.Invoke()==true;
        internal void SetSaveBusy(Func<bool> value) { saveBusy = value; }
        internal bool OperationBusy => pending != null || applying || bulldozerTrace?.Busy==true || bulldozerTrace?.MovePreviewActive==true || bulldozerTrace?.NestedTestRunning==true;
        public int UndoCount => replacementUndo!=null?1:history.UndoCount + (bulldozerTrace?.UndoCount ?? 0);
        public int RedoCount => history.RedoCount + (bulldozerTrace?.RedoCount ?? 0);
        public string UndoTarget => replacementUndo?.Label ?? (bulldozerTrace?.HasRecord==true?bulldozerTrace.UndoTarget:history.Latest?.Label);
        public string RedoTarget => bulldozerTrace?.HasRecord==true?bulldozerTrace.RedoTarget:history.NextRedo?.Label;
        public string Status => bulldozerTrace?.HasRecord == true || bulldozerTrace?.Busy == true ? bulldozerTrace.Status : status;
        public string ActiveAssetName => context?.Asset?.name ?? bulldozerTrace?.AssetName;
        public bool CanArmMainTest => !Saving && enabledFeature && settings?.recovery==true && bulldozerTrace?.CanArmMain==true;
        public bool MainHistoryEnabled => bulldozerTrace?.MainHistoryEnabled==true;
        public void ArmMainTest(){if(MainHistoryEnabled)return;if(CanArmMainTest){Clear("Enabling standalone prop Undo — previous history cleared");bulldozerTrace.ArmMain();}}
        public bool CanArmBulldozerTest => !Saving && enabledFeature && settings?.recovery==true && bulldozerTrace?.CanArm==true;
        public bool NestedHistoryEnabled => bulldozerTrace?.NestedHistoryEnabled==true;
        public void EnableNestedHistory(){if(NestedHistoryEnabled&&!bulldozerTrace.NestedTestRunning){bulldozerTrace.EnableNestedHistory();return;}if(!CanArmBulldozerTest){bulldozerTrace?.ReportTestBlocked(StartTestBlockReason??"Recovery is unavailable.");return;}Clear("Enabling nested prop Undo — previous history cleared");bulldozerTrace.EnableNestedHistory();}
        public void StartNestedRecoveryTest(int count=1){Mod.Log.Info("Start test requested; selected asset="+(context?.Asset?.name??"none"));if(!CanArmBulldozerTest){string reason=StartTestBlockReason??"The test is unavailable.";if(bulldozerTrace!=null)bulldozerTrace.ReportTestBlocked(reason);else Mod.Log.Info("Cannot start test: "+reason);return;}Clear("Starting automatic nested recovery test — list history cleared");bulldozerTrace.StartNestedRecoveryTest(count);Mod.Log.Info("Start test result: "+bulldozerTrace.Status);}
        public void CreateAreaFixture(){if(CanArmBulldozerTest){Clear("Creating disposable area test copy — list history cleared");bulldozerTrace.CreateAreaFixture();}}
        public void InspectNestedFixture(){bulldozerTrace?.InspectNestedFixture();}
        public void CreateNestedFixture(){if(CanArmBulldozerTest){Clear("Creating disposable nested fixture — list history cleared");bulldozerTrace.CreateNestedFixture();}}
        public void ArmNestedForestTest(){if(CanArmBulldozerTest){Clear("Arming nested set test — list history cleared");bulldozerTrace.ArmNestedForest();}} public void ArmNestedGroupTest(){if(CanArmBulldozerTest){Clear("Arming temporary nested group test — list history cleared");bulldozerTrace.ArmNestedGroup();}} public void ArmLastPropTest(){if(CanArmBulldozerTest){Clear("Arming last bound prop test — list history cleared");bulldozerTrace.ArmLast();}}
        public void ArmBulldozerTest(){if(CanArmBulldozerTest){Clear("Arming duplicate-asset Sub-element history test — list history cleared");bulldozerTrace.Arm();}}
        public string[] Recent => replacementUndo!=null?new[]{replacementUndo.Label}:(bulldozerTrace?.HasRecord==true?bulldozerTrace.Recent:new string[0]).Concat(history.Recent.Reverse().Select(e => e.Label)).ToArray();
        public bool CanUndo => !Saving && enabledFeature && settings?.recovery==true && (replacementUndo!=null?ReplacementRecoveryReady && replacementUndo.ContextValid && replacementUndo.CanUndo:bulldozerTrace?.HasRecord==true?bulldozerTrace.CanUndo:CanAct && history.UndoCount>0);
        public bool CanRedo => !Saving && enabledFeature && settings?.recovery==true && (bulldozerTrace?.HasRecord==true?bulldozerTrace.CanRedo:CanAct && history.RedoCount>0);
        private bool CanAct => settings != null && settings.recovery && enabledFeature && pending == null && bulldozerTrace?.Busy!=true && Ready(Inspector) && !BusyUI();
        private readonly MW.EditorTools.Services.DeferredShortcut shortcut=new MW.EditorTools.Services.DeferredShortcut();
        private PrefabBase shortcutAsset;
        private Func<bool> shortcutAllowed;
        public void RequestShortcut(int action,Func<bool> allowed) {
            if(allowed?.Invoke()!=true || !(action==0?CanUndo:action==1&&CanRedo))return;
            if(!shortcut.Request(action,Time.frameCount))return;
            shortcutAsset=context.Asset;shortcutAllowed=allowed;
            shortcutReplacement=action==0?replacementUndo:null;
            Mod.Log.Info("Shortcut queued: "+(action==0?"Undo":"Redo"));
        }
        public void Undo() { if(!CanUndo)return;if(replacementUndo!=null)UndoReplacement();else if(bulldozerTrace?.HasRecord==true)bulldozerTrace.Undo();else Command(true); }
        public void Redo() { if(!CanRedo)return;if(bulldozerTrace?.HasRecord==true)bulldozerTrace.Redo();else Command(false); }
        public void ClearHistory() { if(bulldozerTrace?.Busy==true)return;bulldozerTrace?.Invalidate();Clear("History cleared"); }
        public void ApplySettings() { bulldozerTrace?.Invalidate();bulldozerTrace?.SetDepth(settings.historyDepth);history.SetDepth(settings.historyDepth); Clear("Settings changed — history cleared"); }
        private string status = "No deletion to undo";
        private EditorSection section;
        private InspectorPanelSystem sectionInspector;

        private sealed class Image
        {
            internal ComponentBase Component;
            internal FieldInfo[] Fields;
            internal PrefabSnapshot[] Values;
            internal bool Active;
            internal bool Matches() => Component != null && Component.active == Active &&
                Fields.Select((f, i) => Values[i].Matches(f.GetValue(Component))).All(v => v);
            internal bool Valid => Component != null && Values.All(v => v.ReferencesValid);
            internal int Weight => Values.Sum(v => v.Weight);
            internal void Write()
            {
                // Materialize every value before touching the live component.
                var values = Values.Select(v => v.Materialize()).ToArray();
                for (int i = 0; i < Fields.Length; i++) Fields[i].SetValue(Component, values[i]);
                Component.active = Active;
            }
        }
        private sealed class Entry
        {
            internal PrefabBase Asset;
            internal ComponentBase[] Components;
            internal Image[] Before, After;
            internal string Label;
            internal int Weight => Before.Sum(i => i.Weight) + After.Sum(i => i.Weight);
        }
        private sealed class Pending
        {
            internal InspectorPanelSystem Inspector;
            internal object Adapter;
            internal ComponentBase Owner;
            internal FieldInfo Field;
            internal PrefabSnapshot Expected;
            internal Entry Entry;
            internal int Frame;
        }

        internal void Initialize(World modWorld, EditorToolsSettings options, EditorContextService editorContext)
        {
            settings = options; context = editorContext; history.SetDepth(settings.historyDepth);
            world = modWorld; thread = Thread.CurrentThread.ManagedThreadId;
            if (active != null || typeof(InspectorPanelSystem).Module.ModuleVersionId.ToString() != "e15551e9-3dea-4855-8dde-1da3cea7d7ef" ||
                typeof(Harmony).Assembly.GetName().Version != new System.Version(2, 4, 2, 0))
                throw new NotSupportedException("Unverified recovery runtime.");
            parentField = Field(typeof(InspectorPanelSystem), "m_ParentObject");
            sectionObjects = Field(typeof(InspectorPanelSystem), "m_SectionObjects");
            dialogCallback = Field(typeof(AppBindings), "m_ConfirmationDialogCallback");
            dismissibleCallback = Field(typeof(AppBindings), "m_DismissibleConfirmationDialogCallback");
            updateParent = Method(typeof(InspectorPanelSystem), "UpdateParent", typeof(bool));
            refresh = Method(typeof(InspectorPanelSystem), "RefreshSections");
            panelChildren = typeof(EditorPanelSystemBase).GetProperty("children", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new MissingMemberException("EditorPanelSystemBase.children");
            var deletedMethods = new[] { Method(typeof(ArrayAdapter), "DeleteElementInternal", typeof(int)), Method(typeof(ListAdapter), "DeleteElementInternal", typeof(int)) };
            if (deletedMethods.Any(m => Harmony.GetPatchInfo(m)?.Owners.Any() == true))
                throw new NotSupportedException("Another list-deletion patch is active.");
            harmony = new Harmony(Id); active = this;
            try
            {
                foreach (var method in deletedMethods)
                    harmony.Patch(method, prefix: Patch(nameof(BeforeDelete)), finalizer: Patch(nameof(DeleteFailed)));
                var clear = Method(typeof(PagedList), "Clear");
                if (Harmony.GetPatchInfo(clear)?.Owners.Any(id => !DeletionPatchPolicy.IsKnownOwner(id)) == true)
                    throw new NotSupportedException("Another list Clear patch is active.");
                harmony.Patch(clear, prefix: new HarmonyMethod(typeof(ListRecovery).GetMethod(nameof(BeforeClear), BindingFlags.Static | BindingFlags.NonPublic))
                    { priority = Priority.Last, after = new[] { DeletionPatchPolicy.ConfirmationOwner } }, finalizer: Patch(nameof(DeleteFailed)));
                harmony.Patch(Method(typeof(InspectorPanelSystem), "OnValueChanged", typeof(IWidget)),
                    postfix: Patch(nameof(ValueChanged)));
                harmony.Patch(Method(typeof(InspectorPanelSystem), "RefreshSections"), postfix: Patch(nameof(RefreshUI)));
                foreach (var method in typeof(GameManager).GetMethods().Where(m => m.Name == "Load" || m.Name == "set_gameMode"))
                    harmony.Patch(method, prefix: Patch(nameof(ClearAllContext)));
                harmony.Patch(Method(typeof(InspectorPanelSystem), "RemoveComponent", typeof(ComponentBase)), prefix: Patch(nameof(ClearAllContext)));
                harmony.Patch(Method(typeof(BulldozeToolSystem), "Apply", typeof(Unity.Jobs.JobHandle)), prefix: Patch(nameof(ClearContext)));
                enabledFeature = true;
                try { bulldozerTrace = new MW.EditorTools.Modules.BulldozerRecovery.BulldozerTrace(world, context); bulldozerTrace.MoveCommitted=()=>Clear("Move committed — previous Undo cleared"); bulldozerTrace.AutomaticCaptureAllowed=()=>enabledFeature && settings?.recovery==true && !Saving; bulldozerTrace.SetDepth(settings.historyDepth); bulldozerTrace.Attach(); }
                catch (Exception error) { bulldozerTrace = null; Mod.Log.Warn("Bulldozer diagnostic unavailable: " + error.Message); }
                Mod.Log.Info("Prefab-list recovery loaded: Object/Area/Net/Effect/Lane rows and Clear; bulldozer routes have separate eligibility checks.");
            }
            catch { Release(); throw; }
        }

        private static FieldInfo Field(Type type, string name) => type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(type.Name, name);
        private static MethodInfo Method(Type type, string name, params Type[] args) => type.GetMethod(name,
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, args, null)
            ?? throw new MissingMethodException(type.Name, name);
        private static HarmonyMethod Patch(string name) => new HarmonyMethod(Method(typeof(ListRecovery), name,
            name == nameof(BeforeDelete) ? new[] { typeof(object), typeof(int) } :
            name == nameof(DeleteFailed) ? new[] { typeof(Exception) } :
            name == nameof(ValueChanged) ? new[] { typeof(InspectorPanelSystem), typeof(IWidget) } :
            name == nameof(RefreshUI) ? new[] { typeof(InspectorPanelSystem) } : Type.EmptyTypes));
        private static void ClearContext() { active?.Clear("List history cleared by bulldozer use"); }
        private static void ClearAllContext(){active?.bulldozerTrace?.Invalidate();active?.Clear("History cleared by asset load or component removal");}
        private static Exception DeleteFailed(Exception __exception)
        {
            if (__exception != null) active?.Clear("List deletion failed; history invalidated");
            return __exception;
        }
        private static bool BeforeDelete(object __instance, int __0)
        {
            if (active == null) return true;
            try { return active.CaptureDeletion(__instance, __0); }
            catch (Exception error)
            {
                active.Clear("Snapshot failed — deletion blocked");
                Mod.Log.Error(error, "Supported list deletion blocked because its snapshot could not be captured.");
                return false;
            }
        }
        private static bool BeforeClear(PagedList __instance, bool __runOriginal)
        {
            if (!__runOriginal || active == null) return true;
            try { return active.CaptureDeletion(__instance.adapter, -1, true); }
            catch (Exception error)
            {
                active.Clear("Snapshot failed — Clear blocked");
                Mod.Log.Error(error, "Supported list Clear blocked because its snapshot could not be captured.");
                return false;
            }
        }
        private static void ValueChanged(InspectorPanelSystem __instance, IWidget __0)
        {
            if (active == null || active.applying || ComponentConfirmation.SuppressClearChange(__0)) return;
            try { active.bulldozerTrace?.Invalidate();active.Commit(__instance, __0); }
            catch (Exception error) { active.Clear("History capture failed"); Mod.Log.Error(error, "List deletion was not recorded. No undo is available for it."); }
        }
        private static void RefreshUI(InspectorPanelSystem __instance)
        {
            if (active == null || !active.enabledFeature) return;
            try { active.EnsureUI(__instance); }
            catch (Exception error) { Mod.Log.Error(error, "Recovery controls unavailable."); }
        }

        private InspectorPanelSystem Inspector => context?.Inspector;
        private bool Ready(InspectorPanelSystem inspector)
        {
            var gm = GameManager.instance;
            return enabledFeature && Thread.CurrentThread.ManagedThreadId == thread && gm != null &&
                gm.gameMode == GameMode.Editor && !gm.isGameLoading && inspector != null && inspector.Enabled &&
                ReferenceEquals(inspector.World, world);
        }
        private PrefabBase Root(InspectorPanelSystem inspector) => parentField.GetValue(inspector) as PrefabBase;
        private bool BusyUI()
        {
            return Saving || context == null || context.DialogBusy;
        }
        private static Image[] Capture(PrefabBase root) => root.components.Where(c => c != null && Supported.Contains(c.GetType()))
            .Select(c =>
            {
                var fields = c.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .OrderBy(f => f.Name, StringComparer.Ordinal).ToArray();
                return new Image { Component = c, Active = c.active, Fields = fields,
                    Values = fields.Select(f => PrefabSnapshot.Take(f.GetValue(c))).ToArray() };
            }).ToArray();

        private bool CaptureDeletion(object adapter, int index, bool clear = false)
        {
            if (settings == null || !settings.recovery) return true;
            var inspector = Inspector;
            if (!Ready(inspector)) return true; // Unrelated gameplay/editor lists retain vanilla behavior.
            var root = Root(inspector);
            if (root == null) return true;
            var elementType = adapter.GetType().GetProperty("elementType").GetValue(adapter) as Type;
            if (!RecoveryListSchema.Elements.Contains(elementType)) return true;
            var accessor = adapter.GetType().GetProperty("accessor").GetValue(adapter) as IValueAccessor;
            if (accessor == null) return true;
            var storage = accessor.GetValue();
            ComponentBase owner = null; FieldInfo field = null;
            for (int i = 0; i < Supported.Length; i++)
            {
                var member = Supported[i].GetField(Members[i]);
                foreach (var component in root.components.Where(c => c != null && c.GetType() == Supported[i]))
                {
                    if (!ReferenceEquals(member.GetValue(component), storage)) continue;
                    if (owner != null || elementType != RecoveryListSchema.Elements[i])
                        throw new NotSupportedException("List storage has ambiguous ownership or an incompatible element type.");
                    owner = component; field = member;
                }
            }
            if (owner == null) return true; // No patch effect on unmapped lists.
            if (applying || pending != null || bulldozerTrace?.Busy==true || root.isReadOnly || BusyUI()) return false;
            bulldozerTrace?.Invalidate();
            var method = clear ? Method(typeof(PagedList), "Clear") : Method(adapter.GetType(), "DeleteElementInternal", typeof(int));
            if (Harmony.GetPatchInfo(method)?.Owners.Any(id => !DeletionPatchPolicy.IsKnownOwner(id)) == true)
                throw new NotSupportedException("List interception conflict.");
            var list = storage as IList;
            if (clear && list != null && list.Count == 0) return true;
            if (!ReferenceEquals(asset, root)) { Clear("New asset — history cleared"); asset = root; }
            var expected = RecoveryListSchema.AfterDeletion(list, index, clear);
            pending = new Pending
            {
                Inspector = inspector, Adapter = adapter, Owner = owner, Field = field, Expected = expected, Frame = Time.frameCount,
                Entry = new Entry { Asset = root, Components = root.components.ToArray(), Before = Capture(root),
                    Label = RecoveryListSchema.Labels[Array.IndexOf(Supported, owner.GetType())] + (clear ? " Clear (" + list.Count + " entries)" : " row " + (index + 1)) }
            };
            return true; // Original array/list mutation and native UI change event proceed unchanged.
        }

        private void Commit(InspectorPanelSystem inspector, IWidget widget)
        {
            if (pending == null) { Clear("History cleared by a non-deletion edit"); return; }
            var request = pending;
            pending = null;
            if (!ReferenceEquals(inspector, request.Inspector) || !(widget is PagedList paged) ||
                !ReferenceEquals(paged.adapter, request.Adapter) || !Ready(inspector) ||
                !ReferenceEquals(Root(inspector), request.Entry.Asset) || !Shape(request.Entry) ||
                !request.Expected.Matches(request.Field.GetValue(request.Owner)))
            { Clear("Deletion event did not match the captured list — undo unavailable"); return; }
            request.Entry.After = Capture(request.Entry.Asset);
            // Every other supported field must remain unchanged by this operation.
            foreach (var before in request.Entry.Before)
            {
                if (before.Component.active != before.Active) { Clear("Component activation changed — undo unavailable"); return; }
                for (int i = 0; i < before.Fields.Length; i++)
                    if (!(ReferenceEquals(before.Component, request.Owner) && before.Fields[i].Equals(request.Field)) &&
                        !before.Values[i].Matches(before.Fields[i].GetValue(before.Component)))
                    { Clear("Related data changed — undo unavailable"); return; }
            }
            if (request.Entry.Weight > 500000) { Clear("Snapshot too large — undo unavailable"); return; }
            if (weight + request.Entry.Weight > 500000) { history.Clear(); weight = 0; }
            replacementUndo=null;history.Record(request.Entry); weight += request.Entry.Weight;
            status = "Recorded " + request.Entry.Label;
            Mod.Log.Info(status);
        }

        private bool Shape(Entry entry) => entry.Asset != null && !entry.Asset.isReadOnly &&
            entry.Asset.components.Count == entry.Components.Length &&
            entry.Asset.components.Zip(entry.Components, (a, b) => ReferenceEquals(a, b) && a != null).All(v => v);
        private bool Validate(Entry entry, Image[] expected, InspectorPanelSystem inspector) => Ready(inspector) &&
            ReferenceEquals(Root(inspector), entry.Asset) && Shape(entry) && expected.All(i => i.Matches() && i.Valid);

        private bool Restore(Entry entry, bool undo)
        {
            var inspector = Inspector;
            var expected = undo ? entry.After : entry.Before;
            var target = undo ? entry.Before : entry.After;
            if (BusyUI() || pending != null || !Validate(entry, expected, inspector) || !target.All(i => i.Valid))
            { status = "Recovery refused: current asset/data no longer matches history"; return false; }
            // Preflight complete materialization before any live writes.
            foreach (var image in target) foreach (var value in image.Values) value.Materialize();
            applying = true;
            try
            {
                foreach (var image in target) image.Write();
                updateParent.Invoke(inspector, new object[] { false });
                if (!target.All(i => i.Matches())) throw new InvalidOperationException("Restored prefab verification failed.");
                ((IList)sectionObjects.GetValue(inspector)).Clear();
                refresh.Invoke(inspector, null);
                status = (undo ? "Undid " : "Redid ") + entry.Label;
                return true;
            }
            catch (Exception error)
            {
                // Restore the original prefab payload. Deferred ECS work still requires live validation.
                try { foreach (var image in expected) image.Write(); updateParent.Invoke(inspector, new object[] { false }); }
                catch (Exception rollback) { Mod.Log.Error(rollback, "Recovery rollback failed. Reload a saved copy before saving."); }
                Mod.Log.Error(error, "Recovery failed; prefab rollback attempted. Check the asset before saving.");
                status = "Recovery failed — check asset before saving";
                enabledFeature = false; // Prevent subsequent writes after an uncertain regeneration result.
                return false;
            }
            finally { applying = false; }
        }

        private void Command(bool undo)
        {
            if (!CanAct) return;
            try { if (undo) history.Undo(e => Restore(e, true)); else history.Redo(e => Restore(e, false)); }
            catch (Exception error) { Clear("Recovery failed before mutation"); Mod.Log.Error(error, "Recovery aborted."); }
        }

        private void EnsureUI(InspectorPanelSystem inspector)
        {
            if (!settings.inspectorControls || !settings.recovery)
            {
                RemoveInspectorUI(); return;
            }
            if (!Ready(inspector) || Root(inspector) == null) return;
            if (!ReferenceEquals(sectionInspector, inspector)) { section = null; sectionInspector = inspector; }
            if (section == null)
            {
                var undo = new Button { path = "mwUndo", displayName = LocalizedString.Value("Undo last action"),
                    action = Undo, disabled = () => !CanUndo,
                    shouldTriggerValueChangedEvent = false };
                var redo = new Button { path = "mwRedo", displayName = LocalizedString.Value("Redo deletion"),
                    action = Redo, disabled = () => !CanRedo,
                    shouldTriggerValueChangedEvent = false };
                var clear = new Button { path = "mwClear", displayName = LocalizedString.Value("Forget last action"),
                    action = ClearHistory, shouldTriggerValueChangedEvent = false };
                section = new EditorSection { path = "mwRecovery", expanded = false,
                    displayName = LocalizedString.Value("MW Editor Tools — Undo"),
                    children = new IWidget[] { new ButtonRow { path = "mwHistoryActions", children = new[] { undo, clear } } } };
            }
            var children = (IList<IWidget>)panelChildren.GetValue(inspector);
            if (!children.Contains(section)) panelChildren.SetValue(inspector, new[] { section }.Cast<IWidget>().Concat(children).ToList());
            section.displayName = LocalizedString.Value("MW Undo — " + status);
            section.SetPropertiesChanged();
        }

        private void Update()
        {
            try
            {
                var inspector = Inspector;
                bulldozerTrace?.Tick();
                if (pending != null && Time.frameCount > pending.Frame + 2) Clear("No matching native change event — undo unavailable");
                CheckReplacementContext();
                if (replacementUndo==null && (!Ready(inspector) || !ReferenceEquals(Root(inspector), asset)))
                {
                    if (replacementUndo!=null || history.UndoCount > 0 || history.RedoCount > 0 || pending != null) Clear("Asset context changed — history cleared");
                    asset = Ready(inspector) ? Root(inspector) : null;
                }
                shortcut.Drain(Time.frameCount,enabledFeature && context.EditorAvailable && !Saving && !context.DialogBusy && ShortcutContextValid && shortcutAllowed?.Invoke()==true,
                    action=> {Mod.Log.Info("Shortcut dispatched from recovery update: "+(action==0?"Undo":"Redo"));if(action==0)Undo();else Redo();});
                EnsureUI(inspector);
            }
            catch (Exception error) { Clear("Recovery context unavailable"); Mod.Log.Error(error, "Recovery update failed."); }
        }
        private void Clear(string message) { if(replacementUndo!=null)Mod.Log.Info("Replacement Undo invalidated: "+message+" inspector="+(context?.Asset?.name??"none"));replacementUndo=null;shortcutReplacement=null;shortcut.Cancel();shortcutAllowed=null;shortcutAsset=null;history.Clear(); pending = null; weight = 0; status = message; }
        public void Release()
        {
            enabled = false; enabledFeature = false; Clear("Recovery stopped");
            try { bulldozerTrace?.Release(); } catch (Exception error) { Mod.Log.Warn("Bulldozer diagnostic cleanup failed: " + error.Message); }
            bulldozerTrace = null;
            harmony?.UnpatchAll(Id);
            RemoveInspectorUI();
            section = null;
            if (ReferenceEquals(active, this)) active = null;
        }
        private void OnDestroy() { Release(); }
        private void RemoveInspectorUI()
        {
            if (sectionInspector != null && section != null && world != null && world.IsCreated)
            { var children = (IList<IWidget>)panelChildren.GetValue(sectionInspector); if (children.Contains(section)) panelChildren.SetValue(sectionInspector, children.Where(c => !ReferenceEquals(c, section)).ToList()); }
        }
    }
}
