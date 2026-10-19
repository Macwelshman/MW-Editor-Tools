using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private sealed class NestedNode {
            internal Entity Original,Owner;
            internal int Parent,Depth;
            internal PrefabBase Prefab;
            internal MainPropData Data;
            internal Game.Tools.LocalTransformCache Local;
            internal bool HasSurface;
            internal bool HasElevation;
            internal Game.Objects.Elevation Elevation;
            internal AreaCompanionEffect[] Effects=Array.Empty<AreaCompanionEffect>();
            internal int[] EnabledEffects=Array.Empty<int>();
            internal bool HasColor,HasDecoration,DecorationEnabled;
            internal Game.Objects.Color Color;
            internal bool HasPlant,HasTree;
            internal Game.Objects.Plant Plant;
            internal Game.Objects.Tree Tree;
            // Renderer-owned buffers are regenerated; never snapshot native allocations.
            internal int EmissiveCount=-1,LightStateCount=-1;
            internal bool HasStreetLight;
            internal bool HasPlaceholder;
            internal Game.Objects.StreetLight StreetLight;
        }
        private static bool SameNestedElevation(NestedNode a,NestedNode b,bool vegetationValues=true)=>a.EmissiveCount==b.EmissiveCount&&a.LightStateCount==b.LightStateCount&&AreaCompanionEffect.Matches(a.Effects,b.Effects)&&a.EnabledEffects.SequenceEqual(b.EnabledEffects)&&a.HasColor==b.HasColor&&a.HasDecoration==b.HasDecoration&&(!vegetationValues||(a.Color.Equals(b.Color)&&a.DecorationEnabled==b.DecorationEnabled))&&a.HasPlant==b.HasPlant&&a.HasTree==b.HasTree&&(!vegetationValues||(a.Plant.Equals(b.Plant)&&a.Tree.Equals(b.Tree)))&&a.HasPlaceholder==b.HasPlaceholder&&NestedElevationPolicy.Matches(a.HasElevation,a.Elevation.m_Elevation,(byte)a.Elevation.m_Flags,b.HasElevation,b.Elevation.m_Elevation,(byte)b.Elevation.m_Flags)&&NestedElevationPolicy.MatchesState(a.HasStreetLight,(byte)a.StreetLight.m_State,b.HasStreetLight,(byte)b.StreetLight.m_State);
        private bool IsEffectContainer(Entity id)=>world.EntityManager.Exists(id)&&world.EntityManager.HasComponent<Game.Tools.EditorContainer>(id)&&world.EntityManager.HasComponent<EffectData>(world.EntityManager.GetComponentData<Game.Tools.EditorContainer>(id).m_Prefab);
        private static IEnumerable<PrefabBase> GroupReferences(NestedNode[] group)=>group.SelectMany(n=>new[]{n.Prefab}.Concat(n.Effects.SelectMany(e=>new[]{e.Prefab,e.Container}))).Where(p=>p!=null);
        private static IEnumerable<Entity> GroupSources(NestedNode[] group)=>group.SelectMany(n=>new[]{n.Original}.Concat(n.Effects.Select(e=>e.Original)));
        private NestedNode[] nestedImage;
        private NestedNode[] CaptureNestedImage(Entity root,bool log=true,bool standalone=false){
            var em=world.EntityManager;var prefabs=world.GetExistingSystemManaged<PrefabSystem>();
            var nodes=new List<NestedNode>();var seen=new HashSet<Entity>();
            var queue=new Queue<Tuple<Entity,Entity,int,int>>();
            if(!standalone&&!em.HasComponent<Game.Common.Owner>(root))throw new NotSupportedException("Nested fixture must have an owning asset.");
            queue.Enqueue(Tuple.Create(root,standalone?Entity.Null:em.GetComponentData<Game.Common.Owner>(root).m_Owner,-1,0));
            while(queue.Count>0){
                var item=queue.Dequeue();var entity=item.Item1;
                if(nodes.Count>=32||item.Item4>8||!seen.Add(entity))throw new NotSupportedException("Nested graph budget/cycle/duplicate refusal.");
                bool standaloneRoot=standalone&&item.Item3==-1;
                if(!em.Exists(entity)||em.HasComponent<Game.Common.Deleted>(entity)||em.HasComponent<Game.Tools.Temp>(entity)||
                    (standaloneRoot?em.HasComponent<Game.Common.Owner>(entity):!em.HasComponent<Game.Common.Owner>(entity)||em.GetComponentData<Game.Common.Owner>(entity).m_Owner!=item.Item2)||!em.HasComponent<PrefabRef>(entity)||!em.HasComponent<Game.Objects.Transform>(entity)||!em.HasComponent<Game.Common.PseudoRandomSeed>(entity)||(!standaloneRoot&&!em.HasComponent<Game.Tools.LocalTransformCache>(entity)))throw new NotSupportedException("Nested graph ownership or typed properties unavailable.");
                using(var types=em.GetComponentTypes(entity,Allocator.Temp))foreach(var type in types){
                    string name=type.GetManagedType()?.FullName??"unknown";
                    if(!MainKnownTypes.Contains(name)&&name!="Game.Rendering.Emissive"&&name!="Game.Rendering.LightState"&&name!="Game.Common.Owner"&&name!="Game.Tools.LocalTransformCache"&&name!="Game.Objects.Elevation"&&name!="Game.Objects.StreetLight"&&name!="Game.Objects.Placeholder"&&name!="Game.Objects.Plant"&&name!="Game.Objects.Tree"&&name!="Game.Effects.EnabledEffect"&&(name!="Game.Simulation.UpdateFrame"||(!em.HasComponent<Game.Objects.Plant>(entity)&&!em.HasComponent<Game.Objects.Tree>(entity))))throw new NotSupportedException("Unhandled nested component: "+name+"; prefab="+prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(entity))?.name+"; effectContainer="+(em.HasComponent<Game.Tools.EditorContainer>(entity)&&em.HasComponent<EffectData>(em.GetComponentData<Game.Tools.EditorContainer>(entity).m_Prefab)));
                }
                if((em.HasBuffer<Game.Net.SubNet>(entity)&&em.GetBuffer<Game.Net.SubNet>(entity,true).Length!=0)||(em.HasBuffer<Game.Areas.SubArea>(entity)&&em.GetBuffer<Game.Areas.SubArea>(entity,true).Length!=0))throw new NotSupportedException("Nested networks/areas unsupported.");
                bool surface=em.HasComponent<Game.Objects.Surface>(entity);
                var node=new NestedNode{Original=entity,Owner=item.Item2,Parent=item.Item3,Depth=item.Item4,Prefab=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(entity)),HasSurface=surface,Local=standaloneRoot?new Game.Tools.LocalTransformCache{m_Probability=100,m_ParentMesh=-1}:em.GetComponentData<Game.Tools.LocalTransformCache>(entity),Data=new MainPropData(em.GetComponentData<Game.Objects.Transform>(entity),em.GetComponentData<Game.Common.PseudoRandomSeed>(entity),surface?em.GetComponentData<Game.Objects.Surface>(entity):default,CopyNestedBuffer<Game.Rendering.MeshColor>(entity),CopyNestedBuffer<Game.Rendering.CustomMeshColor>(entity))};
                if(node.Parent==-1 && em.Exists(node.Owner) && em.HasComponent<PrefabRef>(node.Owner)) {
                    var owningAsset=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(node.Owner));
                    node.Local.m_PrefabSubIndex=BoundDescriptorIndex.Resolve(owningAsset?.GetComponent<ObjectSubObjects>()?.m_SubObjects,node.Prefab,node.Local);
                }
                if(em.HasBuffer<Game.Effects.EnabledEffect>(entity)){var enabled=em.GetBuffer<Game.Effects.EnabledEffect>(entity,true);if(enabled.Length>256)throw new NotSupportedException("Enabled effect buffer exceeds budget.");node.EnabledEffects=EnabledEffectImage.Capture(enabled.AsNativeArray().ToArray());}
                node.Effects=CaptureAreaEffects(entity);
                node.HasColor=em.HasComponent<Game.Objects.Color>(entity);if(node.HasColor)node.Color=em.GetComponentData<Game.Objects.Color>(entity);
                node.HasDecoration=em.HasComponent<Game.Objects.Decoration>(entity);if(node.HasDecoration)node.DecorationEnabled=em.IsComponentEnabled<Game.Objects.Decoration>(entity);
                node.HasPlant=em.HasComponent<Game.Objects.Plant>(entity);if(node.HasPlant){node.Plant=em.GetComponentData<Game.Objects.Plant>(entity);if(!Unity.Mathematics.math.isfinite(node.Plant.m_Pollution))throw new NotSupportedException("Nonfinite plant state.");}
                node.HasTree=em.HasComponent<Game.Objects.Tree>(entity);if(node.HasTree)node.Tree=em.GetComponentData<Game.Objects.Tree>(entity);
                node.HasElevation=em.HasComponent<Game.Objects.Elevation>(entity);
                if(node.HasElevation)node.Elevation=em.GetComponentData<Game.Objects.Elevation>(entity);
                node.EmissiveCount=em.HasBuffer<Game.Rendering.Emissive>(entity)?em.GetBuffer<Game.Rendering.Emissive>(entity,true).Length:-1;
                node.LightStateCount=em.HasBuffer<Game.Rendering.LightState>(entity)?em.GetBuffer<Game.Rendering.LightState>(entity,true).Length:-1;
                if(node.EmissiveCount>4096||node.LightStateCount>4096)throw new NotSupportedException("Derived light buffer exceeds capture budget.");
                node.HasStreetLight=em.HasComponent<Game.Objects.StreetLight>(entity);
                node.HasPlaceholder=em.HasComponent<Game.Objects.Placeholder>(entity);
                if(node.HasStreetLight)node.StreetLight=em.GetComponentData<Game.Objects.StreetLight>(entity);
                if(node.Prefab==null||!node.Data.Matches(node.Data.Clone()))throw new NotSupportedException("Nested detached data invalid.");
                int index=nodes.Count;nodes.Add(node);
                if(log)Mod.Log.Info("Nested typed node index="+index+" parent="+node.Parent+" original="+entity+" prefab="+node.Prefab.name+" seed="+node.Data.Seed.m_Seed+" probability="+node.Local.m_Probability+" group="+node.Local.m_GroupIndex+" parentMesh="+node.Local.m_ParentMesh);
                if(em.HasBuffer<Game.Objects.SubObject>(entity)){
                    var children=em.GetBuffer<Game.Objects.SubObject>(entity,true);
                    if(children.Length+queue.Count+nodes.Count>32)throw new NotSupportedException("Nested graph exceeds node budget.");
                    // Stable prefab identity order makes sibling capture independent of native buffer order.
                    var ordered=Enumerable.Range(0,children.Length).Select(i=>children[i].m_SubObject).Where(child=>!IsEffectContainer(child)).OrderBy(child=>em.Exists(child)&&em.HasComponent<PrefabRef>(child)?prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(child))?.GetInstanceID()??int.MinValue:int.MinValue).ToArray();
                    foreach(var child in ordered)queue.Enqueue(Tuple.Create(child,entity,index,item.Item4+1));
                }
            }
            return nodes.ToArray();
        }
        private T[] CopyNestedBuffer<T>(Entity entity) where T:unmanaged,IBufferElementData {
            var em=world.EntityManager;if(!em.HasBuffer<T>(entity))return null;
            var buffer=em.GetBuffer<T>(entity,true);if(buffer.Length>256)throw new NotSupportedException("Nested colour buffer exceeds budget.");
            using(var values=buffer.ToNativeArray(Allocator.Temp))return values.ToArray();
        }
        private void ValidateDetachedNestedImage(){
            if(nestedImage==null)return;
            bool detached=nestedImage.All(node=>node.Prefab!=null&&node.Data.Matches(node.Data.Clone()));
            bool gone=nestedImage.All(node=>!world.EntityManager.Exists(node.Original));
            Mod.Log.Info("Nested typed detached checkpoint nodes="+nestedImage.Length+" valid="+detached+" allOriginalsGone="+gone+"; exact typed transform/seed/colour, surface presence, local cache and parent indices captured; no recovery enabled.");
            nestedImage=null;
        }
    }
}
