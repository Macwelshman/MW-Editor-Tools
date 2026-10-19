using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using Game.Prefabs;
using Game.Reflection;
using Game.SceneFlow;
using Game.UI;
using Game.UI.Editor;
using Game.UI.Localization;
using Game.UI.Widgets;
using HarmonyLib;
using UnityEngine;
using MW.EditorTools.History;

namespace MW.EditorTools.Modules.DeletionProtection {
    public sealed partial class ComponentConfirmation {
        private InspectorPanelSystem clearInspector;
        private PagedList blockedClearChange;
        private MethodInfo clearMethod, listChanged;
        private void InstallClearProtection(){
            clearMethod=typeof(PagedList).GetMethod("Clear",Type.EmptyTypes);
            listChanged=typeof(InspectorPanelSystem).GetMethod("OnValueChanged",PrivateInstance,null,new[]{typeof(IWidget)},null);
            var refresh=typeof(InspectorPanelSystem).GetMethod("RefreshSections",PrivateInstance);
            if(clearMethod==null||listChanged==null||refresh==null)throw new NotSupportedException("List Clear signatures unavailable.");
            if(Harmony.GetPatchInfo(clearMethod)?.Owners.Any()==true)throw new NotSupportedException("Another patch owns list Clear.");
            harmony.Patch(clearMethod,prefix:Patch(nameof(BeforeClear)));
            harmony.Patch(refresh,postfix:Patch(nameof(TrackClearInspector)));
            harmony.Patch(listChanged,prefix:Patch(nameof(BeforeClearChanged)));
        }
        private static void TrackClearInspector(InspectorPanelSystem __instance){if(active!=null)active.clearInspector=__instance;}
        // The original click must not update the prefab while the decision is pending.
        internal static bool SuppressClearChange(IWidget widget)=>widget!=null&&active!=null&&(ReferenceEquals(active.blockedClearChange,widget)||ReferenceEquals(active.pending?.List,widget));
        private static bool BeforeClearChanged(IWidget __0)=>!SuppressClearChange(__0);
        private static bool BeforeClear(PagedList __instance){
            if(active==null)return true;
            try{return active.InterceptClear(__instance);}
            catch(Exception error){active.Invalidate();Mod.Log.Error(error,"List Clear blocked because confirmation failed.");return false;}
        }
        private bool InterceptClear(PagedList list){
            if(settings!=null&&!settings.confirmation){Invalidate();return true;}
            if(bypass!=null&&ReferenceEquals(bypass.List,list)){bypass=null;return true;}
            var inspector=clearInspector;
            if(!Ready(inspector))return true; // Non-inspector lists retain native behaviour.
            var root=parent.GetValue(inspector) as PrefabBase;
            if(root==null)return true;
            var adapter=list.adapter;
            var accessor=adapter?.GetType().GetProperty("accessor")?.GetValue(adapter) as IValueAccessor;
            var storage=accessor?.GetValue() as IList;
            if(storage==null||storage.Count==0)return true;
            ComponentBase component=null;FieldInfo member=null;
            foreach(var candidate in root.components.Where(value=>value!=null)){
                foreach(var field in candidate.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance)){
                    if(!typeof(IList).IsAssignableFrom(field.FieldType)||!ReferenceEquals(field.GetValue(candidate),storage))continue;
                    if(component!=null)throw new NotSupportedException("List storage has more than one owner.");
                    component=candidate;member=field;
                }
            }
            if(component==null)return true;
            blockedClearChange=list;
            if(Thread.CurrentThread.ManagedThreadId!=mainThread||root.isReadOnly||gate.Busy)return false;
            if(Harmony.GetPatchInfo(clearMethod)?.Owners.Any(id=>!KnownOwner(id))==true)throw new NotSupportedException("List Clear patch conflict.");
            var ui=GameManager.instance.userInterface?.appBindings;
            if(ui==null||!ui.ready||callback.GetValue(ui)!=null||dismissibleCallback.GetValue(ui)!=null)return false;
            var image=PrefabSnapshot.Take(storage);
            pending=new Request{Token=gate.Begin(Time.realtimeSinceStartup),Inspector=inspector,World=inspector.World,Prefab=root,
                Component=component,Components=root.components.ToArray(),Selection=inspector.selectedObject,Parent=parent.GetValue(inspector),
                List=list,Storage=storage,ListField=member,ListSnapshot=image};
            var token=pending.Token;
            showingOwnDialog=true;
            try{
                ui.ShowConfirmationDialog(new ConfirmationDialog(LocalizedString.Value("MW Editor Tools"),
                    LocalizedString.Value("Clear all "+storage.Count+" entries from "+component.GetType().Name+" / "+member.Name+" on "+root.name+"?\n\nThis clears the entire list, including entries hidden by search or pagination. Undo is available only if this Clear is recorded in History."),
                    LocalizedString.Value("Clear all"),LocalizedString.Value("Cancel"),new LocalizedString[0]),
                    action=>answers.Enqueue(new Answer{Token=token,Action=action}));
            }finally{showingOwnDialog=false;}
            Mod.Log.Info("List Clear confirmation opened; asset="+root.name+" component="+component.GetType().Name+" field="+member.Name+" entries="+storage.Count);
            return false;
        }
        private bool ValidClear(Request request){
            if(settings==null||!settings.confirmation||!installed||!Ready(request.Inspector)||!ReferenceEquals(request.Inspector.World,request.World)||
                request.Prefab==null||request.Prefab.isReadOnly||request.Component==null||!ReferenceEquals(request.Component.prefab,request.Prefab)||
                !ReferenceEquals(request.Inspector.selectedObject,request.Selection)||!ReferenceEquals(parent.GetValue(request.Inspector),request.Parent)||
                !request.Prefab.components.SequenceEqual(request.Components,ReferenceComparer.Instance)||
                !ReferenceEquals(request.ListField.GetValue(request.Component),request.Storage)||!request.ListSnapshot.ReferencesValid||!request.ListSnapshot.Matches(request.Storage))return false;
            var adapter=request.List.adapter;
            var accessor=adapter?.GetType().GetProperty("accessor")?.GetValue(adapter) as IValueAccessor;
            return accessor!=null&&ReferenceEquals(accessor.GetValue(),request.Storage)&&Harmony.GetPatchInfo(clearMethod)?.Owners.All(KnownOwner)==true;
        }
        private void ApplyClear(Request request){
            blockedClearChange=null;
            clearMethod.Invoke(request.List,null);
            listChanged.Invoke(request.Inspector,new object[]{request.List});
        }
    }
}
