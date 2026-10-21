using System;
using System.Collections.Generic;
using System.Reflection;
using MW.EditorTools.Modules.ComponentManager.GameIntegration;
using Game.Prefabs;
using Game.UI.Editor;
using Game.UI.Widgets;
class Program {
 static void Check(bool ok){if(!ok)throw new Exception("Heading safety failure");}
 static void Main(){
 var a=new ComponentBase();var b=new ComponentBase();var inspector=new InspectorPanelSystem();var other=new InspectorPanelSystem();
 var root=new Widget{path="scroll",visibleChildren=new List<IWidget>{new EditorSection{path="Effects",onCopy=inspector.Callback(a)},new EditorSection{path="Other",onCopy=other.Callback(b)}}};
 var result=new Dictionary<ComponentBase,string>();
 typeof(ComponentHeadingReader).GetMethod("Walk",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{new List<IWidget>{root},"editorPanel",inspector,result,0});
 Check(result.Count==1&&result[a]=="editorPanel.scroll.Effects");
 root.visibleChildren.Add(new EditorSection{path="Duplicate",onCopy=inspector.Callback(a)});result.Clear();
 typeof(ComponentHeadingReader).GetMethod("Walk",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{new List<IWidget>{root},"editorPanel",inspector,result,0});Check(result[a]=="");
 var rev=new SelectionRevision();var p=new object();var token=rev.Observe(p,p,new object[]{a,b});Check(token==rev.Observe(p,p,new object[]{a,b}));Check(token!=rev.Observe(p,p,new object[]{b,a}));
 Console.WriteLine("PASS: callback instance identity, inspector ownership, full path, ambiguous mapping refusal and stale component revisions.");
 }
}
namespace Game.Prefabs {class ComponentBase{}}
namespace Game.UI.Widgets {
 interface IWidget{string path{get;} IList<IWidget> visibleChildren{get;}}
 class Widget:IWidget {public string path{get;set;} public IList<IWidget> visibleChildren{get;set;}}
}
namespace Game.UI.Editor {
 interface IEditorPanel{IList<IWidget> children{get;}}
 class EditorSection:Widget{public Action onCopy;}
 class InspectorPanelSystem:IEditorPanel {
 public IList<IWidget> children{get;set;}
 public Action Callback(ComponentBase c)=>new Capture{comp=c,owner=this}.Run;
 class Capture {public ComponentBase comp;public InspectorPanelSystem owner;public void Run(){}}
 }
}
