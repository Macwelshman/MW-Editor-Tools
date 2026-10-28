using System;
using Colossal.Logging;
using Game;
using Game.Modding;
using UnityEngine;
using MW.EditorTools.Modules.DeletionProtection;

namespace MW.EditorTools
{
    public sealed class Mod : IMod
    {
        internal static readonly ILog Log = LogManager.GetLogger("MWEditorTools").SetShowsErrorsInUI(true);
        private GameObject host; private MW.EditorTools.UI.EditorToolsUISystem ui;
        private MW.EditorTools.Modules.MultiSave.MultiSaveSystem multiSave;
        private MW.EditorTools.Modules.ColourVariations.MWUISystem colours;
        private MW.EditorTools.Modules.ColourVariations.PaletteWindow colourWindow;
        private MW.EditorTools.Modules.ComponentManager.Systems.ComponentToolSystem componentTool;
        private MW.EditorTools.Modules.PropReplacer.PropReplacerSystem propReplacer;
        private MW.EditorTools.Modules.PropReplacer.SurfaceRenderSystem surfaceRenderer;
        private MW.EditorTools.Modules.Targeting.EditorTargetingTool targeting;
        private MW.EditorTools.Modules.Targeting.TargetingEditGuard targetingEditGuard;
        private MW.EditorTools.Modules.Targeting.OutlineColourSystem outlineColours;
        private MW.EditorTools.Settings.EditorToolsOptions options;
        private MW.EditorTools.Settings.OptionsLocale optionsLocale;
        // The game's ModInfo.Load uses GetUninitializedObject: field initializers
        // and constructors do not run. Initialize lifecycle state in OnLoad.
        private System.Collections.Generic.List<string> optionsLocales;
        private MW.EditorTools.Modules.EditorEnvironment.EditorEnvironmentSystem environment;

