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
    [SettingsUIKeyboardAction("Undo", ActionType.Button)]
    [SettingsUIKeyboardAction("Redo", ActionType.Button)]
    [SettingsUIKeyboardAction("BrowserBuildings", ActionType.Button)]
    [SettingsUIKeyboardAction("BrowserProps", ActionType.Button)]
    [SettingsUIKeyboardAction("BrowserFoliage", ActionType.Button)]
    [SettingsUIKeyboardAction("BrowserAreas", ActionType.Button)]
    [SettingsUIKeyboardAction("BrowserEffects", ActionType.Button)]
    [SettingsUIKeyboardAction("BrowserSurfaces", ActionType.Button)]
    [SettingsUIGroupOrder("Recovery", "Browser")]
    [SettingsUIShowGroupName("Recovery", "Browser")]
    public sealed class EditorToolsOptions : ModSetting
    {
        private readonly EditorToolsSettings current;
        private readonly ListRecovery recovery;
        private readonly Action resetPosition;
        private readonly ProxyBinding undoTemplate, redoTemplate;
        private readonly ProxyBinding[] browserTemplates;
        internal const string Help = "Click a shortcut field and press the complete combination with a letter A–Z (for example Ctrl+Alt+T), then click Apply changes to activate and save. History limit: 20–50; changing it clears history. Defaults: Ctrl+Z Undo and Ctrl+Shift+Z Redo. Extra modifiers prevent activation; identical shortcuts are inactive. Shortcuts pause while typing or recording, during native dialogs or if focus reporting stops. Invalid or empty bindings retain the previous shortcut.";
        internal EditorToolsOptions(IMod mod, EditorToolsSettings settings, ListRecovery history, Action reset) : base(mod)
        { current = settings; recovery = history; resetPosition = reset; undoTemplate = UndoShortcut; redoTemplate = RedoShortcut; browserTemplates=new[]{BuildingsShortcut,PropsShortcut,FoliageShortcut,AreasShortcut,EffectsShortcut,SurfacesShortcut}; ReadCurrent(); }
        [SettingsUISection("Recovery")]
        public bool EnableShortcuts { get; set; }
        [SettingsUISection("Recovery")]
        public bool ConfirmDeletion { get; set; }
        [SettingsUISection("Recovery")]
        public bool EnableRecovery { get; set; }
        [SettingsUISection("Recovery")]
        public bool InspectorControls { get; set; }
        [SettingsUISection("Recovery"), SettingsUITextInput]
        public string HistoryLimit { get; set; }
        [SettingsUISection("Recovery"), SettingsUIKeyboardBinding(BindingKeyboard.Z, "Undo", ctrl: true)]
        public ProxyBinding UndoShortcut { get; set; }
        [SettingsUISection("Recovery"), SettingsUIKeyboardBinding(BindingKeyboard.Z, "Redo", ctrl: true, shift: true)]
        public ProxyBinding RedoShortcut { get; set; }
        [SettingsUISection("Recovery"), SettingsUIButton] public bool ApplyChanges { set { Commit(); } }
        [SettingsUISection("Recovery"), SettingsUIMultilineText]
        public string Guidance => Help;
        [SettingsUISection("Recovery"), SettingsUIButton]
        public bool ResetWindowPosition { set { resetPosition(); } }
        [SettingsUISection("Browser")] public bool EnableBrowserShortcuts { get; set; }
        [SettingsUISection("Browser"), SettingsUIKeyboardBinding(BindingKeyboard.B, "BrowserBuildings", ctrl: true, alt: true)]
        public ProxyBinding BuildingsShortcut { get; set; }
        [SettingsUISection("Browser"), SettingsUIKeyboardBinding(BindingKeyboard.P, "BrowserProps", ctrl: true, alt: true)]
        public ProxyBinding PropsShortcut { get; set; }
        [SettingsUISection("Browser"), SettingsUIKeyboardBinding(BindingKeyboard.F, "BrowserFoliage", ctrl: true, alt: true)]
        public ProxyBinding FoliageShortcut { get; set; }
        [SettingsUISection("Browser"), SettingsUIKeyboardBinding(BindingKeyboard.A, "BrowserAreas", ctrl: true, alt: true)]
        public ProxyBinding AreasShortcut { get; set; }
        [SettingsUISection("Browser"), SettingsUIKeyboardBinding(BindingKeyboard.E, "BrowserEffects", ctrl: true, alt: true)]
        public ProxyBinding EffectsShortcut { get; set; }
        [SettingsUISection("Browser"), SettingsUIKeyboardBinding(BindingKeyboard.S, "BrowserSurfaces", ctrl: true, alt: true)]
        public ProxyBinding SurfacesShortcut { get; set; }
        [SettingsUISection("Browser"), SettingsUIButton] public bool ApplyBrowserChanges { set { Commit(); } }
        [SettingsUISection("Browser"), SettingsUIMultilineText] public string BrowserGuidance => "Enable browser shortcuts, record a combination with a letter A–Z, then click Apply changes. Defaults use Ctrl+Alt plus B, P, F, A, E or S. Duplicate combinations with other enabled MW shortcuts are inactive. Existing browser search text is retained. Shortcuts pause while typing, recording, using dialogs, saving or recovering.";
        private void ReadCurrent()
        {
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
            current.browserShortcuts=EnableBrowserShortcuts;
            var browserBindings=new[]{BuildingsShortcut,PropsShortcut,FoliageShortcut,AreasShortcut,EffectsShortcut,SurfacesShortcut};
            for(int i=0;i<browserBindings.Length;i++) if(NativeShortcutCodec.TryRead(browserBindings[i],out key,out mask)) current.SetBrowserShortcut(i,key,mask);
            current.Save();
            if (clear) recovery?.ApplySettings();
            ReadCurrent();
            base.Apply();
        }
        public override void SetDefaults()
        { EnableBrowserShortcuts=false;
            if(browserTemplates!=null) {BuildingsShortcut=NativeShortcutCodec.Restore(browserTemplates[0],"B",5);PropsShortcut=NativeShortcutCodec.Restore(browserTemplates[1],"P",5);FoliageShortcut=NativeShortcutCodec.Restore(browserTemplates[2],"F",5);AreasShortcut=NativeShortcutCodec.Restore(browserTemplates[3],"A",5);EffectsShortcut=NativeShortcutCodec.Restore(browserTemplates[4],"E",5);SurfacesShortcut=NativeShortcutCodec.Restore(browserTemplates[5],"S",5);}
            EnableShortcuts = true; ConfirmDeletion=true;EnableRecovery=true;InspectorControls=true;HistoryLimit = "20";
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
                {options.GetOptionGroupLocaleID("Recovery"), "Editor tools"},
                {options.GetOptionLabelLocaleID(nameof(options.ConfirmDeletion)), "Confirm component removal and Clear lists"},
                {options.GetOptionDescLocaleID(nameof(options.ConfirmDeletion)), "Show a confirmation before removing a component or clearing its list. Click Apply changes to save."},
                {options.GetOptionLabelLocaleID(nameof(options.EnableRecovery)), "Enable supported Undo / Redo"},
                {options.GetOptionDescLocaleID(nameof(options.EnableRecovery)), "Record supported deletions. Changing this setting clears history. Click Apply changes to save."},
                {options.GetOptionLabelLocaleID(nameof(options.InspectorControls)), "Show recovery controls in the inspector"},
                {options.GetOptionDescLocaleID(nameof(options.InspectorControls)), "Show Undo and Redo in the asset inspector. Click Apply changes to save."},
                {options.GetOptionLabelLocaleID(nameof(options.EnableShortcuts)), "Enable Undo / Redo shortcuts"},
                {options.GetOptionDescLocaleID(nameof(options.EnableShortcuts)), "Enable shortcuts for supported Undo / Redo in the Asset Editor."},
                {options.GetOptionLabelLocaleID(nameof(options.HistoryLimit)), "History limit"},
                {options.GetOptionDescLocaleID(nameof(options.HistoryLimit)), "Enter 20–50. Changing this limit clears recovery history."},
                {options.GetOptionLabelLocaleID(nameof(options.UndoShortcut)), "Undo shortcut"},
                {options.GetOptionDescLocaleID(nameof(options.UndoShortcut)), "Click the binding and press the complete key combination. Click Apply changes to save."},
                {options.GetOptionLabelLocaleID(nameof(options.RedoShortcut)), "Redo shortcut"},
                {options.GetOptionDescLocaleID(nameof(options.RedoShortcut)), "Click the binding and press the complete key combination. Click Apply changes to save."},
                {options.GetOptionLabelLocaleID(nameof(options.Guidance)), EditorToolsOptions.Help},
                {options.GetOptionDescLocaleID(nameof(options.Guidance)), EditorToolsOptions.Help},
                {options.GetOptionLabelLocaleID(nameof(options.ResetWindowPosition)), "Reset window position"},
                {options.GetOptionDescLocaleID(nameof(options.ResetWindowPosition)), "Move the MW Editor Tools window back to its default position."}
            };
            entries[options.GetBindingKeyLocaleID("Undo")]="Undo shortcut";
            entries[options.GetBindingKeyLocaleID("Redo")]="Redo shortcut";
            entries[options.GetBindingMapLocaleID()]="MW Editor Tools";
            entries[options.GetOptionLabelLocaleID(nameof(options.ApplyChanges))] = "Apply changes";
            entries[options.GetOptionDescLocaleID(nameof(options.ApplyChanges))] = "Validate, activate and save the edited history and shortcut options.";
            entries[options.GetOptionGroupLocaleID("Browser")]="Browser category shortcuts";
            entries[options.GetOptionLabelLocaleID(nameof(options.EnableBrowserShortcuts))]="Enable browser category shortcuts";
            entries[options.GetOptionDescLocaleID(nameof(options.EnableBrowserShortcuts))]="Open the Asset Editor browser at the chosen category. Click Apply changes to save.";
            entries[options.GetOptionLabelLocaleID(nameof(options.BrowserGuidance))]=options.BrowserGuidance;
            entries[options.GetOptionDescLocaleID(nameof(options.BrowserGuidance))]=options.BrowserGuidance;
            entries[options.GetOptionLabelLocaleID(nameof(options.ApplyBrowserChanges))]="Apply changes";
            entries[options.GetOptionDescLocaleID(nameof(options.ApplyBrowserChanges))]="Activate and save all edited MW Editor Tools options.";
            entries[options.GetOptionLabelLocaleID(nameof(options.BuildingsShortcut))]="Buildings";
            entries[options.GetOptionDescLocaleID(nameof(options.BuildingsShortcut))]="Open the browser and select Buildings. Click Apply changes to save.";
            entries[options.GetBindingKeyLocaleID("BrowserBuildings")]="Browser: Buildings";
            entries[options.GetOptionLabelLocaleID(nameof(options.PropsShortcut))]="Props";
            entries[options.GetOptionDescLocaleID(nameof(options.PropsShortcut))]="Open the browser and select Props. Click Apply changes to save.";
            entries[options.GetBindingKeyLocaleID("BrowserProps")]="Browser: Props";
            entries[options.GetOptionLabelLocaleID(nameof(options.FoliageShortcut))]="Foliage";
            entries[options.GetOptionDescLocaleID(nameof(options.FoliageShortcut))]="Open the browser and select Foliage. Click Apply changes to save.";
            entries[options.GetBindingKeyLocaleID("BrowserFoliage")]="Browser: Foliage";
            entries[options.GetOptionLabelLocaleID(nameof(options.AreasShortcut))]="Areas";
            entries[options.GetOptionDescLocaleID(nameof(options.AreasShortcut))]="Open the browser and select Areas. Click Apply changes to save.";
            entries[options.GetBindingKeyLocaleID("BrowserAreas")]="Browser: Areas";
            entries[options.GetOptionLabelLocaleID(nameof(options.EffectsShortcut))]="Effects";
            entries[options.GetOptionDescLocaleID(nameof(options.EffectsShortcut))]="Open the browser and select Effects. Click Apply changes to save.";
            entries[options.GetBindingKeyLocaleID("BrowserEffects")]="Browser: Effects";
            entries[options.GetOptionLabelLocaleID(nameof(options.SurfacesShortcut))]="Surfaces";
            entries[options.GetOptionDescLocaleID(nameof(options.SurfacesShortcut))]="Open the browser and select Surfaces. Click Apply changes to save.";
            entries[options.GetBindingKeyLocaleID("BrowserSurfaces")]="Browser: Surfaces";
            return entries;
        }
        public void Unload() { }
    }
}

