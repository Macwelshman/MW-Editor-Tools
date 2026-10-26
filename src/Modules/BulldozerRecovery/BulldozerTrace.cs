using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Prefabs;
using Game.Tools;
using Game.UI.Editor;
using HarmonyLib;
using MW.EditorTools.History;
using MW.EditorTools.Services;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game;
namespace MW.EditorTools.Modules.BulldozerRecovery
{
    // Bounded Sub-element recovery experiment. Native deletion remains unchanged.
    internal sealed partial class BulldozerTrace
    {
        private const string Id="MW.EditorTools.BulldozerTrace.0.4.86";
        private static BulldozerTrace active;
        private readonly World world;
        private readonly EditorContextService context;
        private readonly BulldozerTraceSession<PrefabBase> session=new BulldozerTraceSession<PrefabBase>();
        private readonly Harmony harmony=new Harmony(Id);
        private readonly FieldInfo parent, saveQuery, updateMap, controlPoints, defaultRaycastEntity;
        private Entity observedOwner;
        private Payload before, after;
        private bool dispatched;
        private readonly FieldInfo updateInstances;
        private int events, contextReports;
        private string lastTool;
        internal string Status { get; private set; }="Undo is automatic for supported prop deletions.";
        private sealed class Payload {
            internal ComponentBase[] Components;
            internal string[] Names;
            internal Dictionary<string,PrefabSnapshot> Values=new Dictionary<string,PrefabSnapshot>();
            internal string Summary;
            internal bool[] Active;
            internal List<FieldValue> Fields=new List<FieldValue>();
            internal int Objects;
            internal bool Shape(PrefabBase asset)=>asset!=null && !asset.isReadOnly && asset.components.Count==Components.Length && asset.components.Zip(Components,(a,b)=>a!=null&&ReferenceEquals(a,b)).All(v=>v);
            internal bool Matches(PrefabBase asset)=>Shape(asset) && Components.Select((c,i)=>c.active==Active[i]).All(v=>v) && Fields.All(v=>v.Data.ReferencesValid && v.Data.Matches(v.Field.GetValue(v.Owner)));
            internal object[] Prepare()=>Fields.Select(v=>v.Data.Materialize()).ToArray();
            internal void Apply(object[] prepared){if(prepared.Length!=Fields.Count)throw new InvalidOperationException("Prepared field mismatch.");for(int i=0;i<Fields.Count;i++)Fields[i].Field.SetValue(Fields[i].Owner,prepared[i]);for(int i=0;i<Components.Length;i++)Components[i].active=Active[i];}
            internal int Weight=>Fields.Sum(v=>v.Data.Weight);
        }
        private sealed class FieldValue {internal ComponentBase Owner;internal FieldInfo Field;internal PrefabSnapshot Data;
        }
        internal BulldozerTrace(World value, EditorContextService selection) {
            world=value; context=selection;
            updateInstances=Field(typeof(ReplacePrefabSystem),"m_UpdateInstances");
            parent=Field(typeof(InspectorPanelSystem),"m_ParentObject");
            controlPoints=Field(typeof(BulldozeToolSystem),"m_ControlPoints");
            defaultRaycastEntity=Field(typeof(DefaultToolSystem),"m_LastRaycastEntity");
            saveQuery=Field(typeof(ApplyPrefabsSystem),"m_SaveInstanceQuery");
            updateMap=Field(typeof(PrefabSystem),"m_UpdateMap");
        }
        private static FieldInfo Field(Type t,string name)=>t.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)??throw new MissingFieldException(t.FullName,name);
        internal void Attach() {
            if(active!=null) throw new InvalidOperationException("Bulldozer diagnostic already attached.");
            active=this;
            try {


                Patch(typeof(BulldozeToolSystem),"Apply",new[]{typeof(Unity.Jobs.JobHandle)},nameof(BeginApply),nameof(EndApply));
                Patch(typeof(BulldozeToolSystem),"GetAllowApply",Type.EmptyTypes,null,nameof(Allowed));
                Patch(typeof(ApplyPrefabsSystem),"OnUpdate",Type.EmptyTypes,nameof(BeforeSync),nameof(AfterSync));
                Patch(typeof(PrefabSystem),"OnUpdate",Type.EmptyTypes,nameof(BeforeRebuild),nameof(AfterRebuild));
                Patch(typeof(ReplacePrefabSystem),"ReplacePrefab",new[]{typeof(Entity),typeof(Entity),typeof(Entity)},nameof(BeforeReplacement),nameof(AfterReplacement));
                Patch(typeof(ReplacePrefabSystem).GetNestedType("Finalize",BindingFlags.Public|BindingFlags.NonPublic),"OnUpdate",Type.EmptyTypes,nameof(BeforeFinalize),nameof(AfterFinalize));
                AttachMoveRecovery();
                AttachAreaMoves();
                Patch(typeof(Game.Rendering.PreCullingSystem),"OnUpdate",Type.EmptyTypes,nameof(BeforeEffectRendering),null);
                Mod.Log.Info("MW Editor Tools: User-approved Sub-element deletion history attached; explicit arm and native completion checks required.");
            } catch { Release(); throw; }
        }
        private void Patch(Type t,string name,Type[] args,string prefix,string postfix) {
            var m=t.GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly,null,args,null)??throw new MissingMethodException(t.FullName,name);
            if(Harmony.GetPatchInfo(m)?.Owners.Any(owner=>owner!=Id && owner!=DeletionPatchPolicy.RecoveryOwner)==true)
                throw new NotSupportedException("Diagnostic patch conflict: "+t.Name+"."+name);
            harmony.Patch(m,prefix:prefix==null?null:Hook(prefix),postfix:postfix==null?null:Hook(postfix),finalizer:Hook(nameof(Failed)));
            patchMonitor.Watch(m);
        }
        private static HarmonyMethod Hook(string name)=>new HarmonyMethod(typeof(BulldozerTrace).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic));
        private static void Guard(Action action) { try { if(active!=null && !active.PatchesCompatible())return; action(); } catch(Exception error) {
            if(active?.preparingAsset!=null){active.EndNestedTestPreparation("Preparation callback failed. Restart without saving.");Mod.Log.Warn(error.Message);return;}
            if(active?.EffectBusy==true){active.FailEffect("Effect callback failed; reload without saving: "+error.Message);Mod.Log.Warn(error.ToString());return;}
            if(active?.MoveBusy==true){active.FailMove("Move callback failed; reload without saving: "+error.Message);Mod.Log.Warn(error.ToString());return;}
            if(active?.NetworkBusy==true){active.FailNetwork("Lane/network callback failed: "+error.Message);Mod.Log.Warn(error.ToString());return;}
            if(active?.StandaloneAreaBusy==true){active.FailStandaloneArea("Surface/area callback failed: "+error.Message);Mod.Log.Warn(error.ToString());return;}
            if(active?.mainBusy==true){active.FailMain("Main recovery callback failed. Reload saved disposable scene.");Mod.Log.Warn(error.Message);return;}
            if(active!=null) { if(active.restoring!=null)active.FailRecovery("Recovery callback failed: "+error.GetType().Name);else {active.ClearRecord();active.armed=false;active.Reset("Bulldozer capture stopped: "+error.GetType().Name);} }
            Mod.Log.Warn("Bulldozer capture/restoration exception: "+error);
        } }
        private PrefabBase Root => context.Inspector==null?null:parent.GetValue(context.Inspector) as PrefabBase;
        // Resolve ownership from native control points; inspector selection is optional.
        private PrefabBase TargetAsset(BulldozeToolSystem tool,bool allowNestedTarget=false) {
            observedOwner=Entity.Null;
            var points=(NativeList<ControlPoint>)controlPoints.GetValue(tool);
            if(!points.IsCreated)return null;
            PrefabBase found=null;
            for(int i=0;i<points.Length;i++) {
                var entity=points[i].m_OriginalEntity;if(entity==Entity.Null)continue;
                if(!world.EntityManager.Exists(entity)||!world.EntityManager.HasComponent<Game.Common.Owner>(entity)||world.EntityManager.HasComponent<Game.Net.Edge>(entity)||world.EntityManager.HasComponent<Game.Net.Node>(entity)||world.EntityManager.HasComponent<Game.Areas.Area>(entity)||world.EntityManager.HasComponent<Game.Buildings.Building>(entity))return null;
                if(world.EntityManager.HasBuffer<Game.Objects.SubObject>(entity)&&world.EntityManager.GetBuffer<Game.Objects.SubObject>(entity,true).Length>0&&!nestedArmed&&!allowNestedTarget)return null;
                var seen=new HashSet<Entity>();
                int depth=0;
                while(world.EntityManager.Exists(entity) && world.EntityManager.HasComponent<Game.Common.Owner>(entity)) {
                    if(!seen.Add(entity)||++depth>32)return null;
                    var owner=world.EntityManager.GetComponentData<Game.Common.Owner>(entity).m_Owner;
                    if(owner==Entity.Null)break;
                    entity=owner;
                }
                if(!world.EntityManager.Exists(entity)||!world.EntityManager.HasComponent<PrefabRef>(entity))return null;
                var prefabs=world.GetExistingSystemManaged<PrefabSystem>();if(prefabs==null)return null;
                var asset=prefabs.GetPrefab<PrefabBase>(world.EntityManager.GetComponentData<PrefabRef>(entity));
                if(found!=null && (!ReferenceEquals(found,asset)||observedOwner!=entity))return null;
                found=asset;observedOwner=entity;
            }
            return found;
        }
        private PrefabBase TargetRoot {
            get {
                if(!context.EditorAvailable || observedOwner==Entity.Null || !world.EntityManager.Exists(observedOwner) || !world.EntityManager.HasComponent<PrefabRef>(observedOwner))return null;
                return world.GetExistingSystemManaged<PrefabSystem>()?.GetPrefab<PrefabBase>(world.EntityManager.GetComponentData<PrefabRef>(observedOwner));
            }
        }
        private static Payload Capture(PrefabBase asset) {
            var result=new Payload {Components=asset.components.ToArray(),Names=asset.components.Select(c=>c==null?"null":c.GetType().Name).ToArray(),Active=asset.components.Select(c=>c!=null&&c.active).ToArray()};
            result.Objects=asset.components.OfType<ObjectSubObjects>().FirstOrDefault()?.m_SubObjects?.Length ?? 0;
            var counts=new List<string>();
            Type[] supported={typeof(ObjectSubObjects),typeof(ObjectSubAreas),typeof(ObjectSubNets),typeof(ObjectSubLanes),typeof(EffectSource)};
            foreach(var component in result.Components.Where(c=>c!=null && supported.Contains(c.GetType())))
                foreach(var field in component.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly)) {
                    var value=field.GetValue(component); var key=component.GetType().Name+"."+field.Name;
                    var snapshot=PrefabSnapshot.Take(value);result.Values.Add(key,snapshot);result.Fields.Add(new FieldValue{Owner=component,Field=field,Data=snapshot});
                    if(value is ICollection list) counts.Add(key+"="+list.Count);
                }
            result.Summary=string.Join(",",counts);
            return result;
        }
        private static void ObserveApply(BulldozeToolSystem __instance)=>Guard(()=> {
            var a=active;if(a==null || !a.context.EditorAvailable || a.disabled) return;
            if(!ReferenceEquals(__instance.World,a.world)) return;
            if(a.restoring!=null){a.FailRecovery("Another deletion interrupted recovery. Restart CS2 without saving the disposable test.");return;}
            if(a.preparingAsset!=null){a.EndNestedTestPreparation("A deletion interrupted preparation. Restart without saving.");return;}
            if(a.StandaloneAreaBusy){a.FailStandaloneArea("Another deletion interrupted area recovery; reload the disposable session without saving.");return;}
            if(a.mainBusy){a.FailMain("Another deletion interrupted prop recovery. Reload the disposable editor copy.");return;}
            // A new native deletion attempt must never expose an older recoverable
            // action if this target is unsupported, capture fails or Apply is denied.
            a.ClearEffects();a.ClearMove();a.ClearAreaMoves();a.records.Clear();a.mainHistory.Clear();a.standaloneAreas.Clear();a.networkHistory.Clear();
            Mod.Log.Info("Bulldozer Undo capture attempt; mode="+__instance.actualMode+"; automatic="+(a.AutomaticCaptureAllowed?.Invoke()==true));
            if(a.BeginEffectDeletion(__instance))return;
            if(a.BeginNetwork(__instance))return;
            if(a.BeginArea(__instance))return;
            a.areaCapture=false;a.beforeAreas=a.afterAreas=null;
            if(a.AutomaticCaptureAllowed?.Invoke()==true){a.armed=a.nestedArmed=a.nestedForestArmed=a.lastArmed=false;a.armedAsset=null;}
            a.EnableAutomaticBoundProps(__instance);
            a.EnableAutomaticMain(__instance);
            a.EverythingBegin(__instance);
            a.MainBegin(__instance);
            if(!a.armed){a.ClearRecord();Mod.Log.Info("Bound prop capture not armed; mode="+__instance.actualMode+"; status="+a.Status);return;}
            if(a.session.Asset!=null) a.Reset("Bulldozer diagnostic: overlapping action; previous observation discarded.");
            a.events++;
            if(a.record!=null){var current=a.records.Latest;var redo=a.records.NextRedo;
                if(!a.OwnerMatches(a.record)||!(current!=null?current.After:redo.Before).Matches(a.record.Asset)||!a.GraphMatches(current!=null?current.AfterGraph:redo.BeforeGraph,a.record.Owner)||!a.NestedStateMatches(a.record,current==null,false)){a.Invalidate();return;}}
            Mod.Log.Info("MW bulldozer attempt #"+a.events+"; inspector root="+(a.Root==null?"none":a.Root.name));
            var asset=a.TargetAsset(__instance);
            if(asset==null||asset.isReadOnly||!ReferenceEquals(asset,a.armedAsset)||(a.record!=null&&a.observedOwner!=a.record.Owner)) {a.Invalidate();return;}
            if((__instance.actualMode!=BulldozeToolSystem.Mode.SubElements && __instance.actualMode!=BulldozeToolSystem.Mode.Everything) || asset is AssetStampPrefab || asset.components.Any(c=>c is ActivityLocation)) {a.Invalidate();return;}
            if(__instance.actualMode==BulldozeToolSystem.Mode.Everything){
                var points=(NativeList<ControlPoint>)a.controlPoints.GetValue(__instance);
                var targets=new HashSet<Entity>();
                for(int i=0;i<points.Length;i++)if(points[i].m_OriginalEntity!=Entity.Null)targets.Add(points[i].m_OriginalEntity);
                if(targets.Count!=1){a.Invalidate();return;}
                Mod.Log.Info("Everything armed owner route; single bound target; asset="+asset.name);
            }
            if(a.nestedArmed)a.BeginNestedGroup(__instance);a.before=Capture(asset);if(a.nestedArmed)a.lastArmed=a.before.Objects==1;if(a.lastArmed&&a.before.Objects!=1){a.Invalidate();a.Status="Last-prop test requires exactly one Object Sub Objects entry.";return;}a.after=null;a.dispatched=false;a.sourceMatched=false;a.beforeGraph=a.CaptureGraph(a.observedOwner);a.session.Begin(asset,Time.frameCount);
            a.Emit("before apply",a.before.Summary);
        });
        private static void Allowed(ToolBaseSystem __instance,bool __result)=>Guard(()=> {
            if(__instance is ObjectToolSystem && active!=null && ReferenceEquals(__instance.World,active.world)&&active.objectInApply)active.objectAccepted=__result;
            if(__instance is BulldozeToolSystem && active!=null && ReferenceEquals(__instance.World,active.world)&&active.networkInApply)active.networkAccepted=__result;
            if(__instance is BulldozeToolSystem && active!=null && ReferenceEquals(__instance.World,active.world)&&active.effectInDelete)active.effectAccepted=__result;
            if(__instance is BulldozeToolSystem && active!=null && ReferenceEquals(__instance.World,active.world)&&active.standaloneAreaInApply)active.standaloneAreaAccepted=__result;
            if(__instance is BulldozeToolSystem && active!=null && ReferenceEquals(__instance.World,active.world)&&active.mainInApply)active.mainAccepted=__result;
            if(__instance is BulldozeToolSystem && active!=null && ReferenceEquals(__instance.World,active.world)) { active.session.AllowApply(__result); if(active.everythingInApply)active.everythingAccepted=__result; }
        });
        private static void EndApply(BulldozeToolSystem __instance,bool __state)=>Guard(()=> {
            if(!__state)return;
            active?.EndEffectDeletion();active?.EndNetworkApply();active?.EndStandaloneAreaApply();
            active?.EverythingEnd(__instance);
            active?.MainEnd();
            var a=active;if(a==null||!a.session.InApply) return;
            bool allowed=a.session.Accepted;a.session.EndApply();
            if(!allowed) a.Reset("Bulldozer diagnostic: native apply denied; no recovery entry.");
            else a.Emit("native apply accepted","waiting for matching SaveInstance");
        });
        private static Exception Failed(Exception __exception) {
            if(__exception!=null) Guard(()=>{if(active?.objectInApply==true||active?.MoveBusy==true)active.FailMove("Native move failed; reload without saving.");else if(active?.NetworkBusy==true)active.FailNetwork("Native lane/network operation failed; reload without saving.");else if(active?.StandaloneAreaBusy==true)active.FailStandaloneArea("Native area deletion/restoration failed; no recovery entry.");else if(active?.restoring!=null)active.FailRecovery("Native recovery operation failed. Restart CS2 without saving the disposable test.");else active?.Reset("Native operation failed; no recovery entry.");});
            return __exception;
        }
        private static void BeforeSync(ApplyPrefabsSystem __instance,out bool __state) {
            __state=false;bool matched=false;
            Guard(()=>active?.ObservePreparationSync(__instance));
            Guard(()=>active?.EverythingBeforeSync(__instance));
            Guard(()=> {
                var a=active;if(a==null || !a.context.EditorAvailable || !ReferenceEquals(__instance.World,a.world) || !a.session.CanSynchronize(a.TargetRoot)) return;
                var query=(EntityQuery)a.saveQuery.GetValue(__instance);
                var prefabs=a.world.GetExistingSystemManaged<PrefabSystem>();if(prefabs==null)return;
                using(var entities=query.ToEntityArray(Allocator.Temp))
                    foreach(var entity in entities)
                        if(entity==a.observedOwner && a.world.EntityManager.HasComponent<PrefabRef>(entity) && ReferenceEquals(prefabs.GetPrefab<PrefabBase>(a.world.EntityManager.GetComponentData<PrefabRef>(entity)),a.session.Asset)) {matched=true;break;}
                if(matched) a.Emit("SaveInstance before native sync",Capture(a.session.Asset).Summary);
            });
            __state=matched;
        }
        private static void AfterSync(bool __state)=>Guard(()=> {
            active?.CompletePreparationSync();
            active?.EverythingAfterSync();
            var a=active;if(!__state||a==null||!a.session.CanSynchronize(a.TargetRoot))return;
            var after=Capture(a.session.Asset);a.after=after;a.afterGraph=a.areaCapture?Array.Empty<GraphValue>():a.CaptureGraph(a.observedOwner);
            var changed=a.before.Values.Where(pair=>!after.Values.ContainsKey(pair.Key)||!pair.Value.Matches(after.Values[pair.Key].Materialize())).Select(pair=>pair.Key).ToArray();
            var removed=a.before.Names.Except(after.Names).ToArray();
            bool shapeValid=a.areaCapture?a.AreaRemovalMatches(a.before,after,a.session.Asset):a.lastArmed?LastRemoval(a.before,after,a.session.Asset):a.before.Shape(a.session.Asset)&&a.before.Active.SequenceEqual(after.Active)&&a.before.Objects==after.Objects+1&&after.Objects>=1;
            if(a.nestedArmed)Mod.Log.Info("Nested removal shape="+shapeValid+" identities="+a.before.Shape(a.session.Asset)+" active="+a.before.Active.SequenceEqual(after.Active)+" beforeObjects="+a.before.Objects+" afterObjects="+after.Objects+" weight="+(a.before.Weight+after.Weight));if(!shapeValid || (a.nestedArmed&&!a.NestedRemovalMatches(a.before,after)) || a.before.Weight+after.Weight>500000 || (!a.areaCapture&&a.before.Fields.Where(v=>v.Owner is ObjectSubAreas).Any(v=>!v.Data.Matches(v.Field.GetValue(v.Owner))))) {
                bool nested=a.nestedArmed; a.Invalidate();a.Status=nested?"Undo unavailable: CS2 also changed other asset data during this deletion. Restart without saving; the test did not pass.":"Bulldozer change is outside supported single-object scope; history cleared.";Mod.Log.Info(a.Status);return;
            }
            a.session.Synchronize(a.TargetRoot);
            a.Emit("native sync finished",after.Summary+"; changed="+string.Join(",",changed)+"; removed components="+string.Join(",",removed));
        });
        private static void BeforeRebuild(PrefabSystem __instance,out bool __state) {
            bool matched=false;Guard(()=> {
                var a=active;if(a==null||(!a.session.Synchronized&&a.restoring==null)||!ReferenceEquals(__instance.World,a.world))return;
                matched=((IDictionary)a.updateMap.GetValue(__instance)).Contains(a.restoring?.Asset ?? a.session.Asset);
            });__state=matched;
        }
        private static void AfterRebuild(bool __state)=>Guard(()=> {
            if(!__state||active==null)return;
            var a=active;
            if(a.restoring==null && a.session.Asset==null)return; // Synchronous finalization may already have completed.
            if(a.restoring!=null){if(!a.restoreSourceMatched){a.FailRecovery("Native restoration source did not match. Restart CS2 without saving the disposable test.");return;}a.dispatched=true;a.Status="Waiting for Undo/Redo instance regeneration…";return;}
            if(a.areaCapture&&!a.AreaCaptureReady())return;
            if(!a.areaCapture&&a.boundBefore!=null&&!a.BoundCaptureReady())return;
            if(a.areaCapture||a.boundBefore!=null)NormalizedAreaFields.Apply(a.before.Values,a.after.Values,(key,value)=>{a.before.Values[key]=value;foreach(var field in a.before.Fields.Where(f=>f.Owner.GetType().Name+"."+f.Field.Name==key))field.Data=value;});
            if(a.sourceMatched && a.after!=null && a.session.Matches(a.TargetRoot) && a.after.Matches(a.session.Asset) && (a.areaCapture?a.AreasMatch(a.afterAreas,a.observedOwner):a.GraphMatches(a.afterGraph,a.observedOwner))) {
                var prepared=a.before.Prepare();
                if(!a.before.Fields.Select((v,i)=>v.Data.Matches(prepared[i])).All(v=>v))throw new InvalidOperationException("Before-image round trip failed.");
                var entry=new Record{BoundBefore=a.boundBefore,BoundAfter=a.boundAfter,AreaNetwork=a.areaCapture?a.areaNetwork:null,AreaEffects=a.areaCapture?a.areaEffects:null,AreaReferences=a.areaCapture?a.areaReferences:null,Area=a.areaCapture,AreaOwnerTransform=a.areaOwnerTransform,BeforeAreas=a.areaCapture?a.beforeAreas:null,AfterAreas=a.areaCapture?a.afterAreas:null,RootTypes=a.nestedForestArmed?NestedRootTypes.Copy(a.capturedRootTypes):null,ForestBefore=a.areaCapture&&a.areaProps.Length>0?a.areaProps:a.forestBefore,ForestAfter=a.areaCapture&&a.areaProps.Length>0?a.CaptureAreaProps(a.observedOwner):null,Nested=a.areaCapture&&a.areaProps.Length>0?Array.Empty<NestedNode>():a.nestedArmed?a.nestedBefore:null,NestedReferences=a.areaCapture?a.areaReferences:a.nestedArmed?a.nestedReferences:null,Last=a.lastArmed,Asset=a.session.Asset,Owner=a.observedOwner,Before=a.before,After=a.after,BeforeGraph=a.beforeGraph,AfterGraph=a.afterGraph};
                // Bound retained snapshot memory as well as entry count. A large next entry starts a fresh history.
                
                if(entry.ForestBefore!=null&&entry.BoundBefore!=null)entry.ForestAfter=a.boundForestAfter;
                if(entry.ForestBefore!=null&&!entry.Area&&entry.BoundBefore==null){entry.ForestAfter=a.CaptureForestNow(entry);var retained=entry.ForestBefore.Where(group=>group[0].Original!=a.nestedBefore[0].Original).ToArray();var matched=MatchForest(retained,entry.ForestAfter);if(matched==null||!retained.Select((group,i)=>a.RetainedAfterRemoval(group,matched[i],a.nestedBefore[0].Local.m_PrefabSubIndex)).All(value=>value))throw new InvalidOperationException("Retained nested groups changed during deletion.");}if(entry.Nested!=null&&!a.NestedReferencesMatch(entry))throw new InvalidOperationException("Nested referenced prefab changed during deletion.");if((entry.Last&&entry.ForestBefore==null)||(entry.Nested!=null&&entry.ForestBefore==null))a.ClearRecord();
                if(a.records.Recent.Sum(RecordWeight)+RecordWeight(entry)>500000)a.ClearRecord();if(RecordWeight(entry)>500000)throw new NotSupportedException("Nested history image exceeds total retained snapshot budget.");a.records.Record(entry);a.Status=entry.Area?"Recorded area/surface deletion (experimental). Undo entries: "+a.UndoCount+".":entry.Nested!=null?"Recorded nested group deletion (experimental). Undo entries: "+a.UndoCount+".":"Recorded Sub-element prop deletion. Undo entries: "+a.UndoCount+".";if(entry.Nested!=null&&entry.ForestBefore==null)a.armed=a.nestedArmed=false;
                Mod.Log.Info(a.Status+" asset="+a.record.Asset.name+"; native source identity matched.");
            } else {a.ClearRecord();a.armed=false;a.Status="Sub-element source or final payload did not match; history cleared.";}
            a.session.Reset();a.before=a.after=null;a.beforeGraph=a.afterGraph=null;a.ClearAreaCompanions();a.ClearBoundCapture();
        });
        private static void BeforeFinalize(GameSystemBase __instance,out bool __state) {
            bool matched=false;Guard(()=> {
                var a=active;if(a==null||!a.dispatched||!ReferenceEquals(__instance.World,a.world))return;
                var replacement=a.world.GetExistingSystemManaged<ReplacePrefabSystem>();if(replacement==null)return;
                var queue=(NativeQueue<Entity>)a.updateInstances.GetValue(replacement);
                using(var owners=queue.ToArray(Allocator.Temp))foreach(var entity in owners)if(entity==a.observedOwner){matched=true;break;}
            });__state=matched;
        }
        private static void AfterFinalize(bool __state)=>Guard(()=> {
            var a=active;if(!__state||a==null||!a.dispatched||a.restoring==null)return;
            a.nativeFinalized=true;a.finalizedFrame=Time.frameCount;
            a.Status="Waiting for native child-object creation…";
            Mod.Log.Info("Native prefab finalization confirmed; waiting for deferred child-object graph validation.");
        });
        private void ValidateRestoration() {
            var a=this;
            var entry=a.restoring;var target=a.restoringUndo?entry.Before:entry.After;
            var graph=a.restoringUndo?entry.BeforeGraph:entry.AfterGraph;
            bool ownerOk=a.OwnerMatches(entry),payloadOk=target.Matches(entry.Asset);
            if(ownerOk&&payloadOk){if(entry.BoundBefore!=null)a.RestoreBoundNetworkSeeds(entry,a.restoringUndo);else if(entry.Area)a.RestoreAreaCompanionSeeds(entry);}
            bool graphOk=ownerOk&&(entry.Area?a.AreaGraphMatches(a.restoringUndo?entry.BeforeAreas:entry.AfterAreas,entry)&&a.AreaCompanionsMatch(entry)&&entry.AreaReferences.All(p=>NestedReferenceMatches(p.Key,p.Value)):a.GraphMatches(graph,entry.Owner));graphOk=graphOk&&a.BoundRecordCompanionsMatch(entry,a.restoringUndo);bool directGraphOk=graphOk;if(ownerOk&&payloadOk&&graphOk&&(a.restoringUndo||entry.ForestBefore!=null)&&a.RestoreNestedSeeds(entry))return;bool nestedOk=ownerOk&&a.NestedStateMatches(entry,a.restoringUndo,Time.frameCount-a.restoreFrame>=120);graphOk=graphOk&&nestedOk;
            if(!graphOk)a.nestedValidatedFrames=0;if(ownerOk&&payloadOk&&!graphOk&&Time.frameCount-a.restoreFrame<=120)return;
            if(!ownerOk||!payloadOk||!graphOk){
                var changed=target.Fields.Where(v=>!v.Data.ReferencesValid||!v.Data.Matches(v.Field.GetValue(v.Owner))).Select(v=>v.Owner.GetType().Name+"."+v.Field.Name);
                Mod.Log.Info("Recovery validation mismatch: owner="+ownerOk+"; payload="+payloadOk+"; componentShape="+target.Shape(entry.Asset)+"; fields="+string.Join(",",changed)+"; graph="+graphOk+"; directGraph="+directGraphOk+"; nestedGraph="+nestedOk+"; seedsRestored="+a.nestedSeedsRestored);
                if(ownerOk&&!entry.Area)a.LogGraphDifference(graph,entry.Owner);
                a.FailRecovery("Restored data failed validation: "+(!ownerOk?"owner":!payloadOk?"prefab payload":"object graph")+". Restart CS2 without saving the disposable test.");return;
            }
            if((entry.Area||entry.Nested!=null)&&++a.nestedValidatedFrames<3)return;bool moved=a.restoringUndo?a.records.Undo(item=>ReferenceEquals(item,entry)):a.records.Redo(item=>ReferenceEquals(item,entry));
            if(!moved){a.FailRecovery("Recovery history changed before completion. Restart CS2 without saving the disposable test.");return;}
            a.restoring=null;a.dispatched=false;
            a.Status=entry.Area?(a.restoringUndo?"Undid area/surface deletion (experimental)":"Redid area/surface deletion (experimental)"):entry.Nested!=null?(a.restoringUndo?"Undid nested group deletion (experimental)":"Redid nested group deletion (experimental)"):(a.restoringUndo?"Undid Sub-element prop deletion (experimental)":"Redid Sub-element prop deletion (experimental)");
            Mod.Log.Info(a.Status+"; payload and object graph verified after native finalization; recursive nested data/ownership verified when applicable; asset="+entry.Asset.name);
        }
        private void Emit(string stage,string detail) {
            Status="Sub-element recovery: "+stage+".";
            Mod.Log.Info("MW bulldozer trace #"+events+" frame="+Time.frameCount+" age="+(Time.frameCount-session.Started)+" stage="+stage+"; "+detail);
        }
        internal void Tick() {
            if(!PatchesCompatible())return;
            try { TickNestedPropDiscovery(); } catch(Exception discoveryError) { Mod.Log.Warn("Read-only nested prop discovery stopped: "+discoveryError.Message); }
            Guard(TickNestedTestPreparation);
            Guard(TickNestedRecoveryTest);
            Guard(TickMoveRecovery);
            Guard(TickAreaMoves);
            Guard(TickEffects);
            Guard(TickNetworkRecovery);
            Guard(TickStandaloneArea);
            Guard(TickMainRecovery);
            Guard(MainTick);
            Guard(EverythingTick);
            Guard(TickNestedFixture);
            Guard(()=> {
                if(!context.EditorAvailable)return;
                var tool=world.GetExistingSystemManaged<ToolSystem>()?.activeTool;
                var name=tool?.GetType().Name ?? "none";
                if(name!=lastTool && contextReports<24) {
                    lastTool=name;contextReports++;
                    Mod.Log.Info("MW bulldozer tool context: active="+name+"; inspector root="+(Root==null?"none":Root.name));
                }
            });
            if((areaCapture||boundBefore!=null)&&restoring==null&&session.Synchronized&&sourceMatched&&after!=null)AfterRebuild(true);
            if(restoring!=null) {
                Guard(()=> {
                    if(!OwnerMatches(restoring)){FailRecovery("Recovery owner changed. Restart CS2 without saving the disposable test.");return;}
                    if(nativeFinalized&&Time.frameCount>finalizedFrame)ValidateRestoration();
                    if(restoring!=null&&Time.frameCount-restoreFrame>120)FailRecovery("Recovery did not complete within its native update window. Restart CS2 without saving the disposable test.");
                });
                return;
            }
            if(record!=null && (!OwnerMatches(record)||(Root!=null&&!ReferenceEquals(Root,record.Asset))))ClearRecord();
            if(armed && (!context.EditorAvailable||(Root!=null&&!ReferenceEquals(Root,armedAsset)))){armed=false;armedAsset=null;}
            if(session.Asset==null)return;
            Guard(()=> {if(!context.EditorAvailable||!session.Matches(TargetRoot))Reset("Bulldozer diagnostic: asset changed; observation discarded.");
                else if(session.Expired(Time.frameCount)){if(boundBefore!=null){var reason=boundRefusal;Reset("Bound deletion Undo unavailable: companion data did not settle. See mod log.");Mod.Log.Info("Bound completion timeout: "+reason);}else Reset("Bulldozer diagnostic: no matching native synchronization within 120 frames.");}});
        }
        private void Reset(string message) {ClearBoundCapture();ClearAreaCompanions();areaCapture=false;beforeAreas=afterAreas=null;session.Reset();before=after=null;beforeGraph=afterGraph=null;dispatched=false;sourceMatched=false;Status=message;Mod.Log.Info(message);}
        internal void Release() {try {harmony.UnpatchAll(Id);}finally {ClearEffects();ClearAreaMoves();ClearMove();MoveCommitted=null;ClearNetwork();ClearStandaloneArea();ClearBoundCapture();session.Reset();before=after=null;ClearRecord();restoring=null;armed=false;armedAsset=null;if(ReferenceEquals(active,this))active=null;} }
    }
}
