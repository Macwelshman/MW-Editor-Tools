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
    [SettingsUIGroupOrder("Recovery", "Browser")]
    [SettingsUIShowGroupName("Recovery", "Browser")]
    public sealed class EditorToolsOptions : ModSetting
    {
        private readonly EditorToolsSettings current;
        private readonly ListRecovery recovery;
        private readonly Action resetPosition;
        private readonly ProxyBinding undoTemplate, redoTemplate, hideUITemplate;
        private readonly ProxyBinding[] browserTemplates;
        internal const string Help = "Undo the last supported action with Ctrl+Z. Apply saves edited options.";
        internal EditorToolsOptions(IMod mod, EditorToolsSettings settings, ListRecovery history, Action reset) : base(mod)
        { current = settings; recovery = history; resetPosition = reset; hideUITemplate = HideUIShortcut.WithModifiers(UndoShortcut.modifiers); undoTemplate = UndoShortcut; redoTemplate = RedoShortcut; browserTemplates=new[]{BuildingsShortcut,PropsShortcut,FoliageShortcut,AreasShortcut,EffectsShortcut,SurfacesShortcut}; ReadCurrent(); }
        [SettingsUISection("Recovery")] public bool EnableHideUIShortcut { get; set; }
        [SettingsUISection("Recovery"), SettingsUIKeyboardBinding(BindingKeyboard.Backquote, "HideUI")]
        public ProxyBinding HideUIShortcut { get; set; }
        [SettingsUISection("Recovery")]
        public bool EnableShortcuts { get; set; }
        [SettingsUISection("Recovery")]
        public bool ConfirmDeletion { get; set; }
        [SettingsUISection("Recovery")]
        public bool EnableRecovery { get; set; }
        [SettingsUISection("Recovery")]
        public bool InspectorControls { get; set; }
        [SettingsUIHidden]
        public string HistoryLimit { get; set; }
        [SettingsUISection("Recovery"), SettingsUIKeyboardBinding(BindingKeyboard.Z, "Undo", ctrl: true)]
        public ProxyBinding UndoShortcut { get; set; }
        [SettingsUIHidden, SettingsUIKeyboardBinding(BindingKeyboard.Z, "Redo", ctrl: true, shift: true)]
        public ProxyBinding RedoShortcut { get; set; }
        [SettingsUISection("Recovery"), SettingsUIButton] public bool ApplyChanges { set { Commit(); } }
        [SettingsUIHidden]
        public string Guidance => Help;
        [SettingsUISection("Recovery"), SettingsUIButton]
        public bool ResetWindowPosition { set { resetPosition(); } }
        [SettingsUISection("Browser")] public bool EnableBrowserShortcuts { get; set; }
        [SettingsUISection("Browser"), SettingsUIKeyboardBinding(BindingKeyboard.B, "BrowserBuildings", alt: true)]
        public ProxyBinding BuildingsShortcut { get; set; }
        [SettingsUISection("Browser"), SettingsUIKeyboardBinding(BindingKeyboard.D, "BrowserProps", alt: true)]
        public ProxyBinding PropsShortcut { get; set; }
        [SettingsUISection("Browser"), SettingsUIKeyboardBinding(BindingKeyboard.F, "BrowserFoliage", alt: true)]
        public ProxyBinding FoliageShortcut { get; set; }
        [SettingsUISection("Browser"), SettingsUIKeyboardBinding(BindingKeyboard.A, "BrowserAreas", alt: true)]
        public ProxyBinding AreasShortcut { get; set; }
        [SettingsUISection("Browser"), SettingsUIKeyboardBinding(BindingKeyboard.E, "BrowserEffects", alt: true)]
        public ProxyBinding EffectsShortcut { get; set; }
        [SettingsUISection("Browser"), SettingsUIKeyboardBinding(BindingKeyboard.S, "BrowserSurfaces", alt: true)]
        public ProxyBinding SurfacesShortcut { get; set; }
        [SettingsUISection("Browser"), SettingsUIButton] public bool ApplyBrowserChanges { set { Commit(); } }
        [SettingsUIHidden] public string BrowserGuidance => "Alt+B Buildings · Alt+D Props · Alt+F Foliage · Alt+A Areas · Alt+E Effects · Alt+S Surfaces.";
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
            if (clear) recovery?.ApplySettings();
            ReadCurrent();
            base.Apply();
        }
        public override void SetDefaults()
        { EnableBrowserShortcuts=false;EnableHideUIShortcut=true;
            if(current!=null)HideUIShortcut=NativeShortcutCodec.Restore(hideUITemplate,"BACKQUOTE",0);
            if(browserTemplates!=null) {BuildingsShortcut=NativeShortcutCodec.Restore(browserTemplates[0],"B",4);PropsShortcut=NativeShortcutCodec.Restore(browserTemplates[1],"D",4);FoliageShortcut=NativeShortcutCodec.Restore(browserTemplates[2],"F",4);AreasShortcut=NativeShortcutCodec.Restore(browserTemplates[3],"A",4);EffectsShortcut=NativeShortcutCodec.Restore(browserTemplates[4],"E",4);SurfacesShortcut=NativeShortcutCodec.Restore(browserTemplates[5],"S",4);}
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

