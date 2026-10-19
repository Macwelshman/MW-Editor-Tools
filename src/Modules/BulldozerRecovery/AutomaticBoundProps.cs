using System;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private void EnableAutomaticBoundProps(BulldozeToolSystem tool) {
            if (AutomaticCaptureAllowed?.Invoke()!=true || Busy || NestedTestRunning || context.DialogBusy || disabled) return;
            if (tool.actualMode!=BulldozeToolSystem.Mode.Everything && tool.actualMode!=BulldozeToolSystem.Mode.SubElements) return;
            var points=(NativeList<ControlPoint>)controlPoints.GetValue(tool);
            if (!points.IsCreated) return;
            var targets=points.AsArray().ToArray().Select(p=>p.m_OriginalEntity).Where(e=>e!=Entity.Null).Distinct().ToArray();
            var em=world.EntityManager;
            bool owned=targets.Length==1 && em.Exists(targets[0]) && em.HasComponent<Game.Common.Owner>(targets[0]) &&
                !em.HasComponent<Game.Net.Edge>(targets[0]) && !em.HasComponent<Game.Net.Node>(targets[0]) &&
                !em.HasComponent<Game.Areas.Area>(targets[0]) && !em.HasComponent<Game.Buildings.Building>(targets[0]);
            PrefabBase capturedAsset=null;
            AutomaticBoundCapture.TryEnable(true,false,true,owned,()=> {
                var asset=TargetAsset(tool,true);
                if (asset==null || (Root!=null&&!ReferenceEquals(asset,Root)) || asset.isReadOnly || asset is AssetStampPrefab ||
                    asset.components.Any(c=>c is ActivityLocation)) {Status="Prop Undo unavailable: the writable owning asset could not be resolved or differs from the inspector.";Mod.Log.Info(Status);return false;}
                if (!UseOrdinaryNestedGroup(asset,observedOwner)) return false;
                capturedAsset=asset;
                // Automatic capture must never run the diagnostic preparation edit.
                // Refuse stale native cache indices instead of synchronizing the asset here.
                var rows=asset.GetComponent<ObjectSubObjects>()?.m_SubObjects;
                var groups=NestedRoots(AdoptedNestedTypes,observedOwner).Select(root=>CaptureNestedImage(root,false)).ToArray();
                foreach (var group in groups) {
                    var root=group[0]; int index=root.Local.m_PrefabSubIndex;
                    if (root.Owner!=observedOwner || rows==null || index<0 || index>=rows.Length || rows[index]==null ||
                        !ReferenceEquals(rows[index].m_Object,root.Prefab) || !rows[index].m_Position.Equals(root.Local.m_Position) ||
                        !rows[index].m_Rotation.Equals(root.Local.m_Rotation)) {
                        Status="Prop Undo unavailable: native placement cache does not match the asset descriptors.";
                        Mod.Log.Info(Status); return false;
                    }
                }
                return groups.Length>0;
            },()=> {
                ClearMain(); ClearRecord();
                armedAsset=capturedAsset; armed=nestedArmed=nestedForestArmed=true; lastArmed=false;
                manualNestedHistory=true; nestedTestAsset=null; nestedTestStep=0;
                Status="Automatic prop Undo ready.";
                Mod.Log.Info("Automatic bound prop Undo enabled after read-only validation; asset="+armedAsset.name);
            },reason=>{Status="Bound prop Undo unavailable: "+reason;Mod.Log.Info(Status);});
        }
    }
}


