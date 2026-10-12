using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private Entity[] everythingEntities;
        private readonly Dictionary<PrefabBase,Payload> everythingRelated=new Dictionary<PrefabBase,Payload>();
        private PrefabBase everythingAsset;
        private Entity everythingOwner;
        private Payload everythingBefore;
        private MainPropImage everythingMain;
        private bool everythingSyncMatched,everythingSynchronized;
        private int everythingAttempts,everythingFrame,everythingCheckpoint;
        private bool everythingInApply,everythingAccepted;
        // Read-only bounded target/owner/child inventory. No recovery images or entity writes.
        private void EverythingBegin(BulldozeToolSystem tool) {
            nestedForest=null;nestedForestReferences=null;nestedImage=null;everythingRelated.Clear();everythingEntities=null;everythingInApply=false;everythingAsset=null;everythingBefore=null;everythingMain=null;everythingSyncMatched=everythingSynchronized=false;
            if(tool.actualMode!=BulldozeToolSystem.Mode.Everything||everythingAttempts>=6)return;
            everythingAttempts++;everythingFrame=Time.frameCount;everythingCheckpoint=0;everythingInApply=true;everythingAccepted=false;
            var points=(NativeList<ControlPoint>)controlPoints.GetValue(tool);
            var seen=new HashSet<Entity>();var queue=new Queue<Entity>();
            for(int i=0;i<points.Length&&seen.Count<64;i++)if(points[i].m_OriginalEntity!=Entity.Null&&seen.Add(points[i].m_OriginalEntity))queue.Enqueue(points[i].m_OriginalEntity);
            var em=world.EntityManager;
            if(seen.Count==1)try{
                var target=seen.Single();
                if(em.HasBuffer<Game.Objects.SubObject>(target)&&em.GetBuffer<Game.Objects.SubObject>(target,true).Length>0)try{nestedImage=CaptureNestedImage(target);try{CaptureNestedForest(target);}catch(Exception forestError){nestedForest=null;nestedForestReferences=null;Mod.Log.Info("Nested forest capture refused: "+forestError.Message);}}catch(Exception error){nestedImage=null;Mod.Log.Info("Nested typed capture refused: "+error.Message);}
                if(!em.HasComponent<Game.Common.Owner>(target))everythingMain=CaptureMainProp(target);
                else{
                    var root=target;var chain=new HashSet<Entity>();
                    while(em.Exists(root)&&em.HasComponent<Game.Common.Owner>(root)){
                        if(!chain.Add(root)||chain.Count>32)throw new NotSupportedException("Owner chain exceeds diagnostic scope.");
                        root=em.GetComponentData<Game.Common.Owner>(root).m_Owner;
                    }
                    if(!em.Exists(root)||!em.HasComponent<PrefabRef>(root))throw new NotSupportedException("No owning prefab.");
                    everythingOwner=root;everythingAsset=world.GetExistingSystemManaged<PrefabSystem>().GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(root));
                    everythingBefore=Capture(everythingAsset);
                    if(everythingBefore.Weight>250000)throw new NotSupportedException("Payload exceeds diagnostic budget.");
                    Mod.Log.Info("Everything before payload asset="+everythingAsset.name+"; "+everythingBefore.Summary);
                }
            }catch(Exception error){everythingBefore=null;everythingMain=null;everythingAsset=null;Mod.Log.Info("Everything payload capture refused: "+error.Message);}
            Mod.Log.Info("Everything read-only trace #"+everythingAttempts+" controlPoints="+points.Length+"; recovery unsupported; inventory capped at 64 entities.");
            bool complete=true;
            while(queue.Count>0){
                var entity=queue.Dequeue();if(!em.Exists(entity))continue;
                if(em.HasBuffer<Game.Objects.SubObject>(entity)){
                    var children=em.GetBuffer<Game.Objects.SubObject>(entity,true);
                    for(int i=0;i<children.Length;i++){
                        var child=children[i].m_SubObject;if(child==Entity.Null||seen.Contains(child))continue;
                        if(seen.Count>=64){complete=false;break;}
                        seen.Add(child);queue.Enqueue(child);
                    }
                }
            }
            var descendants=seen.ToArray();
            var prefabs=world.GetExistingSystemManaged<PrefabSystem>();
            try{
                foreach(var entity in descendants){
                    if(!em.Exists(entity)||!em.HasComponent<PrefabRef>(entity))continue;
                    var value=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(entity));
                    if(value!=null&&value.GetComponent<ObjectSubObjects>()?.m_SubObjects?.Length>0&&!everythingRelated.ContainsKey(value)){
                        var image=Capture(value);
                        if(everythingRelated.Count>=8||image.Weight+everythingRelated.Values.Sum(item=>item.Weight)>250000)throw new NotSupportedException("Related prefab payload budget exceeded.");
                        everythingRelated.Add(value,image);Mod.Log.Info("Nested referenced payload before: prefab="+value.name+"; "+image.Summary);
                    }
                }
            }catch(Exception error){everythingRelated.Clear();Mod.Log.Info("Nested referenced payload capture refused: "+error.Message);}
            foreach(var entity in descendants){
                var current=entity;var chain=new HashSet<Entity>();
                while(em.Exists(current)&&em.HasComponent<Game.Common.Owner>(current)){
                    if(!chain.Add(current)||chain.Count>32){complete=false;break;}
                    current=em.GetComponentData<Game.Common.Owner>(current).m_Owner;
                    if(current==Entity.Null)break;
                    if(seen.Contains(current))continue;
                    if(seen.Count>=64){complete=false;break;}
                    seen.Add(current);
                }
            }
            Mod.Log.Info("Everything focused inventory descendants="+descendants.Length+" withAncestors="+seen.Count+" completeWithinBudget="+complete+"; ancestor siblings excluded.");
            everythingEntities=new Entity[seen.Count];seen.CopyTo(everythingEntities);
            foreach(var entity in everythingEntities)LogMainEntity("Everything before",entity,true);
        }
        private void EverythingEnd(BulldozeToolSystem tool) {
            if(!everythingInApply)return;everythingInApply=false;
            Mod.Log.Info("Everything read-only trace #"+everythingAttempts+" native allowed="+everythingAccepted+"; frame="+Time.frameCount);
            foreach(var entity in everythingEntities)LogMainEntity("Everything after Apply",entity,false);
        }
        private void EverythingBeforeSync(ApplyPrefabsSystem system){
            everythingSyncMatched=false;
            if(!context.EditorAvailable||!ReferenceEquals(system.World,world)||!everythingAccepted||everythingBefore==null||everythingSynchronized)return;
            var em=world.EntityManager;
            if(!em.Exists(everythingOwner)||!em.HasComponent<PrefabRef>(everythingOwner))return;
            if(!ReferenceEquals(world.GetExistingSystemManaged<PrefabSystem>().GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(everythingOwner)),everythingAsset))return;
            var query=(EntityQuery)saveQuery.GetValue(system);
            using(var entities=query.ToEntityArray(Allocator.Temp))foreach(var entity in entities)if(entity==everythingOwner){everythingSyncMatched=true;break;}
            if(everythingSyncMatched)Mod.Log.Info("Everything matched SaveInstance before native sync; asset="+everythingAsset.name);
        }
        private void EverythingAfterSync(){
            if(!everythingSyncMatched)return;everythingSyncMatched=false;
            try{
                var after=Capture(everythingAsset);
                var changed=everythingBefore.Values.Where(pair=>!after.Values.ContainsKey(pair.Key)||!pair.Value.Matches(after.Values[pair.Key].Materialize())).Select(pair=>pair.Key);
                var removed=everythingBefore.Names.Except(after.Names).ToArray();
                Mod.Log.Info("Everything native payload synchronized; asset="+everythingAsset.name+"; beforeObjects="+everythingBefore.Objects+" afterObjects="+after.Objects+" componentShape="+everythingBefore.Shape(everythingAsset)+" changed="+string.Join(",",changed)+" removed="+string.Join(",",removed));
                if(everythingBefore.Objects==1&&after.Objects==0&&removed.SequenceEqual(new[]{nameof(ObjectSubObjects)})){
                    bool roundTrip=everythingBefore.Values.All(pair=>pair.Value.ReferencesValid&&pair.Value.Matches(pair.Value.Materialize()));
                    var original=everythingBefore.Components.First(component=>component is ObjectSubObjects);
                    bool destroyed=original==null;
                    bool otherShape=everythingAsset.components.SequenceEqual(everythingBefore.Components.Where(component=>!(component is ObjectSubObjects)));
                    Mod.Log.Info("Last prop detached payload checkpoint: originalComponentDestroyed="+destroyed+" otherComponentIdentityOrder="+otherShape+" allCapturedFieldsRoundTrip="+roundTrip+"; no component reconstruction or Undo enabled.");
                }
                everythingSynchronized=true;
            }catch(Exception error){Mod.Log.Info("Everything after-image refused: "+error.Message);everythingBefore=null;}
        }
        private void EverythingTick(){
            if(everythingEntities==null||everythingInApply)return;
            if(!context.EditorAvailable){everythingEntities=null;return;}
            int[] ages={1,5,30,120};int age=Time.frameCount-everythingFrame;
            if(everythingCheckpoint>=ages.Length||age<ages[everythingCheckpoint])return;
            foreach(var entity in everythingEntities)LogMainEntity("Everything age="+age,entity,false);
            if(++everythingCheckpoint==ages.Length){
                ValidateDetachedNestedImage();ValidateNestedForest();
                foreach(var pair in everythingRelated)Mod.Log.Info("Nested referenced payload after deletion: prefab="+pair.Key.name+" unchanged="+NestedReferenceMatches(pair.Key,pair.Value));
                everythingRelated.Clear();
                if(everythingMain!=null)Mod.Log.Info("Everything standalone detached image valid="+everythingMain.DetachedValid+" originalGone="+!world.EntityManager.Exists(everythingMain.Original));
                if(everythingBefore!=null)Mod.Log.Info("Everything matched native prefab synchronization="+everythingSynchronized);
                everythingEntities=null;everythingAsset=null;everythingBefore=null;everythingMain=null;
            }
        }
    }
}





