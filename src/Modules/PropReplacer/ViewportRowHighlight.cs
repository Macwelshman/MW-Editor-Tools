using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Prefabs;
using Game.Reflection;
using Game.Tools;
using EditorContainer=Game.Tools.EditorContainer;
using Game.UI.Editor;
using Game.UI.Widgets;
using Unity.Entities;
using MW.EditorTools.Modules.Targeting;
using MW.EditorTools.Modules.ComponentManager.GameIntegration;

namespace MW.EditorTools.Modules.PropReplacer {
 public sealed partial class PropReplacerSystem {
  private void PublishViewportRowHighlight(State state) {
   var tool=World.GetExistingSystemManaged<EditorTargetingTool>();
   if(tool==null||context?.Available!=true||!ReferenceEquals(World.GetExistingSystemManaged<ToolSystem>()?.activeTool,tool))return;
   tool.GetOutlineTargets(out _,out var id);if(id==Entity.Null)return;
   try {
    var em=World.EntityManager;
    var registry=World.GetExistingSystemManaged<PrefabSystem>();
    var containerId=em.HasComponent<EditorContainer>(id)?em.GetComponentData<EditorContainer>(id).m_Prefab:Entity.Null;
    var container=containerId!=Entity.Null&&em.Exists(containerId)?registry.GetPrefab<PrefabBase>(containerId):null;
    // Match clipboard source classification, not the display/filter category:
    // a network or lane can also carry marker/editor-container components.
    var type=ViewportRowType.Resolve(container is EffectPrefab,container is NetLanePrefab,em.HasComponent<Game.Areas.Area>(id),em.HasComponent<Game.Net.Edge>(id)||em.HasComponent<Game.Net.Node>(id),em.HasComponent<Game.Net.Lane>(id));
    var row=ResolveViewportRow(id,type,true);
    // Native Object Info must be showing this exact owning asset. Never mark a
    // same-named row in another asset or an unrelated nested instance list.
    if(!ReferenceEquals(context.Asset,row.Root))return;
    state.highlightKey=id.ToString();
    state.highlightSourceIndex=row.Index;
    state.highlightStatus="Viewport selection: "+row.Label+" element "+(row.Index+1)+" in Object Info.";
    if(ComponentHeadingReader.Read(context.Inspector).TryGetValue(row.Owner,out var component))state.highlightComponent=component;
    var matches=new List<Tuple<string,int,int>>();
    FindViewportList(((IEditorPanel)context.Inspector).children,"editorPanel",row,matches,0);
    if(matches.Count==1){state.highlightList=matches[0].Item1;state.highlightIndex=matches[0].Item2;state.highlightPage=matches[0].Item3;}
   }catch(Exception) {state.highlightStatus="The viewport selection has no unique component-list row available.";}
  }

  private static void FindViewportList(IList<IWidget> widgets,string parent,Row row,List<Tuple<string,int,int>> matches,int depth) {
   if(widgets==null||depth>24||widgets.Count>4096)return;
   foreach(var widget in widgets) {
    if(widget==null)continue;
    var path=parent+"."+widget.path.ToString();
    if(widget is PagedList list) {
     var adapter=list.adapter;
     var accessor=adapter?.GetType().GetProperty("accessor")?.GetValue(adapter) as IValueAccessor;
     if(ReferenceEquals(accessor?.GetValue(),row.ListField.GetValue(row.Owner))) {
      FieldInfo filter=null;for(var t=adapter.GetType();t!=null&&filter==null;t=t.BaseType)filter=t.GetField("m_FilteredIndices",BindingFlags.Instance|BindingFlags.NonPublic);
      if(filter==null)continue;
      var index=ViewportListIndex.Resolve(filter.GetValue(adapter) as IList<int>,adapter.length,row.Index);
      matches.Add(Tuple.Create(path,index,index<0?-1:index/Math.Max(1,list.pageSize)));
     }
    }
    FindViewportList(widget.visibleChildren,path,row,matches,depth+1);
   }
  }
 }
}
