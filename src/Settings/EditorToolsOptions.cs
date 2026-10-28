using System;
using System.Collections.Generic;
using System.Globalization;
using Colossal;
using Game.Modding;
using Game.Settings;
using Game.Input;
using MW.EditorTools.Modules.DeletionProtection;

namespace MW.EditorTools.Settings
{
    [SettingsUIKeyboardAction("HideUI", ActionType.Button)]
    [SettingsUIKeyboardAction("Undo", ActionType.Button)]
    [SettingsUIKeyboardAction("Redo", ActionType.Button)]
    [SettingsUIKeyboardAction("BrowserBuildings", ActionType.Button)]
    [SettingsUIKeyboardAction("BrowserProps", ActionType.Button)]
    [SettingsUIKeyboardAction("BrowserFoliage", ActionType.Button)]
    [SettingsUIKeyboardAction("BrowserAreas", ActionType.Button)]
    [SettingsUIKeyboardAction("BrowserEffects", ActionType.Button)]
    [SettingsUIKeyboardAction("BrowserSurfaces", ActionType.Button)]
    [SettingsUITabOrder("KeyboardShortcuts", "Tools", "Environment")]
    [SettingsUIGroupOrder("Interface", "RecoveryShortcuts", "Browser", "Recovery", "Window", "EnvironmentToggle", "Preset")]
    [SettingsUIShowGroupName("Interface", "RecoveryShortcuts", "Browser", "Recovery", "Window", "Preset")]
    public sealed class EditorToolsOptions : ModSetting
    {
        private readonly EditorToolsSettings current;
        private readonly ListRecovery recovery;
        private readonly Action resetPosition;
        private readonly EnvironmentSettings environment;
        private readonly ProxyBinding undoTemplate, redoTemplate, hideUITemplate;
        private readonly ProxyBinding[] browserTemplates;
        internal const string Help = "Undo the last supported action with Ctrl+Z. Apply saves edited options.";
        internal EditorToolsOptions(IMod mod, EditorToolsSettings settings, ListRecovery history, Action reset, EnvironmentSettings preset = null) : base(mod)
        { current = settings; recovery = history; resetPosition = reset; environment = preset; hideUITemplate = HideUIShortcut.WithModifiers(UndoShortcut.modifiers); undoTemplate = UndoShortcut; redoTemplate = RedoShortcut; browserTemplates=new[]{BuildingsShortcut,PropsShortcut,FoliageShortcut,AreasShortcut,EffectsShortcut,SurfacesShortcut}; ReadCurrent(); }
        [SettingsUISection("KeyboardShortcuts", "Interface")] public bool EnableHideUIShortcut { get; set; }
        [SettingsUISection("KeyboardShortcuts", "Interface"), SettingsUIKeyboardBinding(BindingKeyboard.Backquote, "HideUI")]
        public ProxyBinding HideUIShortcut { get; set; }
        [SettingsUISection("KeyboardShortcuts", "RecoveryShortcuts")]
        public bool EnableShortcuts { get; set; }
        [SettingsUISection("Tools", "Recovery")]
        public bool ConfirmDeletion { get; set; }
        [SettingsUISection("Tools", "Recovery")]
        public bool EnableRecovery { get; set; }
        [SettingsUISection("Tools", "Recovery")]
        public bool InspectorControls { get; set; }
        [SettingsUIHidden]
        public string HistoryLimit { get; set; }
        [SettingsUISection("KeyboardShortcuts", "RecoveryShortcuts"), SettingsUIKeyboardBinding(BindingKeyboard.Z, "Undo", ctrl: true)]
        public ProxyBinding UndoShortcut { get; set; }
        [SettingsUIHidden, SettingsUIKeyboardBinding(BindingKeyboard.Z, "Redo", ctrl: true, shift: true)]
        public ProxyBinding RedoShortcut { get; set; }
        [SettingsUISection("Tools", "Window"), SettingsUIButton] public bool ApplyChanges { set { Commit(); } }
        [SettingsUIHidden]
        public string Guidance => Help;
        [SettingsUISection("Tools", "Window"), SettingsUIButton]
        public bool ResetWindowPosition { set { resetPosition(); } }
        [SettingsUISection("KeyboardShortcuts", "Browser")] public bool EnableBrowserShortcuts { get; set; }
        [SettingsUISection("KeyboardShortcuts", "Browser"), SettingsUIKeyboardBinding(BindingKeyboard.B, "BrowserBuildings", alt: true)]
        public ProxyBinding BuildingsShortcut { get; set; }
        [SettingsUISection("KeyboardShortcuts", "Browser"), SettingsUIKeyboardBinding(BindingKeyboard.D, "BrowserProps", alt: true)]
        public ProxyBinding PropsShortcut { get; set; }
        [SettingsUISection("KeyboardShortcuts", "Browser"), SettingsUIKeyboardBinding(BindingKeyboard.F, "BrowserFoliage", alt: true)]
        public ProxyBinding FoliageShortcut { get; set; }
        [SettingsUISection("KeyboardShortcuts", "Browser"), SettingsUIKeyboardBinding(BindingKeyboard.A, "BrowserAreas", alt: true)]
        public ProxyBinding AreasShortcut { get; set; }
        [SettingsUISection("KeyboardShortcuts", "Browser"), SettingsUIKeyboardBinding(BindingKeyboard.E, "BrowserEffects", alt: true)]
        public ProxyBinding EffectsShortcut { get; set; }
        [SettingsUISection("KeyboardShortcuts", "Browser"), SettingsUIKeyboardBinding(BindingKeyboard.S, "BrowserSurfaces", alt: true)]
        public ProxyBinding SurfacesShortcut { get; set; }
        [SettingsUISection("KeyboardShortcuts", "Browser"), SettingsUIButton] public bool ApplyBrowserChanges { set { Commit(); } }
        [SettingsUIHidden] public string BrowserGuidance => "Alt+B Buildings · Alt+D Props · Alt+F Foliage · Alt+A Areas · Alt+E Effects · Alt+S Surfaces.";
        [SettingsUIHidden] public bool EnvironmentUnavailable => environment == null;
        [SettingsUIHidden] public bool EnvironmentControlsDisabled => EnvironmentUnavailable || !EnableEnvironment;
        [SettingsUISection("Environment", "EnvironmentToggle"), SettingsUIDisableByCondition(typeof(EditorToolsOptions), nameof(EnvironmentUnavailable))]
        public bool EnableEnvironment { get; set; }
        [SettingsUISection("Environment", "Preset"), SettingsUIMultilineText]
        public string EnvironmentGuidance => environment == null ? "Environment is unavailable. Check the mod log and disable standalone MW Editor Environment before reloading." :
            "Apply changes saves settings from all tabs. Environment changes take effect on the next editor load.";
        [SettingsUISection("Environment", "Preset"), SettingsUISlider(min = 0f, max = 23.75f, step = 0.25f, unit = "floatSingleFraction"), SettingsUIDisableByCondition(typeof(EditorToolsOptions), nameof(EnvironmentControlsDisabled))]
        public float EnvironmentHour { get; set; }
        [SettingsUISection("Environment", "Preset"), SettingsUISlider(min = 1, max = 365, step = 1), SettingsUIDisableByCondition(typeof(EditorToolsOptions), nameof(EnvironmentControlsDisabled))]
        public int EnvironmentDayOfYear { get; set; }
        [SettingsUISection("Environment", "Preset"), SettingsUISlider(min = 0, max = 100, step = 1), SettingsUIDisableByCondition(typeof(EditorToolsOptions), nameof(EnvironmentControlsDisabled))]
        public int EnvironmentPrecipitation { get; set; }
        [SettingsUISection("Environment", "Preset"), SettingsUISlider(min = 0, max = 100, step = 1), SettingsUIDisableByCondition(typeof(EditorToolsOptions), nameof(EnvironmentControlsDisabled))]
        public int EnvironmentCloudCoverage { get; set; }
        [SettingsUISection("Environment", "Preset"), SettingsUIDisableByCondition(typeof(EditorToolsOptions), nameof(EnvironmentControlsDisabled))]
        public bool EnvironmentSnow { get; set; }
        [SettingsUISection("Environment", "Preset"), SettingsUIButton]
        public bool ApplyEnvironmentChanges { set { Commit(); } }
        private void ReadCurrent()
        {
            EnableHideUIShortcut=current.hideUIShortcut;
            HideUIShortcut=NativeShortcutCodec.Restore(hideUITemplate,current.hideUIKey,current.hideUIModifiers);
            EnableBrowserShortcuts=current.browserShortcuts;
            BuildingsShortcut=NativeShortcutCodec.Restore(browserTemplates[0],current.BrowserKey(0),current.BrowserModifiers(0));
            PropsShortcut=NativeShortcutCodec.Restore(browserTemplates[1],current.BrowserKey(1),current.BrowserModifiers(1));
            FoliageShortcut=NativeShortcutCodec.Restore(browserTemplates[2],current.BrowserKey(2),current.BrowserModifiers(2));
            AreasShortcut=NativeShortcutCodec.Restore(browserTemplates[3],current.BrowserKey(3),current.BrowserModifiers(3));
            EffectsShortcut=NativeShortcutCodec.Restore(browserTemplates[4],current.BrowserKey(4),current.BrowserModifiers(4));
            SurfacesShortcut=NativeShortcutCodec.Restore(browserTemplates[5],current.BrowserKey(5),current.BrowserModifiers(5));
            EnableShortcuts = current.shortcuts;
            ConfirmDeletion=current.confirmation;EnableRecovery=current.recovery;InspectorControls=current.inspectorControls;
            HistoryLimit = current.historyDepth.ToString(CultureInfo.InvariantCulture);
            UndoShortcut = NativeShortcutCodec.Restore(undoTemplate,current.undoKey,current.undoModifiers);
            RedoShortcut = NativeShortcutCodec.Restore(redoTemplate,current.redoKey,current.redoModifiers);
            EnableEnvironment=environment?.EnableEnvironment ?? true;
            EnvironmentHour=environment?.Hour ?? EnvironmentSettings.DefaultHour;
            EnvironmentDayOfYear=environment?.DayOfYear ?? EnvironmentSettings.DefaultDayOfYear;
            EnvironmentPrecipitation=environment?.Precipitation ?? EnvironmentSettings.DefaultPrecipitation;
            EnvironmentCloudCoverage=environment?.CloudCoverage ?? EnvironmentSettings.DefaultCloudCoverage;
            EnvironmentSnow=environment?.Snow ?? false;
        }
        public override void Apply()
        { base.Apply(); } // Native text widgets apply on each keystroke: preserve draft values.
        public override void ApplyAndSave() => Apply();
        private void Commit()
        {
            bool clear = current.ApplyOptions(EnableShortcuts, HistoryLimit, current.undoKey, current.redoKey);
            clear |= current.recovery!=EnableRecovery;
            current.confirmation=ConfirmDeletion;current.recovery=EnableRecovery;current.inspectorControls=InspectorControls;
            string key; int mask;
            if (NativeShortcutCodec.TryRead(UndoShortcut,out key,out mask)) { current.undoKey=key; current.undoModifiers=mask; }
            if (NativeShortcutCodec.TryRead(RedoShortcut,out key,out mask)) { current.redoKey=key; current.redoModifiers=mask; }
            current.hideUIShortcut=EnableHideUIShortcut;
            if(NativeShortcutCodec.TryRead(HideUIShortcut,out key,out mask,true)){current.hideUIKey=key;current.hideUIModifiers=mask;}
            current.browserShortcuts=EnableBrowserShortcuts;
            var browserBindings=new[]{BuildingsShortcut,PropsShortcut,FoliageShortcut,AreasShortcut,EffectsShortcut,SurfacesShortcut};
            for(int i=0;i<browserBindings.Length;i++) if(NativeShortcutCodec.TryRead(browserBindings[i],out key,out mask)) current.SetBrowserShortcut(i,key,mask);
            current.Save();
            if(environment!=null) {
                environment.EnableEnvironment=EnableEnvironment;
                environment.Hour=EnvironmentHour;environment.DayOfYear=EnvironmentDayOfYear;
                environment.Precipitation=EnvironmentPrecipitation;environment.CloudCoverage=EnvironmentCloudCoverage;environment.Snow=EnvironmentSnow;
                environment.Normalize();environment.ApplyAndSave();
            }
            if (clear) recovery?.ApplySettings();
            ReadCurrent();
            base.Apply();
        }
        public override void SetDefaults()
        { EnableBrowserShortcuts=false;EnableHideUIShortcut=true;
            if(current!=null)HideUIShortcut=NativeShortcutCodec.Restore(hideUITemplate,"BACKQUOTE",0);
            if(browserTemplates!=null) {BuildingsShortcut=NativeShortcutCodec.Restore(browserTemplates[0],"B",4);PropsShortcut=NativeShortcutCodec.Restore(browserTemplates[1],"D",4);FoliageShortcut=NativeShortcutCodec.Restore(browserTemplates[2],"F",4);AreasShortcut=NativeShortcutCodec.Restore(browserTemplates[3],"A",4);EffectsShortcut=NativeShortcutCodec.Restore(browserTemplates[4],"E",4);SurfacesShortcut=NativeShortcutCodec.Restore(browserTemplates[5],"S",4);}
            EnableShortcuts = true; ConfirmDeletion=true;EnableRecovery=true;InspectorControls=true;HistoryLimit = "20";
            EnableEnvironment=true;EnvironmentHour=EnvironmentSettings.DefaultHour;EnvironmentDayOfYear=EnvironmentSettings.DefaultDayOfYear;
            EnvironmentPrecipitation=EnvironmentSettings.DefaultPrecipitation;EnvironmentCloudCoverage=EnvironmentSettings.DefaultCloudCoverage;EnvironmentSnow=false;
            if (current != null) { UndoShortcut=NativeShortcutCodec.Restore(undoTemplate,"Z",1); RedoShortcut=NativeShortcutCodec.Restore(redoTemplate,"Z",3); } }
    }
    internal sealed class OptionsLocale : IDictionarySource
    {
        private readonly EditorToolsOptions options;
        internal OptionsLocale(EditorToolsOptions value) { options = value; }
        public IEnumerable<KeyValuePair<string,string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string,int> counts)
        {
            var entries = new Dictionary<string,string> {
                {options.GetSettingsLocaleID(), "MW Editor Tools"},
                {options.GetOptionTabLocaleID("KeyboardShortcuts"), "Keyboard Shortcuts"},
                {options.GetOptionTabLocaleID("Tools"), "Tools"},
                {options.GetOptionTabLocaleID("Environment"), "Environment"},
                {options.GetOptionGroupLocaleID("Interface"), "Interface"},
                {options.GetOptionGroupLocaleID("RecoveryShortcuts"), "Undo shortcut"},
                {options.GetOptionGroupLocaleID("Window"), "Window and settings"},
                {options.GetOptionGroupLocaleID("Preset"), "Editor environment preset"},
                {options.GetOptionGroupLocaleID("Recovery"), "Editor tools"},
                {options.GetOptionLabelLocaleID(nameof(options.ConfirmDeletion)), "Confirm deletions"},
                {options.GetOptionDescLocaleID(nameof(options.ConfirmDeletion)), "Confirm component removal and clearing lists."},
                {options.GetOptionLabelLocaleID(nameof(options.EnableRecovery)), "Enable Undo"},
                {options.GetOptionDescLocaleID(nameof(options.EnableRecovery)), "Capture supported moves, node edits and deletions. Changing this clears Undo."},
                {options.GetOptionLabelLocaleID(nameof(options.InspectorControls)), "Show inspector Undo"},
                {options.GetOptionDescLocaleID(nameof(options.InspectorControls)), "Show the Undo button in Object Info."},
                {options.GetOptionLabelLocaleID(nameof(options.EnableShortcuts)), "Enable Undo shortcut"},
                {options.GetOptionDescLocaleID(nameof(options.EnableShortcuts)), "Use the configured Undo shortcut in the Asset Editor."},
                {options.GetOptionLabelLocaleID(nameof(options.HistoryLimit)), "History limit"},
                {options.GetOptionDescLocaleID(nameof(options.HistoryLimit)), "Enter 20–50. Changing this limit clears recovery history."},
                {options.GetOptionLabelLocaleID(nameof(options.UndoShortcut)), "Undo shortcut"},
                {options.GetOptionDescLocaleID(nameof(options.UndoShortcut)), "Click and press a key combination, then Apply."},
                {options.GetOptionLabelLocaleID(nameof(options.RedoShortcut)), "Redo shortcut"},
                {options.GetOptionDescLocaleID(nameof(options.RedoShortcut)), "Click and press a key combination, then Apply."},
                {options.GetOptionLabelLocaleID(nameof(options.Guidance)), EditorToolsOptions.Help},
                {options.GetOptionDescLocaleID(nameof(options.Guidance)), EditorToolsOptions.Help},
                {options.GetOptionLabelLocaleID(nameof(options.ResetWindowPosition)), "Reset window position"},
                {options.GetOptionDescLocaleID(nameof(options.ResetWindowPosition)), "Move the MW Editor Tools window back to its default position."}
            };
            entries[options.GetOptionLabelLocaleID(nameof(options.EnableHideUIShortcut))]="Enable hide UI shortcut";
            entries[options.GetOptionDescLocaleID(nameof(options.EnableHideUIShortcut))]="Toggle the native free-camera screen in the Asset Editor.";
            entries[options.GetOptionLabelLocaleID(nameof(options.HideUIShortcut))]="Hide / show UI shortcut";
            entries[options.GetOptionDescLocaleID(nameof(options.HideUIShortcut))]="Default Backquote (`). Press again or Escape to restore the UI. Press a single key or a key combination, then Apply. Conflicting MW shortcuts are suppressed.";
            entries[options.GetBindingKeyLocaleID("HideUI")]="Hide / show UI";
            entries[options.GetBindingKeyLocaleID("Undo")]="Undo shortcut";
            entries[options.GetBindingKeyLocaleID("Redo")]="Redo shortcut";
            entries[options.GetBindingMapLocaleID()]="MW Editor Tools";
            entries[options.GetOptionLabelLocaleID(nameof(options.ApplyChanges))] = "Apply changes";
            entries[options.GetOptionDescLocaleID(nameof(options.ApplyChanges))] = "Save edited options.";
            entries[options.GetOptionLabelLocaleID(nameof(options.EnvironmentGuidance))]=options.EnvironmentGuidance;
            entries[options.GetOptionDescLocaleID(nameof(options.EnvironmentGuidance))]=options.EnvironmentGuidance;
            entries[options.GetOptionLabelLocaleID(nameof(options.EnableEnvironment))]="Enable environment";
            entries[options.GetOptionDescLocaleID(nameof(options.EnableEnvironment))]="Apply the saved environment preset on the next editor load. When disabled, use the native editor environment. Click Apply changes to save.";
            entries[options.GetOptionLabelLocaleID(nameof(options.EnvironmentHour))]="Time of day (hours)";
            entries[options.GetOptionDescLocaleID(nameof(options.EnvironmentHour))]="Editor lighting time in 15-minute steps (12 = noon; 12.5 = 12:30). Applies on the next editor load.";
            entries[options.GetOptionLabelLocaleID(nameof(options.EnvironmentDayOfYear))]="Time of year (day 1–365)";
            entries[options.GetOptionDescLocaleID(nameof(options.EnvironmentDayOfYear))]="1 = Jan 1; 91 = Apr 1; 172 = Jun 21; 274 = Oct 1. Seasons depend on the map climate and hemisphere. Applies on the next editor load.";
            entries[options.GetOptionLabelLocaleID(nameof(options.EnvironmentPrecipitation))]="Precipitation (%)";
            entries[options.GetOptionDescLocaleID(nameof(options.EnvironmentPrecipitation))]="Rain or snow intensity from 0 to 100%. Applies on the next editor load.";
            entries[options.GetOptionLabelLocaleID(nameof(options.EnvironmentCloudCoverage))]="Cloud coverage (%)";
            entries[options.GetOptionDescLocaleID(nameof(options.EnvironmentCloudCoverage))]="Cloud coverage from 0 to 100%. Applies on the next editor load.";
            entries[options.GetOptionLabelLocaleID(nameof(options.EnvironmentSnow))]="Snow";
            entries[options.GetOptionDescLocaleID(nameof(options.EnvironmentSnow))]="Creates ground snow on the next editor load, including while paused, and holds temperature below freezing. Precipitation controls falling snow. Enable snow visuals in the game's gameplay settings. Coverage can be saved with the map.";
            entries[options.GetOptionLabelLocaleID(nameof(options.ApplyEnvironmentChanges))]="Apply changes";
            entries[options.GetOptionDescLocaleID(nameof(options.ApplyEnvironmentChanges))]="Save edited options from all tabs; environment settings apply on the next editor load.";
            entries[options.GetOptionGroupLocaleID("Browser")]="Browser shortcuts";
            entries[options.GetOptionLabelLocaleID(nameof(options.EnableBrowserShortcuts))]="Enable browser shortcuts";
            entries[options.GetOptionDescLocaleID(nameof(options.EnableBrowserShortcuts))]="Open browser categories using the shortcuts below.";
            entries[options.GetOptionLabelLocaleID(nameof(options.BrowserGuidance))]=options.BrowserGuidance;
            entries[options.GetOptionDescLocaleID(nameof(options.BrowserGuidance))]=options.BrowserGuidance;
            entries[options.GetOptionLabelLocaleID(nameof(options.ApplyBrowserChanges))]="Apply changes";
            entries[options.GetOptionDescLocaleID(nameof(options.ApplyBrowserChanges))]="Save edited options.";
            entries[options.GetOptionLabelLocaleID(nameof(options.BuildingsShortcut))]="Buildings";
            entries[options.GetOptionDescLocaleID(nameof(options.BuildingsShortcut))]="Open Buildings in the browser.";
            entries[options.GetBindingKeyLocaleID("BrowserBuildings")]="Browser: Buildings";
            entries[options.GetOptionLabelLocaleID(nameof(options.PropsShortcut))]="Props";
            entries[options.GetOptionDescLocaleID(nameof(options.PropsShortcut))]="Open Props in the browser.";
            entries[options.GetBindingKeyLocaleID("BrowserProps")]="Browser: Props";
            entries[options.GetOptionLabelLocaleID(nameof(options.FoliageShortcut))]="Foliage";
            entries[options.GetOptionDescLocaleID(nameof(options.FoliageShortcut))]="Open Foliage in the browser.";
            entries[options.GetBindingKeyLocaleID("BrowserFoliage")]="Browser: Foliage";
            entries[options.GetOptionLabelLocaleID(nameof(options.AreasShortcut))]="Areas";
            entries[options.GetOptionDescLocaleID(nameof(options.AreasShortcut))]="Open Areas in the browser.";
            entries[options.GetBindingKeyLocaleID("BrowserAreas")]="Browser: Areas";
            entries[options.GetOptionLabelLocaleID(nameof(options.EffectsShortcut))]="Effects";
            entries[options.GetOptionDescLocaleID(nameof(options.EffectsShortcut))]="Open Effects in the browser.";
            entries[options.GetBindingKeyLocaleID("BrowserEffects")]="Browser: Effects";
            entries[options.GetOptionLabelLocaleID(nameof(options.SurfacesShortcut))]="Surfaces";
            entries[options.GetOptionDescLocaleID(nameof(options.SurfacesShortcut))]="Open Surfaces in the browser.";
            entries[options.GetBindingKeyLocaleID("BrowserSurfaces")]="Browser: Surfaces";
            return entries;
        }
        public void Unload() { }
    }
}

