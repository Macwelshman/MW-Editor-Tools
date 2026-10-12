using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private int nestedDiscoveryFrame;
        private bool nestedDiscoveryDone;
        // Definition-only inventory. Placement, typed ECS data and recovery still require the existing checks.
        private static bool DiscoverableNestedDefinition(ObjectPrefab root,out int count){
            count=0;
            var prefabs=new List<ObjectPrefab>();var parents=new List<int>();var depths=new List<int>();
            var queue=new Queue<Tuple<ObjectPrefab,int,int>>();queue.Enqueue(Tuple.Create(root,-1,0));
            while(queue.Count>0){
                var item=queue.Dequeue();var prefab=item.Item1;
                if(prefab==null||prefabs.Count>=8||item.Item3>4||prefabs.Contains(prefab)||prefab is BuildingPrefab||prefab.GetComponent<ActivityLocation>()!=null||(prefab.GetComponent<ObjectSubAreas>()?.m_SubAreas?.Length??0)>0||(prefab.GetComponent<ObjectSubNets>()?.m_SubNets?.Length??0)>0||(prefab.GetComponent<EffectSource>()?.m_Effects?.Count??0)>0)return false;
                int index=prefabs.Count;prefabs.Add(prefab);parents.Add(item.Item2);depths.Add(item.Item3);
                var links=prefab.GetComponent<ObjectSubObjects>()?.m_SubObjects;
                if(links==null)continue;
                if(links.Length+prefabs.Count+queue.Count>8)return false;
                foreach(var link in links){
                    if(link==null||link.m_Object==null||link.m_Probability!=100||link.m_GroupIndex!=0)return false;
                    queue.Enqueue(Tuple.Create(link.m_Object,index,item.Item3+1));
                }
            }
            count=prefabs.Count;
            return NestedPropPolicy.Accept(parents.ToArray(),depths.ToArray(),prefabs.Cast<object>().ToArray(),prefabs.Select(value=>true).ToArray());
        }
        private void TickNestedPropDiscovery(){
            if(nestedDiscoveryDone)return;
            if(!context.EditorAvailable){nestedDiscoveryFrame=0;return;}
            if(nestedDiscoveryFrame==0){nestedDiscoveryFrame=Time.frameCount;return;}
            if(Time.frameCount-nestedDiscoveryFrame<120)return;
            nestedDiscoveryDone=true;
            var em=world.EntityManager;var system=world.GetExistingSystemManaged<PrefabSystem>();
            var names=new List<string>();int scanned=0;
            using(var query=em.CreateEntityQuery(ComponentType.ReadOnly<PrefabData>(),ComponentType.ReadOnly<ObjectData>()))
            using(var entities=query.ToEntityArray(Allocator.Temp)){
                foreach(var entity in entities){
                    if(!system.TryGetPrefab<ObjectPrefab>(entity,out var prefab)||prefab==null||prefab.name.StartsWith("MW Nested ",StringComparison.Ordinal))continue;
                    scanned++;
                    if(DiscoverableNestedDefinition(prefab,out int count))names.Add(prefab.name+" ("+count+" props)");
                }
            }
            names.Sort(StringComparer.Ordinal);
            Mod.Log.Info("Nested prop discovery: read-only definition scan; object prefabs="+scanned+" candidates="+names.Count+". Typed placement/recovery UNPROVEN. Candidates: "+string.Join("; ",names.Take(20)));
        }
    }
}
