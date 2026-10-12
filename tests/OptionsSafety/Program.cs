using System;
using System.Collections.Generic;
using MW.EditorTools.Settings;
class Program {
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    static void Main(){
        var settings=new EditorToolsSettings();var recovery=new MW.EditorTools.Modules.DeletionProtection.ListRecovery();int resets=0;
        var options=new EditorToolsOptions(null,settings,recovery,()=>resets++);
        foreach(var text in new[]{"3","","4","40"}) {options.HistoryLimit=text;options.ApplyAndSave();Check(options.HistoryLimit==text && settings.historyDepth==20 && settings.Saves==0,"per-keystroke limit preserved, active value unchanged");}
        var template=options.UndoShortcut;
        options.UndoShortcut=NativeShortcutCodec.Restore(template,"T",5);
        options.RedoShortcut=NativeShortcutCodec.Restore(template,"R",3);
        options.Apply();Check(settings.undoKey=="Z","recorded draft does not activate before commit");
        options.ApplyChanges=true;
        Check(settings.historyDepth==40 && settings.undoKey=="T" && settings.undoModifiers==5 && settings.redoModifiers==3 && settings.Saves==1,"recorded combinations commit once");
        string key;int mask;
        for(int expected=0;expected<16;expected++) {var binding=NativeShortcutCodec.Restore(template,"T",expected);Check(NativeShortcutCodec.TryRead(binding,out key,out mask)&&key=="T"&&mask==expected,"modifier recording round trip");}
        Check(!NativeShortcutCodec.TryRead(template.WithPath("<Mouse>/leftButton"),out key,out mask),"nonkeyboard rejected");
        Check(!NativeShortcutCodec.TryRead(template.WithPath(""),out key,out mask),"empty capture retains previous shortcut");
        Check(recovery.Changes==1,"explicit limit change updates history once");
        options.ResetWindowPosition=true;Check(resets==1 && settings.Saves==1,"reset independent of draft commit");
        Check(options.ConfirmDeletion&&options.EnableRecovery&&options.InspectorControls,"all moved settings load active values");
        options.ConfirmDeletion=false;options.EnableRecovery=false;options.InspectorControls=false;options.ApplyAndSave();
        Check(settings.confirmation&&settings.recovery&&settings.inspectorControls,"moved setting drafts wait for Apply changes");
        options.ApplyChanges=true;
        Check(!settings.confirmation&&!settings.recovery&&!settings.inspectorControls&&settings.Saves==2&&recovery.Changes==2,"moved settings saved and recovery toggle invalidates history once");
        options.ConfirmDeletion=true;options.InspectorControls=true;options.ApplyChanges=true;
        Check(settings.confirmation&&settings.inspectorControls&&recovery.Changes==2,"display and confirmation change preserve history");
        options.EnableBrowserShortcuts=true; options.PropsShortcut=NativeShortcutCodec.Restore(template,"Q",7); options.Apply(); Check(!settings.browserShortcuts&&settings.BrowserKey(1)=="P","browser drafts remain inactive"); options.ApplyChanges=true; Check(settings.browserShortcuts&&settings.BrowserKey(1)=="Q"&&settings.BrowserModifiers(1)==7,"browser combination committed"); options.PropsShortcut=template.WithPath(""); options.ApplyChanges=true; Check(settings.BrowserKey(1)=="Q","empty browser binding retains previous value");
        options.SetDefaults();Check(!options.EnableBrowserShortcuts&&NativeShortcutCodec.TryRead(options.PropsShortcut,out key,out mask)&&key=="P"&&mask==5,"browser defaults restored without activating");Check(options.ConfirmDeletion&&options.EnableRecovery&&options.InspectorControls,"defaults include all moved settings");
        Console.WriteLine("PASS: "+checks+" Options editing assertions against actual Options source; native typing test pending.");
    }
}
namespace Colossal {public interface IDictionaryEntryError{}public interface IDictionarySource {IEnumerable<KeyValuePair<string,string>> ReadEntries(IList<IDictionaryEntryError> errors,Dictionary<string,int> counts);void Unload();}}
namespace Game.Modding {
    public interface IMod{}
    public class ModSetting {public ModSetting(IMod mod){foreach(var name in new[]{"UndoShortcut","RedoShortcut","BuildingsShortcut","PropsShortcut","FoliageShortcut","AreasShortcut","EffectsShortcut","SurfacesShortcut"}) GetType().GetProperty(name).SetValue(this,new Game.Input.ProxyBinding {path="<Keyboard>/z",modifiers=new[]{new Game.Input.ProxyModifier{m_Path="<Keyboard>/ctrl",m_Name="modifier"}}});}public virtual void Apply(){}public virtual void ApplyAndSave(){}public virtual void SetDefaults(){}public string GetBindingKeyLocaleID(string n)=>"binding."+n; public string GetBindingMapLocaleID()=>"map";public string GetSettingsLocaleID()=>"settings";public string GetOptionGroupLocaleID(string n)=>"group."+n;public string GetOptionLabelLocaleID(string n)=>"label."+n;public string GetOptionDescLocaleID(string n)=>"desc."+n;}
}
namespace Game.Settings {
    public class SettingsUIGroupOrderAttribute:Attribute{public SettingsUIGroupOrderAttribute(params string[] groups){}}
    public class SettingsUIShowGroupNameAttribute:Attribute{public SettingsUIShowGroupNameAttribute(params string[] groups){}}
    public class SettingsUISectionAttribute:Attribute{public SettingsUISectionAttribute(string section){}}
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)] public class SettingsUIKeyboardActionAttribute:Attribute{public SettingsUIKeyboardActionAttribute(string name,Game.Input.ActionType type){}}
    public class SettingsUIKeyboardBindingAttribute:Attribute{public SettingsUIKeyboardBindingAttribute(Game.Input.BindingKeyboard key,string action,bool ctrl=false,bool shift=false,bool alt=false){}}
    public class SettingsUITextInputAttribute:Attribute{}
    public class SettingsUIMultilineTextAttribute:Attribute{}
    public class SettingsUIButtonAttribute:Attribute{}
}
namespace MW.EditorTools.Settings {
    // Native persistence is mocked; real validation is covered in Phase4Safety.
    public class EditorToolsSettings {public bool browserShortcuts;public string[] keys={"B","P","F","A","E","S"};public int[] masks={5,5,5,5,5,5};public string BrowserKey(int i)=>keys[i];public int BrowserModifiers(int i)=>masks[i];public void SetBrowserShortcut(int i,string k,int m){keys[i]=k;masks[i]=m;}public bool shortcuts=true,confirmation=true,recovery=true,inspectorControls=true;public int historyDepth=20,undoModifiers=1,redoModifiers=3,Saves;public string undoKey="Z",redoKey="Z";public void Save(){Saves++;}public bool ApplyOptions(bool enabled,string limit,string undo,string redo){var old=historyDepth;historyDepth=int.Parse(limit);shortcuts=enabled;undoKey=undo.Trim().ToUpperInvariant();redoKey=redo.Trim().ToUpperInvariant();return old!=historyDepth;}}
}
namespace MW.EditorTools.Modules.DeletionProtection {public class ListRecovery {public int Changes;public void ApplySettings(){Changes++;}}}
namespace Game.Input {
public enum BindingKeyboard{Z,B,P,F,A,E,S} public enum ActionType{Button}
public struct ProxyModifier{public string m_Path,m_Name;}
public struct ProxyBinding {public string path;public System.Collections.Generic.IReadOnlyList<ProxyModifier> modifiers;public bool isKeyboard=>path!=null&&path.StartsWith("<Keyboard>/");public ProxyBinding WithPath(string value){var copy=this;copy.path=value;return copy;}public ProxyBinding WithModifiers(System.Collections.Generic.IReadOnlyList<ProxyModifier> value){var copy=this;copy.modifiers=value;return copy;}}
}

