using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private sealed class BoundCompanions {
            internal Entity Owner,OwnerPrefab;
            internal Dictionary<Entity,AreaCompanionNetwork> AreaLanes=new Dictionary<Entity,AreaCompanionNetwork>();
            internal AreaImage[] Areas;
            internal AreaCompanionEffect[] Effects;
            internal AreaCompanionNetwork Network;
            internal Game.Objects.Transform Transform;
            internal Dictionary<PrefabBase,Payload> References;
            internal BoundCompanions WithoutArea(Entity id){var copy=(BoundCompanions)MemberwiseClone();copy.Areas=Areas.Where(a=>a.Original!=id).ToArray();copy.AreaLanes=AreaLanes.Where(p=>p.Key!=id).ToDictionary(p=>p.Key,p=>p.Value);return copy;}
            internal BoundCompanions WithNetwork(AreaCompanionNetwork network){var copy=(BoundCompanions)MemberwiseClone();copy.Network=network;return copy;}
            internal int Weight=>AreaWeight(Areas)+96*Effects.Length+BoundNetworkItems(this).Sum(i=>192+16*(i.Connections.Length+i.SubNets.Length+i.SubLanes.Length))+References.Values.Sum(p=>p.Weight);
        }
        private BoundCompanions boundBefore,boundAfter;
        private NestedNode[][] boundForestAfter;
        private readonly BoundCompletionGate boundCompletion=new BoundCompletionGate();
        private sealed class BoundWrite {internal NestedNode Expected,Actual;internal int Index;internal bool NetworkSeed;internal Entity Target;internal Game.Common.PseudoRandomSeed BeforeSeed,AfterSeed;}
        private bool boundValuesRestored;
        private string boundRefusal;
        private SpawnAccessImage[] boundConnectionCheckpoint;
        private void ClearBoundCapture(){ClearSpawnDeletionCapture();boundBefore=boundAfter=null;boundForestAfter=null;boundCompletion.Reset();boundValuesRestored=false;boundRefusal=null;boundConnectionCheckpoint=null;}
        private BoundCompanions CaptureBoundCompanions(Entity owner,bool areaLanes=false,bool allowInvalidNetworkLocal=false){
            var em=world.EntityManager;
            var image=new BoundCompanions{Owner=owner,OwnerPrefab=em.GetComponentData<PrefabRef>(owner).m_Prefab,Areas=CaptureAreas(owner),Effects=CaptureAreaEffects(owner),Network=CaptureAreaNetwork(owner,allowInvalidNetworkLocal),Transform=em.GetComponentData<Game.Objects.Transform>(owner)};
            if(areaLanes||areaCapture)image.AreaLanes=CaptureAreaLaneImages(image.Areas,allowInvalidNetworkLocal);
            image.References=image.Areas.SelectMany(a=>new[]{a.Prefab,a.Container}).Concat(image.Effects.SelectMany(e=>new[]{e.Prefab,e.Container})).Concat(image.Network.Items.SelectMany(i=>new[]{i.Prefab,i.Container})).Concat(image.Network.Anchors.Select(a=>a.Prefab)).Concat(image.AreaLanes.Values.SelectMany(g=>g.Items).SelectMany(i=>new[]{i.Prefab,i.Container})).Where(p=>p!=null).Distinct().ToDictionary(p=>p,Capture);
            if(image.Weight>250000)throw new NotSupportedException("Bound companion snapshot exceeds budget.");
            return image;
        }
        private bool BoundCompanionsMap(BoundCompanions expected,BoundCompanions actual,Entity owner,out Dictionary<Entity,Entity> map,PrefabBase asset,bool seeds=true){
            map=null;
            bool valid=expected!=null&&actual!=null&&expected.Owner==owner&&actual.Owner==owner&&expected.OwnerPrefab!=Entity.Null&&actual.OwnerPrefab!=Entity.Null&&OwnerMatches(new Record{Owner=owner,Asset=asset})&&expected.Transform.Equals(actual.Transform)&&AreasMatch(expected.Areas,owner)&&
                CompanionSourcesValid(expected.Areas.Select(a=>a.Original),actual.Areas.Select(a=>a.Original))&&AreaCompanionEffect.Matches(expected.Effects,actual.Effects)&&
                CompanionSourcesValid(expected.Effects.Select(e=>e.Original),actual.Effects.Select(e=>e.Original))&&
                CompanionSourcesValid(expected.Network.Items.Select(i=>i.Id),actual.Network.Items.Select(i=>i.Id))&&
                expected.References.All(p=>NestedReferenceMatches(p.Key,p.Value))&&expected.Network.TryMap(actual.Network,seeds,out map);
            if(valid){
                map=EntityReferenceMap.WithOwnerPrefab(map,owner,expected.OwnerPrefab,actual.OwnerPrefab);
                valid=MapAreaLanes(expected,actual,map,seeds);
            }
            return valid;
        }
        // Every added mapping comes from complete typed graph/placement matches.
        // Conflicting/reused identities are refused rather than overwritten.
        private static bool AddForestMap(NestedNode[][] expected,NestedNode[][] actual,Dictionary<Entity,Entity> map){
            if(expected.Length!=actual.Length)return false;
            for(int g=0;g<expected.Length;g++){
                if(expected[g].Length!=actual[g].Length)return false;
                for(int n=0;n<expected[g].Length;n++){
                    var e=expected[g][n];var a=actual[g][n];
                    if(e.Parent!=a.Parent||e.Depth!=a.Depth||!ReferenceEquals(e.Prefab,a.Prefab)||!SameNestedPlacement(e,a)||!EntityReferenceMap.Add(map,e.Original,a.Original))return false;
                    if(e.Lanes.Graph!=null){if(a.Lanes.Graph==null||!e.Lanes.Graph.TryMap(a.Lanes.Graph,true,out var lanes))return false;foreach(var pair in lanes)if(!EntityReferenceMap.Add(map,pair.Key,pair.Value))return false;}
                }
            }
            return true;
        }
        private string retainedReferenceRefusal;
        private bool ReferenceMapBlocked(string reason){if(reason!=retainedReferenceRefusal){retainedReferenceRefusal=reason;Mod.Log.Info("Retained reference map refused: "+reason);}return false;}
        private bool BoundRecordMap(Record entry,bool beforeSide,NestedNode[][] expected,NestedNode[][] actual,out Dictionary<Entity,Entity> map){
            map=null;var image=beforeSide?entry.BoundBefore:entry.BoundAfter;
            if(image==null){
                if(entry.OwnerPrefab==Entity.Null)return true;
                if(!OwnerMatches(new Record{Asset=entry.Asset,Owner=entry.Owner}))return ReferenceMapBlocked("owner no longer resolves to the guarded editable asset");
                map=EntityReferenceMap.WithOwnerPrefab(new Dictionary<Entity,Entity>(),entry.Owner,entry.OwnerPrefab,world.EntityManager.GetComponentData<PrefabRef>(entry.Owner).m_Prefab);
                if(entry.ReferenceNetwork!=null){
                    var network=CaptureAreaNetwork(entry.Owner,true,entry.ReferenceNetwork.Anchors.Length>0);
                    if(!NetworkSettled(network))return ReferenceMapBlocked("root network creation/update is still pending");
                    if(!entry.ReferenceNetwork.TryMap(network,false,out var networkMap,false,true,entry.ReferenceRebuiltObjects,entry.ReferenceMarkerOwner))return ReferenceMapBlocked("root network: "+entry.ReferenceNetwork.ExplainMismatch(network,false,false,true,entry.ReferenceRebuiltObjects,entry.ReferenceMarkerOwner));
                    if(entry.ReferenceNetwork.Items.Any(i=>i.HasLocal&&!AreaCompanionNetwork.CanRestoreLocal(i.Local,network.Items.Single(a=>a.Id==networkMap[i.Id]).Local)))return ReferenceMapBlocked("root network has a changed finite cache or nonfinite recorded cache");
                    foreach(var pair in networkMap)if(!EntityReferenceMap.Add(map,pair.Key,pair.Value))return false;
                    EntityReferenceMap.CopyDirections(networkMap,map);
                }
                if(entry.ReferenceAreas!=null&&!AreaReferenceMap(entry.ReferenceAreas,entry.Owner,map,entry.ReferenceNetwork!=null,false))return ReferenceMapBlocked("area geometry or generated area-lane graph differs");
                return AddForestMap(expected,actual,map);
            }
            var live=CaptureBoundCompanions(entry.Owner,image.AreaLanes.Count>0);
            return BoundLiveSettled(live,entry.Owner,actual.SelectMany(GroupSources))&&BoundCompanionsMap(image,live,entry.Owner,out map,entry.Asset)&&AddForestMap(expected,actual,map);
        }
        private bool BoundRecordCompanionsMatch(Record entry,bool beforeSide,bool seeds=true){
            var image=beforeSide?entry.BoundBefore:entry.BoundAfter;
            if(image==null)return true;
            try{var live=CaptureBoundCompanions(entry.Owner,image.AreaLanes.Count>0);return BoundLiveSettled(live,entry.Owner,Enumerable.Empty<Entity>())&&BoundCompanionsMap(image,live,entry.Owner,out _,entry.Asset,seeds);}catch(NotSupportedException){return false;}
        }
        private bool RestoreBoundNetworkSeeds(Record entry,bool beforeSide){
            var expected=beforeSide?entry.BoundBefore:entry.BoundAfter;if(expected==null)return true;
            var live=CaptureBoundCompanions(entry.Owner,expected.AreaLanes.Count>0);
            if(!BoundLiveSettled(live,entry.Owner,Enumerable.Empty<Entity>())||!BoundCompanionsMap(expected,live,entry.Owner,out var map,entry.Asset,false))return false;
            var em=world.EntityManager;
            var writes=BoundNetworkItems(expected).Where(i=>i.HasSeed).Select(i=>new BoundWrite{Target=map[i.Id],AfterSeed=i.Seed,BeforeSeed=BoundNetworkItems(live).Single(a=>a.Id==map[i.Id]).Seed}).Where(w=>!w.BeforeSeed.Equals(w.AfterSeed)).ToArray();
            return RetainedValueTransaction.Apply(writes,p=>em.GetComponentData<Game.Common.PseudoRandomSeed>(p.Target).Equals(p.BeforeSeed),p=>em.SetComponentData(p.Target,p.AfterSeed),p=>em.SetComponentData(p.Target,p.BeforeSeed));
        }
        private bool BoundLiveSettled(BoundCompanions current,Entity owner,IEnumerable<Entity> props){
            var em=world.EntityManager;
            var live=props.Concat(current.Areas.Select(a=>a.Original)).Concat(current.Effects.Select(e=>e.Original)).Concat(BoundNetworkItems(current).Select(i=>i.Id)).Concat(current.Network.Anchors.Select(a=>a.Id)).Concat(new[]{owner}).Distinct();
            return live.All(id=>em.Exists(id)&&!em.HasComponent<Game.Common.Deleted>(id)&&!em.HasComponent<Game.Common.Created>(id)&&!em.HasComponent<Game.Common.Updated>(id)&&!em.HasComponent<Game.Tools.Temp>(id));
        }
        private static bool RetainedValuesPreflight(NestedNode e,NestedNode a,int rootIndex,Dictionary<Entity,Entity> map){
            var local=e.Local;if(e.Parent==-1)local.m_PrefabSubIndex=rootIndex;
            var unchanged=new MainPropData(e.Data.Transform,e.Data.Seed,a.Data.Surface,e.Data.Colours,a.Data.CustomColours);
            return e.Parent==a.Parent&&e.Depth==a.Depth&&ReferenceEquals(e.Prefab,a.Prefab)&&e.HasSurface==a.HasSurface&&SameNestedElevation(e,a,false,map)&&local.Equals(a.Local)&&SameNestedPlacement(e,a)&&e.Data.Matches(unchanged)&&
                ((e.Data.Colours==null)==(a.Data.Colours==null))&&(e.Data.Colours?.Length??0)==(a.Data.Colours?.Length??0);
        }
        private void PutRetainedValues(NestedNode expected,Entity id){
            var em=world.EntityManager;em.SetComponentData(id,expected.Data.Seed);em.SetComponentData(id,expected.Data.Transform);
            if(expected.HasColor)em.SetComponentData(id,expected.Color);if(expected.HasDecoration)em.SetComponentEnabled<Game.Objects.Decoration>(id,expected.DecorationEnabled);
            if(expected.HasPlant)em.SetComponentData(id,expected.Plant);if(expected.HasTree)em.SetComponentData(id,expected.Tree);
            if(expected.Data.Colours!=null){var buffer=em.GetBuffer<Game.Rendering.MeshColor>(id);for(int i=0;i<expected.Data.Colours.Length;i++)buffer[i]=expected.Data.Colours[i];}
        }
        private bool BoundCaptureReady(){
            if(boundBefore==null)return true;
            try{
                var references=areaCapture?areaReferences:nestedReferences;
                if(after==null||!session.Matches(TargetRoot)||!after.Matches(session.Asset)||!OwnerMatches(new Record{Asset=session.Asset,Owner=observedOwner})||references==null||!references.All(p=>NestedReferenceMatches(p.Key,p.Value)))return BoundWait("source, owner or prefab references changed");
                var current=CaptureBoundCompanions(observedOwner,boundBefore.AreaLanes.Count>0);
                var expectedCompanions=areaCapture?boundBefore.WithoutArea(deletedArea):boundBefore;
                if(!areaCapture&&nestedBefore.Any(n=>n.SpawnAccess.Present)){
                    var network=ProjectSpawnDeletion(boundBefore.Network,current.Network,nestedBefore.Select(n=>n.Original));
                    if(network==null)return BoundWait(spawnDeletionRefusal??"deleted spawn/access object's generated connection closure differs");
                    expectedCompanions=boundBefore.WithNetwork(network);
                }
                if(!BoundCompanionsMap(expectedCompanions,current,observedOwner,out var map,session.Asset,false))return BoundWait("area/effect/network companion graph is not equivalent");
                var retained=areaCapture?areaProps:forestBefore.Where(g=>g[0].Original!=nestedBefore[0].Original).ToArray();
                var actual=MatchForest(retained,areaCapture?CaptureAreaProps(observedOwner):NestedRoots(capturedRootTypes,observedOwner).Select(root=>CaptureNestedImage(root,false)).ToArray());
                if(actual==null||!AddForestMap(retained,actual,map))return BoundWait("retained topology/placement or reference mapping differs");
                if(!CompanionSourcesValid(retained.SelectMany(GroupSources),actual.SelectMany(GroupSources)))return BoundWait("a retained source survives outside its mapped graph");
                retained=AdaptDeletedAreaConnections(retained,actual,current,map);
                if(!areaCapture&&nestedBefore.Any(n=>n.SpawnAccess.Present)){
                    retained=AdaptSpawnDeletionConnections(retained,actual,current,map);
                    if(retained==null)return BoundWait("spawn closure: retained spawn/access connections or references changed outside the verified deletion");
                }
                var em=world.EntityManager;
                if(retained.SelectMany(g=>g).Any(n=>em.Exists(n.Original)&&em.HasComponent<Game.Common.Deleted>(n.Original))||!BoundLiveSettled(current,observedOwner,actual.SelectMany(GroupSources)))return BoundWait("waiting for native object/companion initialization");
                var rows=session.Asset.GetComponent<ObjectSubObjects>()?.m_SubObjects;
                var indices=retained.Select(g=>BoundDescriptorIndex.ResolveNative(rows,g[0].Prefab,g[0].Local,boundBefore.Transform,g[0].Data.Transform)).ToArray();
                var seedWrites=BoundNetworkItems(expectedCompanions).Where(i=>i.HasSeed).Select(i=>new BoundWrite{NetworkSeed=true,Target=map[i.Id],AfterSeed=i.Seed,BeforeSeed=BoundNetworkItems(current).Single(a=>a.Id==map[i.Id]).Seed}).Where(w=>!w.BeforeSeed.Equals(w.AfterSeed)).ToArray();
                bool exact=seedWrites.Length==0&&retained.Select((g,i)=>NestedGroupAtIndex(g,actual[i],indices[i],map)).All(v=>v);
                if(!exact){
                    for(int g=0;g<retained.Length;g++)for(int n=0;n<retained[g].Length;n++)if(!RetainedValuesPreflight(retained[g][n],actual[g][n],indices[g],map))return BoundWait("retained values preflight failed: "+retained[g][n].Prefab.name+"; companion="+SameNestedElevation(retained[g][n],actual[g][n],false,map)+" local="+(n==0?indices[g].ToString():"nested")+" placement="+SameNestedPlacement(retained[g][n],actual[g][n])+NestedCompanionDifference(retained[g][n],actual[g][n],map));
                    if(boundValuesRestored)return BoundWait("retained values changed again after restoration");
                    var writes=retained.SelectMany((group,g)=>group.Select((node,n)=>new BoundWrite{Expected=node,Actual=actual[g][n],Index=indices[g]})).Concat(seedWrites).ToArray();
                    if(!RetainedValueTransaction.Apply(writes,p=>p.NetworkSeed?em.GetComponentData<Game.Common.PseudoRandomSeed>(p.Target).Equals(p.BeforeSeed):RetainedValuesPreflight(p.Expected,p.Actual,p.Index,map),p=>{if(p.NetworkSeed)em.SetComponentData(p.Target,p.AfterSeed);else PutRetainedValues(p.Expected,p.Actual.Original);},p=>{if(p.NetworkSeed)em.SetComponentData(p.Target,p.BeforeSeed);else PutRetainedValues(p.Actual,p.Actual.Original);}))return BoundWait("retained write preflight changed");
                    boundValuesRestored=true;return BoundWait("retained typed values restored; awaiting exact validation");
                }
                {
                    var connections=actual.SelectMany(g=>g.Select(n=>n.SpawnAccess)).ToArray();
                    if(boundConnectionCheckpoint==null||boundConnectionCheckpoint.Length!=connections.Length||boundConnectionCheckpoint.Where((image,i)=>!image.Matches(connections[i],map)).Any())boundCompletion.Reset();
                    boundConnectionCheckpoint=connections;
                }
                if(!boundCompletion.Observe(Time.frameCount,map)){Status="Checking bound deletion and companion data…";return false;}
                boundAfter=current;boundForestAfter=actual;return true;
            }catch(NotSupportedException error){return BoundWait(error.Message);}
        }
        private static IEnumerable<AreaCompanionNetwork.Item> BoundNetworkItems(BoundCompanions image)=>image.Network.Items.Concat(image.AreaLanes.Values.SelectMany(g=>g.Items));
        private bool BoundWait(string reason){boundCompletion.Reset();Status="Checking bound deletion and companion data…";if(reason!=boundRefusal){boundRefusal=reason;Mod.Log.Info("Bound deletion settling: "+reason);}return false;}
    }
}

