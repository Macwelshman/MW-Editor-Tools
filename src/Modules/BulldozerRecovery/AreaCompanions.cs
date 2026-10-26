using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;
using Unity.Mathematics;
using EditorContainer=Game.Tools.EditorContainer;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private AreaCompanionNetwork areaNetwork;
        private AreaCompanionEffect[] areaEffects;
        private NestedNode[][] areaProps;
        private Dictionary<PrefabBase,Payload> areaReferences;
        private void ClearAreaCompanions(){areaNetwork=null;areaEffects=null;areaProps=null;areaReferences=null;}
        private NestedNode[][] CaptureAreaProps(Entity owner) {
            var em=world.EntityManager;var result=new List<NestedNode[]>();
            if(!em.HasBuffer<Game.Objects.SubObject>(owner))return result.ToArray();
            var prefabs=world.GetExistingSystemManaged<PrefabSystem>();
            foreach(var child in em.GetBuffer<Game.Objects.SubObject>(owner,true)) {
                var id=child.m_SubObject;if(!em.Exists(id)||em.HasComponent<Game.Common.Deleted>(id))continue;
                if(em.HasComponent<EditorContainer>(id)&&em.HasComponent<EffectData>(em.GetComponentData<EditorContainer>(id).m_Prefab))continue;
                if(result.Count>=128)throw new NotSupportedException("Area companion props exceed the 128-root capture budget.");
                var group=CaptureNestedImage(id,false);OrdinaryGroupSupported(group,true);
                if(group[0].Owner!=owner)throw new NotSupportedException("Area companion prop owner changed.");
                result.Add(group);
            }
            var forest=result.ToArray();
            if(forest.SelectMany(g=>g.Select(n=>n.Original)).Distinct().Count()!=forest.Sum(g=>g.Length)||MatchForest(forest,forest)==null)
                throw new NotSupportedException("Area companion props share nodes or placements.");
            return forest;
        }
        private AreaCompanionEffect[] CaptureAreaEffects(Entity owner) {
            var em=world.EntityManager;var result=new List<AreaCompanionEffect>();var prefabs=world.GetExistingSystemManaged<PrefabSystem>();
            if(!em.HasBuffer<Game.Objects.SubObject>(owner))return result.ToArray();
            foreach(var child in em.GetBuffer<Game.Objects.SubObject>(owner,true)) {
                var id=child.m_SubObject;if(!em.Exists(id)||em.HasComponent<Game.Common.Deleted>(id)||!em.HasComponent<EditorContainer>(id))continue;
                var container=em.GetComponentData<EditorContainer>(id);var prefab=prefabs.GetPrefab<PrefabBase>(container.m_Prefab);
                if(!em.HasComponent<EffectData>(container.m_Prefab))continue;
                if(result.Count>=128||!em.HasComponent<Game.Common.Owner>(id)||em.GetComponentData<Game.Common.Owner>(id).m_Owner!=owner||
                    (em.HasBuffer<Game.Objects.SubObject>(id)&&em.GetBuffer<Game.Objects.SubObject>(id,true).Length!=0))throw new NotSupportedException("Unsupported effect companion ownership or children.");
                var value=new AreaCompanionEffect{Original=id,Prefab=em.HasComponent<PrefabRef>(id)?prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(id)):null,Container=prefab,
                    Scale=container.m_Scale,Intensity=container.m_Intensity,Group=container.m_GroupIndex,HasTransform=em.HasComponent<Game.Objects.Transform>(id)};
                if(value.HasTransform){var t=em.GetComponentData<Game.Objects.Transform>(id);value.Position=t.m_Position;value.Rotation=t.m_Rotation;}
                value.HasLocal=em.HasComponent<LocalTransformCache>(id);
                if(value.HasLocal){value.Local=AreaCompanionEffect.DetachedLocal(em.GetComponentData<LocalTransformCache>(id));}
                value.HasElevation=em.HasComponent<Game.Objects.Elevation>(id);
                if(value.HasElevation)value.Elevation=em.GetComponentData<Game.Objects.Elevation>(id);
                if(!math.all(math.isfinite(value.Scale))||!math.isfinite(value.Intensity)||!math.all(math.isfinite(value.Position))||!math.all(math.isfinite(value.Rotation.value)))throw new NotSupportedException("Nonfinite effect companion geometry.");
                result.Add(value);
            }
            return result.ToArray();
        }
        private AreaCompanionNetwork CaptureAreaNetwork(Entity owner,bool allowInvalidLocal=false,bool objectAnchors=true) {
            var em=world.EntityManager;var result=new AreaCompanionNetwork{Owner=owner};var pending=new Queue<Entity>();var seen=new HashSet<Entity>();int linkCount=0,connectionCount=0;
            Action<Entity> links=id=> {
                linkCount+=(em.HasBuffer<Game.Net.SubNet>(id)?em.GetBuffer<Game.Net.SubNet>(id,true).Length:0)+(em.HasBuffer<Game.Net.SubLane>(id)?em.GetBuffer<Game.Net.SubLane>(id,true).Length:0);
                if((em.HasBuffer<Game.Net.SubNet>(id)&&em.GetBuffer<Game.Net.SubNet>(id,true).Length>1024)||
                    (em.HasBuffer<Game.Net.SubLane>(id)&&em.GetBuffer<Game.Net.SubLane>(id,true).Length>1024)||linkCount>8192)throw new NotSupportedException("Network companion link budget exceeded.");
                if(em.HasBuffer<Game.Net.SubNet>(id))foreach(var link in em.GetBuffer<Game.Net.SubNet>(id,true))pending.Enqueue(link.m_SubNet);
                if(em.HasBuffer<Game.Net.SubLane>(id))foreach(var link in em.GetBuffer<Game.Net.SubLane>(id,true))pending.Enqueue(link.m_SubLane);
            };
            links(owner);var items=new List<AreaCompanionNetwork.Item>();var prefabs=world.GetExistingSystemManaged<PrefabSystem>();
            if(em.HasBuffer<Game.Net.SubNet>(owner))result.RootNets=em.GetBuffer<Game.Net.SubNet>(owner,true).AsNativeArray().ToArray().Select(n=>n.m_SubNet).ToArray();
            if(em.HasBuffer<Game.Net.SubLane>(owner))result.RootLanes=em.GetBuffer<Game.Net.SubLane>(owner,true).AsNativeArray().ToArray();
            while(pending.Count>0) {
                var id=pending.Dequeue();if(id==Entity.Null||seen.Contains(id))continue;
                if(!em.Exists(id)||em.HasComponent<Game.Common.Deleted>(id))throw new NotSupportedException("A network companion is missing or pending deletion.");
                if(seen.Count>=1024||id==owner||!em.HasComponent<Game.Common.Owner>(id)||!em.HasComponent<PrefabRef>(id))throw new NotSupportedException("Unsupported network companion ownership or budget.");
                seen.Add(id);var item=new AreaCompanionNetwork.Item{Id=id,Owner=em.GetComponentData<Game.Common.Owner>(id).m_Owner,Prefab=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(id)),
                    HasParking=em.HasComponent<Game.Net.ParkingLane>(id),HasConnection=em.HasComponent<Game.Net.ConnectionLane>(id),HasSecondary=em.HasComponent<Game.Net.SecondaryLane>(id),HasNode=em.HasComponent<Game.Net.Node>(id),HasEdge=em.HasComponent<Game.Net.Edge>(id),HasLane=em.HasComponent<Game.Net.Lane>(id),HasCurve=em.HasComponent<Game.Net.Curve>(id),
                    HasLocal=em.HasComponent<LocalCurveCache>(id),HasContainer=em.HasComponent<EditorContainer>(id),HasSeed=em.HasComponent<Game.Common.PseudoRandomSeed>(id)};
                if(item.Prefab==null||(!item.HasNode&&!item.HasEdge&&!item.HasLane))throw new NotSupportedException("Unsupported network companion schema.");
                if(item.HasNode){item.Node=em.GetComponentData<Game.Net.Node>(id);if(!math.all(math.isfinite(item.Node.m_Position))||!math.all(math.isfinite(item.Node.m_Rotation.value)))throw new NotSupportedException("Nonfinite network node.");}
                if(item.HasEdge){item.Edge=em.GetComponentData<Game.Net.Edge>(id);pending.Enqueue(item.Edge.m_Start);pending.Enqueue(item.Edge.m_End);}
                if(item.HasConnection)item.Connection=em.GetComponentData<Game.Net.ConnectionLane>(id);
                if(item.HasParking){item.Parking=em.GetComponentData<Game.Net.ParkingLane>(id);if(!math.isfinite(item.Parking.m_FreeSpace))throw new NotSupportedException("Nonfinite parking lane data.");}
                item.HasGarage=em.HasComponent<Game.Net.GarageLane>(id);if(item.HasGarage)item.Garage=em.GetComponentData<Game.Net.GarageLane>(id);
                item.HasCar=em.HasComponent<Game.Net.CarLane>(id);if(item.HasCar)item.Car=em.GetComponentData<Game.Net.CarLane>(id);
                item.HasPedestrian=em.HasComponent<Game.Net.PedestrianLane>(id);if(item.HasPedestrian)item.Pedestrian=em.GetComponentData<Game.Net.PedestrianLane>(id);
                item.HasSlave=em.HasComponent<Game.Net.SlaveLane>(id);if(item.HasSlave)item.Slave=em.GetComponentData<Game.Net.SlaveLane>(id);
                if(item.HasLane)item.Lane=em.GetComponentData<Game.Net.Lane>(id);
                if(item.HasCurve){item.Curve=em.GetComponentData<Game.Net.Curve>(id);if(!FiniteCurve(item.Curve.m_Bezier)||!math.isfinite(item.Curve.m_Length))throw new NotSupportedException("Nonfinite network curve.");}
                if(item.HasLocal){item.Local=em.GetComponentData<LocalCurveCache>(id);if(!allowInvalidLocal&&!FiniteCurve(item.Local.m_Curve))throw new NotSupportedException("Nonfinite local network curve.");}
                if(item.HasContainer){item.ContainerData=em.GetComponentData<EditorContainer>(id);item.Container=prefabs.GetPrefab<PrefabBase>(item.ContainerData.m_Prefab);if(item.Container==null||!math.all(math.isfinite(item.ContainerData.m_Scale))||!math.isfinite(item.ContainerData.m_Intensity))throw new NotSupportedException("Invalid network container.");}
                if(item.HasSeed)item.Seed=em.GetComponentData<Game.Common.PseudoRandomSeed>(id);
                if(em.HasBuffer<Game.Net.ConnectedEdge>(id)){
                    var edges=em.GetBuffer<Game.Net.ConnectedEdge>(id,true);connectionCount+=edges.Length;if(edges.Length>2048||connectionCount>8192)throw new NotSupportedException("Network connection budget exceeded.");
                    item.Connections=edges.AsNativeArray().ToArray().Select(e=>e.m_Edge).ToArray();
                }
                items.Add(item);links(id);
                if(em.HasBuffer<Game.Net.SubNet>(id))item.SubNets=em.GetBuffer<Game.Net.SubNet>(id,true).AsNativeArray().ToArray().Select(n=>n.m_SubNet).ToArray();
                if(em.HasBuffer<Game.Net.SubLane>(id))item.SubLanes=em.GetBuffer<Game.Net.SubLane>(id,true).AsNativeArray().ToArray();
            }
            result.Items=items.ToArray();
            if(objectAnchors)CaptureNetworkObjectAnchors(result);
            if(!result.TryMap(result,true,out _,!allowInvalidLocal))throw new NotSupportedException("Ambiguous or externally connected network companions: "+result.ExplainMismatch(result,true,!allowInvalidLocal));
            return result;
        }
        private static bool FiniteCurve(Colossal.Mathematics.Bezier4x3 curve)=>math.all(math.isfinite(curve.a))&&math.all(math.isfinite(curve.b))&&math.all(math.isfinite(curve.c))&&math.all(math.isfinite(curve.d));
        private bool AreaCompanionsMatch(Record entry,bool seeds=true) {
            try{
                var effects=CaptureAreaEffects(entry.Owner);var network=CaptureAreaNetwork(entry.Owner);
                return AreaCompanionEffect.Matches(entry.AreaEffects,effects)&&entry.AreaNetwork.TryMap(network,seeds,out _)&&
                    CompanionSourcesValid((entry.AreaEffectsFrom??entry.AreaEffects).Select(e=>e.Original),effects.Select(e=>e.Original))&&
                    CompanionSourcesValid((entry.AreaNetworkFrom??entry.AreaNetwork).Items.Select(i=>i.Id),network.Items.Select(i=>i.Id));
            }
            catch(NotSupportedException){return false;}
        }
        private bool CompanionSourcesValid(IEnumerable<Entity> sources,IEnumerable<Entity> live) {
            return AreaCompanionNetwork.SourcesValid(sources,live,id=>world.EntityManager.Exists(id));
        }
        private bool AreaLiveCompanionsMatch() {
            var actual=CaptureAreaProps(observedOwner);var mapped=MatchForest(areaProps,actual);
            return mapped!=null&&areaProps.Select((g,i)=>NestedGroupAtIndex(g,mapped[i],BoundDescriptorIndex.Resolve(TargetRoot?.GetComponent<ObjectSubObjects>()?.m_SubObjects,g[0].Prefab,g[0].Local))).All(v=>v)&&
                !world.EntityManager.Exists(deletedArea)&&CompanionSourcesValid(beforeAreas.Where(a=>a.Original!=deletedArea).Select(a=>a.Original),CaptureAreas(observedOwner).Select(a=>a.Original))&&
                CompanionSourcesValid(areaProps.SelectMany(GroupSources),actual.SelectMany(GroupSources))&&
                areaReferences.All(p=>NestedReferenceMatches(p.Key,p.Value))&&AreaCompanionsMatch(new Record{Owner=observedOwner,AreaEffects=areaEffects,AreaNetwork=areaNetwork});
        }
        private bool RestoreAreaCompanionSeeds(Record entry) {
            try {
            if(!AreaCompanionsMatch(entry,false))return false;
            var actual=CaptureAreaNetwork(entry.Owner);
            if(!entry.AreaNetwork.TryMap(actual,false,out var map))return false;
            foreach(var item in entry.AreaNetwork.Items.Where(i=>i.HasSeed))world.EntityManager.SetComponentData(map[item.Id],item.Seed);
            return true;
            }catch(NotSupportedException){return false;}
        }
    }
}
