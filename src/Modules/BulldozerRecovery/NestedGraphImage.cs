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
            internal bool HasStreetLight;
            internal bool HasPlaceholder;
            internal Game.Objects.StreetLight StreetLight;
        }
        private static bool SameNestedElevation(NestedNode a,NestedNode b)=>a.HasPlaceholder==b.HasPlaceholder&&NestedElevationPolicy.Matches(a.HasElevation,a.Elevation.m_Elevation,(byte)a.Elevation.m_Flags,b.HasElevation,b.Elevation.m_Elevation,(byte)b.Elevation.m_Flags)&&NestedElevationPolicy.MatchesState(a.HasStreetLight,(byte)a.StreetLight.m_State,b.HasStreetLight,(byte)b.StreetLight.m_State);
        private NestedNode[] nestedImage;
        private NestedNode[] CaptureNestedImage(Entity root,bool log=true){
            var em=world.EntityManager;var prefabs=world.GetExistingSystemManaged<PrefabSystem>();
            var nodes=new List<NestedNode>();var seen=new HashSet<Entity>();
            var queue=new Queue<Tuple<Entity,Entity,int,int>>();
            if(!em.HasComponent<Game.Common.Owner>(root))throw new NotSupportedException("Nested fixture must have an owning asset.");
            queue.Enqueue(Tuple.Create(root,em.GetComponentData<Game.Common.Owner>(root).m_Owner,-1,0));
            while(queue.Count>0){
                var item=queue.Dequeue();var entity=item.Item1;
                if(nodes.Count>=32||item.Item4>8||!seen.Add(entity))throw new NotSupportedException("Nested graph budget/cycle/duplicate refusal.");
                if(!em.Exists(entity)||!em.HasComponent<Game.Common.Owner>(entity)||em.GetComponentData<Game.Common.Owner>(entity).m_Owner!=item.Item2||!em.HasComponent<PrefabRef>(entity)||!em.HasComponent<Game.Objects.Transform>(entity)||!em.HasComponent<Game.Common.PseudoRandomSeed>(entity)||!em.HasComponent<Game.Tools.LocalTransformCache>(entity))throw new NotSupportedException("Nested graph ownership or typed properties unavailable.");
                using(var types=em.GetComponentTypes(entity,Allocator.Temp))foreach(var type in types){
                    string name=type.GetManagedType()?.FullName??"unknown";
                    if(!MainKnownTypes.Contains(name)&&name!="Game.Common.Owner"&&name!="Game.Tools.LocalTransformCache"&&name!="Game.Objects.Elevation"&&name!="Game.Objects.StreetLight"&&name!="Game.Objects.Placeholder")throw new NotSupportedException("Unhandled nested component: "+name);
                }
                if((em.HasBuffer<Game.Net.SubNet>(entity)&&em.GetBuffer<Game.Net.SubNet>(entity,true).Length!=0)||(em.HasBuffer<Game.Areas.SubArea>(entity)&&em.GetBuffer<Game.Areas.SubArea>(entity,true).Length!=0))throw new NotSupportedException("Nested networks/areas unsupported.");
                bool surface=em.HasComponent<Game.Objects.Surface>(entity);
                var node=new NestedNode{Original=entity,Owner=item.Item2,Parent=item.Item3,Depth=item.Item4,Prefab=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(entity)),HasSurface=surface,Local=em.GetComponentData<Game.Tools.LocalTransformCache>(entity),Data=new MainPropData(em.GetComponentData<Game.Objects.Transform>(entity),em.GetComponentData<Game.Common.PseudoRandomSeed>(entity),surface?em.GetComponentData<Game.Objects.Surface>(entity):default,CopyNestedBuffer<Game.Rendering.MeshColor>(entity),CopyNestedBuffer<Game.Rendering.CustomMeshColor>(entity))};
                node.HasElevation=em.HasComponent<Game.Objects.Elevation>(entity);
                if(node.HasElevation)node.Elevation=em.GetComponentData<Game.Objects.Elevation>(entity);
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
                    var ordered=Enumerable.Range(0,children.Length).Select(i=>children[i].m_SubObject).OrderBy(child=>em.Exists(child)&&em.HasComponent<PrefabRef>(child)?prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(child))?.GetInstanceID()??int.MinValue:int.MinValue).ToArray();
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
