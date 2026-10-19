using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using MW.EditorTools.History;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private sealed class MoveEntry {
            internal Entity Target;
            internal NestedNode[] Before,After;
            internal NetworkEntry BoundBefore,BoundAfter;
            internal Dictionary<PrefabBase,Payload> References;
            internal bool Bound=>BoundBefore!=null;
        }
        private readonly LastDeletion<MoveEntry> moveHistory=new LastDeletion<MoveEntry>();
        private MoveEntry moveCandidate;
        private Entity movePreviewTarget;
        private bool movePreviewEntered;
        private bool objectInApply,objectAccepted,objectMoveApplying,moveCommitted,moveRestoring,moveDisabled;
        private int moveFrame,moveStable,moveLastFrame=-1;
        private System.Reflection.FieldInfo moveTargetField;
        private string moveBlockReason;
        internal Action MoveCommitted;
        private bool MoveBusy=>moveCandidate!=null&&(moveCommitted||moveRestoring);
        private bool MovePreview=>movePreviewEntered&&!moveCommitted&&!moveRestoring;
        internal bool MovePreviewActive=>MovePreview||AreaMovePreview;
        private bool MoveMatchesTool(ObjectToolSystem tool)=>movePreviewEntered&&moveTargetField!=null&&((Entity)moveTargetField.GetValue(tool)).Equals(movePreviewTarget);
        private void AttachMoveRecovery() {
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public;
            var start=typeof(ObjectToolSystem).GetMethod("StartMoving",flags,null,new[]{typeof(Entity)},null);
            var apply=typeof(ObjectToolSystem).GetMethod("Apply",flags,null,new[]{typeof(JobHandle),typeof(bool)},null);
            var allowed=typeof(ObjectToolSystem).GetMethod("GetAllowApply",flags,null,Type.EmptyTypes,null);
            try {
                moveTargetField=typeof(ObjectToolSystem).GetField("m_MovingObject",flags)??throw new MissingFieldException("ObjectToolSystem.m_MovingObject");
                Patch(typeof(ObjectToolSystem),"StartMoving",new[]{typeof(Entity)},nameof(BeforeMoveStart),null);
                Patch(typeof(ObjectToolSystem),"Apply",new[]{typeof(JobHandle),typeof(bool)},nameof(BeforeObjectApply),nameof(AfterObjectApply));
                Patch(typeof(ObjectToolSystem),"GetAllowApply",Type.EmptyTypes,null,nameof(MoveAllowed));
                Mod.Log.Info("Built-in prop move Undo attached; native completion checks required.");
            }catch(Exception error){
                if(start!=null)harmony.Unpatch(start,HarmonyLib.HarmonyPatchType.All,Id);
                if(apply!=null)harmony.Unpatch(apply,HarmonyLib.HarmonyPatchType.All,Id);
                if(allowed!=null)harmony.Unpatch(allowed,HarmonyLib.HarmonyPatchType.All,Id);
                moveDisabled=true;Mod.Log.Warn("Move Undo unavailable; deletion recovery preserved: "+error.Message);
            }
        }
        private void ClearMove(){moveHistory.Clear();moveCandidate=null;movePreviewEntered=moveCommitted=moveRestoring=objectInApply=false;moveStable=0;}
        private void ClearMovePrevious(){effectHistory.Clear();records.Clear();networkHistory.Clear();mainHistory.Clear();standaloneAreas.Clear();areaMoveHistory.Clear();MoveCommitted?.Invoke();}
        private void FailMove(string reason){bool committed=moveCommitted||moveRestoring||(objectInApply&&objectAccepted);moveDisabled|=moveRestoring;if(committed)ClearMovePrevious();ClearMove();Status=reason;Mod.Log.Warn(reason);}
        private NetworkEntry CaptureMoveOwner(Entity owner,bool effect=false) {
            var em=world.EntityManager;
            if(!em.Exists(owner)||em.HasComponent<Game.Common.Owner>(owner)||!em.HasComponent<PrefabRef>(owner)||!em.HasComponent<Game.Objects.Transform>(owner))throw new NotSupportedException("Move must remain directly bound to the edited asset.");
            var asset=world.GetExistingSystemManaged<PrefabSystem>().GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(owner));
            if(asset==null||asset.isReadOnly||asset is AssetStampPrefab||asset.components.Any(c=>c is ActivityLocation))throw new NotSupportedException("Move owner is unsupported or read-only.");
            var entry=new NetworkEntry{Asset=asset,Owner=owner,Transform=em.GetComponentData<Game.Objects.Transform>(owner),Before=Capture(asset),
                Network=CaptureAreaNetwork(owner,false,effect),Props=CaptureAreaProps(owner),Areas=CaptureAreas(owner),Effects=CaptureAreaEffects(owner),Move=true};
            entry.References=entry.Props.SelectMany(GroupReferences).Concat(entry.Areas.SelectMany(a=>new[]{a.Prefab,a.Container})).Concat(entry.Effects.SelectMany(e=>new[]{e.Prefab,e.Container}))
                .Concat(entry.Network.Items.SelectMany(i=>new[]{i.Prefab,i.Container})).Where(p=>p!=null).Distinct().ToDictionary(p=>p,Capture);
            if(entry.Before.Weight+entry.References.Values.Sum(p=>p.Weight)>250000||!entry.Before.Matches(asset)||!NetworkOwnerValid(entry)||!NetworkSettled(entry.Network)||!NetworkCompanionsMatch(entry,true))throw new NotSupportedException("Move owner has pending or ambiguous companion data.");
            return entry;
        }
        private NestedNode[] CaptureMoveGroup(Entity id) {
            var em=world.EntityManager;
            if(!em.Exists(id)||em.HasComponent<Game.Buildings.Building>(id)||em.HasComponent<Game.Buildings.ServiceUpgrade>(id)||em.HasComponent<Game.Net.Edge>(id)||em.HasComponent<Game.Net.Node>(id)||em.HasComponent<Game.Areas.Area>(id))throw new NotSupportedException("Move Undo currently supports props, foliage and prop groups.");
            bool standalone=!em.HasComponent<Game.Common.Owner>(id);
            if(standalone)CaptureMainProp(id,false);
            var group=CaptureNestedImage(id,false,standalone);OrdinaryGroupSupported(group,true);
            if(group.Any(n=>!PropMovePolicy.Finite(n.Data.Transform)))throw new NotSupportedException("Invalid move transform.");
            return group;
        }
        private bool MoveGroupSettled(NestedNode[] group)=>group.All(n=>world.EntityManager.Exists(n.Original)&&!world.EntityManager.HasComponent<Game.Common.Created>(n.Original)&&!world.EntityManager.HasComponent<Game.Common.Updated>(n.Original)&&!world.EntityManager.HasComponent<Temp>(n.Original));
        private static bool MoveShape(NestedNode[] before,NestedNode[] after) {
            if(before==null||after==null||before.Length!=after.Length||before.Length==0)return false;
            for(int i=0;i<before.Length;i++){
                var b=before[i];var a=after[i];
                if(b.Parent!=a.Parent||b.Depth!=a.Depth||!ReferenceEquals(b.Prefab,a.Prefab)||b.EmissiveCount!=a.EmissiveCount||b.LightStateCount!=a.LightStateCount||b.HasSurface!=a.HasSurface||b.HasElevation!=a.HasElevation||b.HasStreetLight!=a.HasStreetLight||b.HasPlant!=a.HasPlant||b.HasTree!=a.HasTree||b.HasColor!=a.HasColor||b.HasDecoration!=a.HasDecoration||b.HasPlaceholder!=a.HasPlaceholder||
                    (b.Data.Colours==null)!=(a.Data.Colours==null)||(b.Data.Colours?.Length??0)!=(a.Data.Colours?.Length??0)||(b.Data.CustomColours==null)!=(a.Data.CustomColours==null)||(b.Data.CustomColours?.Length??0)!=(a.Data.CustomColours?.Length??0)||
                    (i>0&&!b.Local.Equals(a.Local))||!PropMovePolicy.ChildPose(before[0].Data.Transform,after[0].Data.Transform,b.Data.Transform,a.Data.Transform))return false;
                if(b.Effects.Length!=a.Effects.Length)return false;
                // Effect transforms move with their captured owner. All effect
                // identities, local attachments and parameters must remain exact.
                for(int e=0;e<b.Effects.Length;e++){
                    var x=b.Effects[e];var y=a.Effects[e];
                    if(!ReferenceEquals(x.Prefab,y.Prefab)||!ReferenceEquals(x.Container,y.Container)||x.HasTransform!=y.HasTransform||x.HasLocal!=y.HasLocal||x.HasElevation!=y.HasElevation||!x.Scale.Equals(y.Scale)||!x.Intensity.Equals(y.Intensity)||x.Group!=y.Group||!x.Local.Equals(y.Local)||!x.Elevation.Equals(y.Elevation))return false;
                    if(x.HasTransform&&!PropMovePolicy.ChildPose(before[0].Data.Transform,after[0].Data.Transform,new Game.Objects.Transform{m_Position=x.Position,m_Rotation=x.Rotation},new Game.Objects.Transform{m_Position=y.Position,m_Rotation=y.Rotation}))return false;
                }
            }
            return before[0].Owner==after[0].Owner;
        }
        private bool CanMoveUndo {
            get{try{var e=moveHistory.Latest;if(e==null)return false;
                if(moveDisabled||MovePreview||Busy||!context.EditorAvailable||context.DialogBusy||AutomaticCaptureAllowed?.Invoke()!=true)return false;
                if(e.References.Any(p=>!NestedReferenceMatches(p.Key,p.Value)))return MoveBlocked("referenced prefab changed");
                if(e.Bound){
                    if(!NetworkOwnerValid(e.BoundAfter))return MoveBlocked("bound owner/context/pending source update differs; inspector="+(Root?.name??"none")+" owner="+e.BoundAfter.Asset.name);
                    if(!e.BoundAfter.Before.Matches(e.BoundAfter.Asset))return MoveBlocked("saved after-image differs: "+string.Join(",",e.BoundAfter.Before.Fields.Where(f=>!f.Data.Matches(f.Field.GetValue(f.Owner))).Select(f=>f.Owner.GetType().Name+"."+f.Field.Name)));
                    if(!NetworkCompanionsMatch(e.BoundAfter,true))return MoveBlocked("bound prop/area/effect companions differ from after-image");
                    var actual=CaptureAreaNetwork(e.BoundAfter.Owner);
                    if(!e.BoundAfter.Network.TryMap(actual,true,out _))return MoveBlocked("bound network after-image differs: "+e.BoundAfter.Network.ExplainMismatch(actual,true,true));
                }else if(!ExactNestedGroup(e.After,CaptureMoveGroup(e.Target)))return MoveBlocked("standalone after-image differs");
                moveBlockReason=null;return true;
            }catch(Exception error){return MoveBlocked(error.Message);}}
        }
        private bool MoveBlocked(string reason){if(reason!=moveBlockReason){moveBlockReason=reason;Mod.Log.Info("Move Undo blocked: "+reason);}return false;}
        private void UndoMove() {
            if(!CanMoveUndo)return;var e=moveHistory.Latest;
            if(e.Bound){StartBoundRestoration(e.BoundBefore);return;}
            moveCandidate=e;moveRestoring=true;moveFrame=Time.frameCount;moveStable=0;moveLastFrame=-1;
            try {
                var actual=CaptureMoveGroup(e.Target);
                // Entire after-state is preflighted before any entity write.
                if(!ExactNestedGroup(e.After,actual))throw new NotSupportedException("Moved prop changed before Undo.");
                for(int i=0;i<e.Before.Length;i++){
                    var b=e.Before[i];var id=actual[i].Original;var em=world.EntityManager;
                    em.SetComponentData(id,b.Data.Transform);em.SetComponentData(id,b.Data.Seed);
                    if(b.HasSurface)em.SetComponentData(id,b.Data.Surface);if(b.HasElevation)em.SetComponentData(id,b.Elevation);if(b.HasStreetLight)em.SetComponentData(id,b.StreetLight);
                    if(b.HasPlant)em.SetComponentData(id,b.Plant);if(b.HasTree)em.SetComponentData(id,b.Tree);if(b.HasColor)em.SetComponentData(id,b.Color);if(b.HasDecoration)em.SetComponentEnabled<Game.Objects.Decoration>(id,b.DecorationEnabled);
                    if(i>0)em.SetComponentData(id,b.Local);
                    var colours=b.Data.Colours;var custom=b.Data.CustomColours;
                    if(colours!=null){var buffer=em.GetBuffer<Game.Rendering.MeshColor>(id);for(int c=0;c<buffer.Length;c++)buffer[c]=colours[c];}
                    if(custom!=null){var buffer=em.GetBuffer<Game.Rendering.CustomMeshColor>(id);for(int c=0;c<buffer.Length;c++)buffer[c]=custom[c];}
                    for(int effect=0;effect<b.Effects.Length;effect++)if(b.Effects[effect].HasTransform){var value=b.Effects[effect];em.SetComponentData(actual[i].Effects[effect].Original,new Game.Objects.Transform{m_Position=value.Position,m_Rotation=value.Rotation});PutMain(actual[i].Effects[effect].Original,new Game.Common.Updated());PutMain(actual[i].Effects[effect].Original,new Game.Common.BatchesUpdated());}
                    PutMain(id,new Game.Common.Updated());PutMain(id,new Game.Common.BatchesUpdated());
                }
                Status="Restoring prop placement…";
            }catch(Exception error){FailMove("Move Undo failed; reload without saving: "+error.Message);}
        }
        private void TickMoveRecovery() {
            if(!context.EditorAvailable){ClearMove();return;}
            if(MovePreview){var tool=world.GetExistingSystemManaged<ToolSystem>()?.activeTool as ObjectToolSystem;if(tool==null||tool.actualMode!=ObjectToolSystem.Mode.Move||!MoveMatchesTool(tool)){moveCandidate=null;movePreviewEntered=false;}return;}
            if(moveCandidate==null)return;
            var e=moveCandidate;bool valid=false;string reason="move topology or rigid child placement differs";
            try {
                var actual=CaptureMoveGroup(e.Target);
                if(MoveGroupSettled(actual)&&e.References.All(p=>NestedReferenceMatches(p.Key,p.Value))) {
                    if(moveRestoring)valid=ExactNestedGroup(e.Before,actual);
                    else if(!PropMovePolicy.Changed(e.Before[0].Data.Transform,actual[0].Data.Transform)){
                        if(!ExactNestedGroup(e.Before,actual)||(e.Bound&&(!e.BoundBefore.Before.Matches(e.BoundBefore.Asset)||!NetworkCompanionsMatch(e.BoundBefore,true)||!e.BoundBefore.Network.TryMap(CaptureAreaNetwork(e.BoundBefore.Owner),true,out _))))throw new NotSupportedException("Unchanged root move changed child data or bound companions.");
                        moveCandidate=null;movePreviewEntered=moveCommitted=false;Status="Move cancelled or unchanged; previous Undo retained.";return;
                    }
                    else if(MoveShape(e.Before,actual)) {
                        if(e.Bound){
                            var after=CaptureMoveOwner(e.BoundBefore.Owner);var prior=e.BoundBefore;
                            var oldOther=prior.Props.Where(g=>g[0].Original!=e.Target).ToArray();var newOther=after.Props.Where(g=>g[0].Original!=e.Target).ToArray();var mapped=MatchForest(oldOther,newOther);
                            if(mapped==null||!oldOther.Select((g,i)=>NestedGroupAtIndex(g,mapped[i],BoundDescriptorIndex.Resolve(after.Asset.GetComponent<ObjectSubObjects>()?.m_SubObjects,g[0].Prefab,g[0].Local))).All(v=>v)||!AreasMatch(prior.Areas,prior.Owner)||!AreaCompanionEffect.Matches(prior.Effects,after.Effects)||!prior.Network.TryMap(after.Network,true,out _)||!ReferenceEquals(prior.Asset,after.Asset)||!prior.Transform.Equals(after.Transform))throw new NotSupportedException("Move changed retained companions or ownership.");
                            if(e.BoundAfter==null||!e.BoundAfter.Before.Matches(after.Asset)){e.BoundAfter=after;moveStable=0;}valid=true;
                        }else valid=true;
                        if(e.After==null||!ExactNestedGroup(e.After,actual)){e.After=actual;moveStable=0;}
                    }
                }
            }catch(NotSupportedException error){valid=false;reason=error.Message;}
            if(!valid)moveStable=0;else if(moveLastFrame!=Time.frameCount){moveLastFrame=Time.frameCount;moveStable++;}
            if(moveStable>=3){
                if(moveRestoring){moveHistory.Undo(value=>ReferenceEquals(value,e));Status="Undid prop move; placement and child graph verified.";}
                else{ClearMovePrevious();moveHistory.Record(e);if(e.Bound){
                    // The live retained companion graphs were proved unchanged;
                    // adopt only their normalized saved representations.
                    foreach(var key in new[]{"ObjectSubAreas.m_SubAreas","ObjectSubNets.m_SubNets","ObjectSubLanes.m_SubLanes","EffectSource.m_Effects"})
                        if(e.BoundBefore.Before.Values.ContainsKey(key)&&e.BoundAfter.Before.Values.TryGetValue(key,out var value)){
                            e.BoundBefore.Before.Values[key]=value;foreach(var field in e.BoundBefore.Before.Fields.Where(f=>f.Owner.GetType().Name+"."+f.Field.Name==key))field.Data=value;
                        }
                    e.BoundBefore.After=e.BoundAfter.Before;e.BoundBefore.Target=e.Target;e.BoundBefore.TargetPrefab=e.Before[0].Prefab;
                }Status="Prop move ready to Undo.";}
                Mod.Log.Info(Status);moveCandidate=null;movePreviewEntered=moveCommitted=moveRestoring=false;
            }else if(Time.frameCount-moveFrame>120){Mod.Log.Info("Move validation timeout: "+reason+" target="+e.Target);FailMove(moveRestoring?"Move Undo did not validate; reload without saving.":"Move did not validate; no Undo entry recorded.");}
        }
    }
}
