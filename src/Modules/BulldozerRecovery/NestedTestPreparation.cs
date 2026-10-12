using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private PrefabBase preparingAsset;
        private Entity preparingOwner;
        private Payload preparingPayload,preparingStablePayload;
        private GraphValue[] preparingGraph;
        private NestedNode[][] preparingForest;
        private Dictionary<PrefabBase,Payload> preparingReferences;
        private HashSet<ComponentBase> preparingEmptyLanes;
        private int preparingFrame,preparingStable;
        private bool preparingSyncPending,preparingSynced,preparingSourceMatched;
        // An explicit test preparation, using the same native synchronization as a normal edit.
        // This changes only the selected disposable asset in memory; it never saves files.
        private bool BeginNestedTestPreparation(){
            if(nestedFixture.asset==null&&!ordinaryNested)return false;
            try{
                var forest=NestedRoots(AdoptedNestedTypes).Select(root=>CaptureNestedImage(root,false)).ToArray();
                if(forest.Length==0)throw new NotSupportedException("No saved grill group is placed.");
                var asset=context.Asset;var owner=forest[0][0].Owner;
                if(asset is AssetStampPrefab||asset.components.Any(component=>component is ActivityLocation))throw new NotSupportedException("This asset has unsupported preparation components.");
                if(!OwnerMatches(new Record{Asset=asset,Owner=owner})||forest.Any(group=>group[0].Owner!=owner))throw new NotSupportedException("The saved group does not have one selected owner.");
                var em=world.EntityManager;var prefab=world.GetExistingSystemManaged<PrefabSystem>().GetEntity(asset);
                using(var query=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<PrefabRef>(),ComponentType.ReadOnly<Game.Objects.Transform>()},None=new[]{ComponentType.ReadOnly<Temp>(),ComponentType.ReadOnly<Game.Common.Deleted>()}})){
                    if(query.CalculateEntityCount()>100000)throw new NotSupportedException("The scene exceeds the preparation budget.");
                    using(var entities=query.ToEntityArray(Allocator.Temp))if(entities.Count(entity=>em.GetComponentData<PrefabRef>(entity).m_Prefab==prefab)!=1)throw new NotSupportedException("Preparation requires exactly one instance of the disposable outer asset.");
                }
                if(!em.HasComponent<Game.Objects.Object>(owner)||em.HasComponent<SaveInstance>(owner)||((IDictionary)updateMap.GetValue(world.GetExistingSystemManaged<PrefabSystem>())).Contains(asset))throw new NotSupportedException("Another native edit is pending.");
                var payload=Capture(asset);
                var emptyLanes=new HashSet<ComponentBase>(asset.components.Where(component=>component.GetType()==typeof(ObjectSubLanes)&&component.GetType().GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.DeclaredOnly).Length==1&&EmptyPreparationField("ObjectSubLanes","m_SubLanes",((ObjectSubLanes)component).m_SubLanes)));
                if(payload.Weight>250000)throw new NotSupportedException("The test asset exceeds the preparation budget.");
                var graph=CaptureGraph(owner);
                var references=forest.SelectMany(group=>group.Select(node=>node.Prefab)).Distinct().ToDictionary(value=>value,Capture);
                if(references.Values.Sum(value=>value.Weight)>250000||references.Any(pair=>!NestedReferenceMatches(pair.Key,pair.Value)))throw new NotSupportedException("The saved references cannot be captured.");
                Invalidate();
                preparingAsset=asset;preparingOwner=owner;preparingPayload=payload;preparingGraph=graph;preparingForest=forest;preparingReferences=references;
                preparingEmptyLanes=emptyLanes;
                preparingFrame=Time.frameCount;preparingStable=0;preparingStablePayload=null;preparingSyncPending=preparingSynced=preparingSourceMatched=false;
                em.AddComponent<SaveInstance>(owner);
                Status="Preparing the loaded test copy. Wait for Test ready before deleting anything. Nothing is saved to disk.";
                Mod.Log.Info(Status+" asset="+asset.name);
            }catch(Exception error){EndNestedTestPreparation("Preparation stopped: "+error.Message+" Restart without saving before further testing.");}
            return true;
        }
        private void ObservePreparationSync(ApplyPrefabsSystem system){
            if(preparingAsset==null||!ReferenceEquals(system.World,world))return;
            var query=(EntityQuery)saveQuery.GetValue(system);
            using(var entities=query.ToEntityArray(Allocator.Temp))preparingSyncPending=entities.Contains(preparingOwner);
        }
        private void CompletePreparationSync(){if(preparingAsset!=null&&preparingSyncPending){preparingSynced=true;preparingSyncPending=false;Mod.Log.Info("Saved test preparation native synchronization completed.");}}
        private void ObservePreparationReplacement(Entity prefab,Entity source){
            if(preparingAsset==null||world.GetExistingSystemManaged<PrefabSystem>().GetEntity(preparingAsset)!=prefab)return;
            if(source!=preparingOwner)throw new InvalidOperationException("Preparation replacement source changed.");
            preparingSourceMatched=true;
        }
        private void TickNestedTestPreparation(){
            if(preparingAsset==null)return;
            if(!context.EditorAvailable||disabled||(context.Asset!=null&&!ReferenceEquals(context.Asset,preparingAsset))||!OwnerMatches(new Record{Asset=preparingAsset,Owner=preparingOwner})){EndNestedTestPreparation("Preparation stopped because the editor or asset changed. Restart without saving.");return;}
            if(Time.frameCount-preparingFrame>120){EndNestedTestPreparation("Preparation timed out. Restart without saving.");return;}
            if(!preparingSynced||!preparingSourceMatched||world.EntityManager.HasComponent<SaveInstance>(preparingOwner)||((IDictionary)updateMap.GetValue(world.GetExistingSystemManaged<PrefabSystem>())).Contains(preparingAsset)){preparingStable=0;return;}
            if(context.DialogBusy||context.Asset==null){preparingStable=0;return;}
            try{
                var current=Capture(preparingAsset);
                var removed=preparingPayload.Components.Where(component=>!current.Components.Contains(component)).ToArray();
                foreach(var component in removed){
                    var type=component.GetType();
                    if(type!=typeof(ObjectSubAreas)&&type!=typeof(ObjectSubNets)&&type!=typeof(ObjectSubLanes)&&type!=typeof(EffectSource))throw new NotSupportedException("Preparation removed a nonempty or unsupported component.");
                    var fields=preparingPayload.Fields.Where(field=>ReferenceEquals(field.Owner,component)).ToArray();
                    if(type==typeof(ObjectSubLanes)?!preparingEmptyLanes.Contains(component):fields.Length==0||fields.Any(field=>!EmptyPreparationField(type.Name,field.Field.Name,field.Data.Materialize())))throw new NotSupportedException("Preparation removed a component with meaningful data.");
                }
                var retained=preparingPayload.Components.Select((component,index)=>new{component,index}).Where(pair=>!removed.Contains(pair.component)).ToArray();
                if(retained.Length!=current.Components.Length||retained.Where((pair,index)=>!ReferenceEquals(pair.component,current.Components[index])||preparingPayload.Active[pair.index]!=current.Active[index]).Any()||preparingPayload.Objects!=current.Objects||!GraphMatches(preparingGraph,preparingOwner)||preparingReferences.Any(pair=>!NestedReferenceMatches(pair.Key,pair.Value)))throw new NotSupportedException("Object counts, placement, component identity or saved references changed.");
                foreach(var field in preparingPayload.Values){
                    if(preparingPayload.Fields.Any(image=>removed.Contains(image.Owner)&&image.Owner.GetType().Name+"."+image.Field.Name==field.Key))continue;
                    if(!current.Values.TryGetValue(field.Key,out var other))throw new NotSupportedException("A captured field disappeared.");
                    var beforeValue=field.Value.Materialize();var afterValue=other.Materialize();
                    if((beforeValue==null)!=(afterValue==null)||(beforeValue is ICollection beforeList&&(!(afterValue is ICollection afterList)||beforeList.Count!=afterList.Count))||(!(beforeValue is ICollection)&&!field.Value.Matches(afterValue)))throw new NotSupportedException("Preparation changed a list size.");
                }
                var actual=MatchForest(preparingForest,NestedRoots(AdoptedNestedTypes).Select(root=>CaptureNestedImage(root,false)).ToArray());
                if(actual==null||!preparingForest.Select((group,i)=>NestedGroupAtIndex(group,actual[i],actual[i][0].Local.m_PrefabSubIndex)&&group.Select((node,n)=>node.Original==actual[i][n].Original).All(value=>value)).All(value=>value))throw new NotSupportedException("The saved group changed during preparation.");
                var entries=preparingAsset.GetComponent<ObjectSubObjects>().m_SubObjects;
                foreach(var group in actual){var node=group[0];int index=node.Local.m_PrefabSubIndex;if(index<0||index>=entries.Length||!ReferenceEquals(entries[index].m_Object,node.Prefab)||!entries[index].m_Position.Equals(node.Local.m_Position)||!entries[index].m_Rotation.Equals(node.Local.m_Rotation))throw new NotSupportedException("The prepared group descriptor does not match its live cache.");}
                if(preparingStablePayload==null||!preparingStablePayload.Matches(preparingAsset)){preparingStablePayload=current;preparingStable=0;}
                if(++preparingStable<3)return;
                Mod.Log.Info("Saved test preparation verified for three frames; native source owner, component/list counts, direct graph, exact nested typed values/identities and external references unchanged; native cache indices aligned. Original disk asset not saved.");
                EndNestedTestPreparation("Loaded test copy prepared.",true);
                BeginArmedNestedTest();
            }catch(Exception error){EndNestedTestPreparation("Preparation stopped: "+error.Message+" Restart without saving.");}
        }

    }
}

