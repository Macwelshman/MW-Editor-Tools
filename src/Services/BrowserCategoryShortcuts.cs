using System;
using System.Linq;
using System.Reflection;
using Game.UI.Editor;
using Unity.Entities;

namespace MW.EditorTools.Services
{
    // Only navigation; use the live menu's underlying indices and native callback.
    internal sealed class BrowserCategoryShortcuts
    {
        private static readonly FieldInfo MenuField=typeof(PrefabToolPanelSystem).GetField("m_CategoryMenu",BindingFlags.Instance|BindingFlags.NonPublic);
        private string category;
        private int requestedFrame, openedFrame=-1;
        private bool stopped;
        internal void Cancel(){category=null;openedFrame=-1;}
        internal void Request(string id,int frame){if(!stopped&&category==null){category=id;requestedFrame=frame;openedFrame=-1;}}
        internal void Update(World world,int frame,bool allowed)
        {
            if(category==null)return;
            if(!allowed||frame-requestedFrame>8){Cancel();return;}
            if(frame<=requestedFrame)return;
            try {
                var tools=world.GetExistingSystemManaged<EditorToolUISystem>();
                var panel=world.GetExistingSystemManaged<PrefabToolPanelSystem>();
                var panels=world.GetExistingSystemManaged<EditorPanelUISystem>();
                if(MenuField==null||tools==null||panel==null||panels==null)throw new NotSupportedException("Native browser controls unavailable.");
                if(openedFrame<0){
                    if(tools.activeTool?.id!="PrefabTool")tools.SelectTool("PrefabTool");
                    if(!ReferenceEquals(panels.activePanel,panel))panels.activePanel=panel;
                    openedFrame=frame;return;
                }
                if(frame<=openedFrame+1)return; // Let native OnStartRunning rebuild the menu.
                if(tools.activeTool?.id!="PrefabTool"||!ReferenceEquals(panels.activePanel,panel)){Cancel();return;}
                var menu=MenuField.GetValue(panel) as HierarchyMenu<EditorAssetCategory>;
                if(menu==null)throw new NotSupportedException("Native category menu unavailable.");
                var items=menu.items.ToArray();
                // IDs are leaf names and can recur beneath another category.
                // Native localization/persistence identifies categories by full path.
                var matches=Enumerable.Range(0,items.Length).Where(i=>items[i].m_Data?.path==category).ToArray();
                if(matches.Length!=1){
                    Mod.Log.Warn("Browser shortcut root unavailable or ambiguous: "+category+"; matches="+matches.Length+"; same-name paths="+
                        string.Join(",",items.Where(item=>item.m_Data?.id==category).Select(item=>item.m_Data.path)));
                    Cancel();return;
                }
                var id=category;Cancel();
                menu.SetItemSelected(matches[0],true);
                menu.SetItemExpanded(matches[0],true);
                EditorAssetCategory selected;
                if(!menu.GetSelectedItem(out selected)||selected?.path!=id)throw new InvalidOperationException("Native browser did not retain category "+id);
                Mod.Log.Info("MW Editor Tools: browser shortcut selected "+id+"; index="+matches[0]);
            } catch(Exception error){Cancel();stopped=true;Mod.Log.Warn("Browser shortcuts stopped for this session: "+error.Message);}
        }
    }
}
