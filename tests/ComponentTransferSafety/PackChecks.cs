using System;
using System.Linq;
using System.Collections.Generic;
using Game.Prefabs;
using Unity.Entities;
using MW.EditorTools.Modules.ComponentManager.Transfer;
using MW.EditorTools.Modules.ComponentManager.Pack;
static class PackChecks {
 internal static void Run(World world,PrefabSystem registry,Action<bool,string> check,Action<Action,string> refuses){
  var source=new PrefabBase{name="source"};var ui=(UIObject)source.AddComponent(typeof(UIObject));ui.m_Icon="source-thumbnail";ui.m_Priority=700;ui.m_Group=new UIGroupPrefab();ui.m_IsDebugObject=true;
  var dest=new PrefabBase{name="destination"};var existing=(UIObject)dest.AddComponent(typeof(UIObject));existing.m_Icon="keep-thumbnail";existing.m_Priority=12;existing.active=false;var group=new UIGroupPrefab();existing.m_Group=group;
  var clip=new TransferClipboard();var target=new TransferTarget(world,dest,dest);var session=new TransferSession();
  clip.Copy(source,new[]{0},new Dictionary<int,string[]>{{0,new[]{"m_Group"}}});session.Paste(target,clip,TransferMode.SettingsOnly);
  check(ReferenceEquals(existing.m_Group,ui.m_Group)&&existing.m_Priority==12&&existing.m_Icon=="keep-thumbnail"&&!existing.m_IsDebugObject&&!existing.active,"selective group preserves priority/icon/flags/activation");
  session.Restore(target);check(ReferenceEquals(existing.m_Group,group)&&existing.m_Icon=="keep-thumbnail","selective restore");
  clip.Copy(source,new[]{0});session.Paste(target,clip,TransferMode.SettingsOnly);
  check(existing.m_Priority==700&&existing.m_IsDebugObject&&existing.m_Icon=="keep-thumbnail","default UI copy always preserves destination thumbnail");session.Restore(target);
  var revision=clip.Revision;clip.Copy(source,new[]{0},new Dictionary<int,string[]>{{0,new[]{"m_Icon"}}});check(revision==clip.Revision,"icon-only request cannot copy source thumbnail");
  clip.Copy(source,new[]{0},new Dictionary<int,string[]>{{0,new string[0]}});check(revision==clip.Revision,"empty selective request preserves clipboard");
  refuses(()=>clip.Copy(source,new[]{0},new Dictionary<int,string[]>{{0,new[]{"unknown"}}}),"unknown field accepted");check(revision==clip.Revision,"invalid selective request replaced clipboard");
  var empty=new PrefabBase{NativeUIIcon="generated-on-add",NativeUIGroup=new UIGroupPrefab()};var emptyTarget=new TransferTarget(world,empty,empty);ui.active=false;clip.Copy(source,new[]{0},new Dictionary<int,string[]>{{0,new[]{"m_Priority"}}});session.Paste(emptyTarget,clip,TransferMode.ComponentsOnly);
  var added=(UIObject)empty.components.Single();check(added.m_Priority==700&&added.m_Icon=="generated-on-add"&&ReferenceEquals(added.m_Group,empty.NativeUIGroup)&&!added.active,"selective addition defaults and activation");session.Restore(emptyTarget);check(empty.components.Count==0,"selective addition restore");
  string[] names={"SR ResidentialLow_02_L1_2x2","SR ResidentialLow_01_L2_2x2","SR ResidentialLow_01_L1_2x3","SR ResidentialLow_01_L1_2x2","SR ResidentialLow_10_L1_2x2","SR ResidentialLow_01_L1_4x3","SR ResidentialLow_01_L1_3x4"};
  check(PriorityOrder.Sort(new[]{"Any asset name","Another name"},null).SequenceEqual(new[]{0,1}),"opt-out numbering accepts arbitrary names and keeps supplied workspace order");
  var order=PriorityOrder.Sort(names,PriorityOrder.DefaultPattern);check(order.SequenceEqual(new[]{3,2,6,5,1,0,4}),"house/level/width/depth natural order");check(PriorityOrder.Values(4,100,10).SequenceEqual(new[]{100,110,120,130}),"priority sequence");
  try{PriorityOrder.Values(2,int.MaxValue,1);throw new Exception("overflow accepted");}catch(OverflowException){check(true,"priority overflow refused");}
  try{PriorityOrder.Sort(new[]{"bad name"},PriorityOrder.DefaultPattern);throw new Exception("unmatched name accepted");}catch(ArgumentException){check(true,"unmatched names refused");}
  try{PriorityOrder.Sort(new[]{names[0],names[0]},PriorityOrder.DefaultPattern);throw new Exception("duplicate names accepted");}catch(ArgumentException){check(true,"duplicate names refused");}
  var custom=@"^(?<pack>.*?) M(?<house>\d+)-(?<level>\d+) (?<width>\d+)x(?<depth>\d+)$";check(PriorityOrder.Sort(new[]{"Other M02-1 4x3","Other M01-5 2x3"},custom).SequenceEqual(new[]{1,0}),"custom naming pattern");
  var largePack=Enumerable.Range(1,1000).Reverse().Select(i=>"AB ResidentialLow_"+i+"_L1_2x2").ToArray();
  check(PriorityOrder.Sort(largePack,PriorityOrder.DefaultPattern).SequenceEqual(Enumerable.Range(0,1000).Reverse()),"maximum-sized pack keeps numeric ordering with shared regex");
  foreach(var invalidPattern in new[]{"",new string('x',501),"(","^.*$"}){
   bool rejected=false;try{PriorityOrder.Sort(new[]{names[0]},invalidPattern);}catch(ArgumentException){rejected=true;}
   check(rejected,"invalid or incomplete naming pattern refused before preview");
  }
  var assets=names.Take(4).Select((n,i)=>{var a=new PrefabBase{name=n};var c=(UIObject)a.AddComponent(typeof(UIObject));c.m_Icon="icon-"+i;c.m_Priority=i+5;return a;}).ToArray();
  PriorityBatch.TestWorkspace=assets;var batch=new PriorityBatch(world);int writes=0;batch.BeforeWrite=()=>writes++;
  batch.Plan(assets,50,1,PriorityOrder.DefaultPattern);check(writes==0&&assets.Select(a=>((UIObject)a.components[0]).m_Priority).SequenceEqual(new[]{5,6,7,8}),"priority preview never mutates");
  var priorities=batch.Preview.Select(c=>c.After).ToArray();check(priorities.SequenceEqual(new[]{50,51,52,53}),"priority preview values");
  batch.Apply();check(writes==1&&assets.Select(a=>((UIObject)a.components[0]).m_Priority).SequenceEqual(new[]{53,52,51,50}),"priority batch applies preview ordering");check(assets.Select((a,i)=>((UIObject)a.components[0]).m_Icon=="icon-"+i).All(x=>x),"priority apply preserves all thumbnails");registry.FailUpdates=1;refuses(batch.Restore,"priority restore failure accepted");check(assets.Select(a=>((UIObject)a.components[0]).m_Priority).SequenceEqual(new[]{53,52,51,50}),"priority failed restore reinstates applied values");batch.Restore();check(assets.Select(a=>((UIObject)a.components[0]).m_Priority).SequenceEqual(new[]{5,6,7,8}),"priority batch restore");
  batch.Plan(assets,90,1,PriorityOrder.DefaultPattern);((UIObject)assets[0].components[0]).m_Icon="later-icon";var count=writes;refuses(batch.Apply,"stale thumbnail preview accepted");check(writes==count,"stale priority apply cleared Undo");
  batch.Plan(assets,90,1,PriorityOrder.DefaultPattern);var name=assets[0].name;assets[0].name="renamed";refuses(batch.Apply,"renamed asset preview accepted");assets[0].name=name;
  batch.Plan(assets,90,1,PriorityOrder.DefaultPattern);PriorityBatch.TestWorkspace=assets.Skip(1).ToArray();refuses(batch.Apply,"removed workspace asset accepted");PriorityBatch.TestWorkspace=assets;
  batch.Plan(assets,90,1,PriorityOrder.DefaultPattern);registry.FailUpdates=1;refuses(batch.Apply,"priority refresh failure accepted");check(assets.Select(a=>((UIObject)a.components[0]).m_Priority).SequenceEqual(new[]{5,6,7,8}),"priority failure restores all previous values");
  assets[0].isReadOnly=true;refuses(()=>batch.Plan(assets,90,1,PriorityOrder.DefaultPattern),"read-only priority target accepted");assets[0].isReadOnly=false;
  batch.Plan(assets,90,1,PriorityOrder.DefaultPattern);batch.Apply();((UIObject)assets[0].components[0]).m_Priority=999;refuses(batch.Restore,"priority restore overwrote later edit");
  var inspector=new Game.UI.Editor.InspectorPanelSystem();
  Func<string,string,Game.UI.Editor.LocalizationField> field=(language,text)=>{var f=new Game.UI.Editor.LocalizationField();f.Initialize(new[]{new Game.UI.Editor.LocalizationField.LocalizationFieldEntry(language,text)});return f;};
  var sourceFields=new[]{field("en-US","Source name"),field("en-US","Source description")};
  var destFields=new[]{field("en-US","Destination name"),field("fr-FR","Description conservée")};
  sourceFields[0].Initialize(new[]{new Game.UI.Editor.LocalizationField.LocalizationFieldEntry("en-US","Source name"),new Game.UI.Editor.LocalizationField.LocalizationFieldEntry("fr-FR","  ")});
  destFields[0].Initialize(new[]{new Game.UI.Editor.LocalizationField.LocalizationFieldEntry("en-US","Destination name"),new Game.UI.Editor.LocalizationField.LocalizationFieldEntry("fr-FR","Nom existant")});
  var localisation=new LocalizationTransfer((ins,a,write)=>ReferenceEquals(a,source)?sourceFields:destFields);int localeWrites=0;localisation.BeforeWrite=()=>localeWrites++;
  source.name="Internal prefab identifier";localisation.Copy(inspector,source,"name");sourceFields[0].localization[0].text="mutated after copy";
  check(localisation.Summary("name").Contains("en-US: Source name")&&!localisation.Summary("name").Contains(source.name),"clipboard summary uses detached localisation text, never prefab name");
  var destinationName=dest.name;var thumbnail=existing.m_Icon;localisation.Paste(inspector,target,"name");
  check(destFields[0].localization[0].text=="Source name"&&destFields[1].localization[0].text=="Description conservée","name-only localisation preserves description and detached clipboard");
  check(destFields[0].localization.Single(e=>e.localeId=="fr-FR").text=="Nom existant","empty source placeholder must not erase a destination translation");
  check(dest.name==destinationName&&existing.m_Icon==thumbnail,"localisation preserves internal name and thumbnail");
  registry.FailUpdates=1;refuses(()=>localisation.Restore(inspector,target),"locale restore failure accepted");check(destFields[0].localization[0].text=="Source name","failed locale restore reinstates pasted text");localisation.Restore(inspector,target);check(destFields[0].localization[0].text=="Destination name","localisation restore");
  localisation.Copy(inspector,source,"description");localisation.Paste(inspector,target,"description");check(destFields[1].localization.Count==2&&destFields[1].localization.Any(e=>e.localeId=="fr-FR")&&destFields[0].localization[0].text=="Destination name","description-only merges languages and preserves name");
  destFields[1].localization[0].text="later translation";refuses(()=>localisation.Restore(inspector,target),"localisation restore overwrote later edit");
  dest.isReadOnly=true;refuses(()=>localisation.Paste(inspector,target,"name"),"localisation accepted read-only target");dest.isReadOnly=false;
  registry.FailUpdates=1;var oldText=destFields[0].localization[0].text;refuses(()=>localisation.Paste(inspector,target,"name"),"localisation refresh failure accepted");check(destFields[0].localization[0].text==oldText,"localisation failure restores text");
  check(localisation.Has("name")&&localisation.Has("description"),"separate localisation clipboards retained");localisation.Clear();check(!localisation.Has("name")&&!localisation.Has("description")&&!localisation.CanRestore,"clear localisation state");
  refuses(()=>localisation.AddName(inspector,target,"en-US","Overwrite"),"add name overwrites existing localisation");
  destFields[0].localization[0].text=" ";var oldDescription=destFields[1].localization[0].text;
  localisation.AddName(inspector,target,"en-US","New localised house");
  check(destFields[0].localization[0].text=="New localised house"&&destFields[1].localization[0].text==oldDescription&&dest.name==destinationName&&existing.m_Icon==thumbnail,"add name fills blank translation, preserves prefab name, description and thumbnail");
  check(!localisation.Has("name")&&localisation.CanRestore,"adding name preserves clipboard and supports restore");
  localisation.Restore(inspector,target);check(destFields[0].localization[0].text==" ","restore blank name");
  localisation.AddName(inspector,target,"de-DE","Neuer Name");check(destFields[0].localization.Any(e=>e.localeId=="de-DE"&&e.text=="Neuer Name"),"add missing language entry");localisation.Restore(inspector,target);
  bool blankRefused=false;try{localisation.AddName(inspector,target,"en-US","  ");}catch(ArgumentException){blankRefused=true;}check(blankRefused,"blank added name rejected");
  bool languageRefused=false;try{localisation.AddName(inspector,target,"../bad","Name");}catch(ArgumentException){languageRefused=true;}check(languageRefused,"invalid language rejected");
  dest.isReadOnly=true;refuses(()=>localisation.AddName(inspector,target,"en-US","Name"),"add name to read-only asset");dest.isReadOnly=false;
  registry.FailUpdates=1;refuses(()=>localisation.AddName(inspector,target,"en-US","Must roll back"),"add name refresh error accepted");check(destFields[0].localization[0].text==" ","failed add restores blank localisation");
  refuses(()=>localisation.AddDescription(inspector,target,"fr-FR","Overwrite"),"add description overwrites existing translation");
  var preservedName=destFields[0].localization[0].text;
  localisation.AddDescription(inspector,target,"de-DE","A house\nwith a garden.");
  check(destFields[1].localization.Any(e=>e.localeId=="de-DE"&&e.text=="A house\nwith a garden.")&&destFields[0].localization[0].text==preservedName&&dest.name==destinationName&&existing.m_Icon==thumbnail,"add multiline description preserves name, prefab and thumbnail");
  check(!localisation.Has("description")&&localisation.CanRestore,"adding description preserves clipboard and supports restore");
  localisation.Copy(inspector,dest,"description");check(localisation.Summary("description").Contains("A house\nwith a garden."),"new description can be copied");
  localisation.Restore(inspector,target);check(!destFields[1].localization.Any(e=>e.localeId=="de-DE"),"restore missing description entry");
  destFields[1].Initialize(destFields[1].localization.Select(e=>new Game.UI.Editor.LocalizationField.LocalizationFieldEntry(e.localeId,e.localeId=="en-US"?" ":e.text)).ToArray());localisation.AddDescription(inspector,target,"en-US","New description");localisation.Restore(inspector,target);check(destFields[1].localization.Single(e=>e.localeId=="en-US").text==" ","restore blank description");
  registry.FailUpdates=1;refuses(()=>localisation.AddDescription(inspector,target,"en-US","Must roll back"),"description refresh error accepted");check(destFields[1].localization.Single(e=>e.localeId=="en-US").text==" ","failed description add restores blank entry");
  dest.isReadOnly=true;refuses(()=>localisation.AddDescription(inspector,target,"en-US","New description"),"description add read-only target");dest.isReadOnly=false;
 }
}
