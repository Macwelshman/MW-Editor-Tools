using System;using System.Collections.Generic;using MW.EditorTools.Services;using Game.UI.Editor;
class Program {
static int checks;static void Check(bool x,string n){if(!x)throw new Exception(n);checks++;}
static (BrowserCategoryShortcuts,Unity.Entities.World,HierarchyMenu<EditorAssetCategory>,EditorToolUISystem,EditorPanelUISystem) Setup(){var w=new Unity.Entities.World();var m=new HierarchyMenu<EditorAssetCategory>();var p=new PrefabToolPanelSystem(m);var t=new EditorToolUISystem();var panels=new EditorPanelUISystem();w.Add(p);w.Add(t);w.Add(panels);t.Open=()=>panels.activePanel=p;return(new(),w,m,t,panels);}
static void Main(){
var (s,w,m,t,p)=Setup();s.Request("Props",10);s.Update(w,10,true);Check(t.Opens==0,"same frame does not open");s.Update(w,11,true);Check(t.Opens==1&&m.Selections==0,"activation separate from selection");m.items=new[]{new HierarchyItem<EditorAssetCategory>{m_Data=new(){id="Buildings"}},new HierarchyItem<EditorAssetCategory>{m_Data=new(){id="Props"}}};s.Update(w,12,true);Check(m.Selections==0,"waits for initialization");s.Update(w,13,true);Check(m.Selected==1&&m.Selections==1&&m.Expanded==1,"resolves actual category index and expands it");s.Update(w,14,true);Check(m.Selections==1,"one selection only");
s.Request("Props",20);s.Update(w,21,false);s.Update(w,22,true);Check(m.Selections==1,"focus loss permanently cancels queued command");s.Request("Props",30);s.Update(w,39,true);Check(m.Selections==1,"expired command discarded");s.Request("Props",40);s.Cancel();s.Update(w,41,true);Check(m.Selections==1,"release cancellation");
s.Request("Missing",50);s.Update(w,51,true);s.Update(w,53,true);Check(m.Selections==1,"missing category refused");s.Request("Props",60);s.Update(w,61,true);p.activePanel=new object();s.Update(w,63,true);Check(m.Selections==1,"user panel switch cancels pending navigation");
t.activeTool=new(){id="PrefabTool"};p.activePanel=null;s.Request("Props",70);s.Update(w,71,true);Check(p.activePanel is PrefabToolPanelSystem,"reopens closed browser while tool remains selected");s.Update(w,73,true);Check(m.Selections==2,"selects category after reopening");
var (s2,w2,m2,t2,p2)=Setup();m2.items=new[]{new HierarchyItem<EditorAssetCategory>{m_Data=new(){id="Props"}},new HierarchyItem<EditorAssetCategory>{m_Data=new(){id="Props"}}};s2.Request("Props",1);s2.Update(w2,2,true);s2.Update(w2,4,true);Check(m2.Selections==0,"ambiguous category refused");
m2.items=new[]{new HierarchyItem<EditorAssetCategory>{m_Data=new(){id="Props",path="Buildings/Props"}},new HierarchyItem<EditorAssetCategory>{m_Data=new(){id="Props",path="Props"}}};s2.Request("Props",5);s2.Update(w2,6,true);s2.Update(w2,8,true);Check(m2.Selected==1&&m2.Selections==1,"duplicate leaf name selects full root path");m2.Selections=0;
m2.Throw=true;m2.items=new[]{new HierarchyItem<EditorAssetCategory>{m_Data=new(){id="Props"}}};s2.Request("Props",10);s2.Update(w2,11,true);s2.Update(w2,13,true);m2.Throw=false;s2.Request("Props",20);s2.Update(w2,21,true);s2.Update(w2,23,true);Check(m2.Selections==0,"native failure stops further dispatch this session");
Console.WriteLine("Browser shortcuts: "+checks+" assertions passed; native runtime acceptance pending.");}}
namespace Unity.Entities {public class World{Dictionary<Type,object> systems=new();public void Add<T>(T x)=>systems[typeof(T)]=x;public T GetExistingSystemManaged<T>()where T:class=>systems.TryGetValue(typeof(T),out var x)?(T)x:null;}}
namespace Game.UI.Editor {
public class EditorAssetCategory {public string id;private string fullPath;public string path{get=>fullPath??id;set=>fullPath=value;}}
public struct HierarchyItem<T>{public T m_Data;}
public class HierarchyMenu<T>{public IEnumerable<HierarchyItem<T>> items=Array.Empty<HierarchyItem<T>>();public int Selections,Selected=-1,Expanded=-1;public bool Throw;public void SetItemSelected(int i,bool v){if(Throw)throw new Exception("native failure");Selections++;Selected=i;}public void SetItemExpanded(int i,bool v){Expanded=i;}public bool GetSelectedItem(out T selected){selected=System.Linq.Enumerable.ElementAt(items,Selected).m_Data;return true;}}
public class PrefabToolPanelSystem {private HierarchyMenu<EditorAssetCategory> m_CategoryMenu;public PrefabToolPanelSystem(HierarchyMenu<EditorAssetCategory> m){m_CategoryMenu=m;}}
public class EditorPanelUISystem {public object activePanel;}
public class Tool{public string id;private string fullPath;public string path{get=>fullPath??id;set=>fullPath=value;}}
public class EditorToolUISystem {public Tool activeTool;public int Opens;public Action Open;public void SelectTool(string id){Opens++;activeTool=new(){id=id};Open();}}
}
namespace MW.EditorTools {static class Mod {public static Logger Log=new();public class Logger {public void Warn(string s){}public void Info(string s){}}}}