        public void OnLoad(UpdateSystem updateSystem)
        {
            optionsLocales = new System.Collections.Generic.List<string>();
            try
            {
                Log.Info("MW Editor Tools " + typeof(Mod).Assembly.GetName().Version + " loading.");
                host = new GameObject("MW Editor Tools — component confirmation");
                UnityEngine.Object.DontDestroyOnLoad(host);
                var settings = MW.EditorTools.Settings.EditorToolsSettings.Load();
                MW.EditorTools.Settings.EnvironmentSettings environmentSettings = null;
                try {
                    environmentSettings = MW.EditorTools.Settings.EnvironmentSettings.Load(this);
                    environment = updateSystem.World.GetOrCreateSystemManaged<MW.EditorTools.Modules.EditorEnvironment.EditorEnvironmentSystem>();
                    environment.Initialize(environmentSettings);
                    updateSystem.UpdateBefore<MW.EditorTools.Modules.EditorEnvironment.EditorEnvironmentSystem,Game.Simulation.PlanetarySystem>(SystemUpdatePhase.PreCulling);
                    Log.Info("MW Editor Tools: Environment preset integrated; saved settings apply on the next editor load.");
                } catch (Exception error) {
                    if(environment!=null)Cleanup(()=>environment.Stop(),"partial Environment");
                    environment=null;environmentSettings=null;
                    Log.Error(error,"Environment preset unavailable; other editor tools remain active.");
                }
                var context = new MW.EditorTools.Services.EditorContextService(updateSystem.World);
                try { host.AddComponent<MW.EditorTools.Modules.Compatibility.AssetIconCreatorCompatibility>().Initialize(); }
                catch (Exception error)
                {
                    Cleanup(() => host.GetComponent<MW.EditorTools.Modules.Compatibility.AssetIconCreatorCompatibility>()?.Release(), "partial Asset Icon Creator compatibility");
                    Log.Error(error, "Asset Icon Creator compatibility unavailable; other editor tools remain active. Automatic UI Object setup is unprotected.");
                }
                var protection = host.AddComponent<ComponentConfirmation>();
                try { protection.Initialize(settings); }
                catch (Exception error)
                {
                    Cleanup(() => protection.Release(), "partial component confirmation");
                    Log.Error(error, "Component confirmation unavailable; other editor tools remain active. Vanilla component removal is unprotected.");
                }
                try { host.AddComponent<ListRecovery>().Initialize(updateSystem.World, settings, context); }
                catch (Exception error)
                {
                    host.GetComponent<ListRecovery>()?.Release();
                    Log.Error(error, "List recovery is unavailable; other editor tools remain active.");
                }
                ui = updateSystem.World.GetOrCreateSystemManaged<MW.EditorTools.UI.EditorToolsUISystem>();
                ui.Initialize(settings, context, host.GetComponent<ListRecovery>(), protection);
                updateSystem.UpdateAt<MW.EditorTools.UI.EditorToolsUISystem>(SystemUpdatePhase.UIUpdate);
                try {
                    targeting=updateSystem.World.GetOrCreateSystemManaged<MW.EditorTools.Modules.Targeting.EditorTargetingTool>();
                    targeting.Initialize(context,host.GetComponent<ListRecovery>(),()=>ui?.ComponentCommandAllowed==true);
                    targetingEditGuard=new MW.EditorTools.Modules.Targeting.TargetingEditGuard(targeting);
                    updateSystem.UpdateAt<MW.EditorTools.Modules.Targeting.EditorTargetingTool>(SystemUpdatePhase.ToolUpdate);
                } catch(Exception error) {
                    Cleanup(()=>targetingEditGuard?.Dispose(),"partial targeting edit guard");targetingEditGuard=null;
                    Cleanup(()=>targeting?.Release(),"partial targeting");targeting=null;
                    Log.Error(error,"Filtered targeting unavailable; other editor modules retain their own availability checks.");
                }
                if(targeting!=null) try {
                    outlineColours=updateSystem.World.GetOrCreateSystemManaged<MW.EditorTools.Modules.Targeting.OutlineColourSystem>();
                    outlineColours.Initialize(targeting);
                    targeting.RestoreOutlineBeforeEdit=()=>outlineColours?.RestoreForInspectorEdit();
                    updateSystem.UpdateAfter<MW.EditorTools.Modules.Targeting.OutlineColourSystem,Game.Rendering.BatchDataSystem>(SystemUpdatePhase.Rendering);
                } catch(Exception error) {
                    Cleanup(()=>outlineColours?.Release(),"partial outline renderer");outlineColours=null;
                    targeting?.SetOutlineStatus("Custom outline colours unavailable in this game build; native highlighting remains active.");
                    Log.Error(error,"Custom outline colours unavailable; targeting and Undo remain active.");
                }
                try {
                    propReplacer=updateSystem.World.GetOrCreateSystemManaged<MW.EditorTools.Modules.PropReplacer.PropReplacerSystem>();
                    propReplacer.Initialize(context,()=>host?.GetComponent<ListRecovery>()?.ClearHistory(),()=>ui?.ClipboardShortcutAllowed==true);
                    propReplacer.CanRecordUndo=()=>host?.GetComponent<ListRecovery>()?.CanRecordReplacementUndo==true;
                    propReplacer.RecordUndo=action=>host.GetComponent<ListRecovery>().RecordReplacementUndo(action);
                    updateSystem.UpdateAt<MW.EditorTools.Modules.PropReplacer.PropReplacerSystem>(SystemUpdatePhase.UIUpdate);
                } catch(Exception error){if(propReplacer!=null)propReplacer.Enabled=false;propReplacer=null;Log.Error(error,"Prop Replacer unavailable; other editor tools remain active.");}
                if(propReplacer!=null)try {
                    surfaceRenderer=updateSystem.World.GetOrCreateSystemManaged<MW.EditorTools.Modules.PropReplacer.SurfaceRenderSystem>();
                    updateSystem.UpdateBefore<MW.EditorTools.Modules.PropReplacer.SurfaceRenderSystem,Game.Rendering.AreaBatchSystem>(SystemUpdatePhase.PreCulling);
                    surfaceRenderer.Initialize(propReplacer);
                }catch(Exception error){Cleanup(()=>surfaceRenderer?.Release(),"partial surface renderer");surfaceRenderer=null;Log.Error(error,"Surface replacement renderer unavailable; other editor tools remain active.");}
                try {
                    componentTool = updateSystem.World.GetOrCreateSystemManaged<MW.EditorTools.Modules.ComponentManager.Systems.ComponentToolSystem>();
                    componentTool.Initialize(()=>host?.GetComponent<ListRecovery>()?.ClearHistory(),()=>ui?.ComponentCommandAllowed==true);
                    componentTool.Enabled = true;
                    updateSystem.UpdateAt<MW.EditorTools.Modules.ComponentManager.Systems.ComponentToolSystem>(SystemUpdatePhase.UIUpdate);
                    Log.Info("MW Editor Tools: component transfer engine integrated in the shared Components tab.");
                } catch(Exception error) {
                    if(componentTool!=null) componentTool.Enabled=false;
                    componentTool=null;
                    Log.Error(error,"Component transfer unavailable; other editor tools remain active.");
                }
                try {
                    multiSave = updateSystem.World.GetOrCreateSystemManaged<MW.EditorTools.Modules.MultiSave.MultiSaveSystem>();
                    multiSave.BlockReason = () => context.DialogBusy ? "Close the open editor dialog before saving." :
                        host?.GetComponent<ListRecovery>()?.OperationBusy==true ? "Wait for the current recovery operation before saving." : null;
                    multiSave.Enabled = true;
                    host.GetComponent<ListRecovery>()?.SetSaveBusy(() => multiSave?.IsBusy==true);
                    updateSystem.UpdateAt<MW.EditorTools.Modules.MultiSave.MultiSaveSystem>(SystemUpdatePhase.UIUpdate);
                } catch(Exception error) {
                    if(multiSave!=null)multiSave.Enabled=false;
                    Cleanup(() => multiSave?.Release(), "partial Multi-Save");
                    multiSave=null;
                    Log.Error(error, "Multi-Save unavailable; other editor tools remain active.");
                }
                try {
                    colourWindow=host.AddComponent<MW.EditorTools.Modules.ColourVariations.PaletteWindow>();
                    colourWindow.BlockReason=()=>context.DialogBusy?"Close the open dialog before editing colours.":
                        multiSave?.IsBusy==true?"Wait for saving to finish before editing colours.":
                        host?.GetComponent<ListRecovery>()?.OperationBusy==true?"Wait for recovery to finish before editing colours.":null;
                    colourWindow.BeforeWrite=()=>host?.GetComponent<ListRecovery>()?.ClearHistory();
                    MW.EditorTools.Modules.ColourVariations.PaletteWindow.Instance=colourWindow;
                    colours=updateSystem.World.GetOrCreateSystemManaged<MW.EditorTools.Modules.ColourVariations.MWUISystem>();
                    colours.Enabled=true;
                    updateSystem.UpdateAt<MW.EditorTools.Modules.ColourVariations.MWUISystem>(SystemUpdatePhase.UIUpdate);
                    Log.Info("MW Editor Tools: Colours engine 0.5.11 integrated in shared tab.");
                } catch(Exception error) {
                    Cleanup(()=>colours?.Release(),"partial Colours");colours=null;
                    MW.EditorTools.Modules.ColourVariations.PaletteWindow.Instance=null;
                    if(colourWindow!=null){colourWindow.enabled=false;UnityEngine.Object.Destroy(colourWindow);}colourWindow=null;
                    Log.Error(error,"Colours unavailable; other editor tools remain active.");
                }
                try {
                    options = new MW.EditorTools.Settings.EditorToolsOptions(this, settings, host.GetComponent<ListRecovery>(), ui.ResetWindowPosition, environmentSettings);
                    options.RegisterKeyBindings();
                    // Registration supplies native recording widgets only. Recovery
                    // still dispatches through the existing guarded polling policy.
                    foreach (var action in options.GetActions()) action.shouldBeEnabled = false;
                    optionsLocale = new MW.EditorTools.Settings.OptionsLocale(options);
                    var localization = Game.SceneFlow.GameManager.instance.localizationManager;
                    foreach (var locale in localization.GetSupportedLocales()) { localization.AddSource(locale, optionsLocale); optionsLocales.Add(locale); }
                    options.RegisterInOptionsUI();
                    Log.Info("MW Editor Tools: Options registered.");
                } catch (Exception error) {
                    Log.Error(error, "MW Editor Tools: Options registration failed; editor tools remain active.");
                    ReleaseOptions();
                }
            }
            catch (Exception error)
            {
                Log.Error(error, "MW Editor Tools protection could not start; vanilla controls remain unprotected.");
                OnDispose();
            }
        }

