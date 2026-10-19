using System;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private PrefabBase effectAuditAsset;
        private int effectAuditFrame;
        private void TickEffectParentPreflight() {
            if(!context.EditorAvailable){effectAuditAsset=null;return;}
            var asset=context.Asset;
            if(asset==null||asset.isReadOnly||asset.GetComponent<EffectSource>()==null||Busy||MovePreview||AreaMovePreview||context.DialogBusy)return;
            if(!ReferenceEquals(asset,effectAuditAsset)){effectAuditAsset=asset;effectAuditFrame=Time.frameCount;return;}
            if(effectAuditFrame<0||Time.frameCount-effectAuditFrame<45)return;
            try {
                var em=world.EntityManager;var prefabs=world.GetExistingSystemManaged<PrefabSystem>();
                using(var query=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<PrefabRef>(),ComponentType.ReadOnly<Game.Objects.Transform>(),ComponentType.ReadOnly<Game.Objects.SubObject>()},None=new[]{ComponentType.ReadOnly<Game.Common.Owner>(),ComponentType.ReadOnly<Temp>(),ComponentType.ReadOnly<Game.Common.Deleted>()}}))
                using(var entities=query.ToEntityArray(Allocator.Temp)){
                    var owners=entities.Where(e=>ReferenceEquals(prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(e)),asset)).ToArray();
                    if(owners.Length!=1){effectAuditFrame=-1;Mod.Log.Info("Effect parent preflight unavailable: matching parent instances="+owners.Length+" asset="+asset.name);return;}
                    if(em.HasComponent<Game.Common.Created>(owners[0])||em.HasComponent<Game.Common.Updated>(owners[0]))return;
                    effectAuditFrame=-1;AuditEffectParent(owners[0],asset);
                }
            }catch(Exception error){effectAuditFrame=-1;Mod.Log.Warn("Read-only effect parent preflight stopped: "+error.Message);}
        }
        private void AuditEffectParent(Entity owner,PrefabBase asset) {
            var em=world.EntityManager;int passed=0,failed=0;
            Mod.Log.Info("Effect parent preflight BEGIN (read-only): asset="+asset.name+" owner="+owner);
            try{var graph=CaptureAreaNetwork(owner,false,true);Mod.Log.Info("Effect parent preflight network PASS items="+graph.Items.Length+" ownedObjectAnchors="+graph.Anchors.Length);}
            catch(Exception error){failed++;Mod.Log.Warn("Effect parent preflight network FAIL: "+error.Message);}
            var roots=em.GetBuffer<Game.Objects.SubObject>(owner,true).AsNativeArray().ToArray();
            if(roots.Length>128){Mod.Log.Warn("Effect parent preflight root budget exceeded: "+roots.Length);return;}
            foreach(var link in roots){var id=link.m_SubObject;if(IsEffectContainer(id))continue;
                NestedNode[] group=null;
                try{group=CaptureNestedImage(id,false);OrdinaryGroupSupported(group,true);passed++;}
                catch(Exception error){failed++;Mod.Log.Warn("Effect parent preflight group FAIL root="+id+" reason="+error.Message);
                    if(group!=null)Mod.Log.Info("Effect parent preflight topology supported="+AllowedBoundTopology(group)+" nodes="+group.Length+" duplicatePrefabNodes="+string.Join(",",Enumerable.Range(0,group.Length).Where(i=>Enumerable.Range(0,i).Any(j=>ReferenceEquals(group[i].Prefab,group[j].Prefab))))+" nonDeterministicNodes="+string.Join(",",Enumerable.Range(0,group.Length).Where(i=>(group[i].Parent==-1?(group[i].Local.m_Probability<0||group[i].Local.m_Probability>100):group[i].Local.m_Probability!=100)||group[i].Local.m_GroupIndex!=0)));
                    if(group!=null)foreach(var p in group.Select((n,i)=>new{n,i})){
                        var n=p.n;Mod.Log.Info("Effect parent preflight node["+p.i+"] entity="+n.Original+" owner="+n.Owner+" parent="+n.Parent+" depth="+n.Depth+" prefab="+n.Prefab.name+" probability="+n.Local.m_Probability+" group="+n.Local.m_GroupIndex+" mesh="+n.Local.m_ParentMesh+" index="+n.Local.m_PrefabSubIndex+" position="+n.Local.m_Position+" rotation="+n.Local.m_Rotation);
                        var rows=n.Prefab.GetComponent<ObjectSubObjects>()?.m_SubObjects;
                        if(rows!=null)foreach(var r in rows.Take(32).Select((v,i)=>new{v,i}))Mod.Log.Info("Effect parent preflight child row["+r.i+"] on "+n.Prefab.name+" prefab="+(r.v?.m_Object?.name??"none")+" probability="+r.v?.m_Probability+" group="+r.v?.m_GroupIndex+" mesh="+r.v?.m_ParentMesh+" position="+r.v?.m_Position+" rotation="+r.v?.m_Rotation);
                    }
                    else try{LogOrdinarySchema(id);}catch(Exception schema){Mod.Log.Info("Effect parent schema inventory stopped: "+schema.Message);}
                }
            }
            Mod.Log.Info("Effect parent preflight END asset="+asset.name+" passedGroups="+passed+" failedChecks="+failed+"; diagnostic only, no recovery enabled or state changed.");
        }
    }
}
