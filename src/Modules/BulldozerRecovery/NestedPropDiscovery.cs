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
        private static string MoveCompanionDefinition(ObjectPrefab root){
            if(!(root is StaticObjectPrefab))return null;
            var queue=new Queue<ObjectPrefab>();var seen=new HashSet<ObjectPrefab>();var reasons=new SortedSet<string>(StringComparer.Ordinal);queue.Enqueue(root);
            while(queue.Count>0){var node=queue.Dequeue();if(node==null||!seen.Add(node))continue;
                if(seen.Count>32){reasons.Add("definition exceeds move group budget");break;}
                if(node.GetComponent<SpawnLocation>()!=null)reasons.Add("spawn");
                if(node.GetComponent<TakeoffLocation>()!=null)reasons.Add("takeoff/access");
                if(node.GetComponent<ParkingSpace>()!=null)reasons.Add("parking-space route data");
                if((node.GetComponent<ObjectSubLanes>()?.m_SubLanes?.Length??0)>0)reasons.Add("attached lanes");
                foreach(var child in node.GetComponent<ObjectSubObjects>()?.m_SubObjects??Array.Empty<ObjectSubObjectInfo>())if(child?.m_Object!=null)queue.Enqueue(child.m_Object);
            }
            if(reasons.Count==0)return null;
            return root.name+" ["+string.Join(", ",reasons)+(seen.Count>1?", nested group":"")+"] "+(root.name=="BikeRack01"&&seen.Count==1?"bike-rack profile; live proof required":"move companion eligibility review required");
        }
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
            // Large definition inventories are troubleshooting aids, not recovery prerequisites.
            // Opt in before starting the game; ordinary completion/failure logs remain enabled.
            if(!string.Equals(Environment.GetEnvironmentVariable("MW_EDITOR_TOOLS_DIAGNOSTICS"),"1",StringComparison.Ordinal)){nestedDiscoveryDone=true;return;}
            if(!context.EditorAvailable){nestedDiscoveryFrame=0;return;}
            if(nestedDiscoveryFrame==0){nestedDiscoveryFrame=Time.frameCount;return;}
            if(Time.frameCount-nestedDiscoveryFrame<120)return;
            nestedDiscoveryDone=true;
            var em=world.EntityManager;var system=world.GetExistingSystemManaged<PrefabSystem>();
            var names=new List<string>();var companionMoves=new List<string>();int scanned=0;
            using(var query=em.CreateEntityQuery(ComponentType.ReadOnly<PrefabData>(),ComponentType.ReadOnly<ObjectData>()))
            using(var entities=query.ToEntityArray(Allocator.Temp)){
                foreach(var entity in entities){
                    if(!system.TryGetPrefab<ObjectPrefab>(entity,out var prefab)||prefab==null||prefab.name.StartsWith("MW Nested ",StringComparison.Ordinal))continue;
                    scanned++;
                    var moveCompanions=MoveCompanionDefinition(prefab);if(moveCompanions!=null)companionMoves.Add(moveCompanions);
                    if(DiscoverableNestedDefinition(prefab,out int count))names.Add(prefab.name+" ("+count+" props)");
                }
            }
            names.Sort(StringComparer.Ordinal);
            Mod.Log.Info("Nested prop discovery: read-only definition scan; object prefabs="+scanned+" candidates="+names.Count+". Typed placement/recovery UNPROVEN. Candidates: "+string.Join("; ",names.Take(20)));
            companionMoves.Sort(StringComparer.Ordinal);
            Mod.Log.Info("Prop move companion audit: static definitions="+companionMoves.Count+"; source definitions only, live move/recovery support not implied.");
            for(int i=0;i<companionMoves.Count;i+=40)Mod.Log.Info("Prop move companion audit candidates "+(i+1)+"-"+Math.Min(i+40,companionMoves.Count)+": "+string.Join("; ",companionMoves.Skip(i).Take(40)));
        }
    }
}
