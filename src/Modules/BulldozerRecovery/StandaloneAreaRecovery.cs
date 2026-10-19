using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using EditorContainer=Game.Tools.EditorContainer;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private sealed class StandaloneAreaEntry {
            internal Entity Original;
            internal PrefabBase Prefab,Container;
            internal Payload Reference,ContainerReference;
            internal AreaCompanionNetwork Network;
            internal Dictionary<PrefabBase,Payload> NetworkReferences;
            internal StandaloneAreaData Data;
            internal bool Surface,Space,HasContainer,HasSeed,HasExpand,Navigation,Hangaround;
            internal Game.Areas.Expand[] Expand;
            internal EditorContainer ContainerData;
            internal Game.Common.PseudoRandomSeed Seed;
            internal bool Valid=>Prefab!=null&&Data!=null&&Data.Matches(new StandaloneAreaData(Data.Flags,Data.Nodes,Data.Local))&&
                NestedReferenceMatches(Prefab,Reference)&&NetworkReferences!=null&&NetworkReferences.All(p=>NestedReferenceMatches(p.Key,p.Value))&&(!HasContainer||(Container!=null&&NestedReferenceMatches(Container,ContainerReference)));
        }
        private readonly MW.EditorTools.History.LastDeletion<StandaloneAreaEntry> standaloneAreas=new MW.EditorTools.History.LastDeletion<StandaloneAreaEntry>();
        private StandaloneAreaEntry standaloneAreaPending;
        private Entity standaloneAreaLive;
        private bool standaloneAreaInApply,standaloneAreaAccepted,standaloneAreaRestoring,standaloneAreaDisabled;
        private int standaloneAreaFrame,standaloneAreaStable;
        private bool StandaloneAreaBusy=>standaloneAreaPending!=null;
        private static readonly HashSet<string> StandaloneAreaKnown=new HashSet<string>{
            "Game.Prefabs.PrefabRef","Game.Areas.Area","Game.Areas.Surface","Game.Areas.Space","Game.Areas.Node","Game.Areas.Triangle",
            "Game.Areas.Geometry","Game.Areas.Expand","Game.Areas.Batch","Game.Areas.Navigation","Game.Areas.HangaroundLocation","Game.Net.SubLane","Game.Net.SubNet","Game.Rendering.CullingInfo","Game.Tools.Hidden","Game.Tools.EditorContainer",
            "Game.Tools.LocalNodeCache","Game.Common.PseudoRandomSeed","Unity.Entities.Simulate"
        };
        private StandaloneAreaEntry CaptureStandaloneArea(Entity id,bool bound=false,bool allowInvalidNetwork=false) {
            var em=world.EntityManager;
            if(!em.Exists(id)||(bound?!em.HasComponent<Game.Common.Owner>(id):em.HasComponent<Game.Common.Owner>(id))||em.HasComponent<Game.Common.Deleted>(id)||em.HasComponent<Temp>(id)||
                !em.HasComponent<Game.Areas.Area>(id)||!em.HasComponent<PrefabRef>(id)||!em.HasBuffer<Game.Areas.Node>(id))throw new NotSupportedException("Not a stable standalone surface/area.");
            using(var types=em.GetComponentTypes(id,Allocator.Temp))foreach(var type in types)
                if(!StandaloneAreaKnown.Contains(type.GetManagedType()?.FullName??"unknown")&&type.GetManagedType()?.FullName!="Game.Common.BatchesUpdated"&&!(bound&&type.GetManagedType()?.FullName=="Game.Common.Owner"))throw new NotSupportedException("Unsupported standalone area component: "+type.GetManagedType()?.FullName);
            var prefabs=world.GetExistingSystemManaged<PrefabSystem>();
            var result=new StandaloneAreaEntry{Original=id,Prefab=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(id)),
                Surface=em.HasComponent<Game.Areas.Surface>(id),Space=em.HasComponent<Game.Areas.Space>(id)};
            if(result.Surface==result.Space||!(result.Prefab is SurfacePrefab||result.Prefab is SpacePrefab))throw new NotSupportedException("Only surface and space area prefabs are supported.");
            if(!em.HasBuffer<Game.Areas.Triangle>(id)||em.GetBuffer<Game.Areas.Triangle>(id,true).Length==0)throw new NotSupportedException("Area triangulation is not ready.");
            var nodes=em.GetBuffer<Game.Areas.Node>(id,true);if(nodes.Length>4096)throw new NotSupportedException("Area vertex budget exceeded.");
            LocalNodeCache[] local=null;
            if(em.HasBuffer<LocalNodeCache>(id)){var cache=em.GetBuffer<LocalNodeCache>(id,true);if(cache.Length>4096)throw new NotSupportedException("Area cache budget exceeded.");local=cache.AsNativeArray().ToArray();}
            result.Data=new StandaloneAreaData(em.GetComponentData<Game.Areas.Area>(id).m_Flags,nodes.AsNativeArray().ToArray(),local);
            result.Navigation=em.HasComponent<Game.Areas.Navigation>(id);result.Hangaround=em.HasComponent<Game.Areas.HangaroundLocation>(id);
            result.HasExpand=em.HasBuffer<Game.Areas.Expand>(id);if(result.HasExpand){var expansion=em.GetBuffer<Game.Areas.Expand>(id,true);if(expansion.Length>4096)throw new NotSupportedException("Surface expansion budget exceeded.");result.Expand=expansion.AsNativeArray().ToArray();if(result.Expand.Any(e=>!math.all(math.isfinite(e.m_Offset))))throw new NotSupportedException("Nonfinite surface expansion offset.");}
            result.Network=CaptureAreaNetwork(id,allowInvalidNetwork);
            result.NetworkReferences=result.Network.Items.SelectMany(i=>new[]{i.Prefab,i.Container}).Where(p=>p!=null).Distinct().ToDictionary(p=>p,Capture);
            result.Reference=Capture(result.Prefab);
            result.HasContainer=em.HasComponent<EditorContainer>(id);
            if(result.HasContainer){result.ContainerData=em.GetComponentData<EditorContainer>(id);result.Container=prefabs.GetPrefab<PrefabBase>(result.ContainerData.m_Prefab);if(result.Container==null||!math.all(math.isfinite(result.ContainerData.m_Scale))||!math.isfinite(result.ContainerData.m_Intensity))throw new NotSupportedException("Invalid standalone area container.");result.ContainerReference=Capture(result.Container);}
            result.HasSeed=em.HasComponent<Game.Common.PseudoRandomSeed>(id);if(result.HasSeed)result.Seed=em.GetComponentData<Game.Common.PseudoRandomSeed>(id);
            if(result.Reference.Weight+(result.ContainerReference?.Weight??0)+result.NetworkReferences.Values.Sum(p=>p.Weight)>250000||!result.Valid)throw new NotSupportedException("Standalone area references changed or exceed budget.");
            return result;
        }
        private void BeginStandaloneArea(BulldozeToolSystem tool,Entity id) {
            if(tool.actualMode!=BulldozeToolSystem.Mode.MainElements&&tool.actualMode!=BulldozeToolSystem.Mode.Everything)throw new NotSupportedException("Use Main-elements or Everything for an unbound surface/area.");
            if(standaloneAreaDisabled)throw new NotSupportedException("Standalone area recovery stopped after an error; reload the editor session.");
            var image=CaptureStandaloneArea(id);
            ClearMain();ClearRecord();armed=nestedArmed=nestedForestArmed=lastArmed=false;armedAsset=null;
            standaloneAreaPending=image;standaloneAreaFrame=Time.frameCount;standaloneAreaStable=0;
            standaloneAreaInApply=true;standaloneAreaAccepted=standaloneAreaRestoring=false;standaloneAreaLive=Entity.Null;
            Status="Capturing unbound surface/area deletion…";Mod.Log.Info(Status+" prefab="+image.Prefab.name+" vertices="+image.Data.Nodes.Length);
        }
        private bool CanStandaloneAreaUndo {
            get{try{return !standaloneAreaDisabled&&!Busy&&context.EditorAvailable&&!context.DialogBusy&&standaloneAreas.Latest!=null&&standaloneAreas.Latest.Valid&&StandaloneAreaSourcesGone(standaloneAreas.Latest);}catch{return false;}}
        }
        private void UndoStandaloneArea() {
            if(!CanStandaloneAreaUndo)return;
            var image=standaloneAreas.Latest;var em=world.EntityManager;
            standaloneAreaPending=image;standaloneAreaRestoring=true;standaloneAreaFrame=Time.frameCount;standaloneAreaStable=0;standaloneAreaLive=Entity.Null;
            try {
                var prefab=world.GetExistingSystemManaged<PrefabSystem>().GetEntity(image.Prefab);
                if(!em.Exists(prefab)||!em.HasComponent<AreaData>(prefab))throw new NotSupportedException("Native area archetype unavailable.");
                var archetype=em.GetComponentData<AreaData>(prefab).m_Archetype;
                using(var types=archetype.GetComponentTypes(Allocator.Temp))foreach(var type in types){var name=type.GetManagedType()?.FullName??"unknown";if(!StandaloneAreaKnown.Contains(name)&&name!="Game.Common.Created"&&name!="Game.Common.Updated"&&name!="Game.Common.Applied")throw new NotSupportedException("Unsupported area archetype component: "+name);}
                using(var created=new NativeArray<Entity>(1,Allocator.Temp)){em.CreateEntity(archetype,created);standaloneAreaLive=created[0];}
                PutMain(standaloneAreaLive,new PrefabRef{m_Prefab=prefab});PutMain(standaloneAreaLive,new Game.Areas.Area{m_Flags=image.Data.Flags});
                if(image.Network.RootLanes.Length>0&&!em.HasBuffer<Game.Net.SubLane>(standaloneAreaLive))em.AddBuffer<Game.Net.SubLane>(standaloneAreaLive);
                if(image.Network.RootNets.Length>0&&!em.HasBuffer<Game.Net.SubNet>(standaloneAreaLive))em.AddBuffer<Game.Net.SubNet>(standaloneAreaLive);
                if(image.Navigation)PutMain(standaloneAreaLive,new Game.Areas.Navigation());if(image.Hangaround)PutMain(standaloneAreaLive,new Game.Areas.HangaroundLocation());
                if(image.HasExpand){var expansion=em.HasBuffer<Game.Areas.Expand>(standaloneAreaLive)?em.GetBuffer<Game.Areas.Expand>(standaloneAreaLive):em.AddBuffer<Game.Areas.Expand>(standaloneAreaLive);expansion.Clear();foreach(var value in image.Expand)expansion.Add(value);}
                if(image.Surface)PutMain(standaloneAreaLive,new Game.Areas.Surface());else PutMain(standaloneAreaLive,new Game.Areas.Space());
                var nodes=em.HasBuffer<Game.Areas.Node>(standaloneAreaLive)?em.GetBuffer<Game.Areas.Node>(standaloneAreaLive):em.AddBuffer<Game.Areas.Node>(standaloneAreaLive);nodes.Clear();foreach(var node in image.Data.Nodes)nodes.Add(node);
                var local=image.Data.Local;if(local!=null){var cache=em.HasBuffer<LocalNodeCache>(standaloneAreaLive)?em.GetBuffer<LocalNodeCache>(standaloneAreaLive):em.AddBuffer<LocalNodeCache>(standaloneAreaLive);cache.Clear();foreach(var node in local)cache.Add(node);}
                if(image.HasContainer){var container=image.ContainerData;container.m_Prefab=world.GetExistingSystemManaged<PrefabSystem>().GetEntity(image.Container);if(!em.Exists(container.m_Prefab))throw new NotSupportedException("Area container prefab is no longer available.");PutMain(standaloneAreaLive,container);}if(image.HasSeed)PutMain(standaloneAreaLive,image.Seed);
                PutMain(standaloneAreaLive,new Game.Common.Applied());PutMain(standaloneAreaLive,new Game.Common.Created());PutMain(standaloneAreaLive,new Game.Common.Updated());
                Status="Waiting for unbound surface/area initialization…";Mod.Log.Info(Status+" entity="+standaloneAreaLive);
            }catch(Exception error){if(standaloneAreaLive!=Entity.Null&&em.Exists(standaloneAreaLive))em.DestroyEntity(standaloneAreaLive);FailStandaloneArea("Unbound area creation failed: "+error.Message);}
        }
        private static bool StandaloneAreaMatches(StandaloneAreaEntry expected,StandaloneAreaEntry actual)=>ReferenceEquals(expected.Prefab,actual.Prefab)&&expected.Surface==actual.Surface&&expected.Space==actual.Space&&expected.Navigation==actual.Navigation&&expected.Hangaround==actual.Hangaround&&expected.Data.Matches(actual.Data)&&
            expected.HasContainer==actual.HasContainer&&(!expected.HasContainer||(ReferenceEquals(expected.Container,actual.Container)&&expected.ContainerData.m_Scale.Equals(actual.ContainerData.m_Scale)&&expected.ContainerData.m_Intensity.Equals(actual.ContainerData.m_Intensity)&&expected.ContainerData.m_GroupIndex==actual.ContainerData.m_GroupIndex))&&expected.HasExpand==actual.HasExpand&&(!expected.HasExpand||expected.Expand.SequenceEqual(actual.Expand))&&expected.HasSeed==actual.HasSeed&&(!expected.HasSeed||expected.Seed.Equals(actual.Seed));
        private bool StandaloneAreaSourcesGone(StandaloneAreaEntry image)=>!world.EntityManager.Exists(image.Original)&&image.Network.Items.All(i=>!world.EntityManager.Exists(i.Id));
        private bool StandaloneAreaNetworkMatches() {
            var expected=standaloneAreaPending.Network;var actual=CaptureAreaNetwork(standaloneAreaLive);
            if(!expected.TryMap(actual,false,out var map)||!CompanionSourcesValid(expected.Items.Select(i=>i.Id),actual.Items.Select(i=>i.Id)))return false;
            bool changed=false;foreach(var item in expected.Items.Where(i=>i.HasSeed)){var id=map[item.Id];if(!world.EntityManager.GetComponentData<Game.Common.PseudoRandomSeed>(id).Equals(item.Seed)){world.EntityManager.SetComponentData(id,item.Seed);changed=true;}}
            if(changed)return false;
            return expected.TryMap(CaptureAreaNetwork(standaloneAreaLive),true,out _);
        }
        private void EndStandaloneAreaApply(){if(!standaloneAreaInApply)return;standaloneAreaInApply=false;if(!standaloneAreaAccepted)ClearStandaloneArea();}
        private void TickStandaloneArea() {
            if(!context.EditorAvailable){ClearStandaloneArea();return;}
            if(standaloneAreas.Latest!=null&&!standaloneAreas.Latest.Valid)standaloneAreas.Clear();
            if(standaloneAreaPending==null||standaloneAreaInApply)return;
            int age=Time.frameCount-standaloneAreaFrame;
            if(!standaloneAreaRestoring){
                if(standaloneAreaAccepted&&StandaloneAreaSourcesGone(standaloneAreaPending)&&standaloneAreaPending.Valid){standaloneAreas.Record(standaloneAreaPending);standaloneAreaPending=null;Status="Recorded unbound surface/area deletion. Ctrl+Z to Undo.";Mod.Log.Info(Status);return;}
            }else if(age>=2){
                bool exact=false;try{exact=standaloneAreaPending.Valid&&!world.EntityManager.Exists(standaloneAreaPending.Original)&&StandaloneAreaMatches(standaloneAreaPending,CaptureStandaloneArea(standaloneAreaLive))&&StandaloneAreaNetworkMatches();}catch(NotSupportedException){}
                if(!exact)standaloneAreaStable=0;
                else if(++standaloneAreaStable>=3){if(!standaloneAreas.Undo(e=>ReferenceEquals(e,standaloneAreaPending))){FailStandaloneArea("Standalone area slot changed during recovery.");return;}standaloneAreaPending=null;standaloneAreaRestoring=false;standaloneAreaLive=Entity.Null;Status="Undid unbound surface/area deletion; geometry and references verified.";Mod.Log.Info(Status);return;}
            }
            if(age>120)FailStandaloneArea("Unbound surface/area recovery did not validate; reload the disposable editor session without saving.");
        }
        private void ClearStandaloneArea(){if(standaloneAreaRestoring&&standaloneAreaLive!=Entity.Null&&world.EntityManager.Exists(standaloneAreaLive)){if(!world.EntityManager.HasComponent<Game.Common.Deleted>(standaloneAreaLive))world.EntityManager.AddComponent<Game.Common.Deleted>(standaloneAreaLive);}standaloneAreas.Clear();standaloneAreaPending=null;standaloneAreaInApply=standaloneAreaRestoring=false;standaloneAreaLive=Entity.Null;}
        private void FailStandaloneArea(string reason){standaloneAreaDisabled=true;ClearStandaloneArea();Status=reason;Mod.Log.Warn(reason);}
    }
}
