using System;
using System.Linq;
using System.Collections.Generic;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        internal void InspectNestedFixture(){
            if(!context.EditorAvailable||context.DialogBusy||Busy)return;
            Guard(()=>{
                if(nestedFixture==null&&!UseSavedNestedFixture())return;
                var em=world.EntityManager;var prefabs=world.GetExistingSystemManaged<PrefabSystem>();
                var prefab=prefabs.GetEntity(nestedFixture);var roots=new List<Entity>();
                using(var query=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<PrefabRef>(),ComponentType.ReadOnly<Game.Objects.Transform>()},None=new[]{ComponentType.ReadOnly<Temp>(),ComponentType.ReadOnly<Game.Common.Deleted>()}})){
                    if(query.CalculateEntityCount()>100000){Status="Fixture scene query exceeds diagnostic budget.";return;}
                    using(var entities=query.ToEntityArray(Allocator.Temp))foreach(var entity in entities)
                        if(em.GetComponentData<PrefabRef>(entity).m_Prefab==prefab){roots.Add(entity);if(roots.Count>=8)break;}
                }
                if(roots.Count==0){Status="No placed nested fixture found. Place it once, Escape, then inspect.";return;}
                Mod.Log.Info("Nested read-only graph inspection rootPrefab="+nestedFixture.name+" sceneRoots="+roots.Count+"; roots capped8, traversal capped64.");
                var queue=new Queue<Entity>(roots);var seen=new HashSet<Entity>();
                while(queue.Count>0&&seen.Count<64){
                    var entity=queue.Dequeue();if(!seen.Add(entity))continue;
                    LogMainEntity("nested inspection",entity,true);
                    if(!em.Exists(entity)||!em.HasBuffer<Game.Objects.SubObject>(entity))continue;
                    var children=em.GetBuffer<Game.Objects.SubObject>(entity,true);
                    for(int i=0;i<children.Length&&i<64;i++){
                        var child=children[i].m_SubObject;
                        Mod.Log.Info("Nested read-only edge parent="+entity+" child="+child+" index="+i);
                        if(!seen.Contains(child)&&queue.Count<64)queue.Enqueue(child);
                    }
                }
                var tool=world.GetExistingSystemManaged<ToolSystem>()?.activeTool as BulldozeToolSystem;
                Mod.Log.Info("Nested inspection bulldozerMode="+(tool==null?"inactive":tool.actualMode.ToString()));
                Status="Nested fixture inspected; "+seen.Count+" entities logged. Nothing deleted; use the separate arm button for the temporary group test.";Mod.Log.Info(Status);
            });
        }
        private bool UseSavedNestedFixture(){
            if(!CanArm||context.Asset==null){Status="Select the saved disposable outer asset first.";return false;}
            var entries=context.Asset.GetComponent<ObjectSubObjects>()?.m_SubObjects;
            if((entries?.Where(value=>value?.m_Object is ObjectPrefab&&(value.m_Object.GetComponent<ObjectSubObjects>()?.m_SubObjects?.Length??0)>0).Select(value=>value.m_Object).Distinct().Count()??0)>1)return false;
            var candidates=entries?.Where(value=>value?.m_Object!=null&&value.m_Object.name.StartsWith("MW Nested Fixture ",StringComparison.Ordinal)).Select(value=>value.m_Object).Distinct().ToArray();
            if(candidates==null||candidates.Length!=1){Status="Inspection requires one saved fixture prefab identity in the selected asset.";return false;}
            var root=candidates[0] as ObjectPrefab;
            var parentLinks=root?.GetComponent<ObjectSubObjects>()?.m_SubObjects;
            var parent=parentLinks?.Length==1?parentLinks[0]?.m_Object as ObjectPrefab:null;
            var leafLinks=parent?.GetComponent<ObjectSubObjects>()?.m_SubObjects;
            var leaf=leafLinks?.Length==1?leafLinks[0]?.m_Object:null;
            if(root==null||parent==null||leaf==null||root.asset==null||parent.asset==null||!parent.name.StartsWith("MW Nested Parent ",StringComparison.Ordinal)||root.name.Substring("MW Nested Fixture ".Length)!=parent.name.Substring("MW Nested Parent ".Length)||leaf.name!="Grill01"||!leaf.isReadOnly||(leaf.GetComponent<ObjectSubObjects>()?.m_SubObjects?.Length??0)!=0){Status="Saved fixture references or three-level schema did not match; recovery remains unarmed.";return false;}
            var groups=NestedRoots(root).Select(entity=>CaptureNestedImage(entity,false)).ToArray();
            var owner=groups.Length==0?Entity.Null:groups[0][0].Owner;
            if(groups.Length<1||!OwnerMatches(new Record{Asset=context.Asset,Owner=owner})||!MW.EditorTools.History.PrefabSnapshot.Take(new[]{NestedLink(parent)}).Matches(parentLinks)||!MW.EditorTools.History.PrefabSnapshot.Take(new[]{NestedLink(leaf)}).Matches(leafLinks)||groups.Any(group=>group.Length!=3||group[0].Owner!=owner||group[1].Parent!=0||group[2].Parent!=1||!ReferenceEquals(group[0].Prefab,root)||!ReferenceEquals(group[1].Prefab,parent)||!ReferenceEquals(group[2].Prefab,leaf))||groups.SelectMany(group=>group.Select(node=>node.Original)).Distinct().Count()!=groups.Length*3||MatchForest(groups,groups)==null){Status="Saved fixture live topology or placement did not match; recovery remains unarmed.";return false;}
            foreach(var prefab in new PrefabBase[]{root,parent,leaf})if(!NestedReferenceMatches(prefab,Capture(prefab))){Status="Saved fixture reference capture failed; recovery remains unarmed.";return false;}
            nestedFixture=root;ordinaryNested=false;
            Mod.Log.Info("Saved nested fixture adopted after read-only schema/reference/typed graph checks; root="+root.name+" parent="+parent.name+" leaf="+leaf.name+" groups="+groups.Length+" nodes="+groups.Length*3+"; recovery remains unarmed.");
            return true;
        }
    }
}
