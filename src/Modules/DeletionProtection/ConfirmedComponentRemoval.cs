using System;
using System.Linq;
using Game.Prefabs;
using Game.UI.Editor;
using MW.EditorTools.Modules.ComponentManager.Transfer;

namespace MW.EditorTools.Modules.DeletionProtection {
    public sealed partial class ComponentConfirmation {
        private void ApplyConfirmedComponentRemoval(Request request) {
            // The deferred dialog callback bypasses the widget's original
            // OnValueChanged event. RemoveComponent alone only edits source DTOs.
            var refresh=typeof(InspectorPanelSystem).GetMethod("RefreshSections",PrivateInstance);
            if(refresh==null)throw new NotSupportedException("Inspector refresh is unavailable.");
            var target=new TransferTarget(request.World,request.Prefab,request.Parent as PrefabBase??request.Prefab);
            target.Validate(); // Registration and nested parent ownership before deletion.
            var remaining=request.Components.Where(c=>!request.Closure.Contains(c.GetType())).ToArray();
            remove.Invoke(request.Inspector,new object[]{request.Component});
            request.Prefab.isDirty=true;
            if(!ReferenceEquals(target.Parent,request.Prefab))target.Parent.isDirty=true;
            // Refresh from the edited source. Do not pull stale live placements
            // back into the source through OnValueChanged/MoveSubObjects.
            target.Refresh();
            if(!request.Prefab.components.SequenceEqual(remaining,ReferenceComparer.Instance))throw new InvalidOperationException("Component removal differed from its captured dependency closure; source refresh submitted, inspect the asset before saving.");
            refresh.Invoke(request.Inspector,null);
            Mod.Log.Info("Component removal refreshed: asset="+request.Prefab.name+" removed="+string.Join(",",request.Closure.Select(t=>t.Name))+" retained="+remaining.Length);
        }
    }
}
