using System;
using System.IO;
using System.Globalization;
using UnityEngine;

namespace MW.EditorTools.Settings
{
    [Serializable]
    public sealed class EditorToolsSettings
    {
        public bool confirmation = true, recovery = true, shortcuts = true;
        public bool inspectorControls = true;
        public int historyDepth = 20;
        public string undoKey = "Z", redoKey = "Z";
        public int undoModifiers = 1, redoModifiers = 3;
        public bool browserShortcuts;
        public string buildingsKey="B", propsKey="P", foliageKey="F", areasKey="A", effectsKey="E", surfacesKey="S";
        public int buildingsModifiers=5, propsModifiers=5, foliageModifiers=5, areasModifiers=5, effectsModifiers=5, surfacesModifiers=5;
        internal static readonly string[] BrowserCategories={"Buildings","Props","Foliage","Areas","Effects","Surfaces"};
        internal string BrowserKey(int i)=>i switch {0=>buildingsKey,1=>propsKey,2=>foliageKey,3=>areasKey,4=>effectsKey,5=>surfacesKey,_=>throw new ArgumentOutOfRangeException(nameof(i))};
        internal int BrowserModifiers(int i)=>i switch {0=>buildingsModifiers,1=>propsModifiers,2=>foliageModifiers,3=>areasModifiers,4=>effectsModifiers,5=>surfacesModifiers,_=>throw new ArgumentOutOfRangeException(nameof(i))};
        internal bool BrowserConflict(string key,int modifiers,int self=-1) {
            if(self>=0 && shortcuts && ((undoKey==key&&undoModifiers==modifiers)||(redoKey==key&&redoModifiers==modifiers)))return true;
            if(browserShortcuts)for(int i=0;i<BrowserCategories.Length;i++)
                if(i!=self&&BrowserKey(i)==key&&BrowserModifiers(i)==modifiers)return true;
            return false;
        }
        internal void SetBrowserShortcut(int i,string key,int mask) {
            switch(i) {
                case 0: buildingsKey=key;buildingsModifiers=mask;break;
                case 1: propsKey=key;propsModifiers=mask;break;
                case 2: foliageKey=key;foliageModifiers=mask;break;
                case 3: areasKey=key;areasModifiers=mask;break;
                case 4: effectsKey=key;effectsModifiers=mask;break;
                case 5: surfacesKey=key;surfacesModifiers=mask;break;
            }
        }
        public float windowX = .4f, windowY = .18f;

        internal static string FilePath => Path.Combine(Application.persistentDataPath, "MWEditorTools-settings.json");
        internal static EditorToolsSettings Load()
        {
            try
            {
                var json = File.Exists(FilePath) ? File.ReadAllText(FilePath) : null;
                var settings = json != null ? JsonUtility.FromJson<EditorToolsSettings>(json) : new EditorToolsSettings();
                if (settings != null && json != null) settings.MigrateModifiers(json);
                settings = settings ?? new EditorToolsSettings(); settings.Normalize(); return settings;
            }
            catch (Exception error) { Mod.Log.Warn("Settings could not be loaded: " + error.Message); return new EditorToolsSettings(); }
        }
        public void Normalize()
        {
            historyDepth = Math.Max(20, Math.Min(50, historyDepth));
            undoKey = NormalizeKey(undoKey); redoKey = NormalizeKey(redoKey);
            for(int i=0;i<BrowserCategories.Length;i++) {
                string key;var mask=BrowserModifiers(i);
                SetBrowserShortcut(i,TryLetter(BrowserKey(i),out key)?key:BrowserCategories[i].Substring(0,1),mask>=0&&mask<=15?mask:5);
            }
            if (undoModifiers < 0 || undoModifiers > 15) undoModifiers = 1;
            if (redoModifiers < 0 || redoModifiers > 15) redoModifiers = 3;
            windowX = Position(windowX, .4f); windowY = Position(windowY, .18f);
        }
        internal static string NormalizeKey(string key) => !string.IsNullOrEmpty(key) && key.Length == 1 &&
            char.ToUpperInvariant(key[0]) >= 'A' && char.ToUpperInvariant(key[0]) <= 'Z' ? key.ToUpperInvariant() : "Z";
        private static float Position(float value, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(0, Math.Min(1, value));
        internal void Save()
        {
            Normalize();
            try
            {
                var temporary = FilePath + ".tmp";
                File.WriteAllText(temporary, ToJson());
                if (File.Exists(FilePath)) File.Replace(temporary, FilePath, null);
                else File.Move(temporary, FilePath);
            }
            catch (Exception error) { Mod.Log.Warn("Settings could not be saved: " + error.Message); }
        }
        // Explicit scalar contract: Unity's runtime serializer omitted this nested
        // settings object from the UI state on the user's installed game build.
        internal string ToJson()
        {
            Normalize();
            return "{\"confirmation\":" + BooleanJson(confirmation) +
                ",\"recovery\":" + BooleanJson(recovery) +
                ",\"shortcuts\":" + BooleanJson(shortcuts) +
                BrowserJson() +
                ",\"inspectorControls\":" + BooleanJson(inspectorControls) +
                ",\"historyDepth\":" + historyDepth.ToString(CultureInfo.InvariantCulture) +
                ",\"undoKey\":\"" + undoKey + "\",\"redoKey\":\"" + redoKey +
                "\",\"undoModifiers\":" + undoModifiers.ToString(CultureInfo.InvariantCulture) +
                ",\"redoModifiers\":" + redoModifiers.ToString(CultureInfo.InvariantCulture) +
                ",\"windowX\":" + windowX.ToString("R", CultureInfo.InvariantCulture) +
                ",\"windowY\":" + windowY.ToString("R", CultureInfo.InvariantCulture) + "}";
        }
        private static string BooleanJson(bool value) => value ? "true" : "false";
        private string BrowserJson() {
            var json=",\"browserShortcuts\":"+BooleanJson(browserShortcuts);
            for(int i=0;i<BrowserCategories.Length;i++) {
                var name=BrowserCategories[i].ToLowerInvariant();
                json+=",\""+name+"Key\":\""+BrowserKey(i)+"\",\""+name+"Modifiers\":"+BrowserModifiers(i).ToString(CultureInfo.InvariantCulture);
            }
            return json;
        }
        internal void MigrateModifiers(string json)
        {
            if (json.IndexOf("\"undoModifiers\"", StringComparison.Ordinal) < 0) undoModifiers = 1;
            if (json.IndexOf("\"redoModifiers\"", StringComparison.Ordinal) < 0) redoModifiers = 3;
            for(int i=0;i<BrowserCategories.Length;i++) {
                var name=BrowserCategories[i].ToLowerInvariant();
                if(json.IndexOf("\""+name+"Key\"",StringComparison.Ordinal)<0) SetBrowserShortcut(i,BrowserCategories[i].Substring(0,1),5);
            }
        }
        internal bool ApplyOptions(bool enabled, string limit, string undo, string redo)
        {
            int depth;
            bool clear = false;
            if (int.TryParse(limit, NumberStyles.Integer, CultureInfo.InvariantCulture, out depth) && depth >= 20 && depth <= 50)
            { clear = depth != historyDepth; historyDepth = depth; }
            shortcuts = enabled;
            string key;
            if (TryLetter(undo, out key)) undoKey = key;
            if (TryLetter(redo, out key)) redoKey = key;
            return clear;
        }
        private static bool TryLetter(string value, out string key)
        {
            key = (value ?? "").Trim().ToUpperInvariant();
            return key.Length == 1 && key[0] >= 'A' && key[0] <= 'Z';
        }
    }
}