        public void OnDispose()
        {
            ReleaseOptions();
            var oldEnvironment=environment;environment=null;
            Cleanup(()=>oldEnvironment?.Stop(),"Environment");
            var oldSurfaceRenderer=surfaceRenderer;surfaceRenderer=null;
            Cleanup(()=>oldSurfaceRenderer?.Release(),"surface renderer");
            var oldOutlines=outlineColours;outlineColours=null;
            Cleanup(()=>oldOutlines?.Release(),"outline renderer");
            Cleanup(()=>targetingEditGuard?.Dispose(),"targeting edit guard");targetingEditGuard=null;
            var oldTargeting=targeting;targeting=null;
            Cleanup(()=>oldTargeting?.Release(),"targeting");
            var oldColours=colours;colours=null;
            MW.EditorTools.Modules.ColourVariations.PaletteWindow.Instance=null;
            Cleanup(()=>oldColours?.Release(),"Colours UI");
            var oldColourWindow=colourWindow;colourWindow=null;
            Cleanup(()=>{if(oldColourWindow!=null){oldColourWindow.BlockReason=null;oldColourWindow.BeforeWrite=null;oldColourWindow.enabled=false;}},"Colours controller");
            var oldMultiSave = multiSave; multiSave = null;
            Cleanup(() => { if(oldMultiSave!=null){oldMultiSave.Enabled=false;oldMultiSave.BlockReason=null;oldMultiSave.Release();} }, "Multi-Save");
            var oldComponentTool=componentTool;componentTool=null;
            Cleanup(()=>{if(oldComponentTool!=null){oldComponentTool.Enabled=false;oldComponentTool.Release();}},"Component transfer");
            var oldPropReplacer=propReplacer;propReplacer=null;
            Cleanup(()=>oldPropReplacer?.Release(),"Prop Replacer");
            var oldUi = ui; ui = null;
            Cleanup(() => oldUi?.Release(), "shared UI");
            var oldHost = host; host = null;
            if (oldHost == null) return;
            Cleanup(() => oldHost.GetComponent<ListRecovery>()?.Release(), "list recovery");
            Cleanup(() => oldHost.GetComponent<ComponentConfirmation>()?.Release(), "component confirmation");
            Cleanup(() => oldHost.GetComponent<MW.EditorTools.Modules.Compatibility.AssetIconCreatorCompatibility>()?.Release(), "Asset Icon Creator compatibility");
            Cleanup(() => UnityEngine.Object.Destroy(oldHost), "host");
        }

        private void ReleaseOptions()
        {
            var oldOptions = options; options = null;
            if (oldOptions != null) Cleanup(() => { foreach (var action in oldOptions.GetActions()) action.shouldBeEnabled = false; }, "shortcut recording actions");
            Cleanup(() => oldOptions?.UnregisterInOptionsUI(), "Options registration");
            var localization = Game.SceneFlow.GameManager.instance?.localizationManager;
            var oldLocale = optionsLocale; optionsLocale = null;
            var oldLocales = optionsLocales; optionsLocales = null;
            if (localization != null && oldLocale != null && oldLocales != null)
                foreach (var locale in oldLocales) Cleanup(() => localization.RemoveSource(locale, oldLocale), "locale " + locale);
        }
        private static void Cleanup(Action action, string resource)
        {
            try { action(); }
            catch (Exception error) { Log.Error(error, "MW Editor Tools: cleanup failed for " + resource + "; remaining cleanup will continue."); }
        }
    }
}


