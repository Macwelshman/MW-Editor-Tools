using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.Prefabs;
using Game.UI.Editor;
using Game.UI.Widgets;
using MW.EditorTools.Modules.ComponentManager.Transfer;
namespace MW.EditorTools.Modules.ComponentManager.Pack {
 internal sealed class LocalizationTransfer {
  private readonly Func<InspectorPanelSystem,PrefabBase,bool,LocalizationField[]> readFields;
  internal LocalizationTransfer(Func<InspectorPanelSystem,PrefabBase,bool,LocalizationField[]> reader=null){readFields=reader??Fields;}
  private static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
  private static readonly MethodInfo Build=typeof(InspectorPanelSystem).GetMethod("BuildLocalizationFields",Flags);
  private static readonly FieldInfo Cache=typeof(InspectorPanelSystem).GetField("m_WipLocalization",Flags);
  private static readonly MethodInfo ReadOnly=typeof(InspectorPanelSystem).GetMethod("IsLocalizationReadOnly",Flags);
  private sealed class Clip {internal LocalizationField.LocalizationFieldEntry[] Entries;}
  private readonly Dictionary<string,Clip> clips=new Dictionary<string,Clip>();
  private sealed class Undo {internal TransferTarget Target;internal LocalizationField Name,Description;internal LocalizationField.LocalizationFieldEntry[] BeforeName,BeforeDescription;internal string After,Components,AssetName;}
  private Undo undo;internal Action BeforeWrite;
  private static LocalizationField.LocalizationFieldEntry[] Snapshot(LocalizationField field)=>field.localization.Select(e=>new LocalizationField.LocalizationFieldEntry(e.localeId,e.text)).ToArray();
  private static string Fingerprint(LocalizationField name,LocalizationField description)=>string.Join(";",new[]{name,description}.Select(f=>string.Join("|",f.localization.Select(e=>(e.localeId??"").Length+":"+e.localeId+":"+(e.text??"").Length+":"+e.text))));
  private static LocalizationField[] Fields(InspectorPanelSystem inspector,PrefabBase asset,bool writing){
   if(!VerifiedGameBuild.Game(typeof(InspectorPanelSystem).Module.ModuleVersionId)||Build==null||Cache==null||ReadOnly==null)throw new NotSupportedException("Localisation API build is unsupported.");
   if(writing&&(bool)ReadOnly.Invoke(inspector,new object[]{asset}))throw new InvalidOperationException("Destination localisation is read-only.");
   Build.Invoke(inspector,new object[]{asset,new List<IWidget>()});var cache=(IDictionary)Cache.GetValue(inspector);var entry=cache[asset];if(entry==null)throw new InvalidOperationException("Open the asset in Object Info first.");
   var t=entry.GetType();return new[]{(LocalizationField)t.GetField("m_NameLocalization").GetValue(entry),(LocalizationField)t.GetField("m_DescriptionLocalization").GetValue(entry)};
  }
  internal string Summary(string kind)=>clips.TryGetValue(kind,out var clip)?string.Join("; ",clip.Entries.Take(3).Select(e=>e.localeId+": "+(e.text.Length>160?e.text.Substring(0,160)+"…":e.text)))+(clip.Entries.Length>3?"; +"+(clip.Entries.Length-3)+" more languages":""):"Empty";
  internal bool Has(string kind)=>clips.ContainsKey(kind);
  internal bool CanRestore=>undo!=null;
  internal string Copy(InspectorPanelSystem inspector,PrefabBase asset,string kind){
   int index=Index(kind);var fields=readFields(inspector,asset,false);var entries=fields[index].ValidEntries().Select(e=>new LocalizationField.LocalizationFieldEntry(e.localeId,e.text)).ToArray();
   if(entries.Length==0||entries.All(e=>string.IsNullOrEmpty(e.text)))throw new InvalidOperationException("No localisation text to copy; previous clipboard preserved.");
   if(entries.Length>256||entries.Sum(e=>(long)(e.text?.Length??0))>1000000||entries.Any(e=>string.IsNullOrEmpty(e.localeId)||e.text?.Length>100000)||entries.Select(e=>e.localeId).Distinct().Count()!=entries.Length)throw new InvalidOperationException("Invalid or oversized localisation entries.");
   clips[kind]=new Clip{Entries=entries};return "Copied localised "+kind+" ("+entries.Length+(entries.Length==1?" language).":" languages).");
  }
  private static int Index(string kind){if(kind=="name")return 0;if(kind=="description")return 1;throw new ArgumentException("Choose name or description.");}
  internal string Paste(InspectorPanelSystem inspector,TransferTarget target,string kind){
   Index(kind);if(!clips.TryGetValue(kind,out var clip))throw new InvalidOperationException("Copy localised "+kind+" first.");
   return Write(inspector,target,kind,clip.Entries,false);
  }
  internal string AddName(InspectorPanelSystem inspector,TransferTarget target,string locale,string text)=>AddText(inspector,target,"name",locale,text);
  internal string AddDescription(InspectorPanelSystem inspector,TransferTarget target,string locale,string text)=>AddText(inspector,target,"description",locale,text);
  private string AddText(InspectorPanelSystem inspector,TransferTarget target,string kind,string locale,string text){
   if(string.IsNullOrWhiteSpace(text)||text.Length>100000)throw new ArgumentException("Enter a localised "+kind+" (up to 100000 characters).");
   if(locale==null||locale.Length>64||!System.Text.RegularExpressions.Regex.IsMatch(locale,@"^[a-zA-Z]{2,3}(?:-[a-zA-Z0-9]{2,8})*$"))throw new ArgumentException("Enter a language ID such as en-US.");
   return Write(inspector,target,kind,new[]{new LocalizationField.LocalizationFieldEntry(locale,text)},true);
  }
  private string Write(InspectorPanelSystem inspector,TransferTarget target,string kind,LocalizationField.LocalizationFieldEntry[] entries,bool emptyOnly){
   int index=Index(kind);target.Validate();var fields=readFields(inspector,target.Prefab,true);
   if(emptyOnly&&fields[index].localization.Any(e=>entries.Any(s=>s.localeId==e.localeId)&&!string.IsNullOrWhiteSpace(e.text)))throw new InvalidOperationException("That language already has a localised "+kind+". Edit it in Object Info instead.");
   var beforeName=Snapshot(fields[0]);var beforeDescription=Snapshot(fields[1]);
   var source=entries.Select(e=>new LocalizationField.LocalizationFieldEntry(e.localeId,e.text)).ToArray();
   var merged=Snapshot(fields[index]).ToList();foreach(var entry in source){var i=merged.FindIndex(e=>e.localeId==entry.localeId);if(i<0)merged.Add(entry);else merged[i]=entry;}
   if(merged.Count>256||merged.Sum(e=>(long)(e.text?.Length??0))>1000000)throw new InvalidOperationException("Localisation exceeds the supported size.");
   if(Snapshot(fields[index]).Select(e=>e.localeId+"\0"+e.text).SequenceEqual(merged.Select(e=>e.localeId+"\0"+e.text)))return "Localised "+kind+" already matches; nothing changed.";
   BeforeWrite?.Invoke();
   try{fields[index].Initialize(merged);fields[index].SetPropertiesChanged();target.Prefab.isDirty=true;target.Refresh();}
   catch{fields[0].Initialize(beforeName);fields[1].Initialize(beforeDescription);fields[0].SetPropertiesChanged();fields[1].SetPropertiesChanged();throw;}
   undo=new Undo{AssetName=target.Prefab.name,Target=target,Name=fields[0],Description=fields[1],BeforeName=beforeName,BeforeDescription=beforeDescription,After=Fingerprint(fields[0],fields[1]),Components=target.Fingerprint()};
   return (emptyOnly?"Added":"Pasted")+" localised "+kind+"; other text, internal asset name and thumbnail preserved. Save the asset normally to persist translations.";
  }
  internal string Restore(InspectorPanelSystem inspector,TransferTarget current){
   if(undo==null||!ReferenceEquals(current.Prefab,undo.Target.Prefab)||!ReferenceEquals(current.Parent,undo.Target.Parent)||!ReferenceEquals(current.World,undo.Target.World)||current.Prefab.name!=undo.AssetName)throw new InvalidOperationException("Select the unchanged latest localisation destination.");current.Validate();var fields=readFields(inspector,current.Prefab,true);
   if(!ReferenceEquals(fields[0],undo.Name)||!ReferenceEquals(fields[1],undo.Description)||Fingerprint(fields[0],fields[1])!=undo.After||current.Fingerprint()!=undo.Components)throw new InvalidOperationException("Later edits prevent localisation Restore.");
   var beforeName=Snapshot(fields[0]);var beforeDescription=Snapshot(fields[1]);BeforeWrite?.Invoke();
   try{fields[0].Initialize(undo.BeforeName);fields[1].Initialize(undo.BeforeDescription);fields[0].SetPropertiesChanged();fields[1].SetPropertiesChanged();current.Prefab.isDirty=true;current.Refresh();undo=null;}
   catch{fields[0].Initialize(beforeName);fields[1].Initialize(beforeDescription);fields[0].SetPropertiesChanged();fields[1].SetPropertiesChanged();throw;}
   return "Previous localisation restored; save the asset normally to persist it.";
  }
  internal void Clear(){clips.Clear();undo=null;}
 }
}
