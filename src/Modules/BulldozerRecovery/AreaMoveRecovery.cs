using System;
using System.Linq;
using System.Reflection;
using Game.Prefabs;
using Game.Tools;
using MW.EditorTools.History;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private sealed class AreaMoveEntry {
            internal Entity Target,Owner,Live;
            internal int AddedNode=-1;
            internal StandaloneAreaEntry Before,After;
            internal NetworkEntry BoundBefore,BoundAfter;
            internal bool Bound=>BoundBefore!=null;
        }
        private readonly LastDeletion<AreaMoveEntry> areaMoveHistory=new LastDeletion<AreaMoveEntry>();
        private AreaMoveEntry areaMoveCandidate;
        private bool areaMovePreview,areaMoveCommitted,areaMoveRestoring,areaMoveInApply,areaMoveDisabled,areaMoveApplyTarget,areaMoveApplyAllowed;
        private int areaMoveFrame,areaMoveStable,areaMoveLastFrame=-1,areaMoveApplyState;
        private FieldInfo areaMoveStateField,areaMovePointsField,areaMoveTempField;
        private bool areaMoveHookFailed;
        private string areaMoveBlock;
        private bool AreaMoveBusy=>areaMoveCandidate!=null&&(areaMoveCommitted||areaMoveRestoring);
        private bool AreaMovePreview=>areaMovePreview&&!areaMoveCommitted&&!areaMoveRestoring;
        private void AttachAreaMoves() {
            var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
            var methods=new[]{typeof(AreaToolSystem).GetMethod("set_recreate",flags),typeof(AreaToolSystem).GetMethod("Apply",flags,null,new[]{typeof(JobHandle),typeof(bool)},null),typeof(AreaToolSystem).GetMethod("Cancel",flags,null,new[]{typeof(JobHandle),typeof(bool)},null),typeof(ToolBaseSystem).GetMethod("GetAllowApply",flags)};
            try {
                areaMoveStateField=Field(typeof(AreaToolSystem),"m_State");areaMovePointsField=Field(typeof(AreaToolSystem),"m_ControlPoints");
                areaMoveTempField=Field(typeof(AreaToolSystem),"m_TempAreaQuery");
                Patch(typeof(AreaToolSystem),"set_recreate",new[]{typeof(Entity)},nameof(BeforeAreaRecreate),null);
                Patch(typeof(AreaToolSystem),"Apply",new[]{typeof(JobHandle),typeof(bool)},nameof(BeforeAreaMoveApply),nameof(AfterAreaMoveApply));
                Patch(typeof(AreaToolSystem),"Cancel",new[]{typeof(JobHandle),typeof(bool)},nameof(BeforeAreaMoveCancel),nameof(AfterAreaMoveCancel));
                Patch(typeof(ToolBaseSystem),"GetAllowApply",Type.EmptyTypes,null,nameof(AreaMoveAllowed));
                Mod.Log.Info("Built-in area/surface move Undo attached; native completion checks required.");
            }catch(Exception error){foreach(var m in methods)if(m!=null){patchMonitor.Forget(m);harmony.Unpatch(m,HarmonyLib.HarmonyPatchType.All,Id);}areaMoveDisabled=true;Mod.Log.Warn("Area move Undo unavailable; prop/deletion recovery preserved: "+error.Message);}
        }
        private void CancelAreaMovePreview(){if(areaMoveCommitted||areaMoveRestoring)return;areaMoveCandidate=null;areaMovePreview=false;areaMoveInApply=false;}
        private bool AreaMovePreviewTargets(AreaToolSystem tool,Entity target) {
            var query=(EntityQuery)areaMoveTempField.GetValue(tool);
            using(var temps=query.ToComponentDataArray<Temp>(Unity.Collections.Allocator.Temp)){
                var retained=areaMoveCandidate?.BoundBefore?.Areas.Where(a=>a.Original!=target).Select(a=>a.Original).ToArray();
                // Bound edits also preview unchanged sibling areas. Only captured,
                // unmodified originals may accompany the single edited target.
                bool single=AreaMovePreviewPolicy.Matches(temps.ToArray(),target,retained);
                if(!single)Mod.Log.Info("Area move preview refused: expected edited target="+target+" outputs="+temps.Length+" originals="+string.Join(",",temps.ToArray().Take(4).Select(t=>t.m_Original+" flags="+t.m_Flags)));
                return single;
            }
        }
        private void ClearAreaMoves(){areaMoveHistory.Clear();areaMoveCandidate=null;areaMovePreview=areaMoveCommitted=areaMoveRestoring=areaMoveInApply=false;areaMoveStable=0;}
        private void FailAreaMove(string message){areaMoveDisabled|=areaMoveRestoring;if(areaMoveCommitted||areaMoveRestoring){ClearMovePrevious();ClearMove();}ClearAreaMoves();Status=message;Mod.Log.Warn(message);}
        private void BeginAreaMove(Entity id) {
            CancelAreaMovePreview();areaMovePreview=true;
            if(areaMoveDisabled||AutomaticCaptureAllowed?.Invoke()!=true||context.DialogBusy)return;
            try {
                var em=world.EntityManager;bool bound=em.HasComponent<Game.Common.Owner>(id);
                var e=new AreaMoveEntry{Target=id,Live=id,Before=CaptureStandaloneArea(id,bound)};
                if(!AreaMoveSettled(id))throw new NotSupportedException("Wait for area initialization.");
                if(bound){e.Owner=em.GetComponentData<Game.Common.Owner>(id).m_Owner;e.BoundBefore=CaptureMoveOwner(e.Owner);e.BoundBefore.AreaMove=true;}
                areaMoveCandidate=e;Status="Capturing original area/surface geometry.";Mod.Log.Info(Status+" prefab="+e.Before.Prefab.name+" bound="+bound);
            }catch(Exception error){Status="Area move Undo unavailable: "+error.Message;Mod.Log.Info(Status);}
        }
        private bool AreaMoveSettled(Entity id){var em=world.EntityManager;return em.Exists(id)&&!em.HasComponent<Game.Common.Created>(id)&&!em.HasComponent<Game.Common.Updated>(id)&&!em.HasComponent<Game.Common.Deleted>(id)&&!em.HasComponent<Temp>(id);}
        private static bool AreaMoveMetadata(StandaloneAreaEntry a,StandaloneAreaEntry b,bool bound)=>ReferenceEquals(a.Prefab,b.Prefab)&&a.Surface==b.Surface&&a.Space==b.Space&&a.Navigation==b.Navigation&&a.Hangaround==b.Hangaround&&a.HasContainer==b.HasContainer&&(!a.HasContainer||(ReferenceEquals(a.Container,b.Container)&&a.ContainerData.m_Scale.Equals(b.ContainerData.m_Scale)&&a.ContainerData.m_Intensity.Equals(b.ContainerData.m_Intensity)&&a.ContainerData.m_GroupIndex==b.ContainerData.m_GroupIndex))&&a.HasSeed==b.HasSeed&&a.HasExpand==b.HasExpand&&(!a.HasExpand||AreaNodeEditPolicy.Expansion(a.Expand,b.Expand,a.Data.Nodes.Length!=b.Data.Nodes.Length))&&AreaNodeEditPolicy.Shape(a.Data,b.Data,bound);
        private Entity FindMovedArea(AreaMoveEntry e,NetworkEntry owner) {
            if(!e.Bound)return e.Target; // Native node edits preserve the original; replacement standalone targets are refused.
            var retained=e.BoundBefore.Areas.Where(a=>a.Original!=e.Target).ToArray();var used=new System.Collections.Generic.HashSet<Entity>();
            foreach(var old in retained){var matches=owner.Areas.Where(a=>AreaEqual(old,a)).ToArray();if(matches.Length!=1||!used.Add(matches[0].Original))throw new NotSupportedException("Retained area geometry changed or is ambiguous.");}
            var remaining=owner.Areas.Where(a=>!used.Contains(a.Original)).ToArray();
            if(remaining.Length!=1||!ReferenceEquals(remaining[0].Prefab,e.Before.Prefab))throw new NotSupportedException("Moved area target is missing or ambiguous.");
            return remaining[0].Original;
        }
        private bool CanAreaMoveUndo {
            get{try{var e=areaMoveHistory.Latest;if(e==null||areaMoveDisabled||Busy||AreaMovePreview||MovePreview||context.DialogBusy||!context.EditorAvailable||AutomaticCaptureAllowed?.Invoke()!=true)return false;
                if(e.Bound&&(e.BoundBefore.References.Any(p=>!NestedReferenceMatches(p.Key,p.Value))||!NetworkOwnerValid(e.BoundAfter)||!e.BoundAfter.Before.Matches(e.BoundAfter.Asset)||!NetworkCompanionsMatch(e.BoundAfter,true)||!e.BoundAfter.Network.TryMap(CaptureAreaNetwork(e.Owner),true,out _)))return AreaMoveBlocked("bound owner, referenced prefab, saved after-image or companions differ");
                var actual=CaptureStandaloneArea(e.Live,e.Bound);
                if(!e.Before.Valid||!e.After.Valid||!StandaloneAreaMatches(e.After,actual)||!e.After.Network.TryMap(actual.Network,true,out _))return AreaMoveBlocked("area geometry, metadata, references or generated companions differ from after-image");
                areaMoveBlock=null;return true;
            }catch(Exception error){return AreaMoveBlocked(error.Message);}}
        }
        private bool AreaMoveBlocked(string reason){if(areaMoveBlock!=reason){areaMoveBlock=reason;Mod.Log.Info("Area move Undo blocked: "+reason);}return false;}
        private void UndoAreaMove() {
            if(!CanAreaMoveUndo)return;var e=areaMoveHistory.Latest;
            if(e.Bound){e.BoundBefore.AreaMoveEntry=e;StartBoundRestoration(e.BoundBefore);return;}
            areaMoveCandidate=e;areaMoveRestoring=true;areaMoveFrame=Time.frameCount;areaMoveStable=0;areaMoveLastFrame=-1;
            try {
                var em=world.EntityManager;var current=CaptureStandaloneArea(e.Live);
                if(!StandaloneAreaMatches(e.After,current)||!e.After.Network.TryMap(current.Network,true,out _))throw new NotSupportedException("Area changed before move Undo.");
                WriteAreaMoveGeometry(e.Live,e.Before);
                Status="Restoring unbound area/surface geometry…";
            }catch(Exception error){FailAreaMove("Area move Undo failed; reload without saving: "+error.Message);}
        }
        private bool AreaMoveRestored(AreaMoveEntry e) {
            var id=e.Live;
            if(e.Bound){var matches=CaptureAreas(e.Owner).Where(a=>AreaEqual(e.BoundBefore.Areas.Single(a=>a.Original==e.Target),a)).ToArray();if(matches.Length!=1)return false;id=matches[0].Original;}
            if(!AreaMoveSettled(id)||!e.Before.Valid||!e.After.Valid)return false;
            var actual=CaptureStandaloneArea(id,e.Bound,true);var seed=actual.Seed;actual.Seed=e.Before.Seed;
            if(!StandaloneAreaMatches(e.Before,actual)||!e.Before.Network.TryMap(actual.Network,false,out var map,false,true)||!CompanionSourcesValid(e.After.Network.Items.Select(i=>i.Id),actual.Network.Items.Select(i=>i.Id)))return false;
            var em=world.EntityManager;
            foreach(var item in e.Before.Network.Items.Where(i=>i.HasLocal))if(!AreaCompanionNetwork.CanRestoreLocal(item.Local,actual.Network.Items.Single(a=>a.Id==map[item.Id]).Local))return false;
            bool changed=false;
            if(e.Before.HasSeed&&!seed.Equals(e.Before.Seed)){em.SetComponentData(id,e.Before.Seed);changed=true;}
            foreach(var item in e.Before.Network.Items){var live=map[item.Id];var a=actual.Network.Items.Single(n=>n.Id==live);if(item.HasSeed&&!item.Seed.Equals(a.Seed)){em.SetComponentData(live,item.Seed);changed=true;}if(item.HasLocal&&!FiniteCurve(a.Local.m_Curve)){em.SetComponentData(live,item.Local);changed=true;}}
            if(changed)return false;
            return e.Before.Network.TryMap(CaptureAreaNetwork(id),true,out _,true,true);
        }
        private void TickAreaMoves() {
            if(!context.EditorAvailable){ClearAreaMoves();return;}
            if(AreaMovePreview){var tool=world.GetExistingSystemManaged<ToolSystem>()?.activeTool as AreaToolSystem;if(!AreaMovePolicy.PreviewActive(tool!=null,tool==null?0:Convert.ToInt32(areaMoveStateField.GetValue(tool)),tool!=null&&tool.recreate!=Entity.Null,areaMoveInApply))CancelAreaMovePreview();return;}
            if(areaMoveCandidate==null||areaMoveInApply)return;
            var e=areaMoveCandidate;bool valid=false;string reason="area geometry or companion mismatch";
            try {
                if(areaMoveRestoring)valid=AreaMoveRestored(e);
                else if(areaMoveCommitted&&e.Before.Valid){
                    NetworkEntry owner=null;
                    if(e.Bound){if(e.BoundBefore.References.Any(p=>!NestedReferenceMatches(p.Key,p.Value)))throw new NotSupportedException("Area move changed referenced companion prefab data.");owner=CaptureMoveOwner(e.Owner);owner.AreaMove=true;var prior=e.BoundBefore;
                        if(!prior.Transform.Equals(owner.Transform)||!ReferenceEquals(prior.Asset,owner.Asset)||owner.Areas.Length!=prior.Areas.Length||!prior.Before.Shape(owner.Asset)||!prior.Before.Active.SequenceEqual(owner.Before.Active))throw new NotSupportedException("Area owner/topology changed.");
                        var ancillary=new[]{"ObjectSubObjects.m_SubObjects","ObjectSubNets.m_SubNets","ObjectSubLanes.m_SubLanes","EffectSource.m_Effects"};
                        var changedFields=prior.Before.Fields.Where(f=>f.Owner.GetType().Name+"."+f.Field.Name!="ObjectSubAreas.m_SubAreas"&&!ancillary.Contains(f.Owner.GetType().Name+"."+f.Field.Name)).Where(f=>!f.Data.Matches(f.Field.GetValue(f.Owner))).Select(f=>f.Owner.GetType().Name+"."+f.Field.Name).ToArray();
                        if(changedFields.Length!=0)throw new NotSupportedException("Area owner metadata changed: "+string.Join(",",changedFields));
                        if(!NetworkRowsMatchGraph(owner.Before,owner.Network))throw new NotSupportedException("Area owner network source descriptors differ from native graph.");
                        var saved=owner.Areas.Select(a=>AreaMovePolicy.SavedPositions(a.Nodes,a.Local,owner.Transform.m_Position,owner.Transform.m_Rotation)).ToArray();
                        if(!AreaMovePolicy.Sources(owner.Asset.GetComponent<ObjectSubAreas>()?.m_SubAreas,owner.Areas.Select(a=>a.Prefab as AreaPrefab).ToArray(),owner.Areas.Select(a=>a.Local).ToArray(),saved))throw new NotSupportedException("Area polygon source descriptors differ from native saved node positions or parent meshes.");

                    }
                    var id=FindMovedArea(e,owner);
                    if(!AreaMoveSettled(id))throw new NotSupportedException("Area native update pending.");
                    var actual=CaptureStandaloneArea(id,e.Bound);
                    if(!AreaMoveMetadata(e.Before,actual,e.Bound))throw new NotSupportedException("Area move changed vertex topology, attachments or metadata: "+AreaMovePolicy.ShapeDifference(e.Before.Data,actual.Data,!e.Bound)+"; prefab="+ReferenceEquals(e.Before.Prefab,actual.Prefab)+" kind="+(e.Before.Surface==actual.Surface&&e.Before.Space==actual.Space)+" navigation="+(e.Before.Navigation==actual.Navigation)+" hangaround="+(e.Before.Hangaround==actual.Hangaround)+" container="+(e.Before.HasContainer==actual.HasContainer&&ReferenceEquals(e.Before.Container,actual.Container)&&e.Before.ContainerData.Equals(actual.ContainerData))+" seedPresence="+(e.Before.HasSeed==actual.HasSeed)+" expansion="+(e.Before.HasExpand==actual.HasExpand&&(!e.Before.HasExpand||AreaMovePolicy.ExpansionShape(e.Before.Expand,actual.Expand))));
                    if(e.Bound){
                        var prior=e.BoundBefore;var props=MatchForest(prior.Props,owner.Props);
                        if(!BoundLiveSettled(owner.CompanionMap,e.Owner,owner.Props.SelectMany(GroupSources)))throw new NotSupportedException("Area edit retained companion initialization is pending.");
                        if(props==null||!AreaCompanionEffect.Matches(prior.Effects,owner.Effects)||!prior.Network.TryMap(owner.Network,true,out var rootMap))
                            throw new NotSupportedException("Area edit retained prop membership, effect values or root network changed: "+prior.Network.ExplainMismatch(owner.Network,true));
                        var referenceMap=EntityReferenceMap.WithOwnerPrefab(rootMap,prior.Owner,prior.OwnerPrefab,owner.OwnerPrefab);
                        if(!MapAreaLanes(prior.CompanionMap.WithoutArea(e.Target),owner.CompanionMap.WithoutArea(id),referenceMap,true))
                            throw new NotSupportedException("Area edit changed a retained area or its generated lane graph.");
                        if(!OwnedConnectionPolicy.MapEditedArea(e.Before.Network,actual.Network,out var editedMap)||!CompanionSourcesValid(e.Before.Network.Items.Select(i=>i.Id),actual.Network.Items.Select(i=>i.Id)))
                            throw new NotSupportedException("Edited area generated lane paths, metadata or surviving source identities differ.");
                        foreach(var link in editedMap)if(!EntityReferenceMap.Add(referenceMap,link.Key,link.Value))throw new NotSupportedException("Edited area reference map is ambiguous.");
                        if(!AddForestMap(prior.Props,props,referenceMap)||!prior.Props.Select((g,i)=>NestedGroupAtIndex(g,props[i],BoundDescriptorIndex.ResolveNative(owner.Asset.GetComponent<ObjectSubObjects>()?.m_SubObjects,g[0].Prefab,g[0].Local,prior.Transform,g[0].Data.Transform),referenceMap)).All(v=>v))
                            throw new NotSupportedException("Area edit changed retained prop data, placement or mapped spawn/access references.");
                    }
                    if(e.Before.Data.Matches(actual.Data)&&StandaloneAreaMatches(e.Before,actual)&&e.Before.Network.TryMap(actual.Network,true,out _)&&(!e.Bound||e.BoundBefore.Before.Matches(owner.Asset))){CancelCommittedAreaMove();Status="Area move unchanged; previous Undo retained.";return;}
                    if(e.After==null||!StandaloneAreaMatches(e.After,actual)||!e.After.Network.TryMap(actual.Network,true,out _)||(e.Bound&&(e.BoundAfter==null||!e.BoundAfter.Before.Matches(owner.Asset)))){e.After=actual;e.Live=id;e.BoundAfter=owner;areaMoveStable=0;}
                    valid=true;
                }
            }catch(NotSupportedException error){reason=error.Message;}
            catch(Exception error){FailAreaMove("Area move recovery failed; reload without saving: "+error.Message);return;}
            if(!valid)areaMoveStable=0;else if(areaMoveLastFrame!=Time.frameCount){areaMoveLastFrame=Time.frameCount;areaMoveStable++;}
            if(areaMoveStable>=3){
                if(areaMoveRestoring){if(!areaMoveHistory.Undo(v=>ReferenceEquals(v,e))){FailAreaMove("Area move slot changed; reload without saving.");return;}Status="Undid unbound area/surface edit; geometry and companions verified.";}
                else {e=CombineAddedNodeMove(e);ClearMovePrevious();ClearMove();
                    if(e.Bound){foreach(var key in new[]{"ObjectSubObjects.m_SubObjects","ObjectSubNets.m_SubNets","ObjectSubLanes.m_SubLanes","EffectSource.m_Effects"})if(e.BoundBefore.Before.Values.ContainsKey(key)&&e.BoundAfter.Before.Values.TryGetValue(key,out var value)){e.BoundBefore.Before.Values[key]=value;foreach(var f in e.BoundBefore.Before.Fields.Where(f=>f.Owner.GetType().Name+"."+f.Field.Name==key))f.Data=value;}
                        // The retained prop rows have adopted the proved native
                        // representation. Align detached root cache expectations to it;
                        // restoration must still check every other captured value.
                        var rows=e.BoundAfter.Asset.GetComponent<ObjectSubObjects>()?.m_SubObjects;
                        var roots=e.BoundBefore.Props.Select(g=>BoundDescriptorIndex.ForNativeSource(rows,g[0].Prefab,g[0].Local,e.BoundBefore.Transform,g[0].Data.Transform)).ToArray();
                        for(int g=0;g<roots.Length;g++)e.BoundBefore.Props[g][0].Local=roots[g];
                        e.BoundBefore.After=e.BoundAfter.Before;e.BoundBefore.Target=e.Target;e.BoundBefore.TargetPrefab=e.Before.Prefab;}
                    e.AddedNode=e.AddedNode>=0?e.AddedNode:AreaNodeEditPolicy.AddedIndex(e.Before.Data,e.After.Data,e.Bound);
                    areaMoveHistory.Record(e);Status="Area/surface "+AreaNodeEditPolicy.Action(e.Before.Data,e.After.Data)+" ready to Undo.";
                }
                Mod.Log.Info(Status);CancelCommittedAreaMove();
            }else if(Time.frameCount-areaMoveFrame>120)FailAreaMove((areaMoveRestoring?"Area move Undo did not validate; reload without saving: ":"Area move did not validate; no Undo entry: ")+reason);
        }
        private void CancelCommittedAreaMove(){areaMoveCandidate=null;areaMovePreview=areaMoveCommitted=areaMoveRestoring=false;areaMoveStable=0;}
    }
}
