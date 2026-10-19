using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        // PathNode stores an entity index, so resolve it only through actual
        // SubObject membership and complete Owner chains, never an invented version.
        private void CaptureNetworkObjectAnchors(AreaCompanionNetwork graph) {
            var known=new HashSet<int>(graph.Items.Select(i=>i.Id.Index)){graph.Owner.Index};
            var missing=graph.Items.Where(i=>i.HasLane).SelectMany(i=>new[]{i.Lane.m_StartNode.GetOwnerIndex(),i.Lane.m_MiddleNode.GetOwnerIndex(),i.Lane.m_EndNode.GetOwnerIndex()})
                .Where(i=>i!=0&&!known.Contains(i)).Distinct().ToArray();
            if(missing.Length==0)return;
            var em=world.EntityManager;var prefabs=world.GetExistingSystemManaged<PrefabSystem>();
            var all=new Dictionary<Entity,AreaCompanionNetwork.Anchor>();var pending=new Queue<Tuple<Entity,int>>();pending.Enqueue(Tuple.Create(graph.Owner,0));var seen=new HashSet<Entity>{graph.Owner};
            while(pending.Count>0){var parent=pending.Dequeue();if(!em.HasBuffer<Game.Objects.SubObject>(parent.Item1))continue;
                foreach(var child in em.GetBuffer<Game.Objects.SubObject>(parent.Item1,true)){
                    var id=child.m_SubObject;
                    if(!em.Exists(id)||em.HasComponent<Game.Common.Deleted>(id)||em.HasComponent<Game.Common.Created>(id)||em.HasComponent<Game.Common.Updated>(id)||em.HasComponent<Temp>(id)||!em.HasComponent<Game.Common.Owner>(id)||em.GetComponentData<Game.Common.Owner>(id).m_Owner!=parent.Item1)throw new NotSupportedException("Network path anchor has pending/missing or inconsistent object membership.");
                    if(!seen.Add(id)||seen.Count>1024||parent.Item2>=8)throw new NotSupportedException("Network path anchor object graph budget/cycle refusal.");
                    if(IsEffectContainer(id))continue;
                    if(!em.HasComponent<Game.Objects.Object>(id)||!em.HasComponent<Game.Objects.Transform>(id)||!em.HasComponent<PrefabRef>(id))continue;
                    var anchor=new AreaCompanionNetwork.Anchor{Id=id,Owner=parent.Item1,Prefab=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(id)),Transform=em.GetComponentData<Game.Objects.Transform>(id),HasLocal=em.HasComponent<LocalTransformCache>(id)};
                    if(anchor.Prefab==null)throw new NotSupportedException("Network path anchor prefab unavailable.");
                    if(anchor.HasLocal)anchor.Local=AreaCompanionEffect.DetachedLocal(em.GetComponentData<LocalTransformCache>(id));
                    all.Add(id,anchor);pending.Enqueue(Tuple.Create(id,parent.Item2+1));
                }
            }
            var selected=new Dictionary<Entity,AreaCompanionNetwork.Anchor>();
            foreach(var index in missing){var matches=all.Values.Where(a=>a.Id.Index==index).ToArray();
                if(matches.Length!=1)throw new NotSupportedException("Network lane path owner index "+index+" is not uniquely inside the edited parent's owned object graph.");
                var anchor=matches[0];while(anchor.Id!=graph.Owner){if(selected.ContainsKey(anchor.Id))break;selected.Add(anchor.Id,anchor);
                    if(anchor.Owner==graph.Owner)break;if(!all.TryGetValue(anchor.Owner,out anchor))throw new NotSupportedException("Network object path anchor has an external owner.");}
            }
            graph.Anchors=selected.Values.ToArray();
        }
    }
}
