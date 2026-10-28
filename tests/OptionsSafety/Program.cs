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
        Check(options.EnableHideUIShortcut,"hide UI enabled by default");
        options.HideUIShortcut=NativeShortcutCodec.Restore(template,"J",4);
        options.ApplyAndSave();Check(settings.hideUIKey=="BACKQUOTE","hide UI draft stays inactive");
        options.UndoShortcut=NativeShortcutCodec.Restore(template,"T",5);
        options.RedoShortcut=NativeShortcutCodec.Restore(template,"R",3);
        options.Apply();Check(settings.undoKey=="Z","recorded draft does not activate before commit");
        options.ApplyChanges=true;
        Check(settings.historyDepth==40 && settings.undoKey=="T" && settings.undoModifiers==5 && settings.redoModifiers==3 && settings.Saves==1,"recorded combinations commit once");
        Check(settings.hideUIKey=="J"&&settings.hideUIModifiers==4,"hide UI binding commits");
        string key;int mask;
        foreach(var named in new[]{"Home","F1","F12","Backquote","Digit1","Space"}) {
            var single=NativeShortcutCodec.Restore(template,named,0);
            Check(NativeShortcutCodec.TryRead(single,out key,out mask,true)&&key==named.ToUpperInvariant()&&mask==0,"single named key round trip");
            Check(!NativeShortcutCodec.TryRead(single,out key,out mask),"other shortcut scopes stay letter-only");
        }
        Check(!NativeShortcutCodec.TryRead(template.WithPath("<Keyboard>/leftCtrl"),out key,out mask,true),"modifier-only binding rejected");
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
        options.HideUIShortcut=NativeShortcutCodec.Restore(template,"Home",0);options.ApplyChanges=true;
        Check(settings.hideUIKey=="HOME"&&settings.hideUIModifiers==0,"standalone Home commits");
        options.SetDefaults();Check(options.EnableHideUIShortcut&&NativeShortcutCodec.TryRead(options.HideUIShortcut,out key,out mask,true)&&key=="BACKQUOTE"&&mask==0,"hide UI reset restores Backquote");Check(!options.EnableBrowserShortcuts&&NativeShortcutCodec.TryRead(options.PropsShortcut,out key,out mask)&&key=="D"&&mask==4,"browser defaults restored without activating");Check(options.ConfirmDeletion&&options.EnableRecovery&&options.InspectorControls,"defaults include all moved settings");
        var defaults=new[]{options.BuildingsShortcut,options.PropsShortcut,options.FoliageShortcut,options.AreasShortcut,options.EffectsShortcut,options.SurfacesShortcut};
        var letters=new[]{"B","D","F","A","E","S"};
        for(int i=0;i<defaults.Length;i++)Check(NativeShortcutCodec.TryRead(defaults[i],out key,out mask)&&key==letters[i]&&mask==4,"six Alt-only browser defaults");
        foreach(var name in new[]{"Guidance","BrowserGuidance"})Check(Attribute.IsDefined(typeof(EditorToolsOptions).GetProperty(name),typeof(Game.Settings.SettingsUIHiddenAttribute)),"long options guidance hidden");
        var order=(Game.Settings.SettingsUITabOrderAttribute)Attribute.GetCustomAttribute(typeof(EditorToolsOptions),typeof(Game.Settings.SettingsUITabOrderAttribute));
        Check(string.Join("|",order.Tabs)=="KeyboardShortcuts|Tools|Environment","three native tabs in requested order");
        foreach(var name in new[]{"EnableHideUIShortcut","HideUIShortcut","EnableShortcuts","UndoShortcut","EnableBrowserShortcuts","BuildingsShortcut","PropsShortcut","FoliageShortcut","AreasShortcut","EffectsShortcut","SurfacesShortcut","ApplyBrowserChanges"})Tab(name,"KeyboardShortcuts");
        foreach(var name in new[]{"ConfirmDeletion","EnableRecovery","InspectorControls","ApplyChanges","ResetWindowPosition"})Tab(name,"Tools");
        foreach(var name in new[]{"EnvironmentGuidance","EnvironmentHour","EnvironmentDayOfYear","EnvironmentPrecipitation","EnvironmentCloudCoverage","EnvironmentSnow","ApplyEnvironmentChanges"})Tab(name,"Environment");
        var preset=new EnvironmentSettings(null){Hour=8.5f,DayOfYear=274,Precipitation=25,CloudCoverage=40,Snow=true};
        var integrated=new EditorToolsOptions(null,settings,recovery,()=>{},preset);
        Check(!integrated.EnvironmentUnavailable&&integrated.EnvironmentHour==8.5f&&integrated.EnvironmentDayOfYear==274&&integrated.EnvironmentSnow,"saved standalone-compatible preset loads into Environment draft");
        integrated.EnvironmentHour=18.75f;integrated.EnvironmentDayOfYear=20;integrated.EnvironmentPrecipitation=60;integrated.EnvironmentCloudCoverage=75;integrated.EnvironmentSnow=false;
        integrated.ApplyAndSave();Check(preset.Hour==8.5f&&preset.Snow&&preset.SaveCalls==0,"native draft edits neither persist nor alter the live preset");
        integrated.ApplyEnvironmentChanges=true;Check(preset.Hour==18.75f&&preset.DayOfYear==20&&preset.Precipitation==60&&preset.CloudCoverage==75&&!preset.Snow&&preset.SaveCalls==1,"explicit Apply saves all environment values once");
        int beforeHistory=recovery.Changes;integrated.EnvironmentHour=float.NaN;integrated.EnvironmentDayOfYear=900;integrated.EnvironmentPrecipitation=-5;integrated.EnvironmentCloudCoverage=101;integrated.ApplyBrowserChanges=true;
        Check(preset.Hour==12&&preset.DayOfYear==365&&preset.Precipitation==0&&preset.CloudCoverage==100,"persisted environment inputs normalized");
        Check(recovery.Changes==beforeHistory,"environment-only change preserves Undo history");
        integrated.SetDefaults();Check(preset.DayOfYear==365&&preset.SaveCalls==2,"reset remains a draft");integrated.ApplyChanges=true;
        Check(preset.Hour==12&&preset.DayOfYear==91&&preset.Precipitation==0&&preset.CloudCoverage==15&&!preset.Snow,"defaults preserve standalone environment defaults");
        Check(options.EnvironmentUnavailable,"unavailable module reported separately");
        Tab("EnableEnvironment","Environment");
        Check(integrated.EnableEnvironment&&!integrated.EnvironmentControlsDisabled,"environment enabled by default with active controls");
        integrated.EnableEnvironment=false;integrated.Apply();Check(preset.EnableEnvironment&&integrated.EnvironmentControlsDisabled,"toggle draft disables controls without saving preset");
        float retainedHour=preset.Hour;integrated.ApplyEnvironmentChanges=true;
        Check(!preset.EnableEnvironment&&preset.Hour==retainedHour,"Apply persists disabled flag without discarding preset values");
        var reloaded=new EditorToolsOptions(null,settings,recovery,()=>{},preset);
        Check(!reloaded.EnableEnvironment&&reloaded.EnvironmentControlsDisabled,"disabled setting restored into Options");
        reloaded.EnableEnvironment=true;reloaded.ApplyChanges=true;Check(preset.EnableEnvironment&&!reloaded.EnvironmentControlsDisabled,"re-enable persists through ordinary Apply");
        var properties=typeof(EditorToolsOptions).GetProperties();
        Check(Array.FindIndex(properties,p=>p.Name=="EnableEnvironment")<Array.FindIndex(properties,p=>p.Name=="EnvironmentGuidance"),"enable checkbox precedes environment guidance and sliders");
        var entries=new Dictionary<string,string>();foreach(var entry in new OptionsLocale(integrated).ReadEntries(null,null))entries.Add(entry.Key,entry.Value);
        Check(entries["tab.KeyboardShortcuts"]=="Keyboard Shortcuts"&&entries["tab.Tools"]=="Tools"&&entries["tab.Environment"]=="Environment","exact requested tab labels localized");
        foreach(var name in new[]{"EnableEnvironment","EnvironmentHour","EnvironmentDayOfYear","EnvironmentPrecipitation","EnvironmentCloudCoverage","EnvironmentSnow"})Check(entries.ContainsKey("label."+name)&&entries.ContainsKey("desc."+name),"environment control labels and help available");
        Console.WriteLine("PASS: "+checks+" Options editing/tab/preset assertions against actual Options source; native menu acceptance pending.");
    }
    static void Tab(string name,string tab){var attr=(Game.Settings.SettingsUISectionAttribute)Attribute.GetCustomAttribute(typeof(EditorToolsOptions).GetProperty(name),typeof(Game.Settings.SettingsUISectionAttribute));Check(attr!=null&&attr.Tab==tab,"explicit tab placement: "+name);}
}
namespace Colossal {public interface IDictionaryEntryError{}public interface IDictionarySource {IEnumerable<KeyValuePair<string,string>> ReadEntries(IList<IDictionaryEntryError> errors,Dictionary<string,int> counts);void Unload();}}
namespace Game.Modding {
    public interface IMod{}
    public class ModSetting {public ModSetting(IMod mod){foreach(var name in new[]{"HideUIShortcut","UndoShortcut","RedoShortcut","BuildingsShortcut","PropsShortcut","FoliageShortcut","AreasShortcut","EffectsShortcut","SurfacesShortcut"}) GetType().GetProperty(name)?.SetValue(this,new Game.Input.ProxyBinding {path="<Keyboard>/z",modifiers=new[]{new Game.Input.ProxyModifier{m_Path="<Keyboard>/ctrl",m_Name="modifier"}}});GetType().GetProperty("HideUIShortcut")?.SetValue(this,new Game.Input.ProxyBinding {path="<Keyboard>/backquote",modifiers=Array.Empty<Game.Input.ProxyModifier>()});}public virtual void Apply(){}public int SaveCalls;public virtual void ApplyAndSave(){SaveCalls++;}public virtual void SetDefaults(){}public string GetOptionTabLocaleID(string n)=>"tab."+n;public string GetBindingKeyLocaleID(string n)=>"binding."+n; public string GetBindingMapLocaleID()=>"map";public string GetSettingsLocaleID()=>"settings";public string GetOptionGroupLocaleID(string n)=>"group."+n;public string GetOptionLabelLocaleID(string n)=>"label."+n;public string GetOptionDescLocaleID(string n)=>"desc."+n;}
}
namespace Game.Settings { public class SettingsUITabOrderAttribute:Attribute{public string[] Tabs;public SettingsUITabOrderAttribute(params string[] tabs){Tabs=tabs;}} public class SettingsUISliderAttribute:Attribute{public float min,max,step;public string unit;} public class SettingsUIDisableByConditionAttribute:Attribute{public SettingsUIDisableByConditionAttribute(Type type,string member){}}
    public class SettingsUIGroupOrderAttribute:Attribute{public SettingsUIGroupOrderAttribute(params string[] groups){}}
    public class SettingsUIShowGroupNameAttribute:Attribute{public SettingsUIShowGroupNameAttribute(params string[] groups){}}
    public class SettingsUISectionAttribute:Attribute{public string Tab,Group;public SettingsUISectionAttribute(string section){Group=section;}public SettingsUISectionAttribute(string tab,string group){Tab=tab;Group=group;}}
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)] public class SettingsUIKeyboardActionAttribute:Attribute{public SettingsUIKeyboardActionAttribute(string name,Game.Input.ActionType type){}}
    public class SettingsUIKeyboardBindingAttribute:Attribute{public SettingsUIKeyboardBindingAttribute(Game.Input.BindingKeyboard key,string action,bool ctrl=false,bool shift=false,bool alt=false){}}
    public class SettingsUITextInputAttribute:Attribute{}
    public class SettingsUIHiddenAttribute:Attribute{}
    public class SettingsUIMultilineTextAttribute:Attribute{}
    public class SettingsUIButtonAttribute:Attribute{}
}
namespace MW.EditorTools.Settings {
    // Native persistence is mocked; real validation is covered in Phase4Safety.
    public class EditorToolsSettings {public bool hideUIShortcut=true;public string hideUIKey="BACKQUOTE";public int hideUIModifiers=0;public bool browserShortcuts;public string[] keys={"B","P","F","A","E","S"};public int[] masks={5,5,5,5,5,5};public string BrowserKey(int i)=>keys[i];public int BrowserModifiers(int i)=>masks[i];public void SetBrowserShortcut(int i,string k,int m){keys[i]=k;masks[i]=m;}public bool shortcuts=true,confirmation=true,recovery=true,inspectorControls=true;public int historyDepth=20,undoModifiers=1,redoModifiers=3,Saves;public string undoKey="Z",redoKey="Z";public void Save(){Saves++;}public bool ApplyOptions(bool enabled,string limit,string undo,string redo){var old=historyDepth;historyDepth=int.Parse(limit);shortcuts=enabled;undoKey=undo.Trim().ToUpperInvariant();redoKey=redo.Trim().ToUpperInvariant();return old!=historyDepth;}}
}
namespace MW.EditorTools.Modules.DeletionProtection {public class ListRecovery {public int Changes;public void ApplySettings(){Changes++;}}}
namespace Game.Input {
public enum BindingKeyboard{Backquote,H,Z,B,D,P,F,A,E,S} public enum ActionType{Button}
public struct ProxyModifier{public string m_Path,m_Name;}
public struct ProxyBinding {public string path;public System.Collections.Generic.IReadOnlyList<ProxyModifier> modifiers;public bool isKeyboard=>path!=null&&path.StartsWith("<Keyboard>/");public ProxyBinding WithPath(string value){var copy=this;copy.path=value;return copy;}public ProxyBinding WithModifiers(System.Collections.Generic.IReadOnlyList<ProxyModifier> value){var copy=this;copy.modifiers=value;return copy;}}
}




namespace Colossal.IO.AssetDatabase { public class FileLocationAttribute:Attribute { public string Path;public FileLocationAttribute(string path){Path=path;} }public class AssetDatabase {public static AssetDatabase global=new AssetDatabase();public void LoadSettings(string key,object value,object defaults){} } }
