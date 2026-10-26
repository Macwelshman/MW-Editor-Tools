using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class EntityReferenceMap {
        // This scope is created only after the exact live owner resolves to the
        // guarded editable asset. It is not a general prefab/entity remapping.
        private sealed class OwnerScope:Dictionary<Entity,Entity> {
            internal Entity Owner,BeforePrefab,AfterPrefab;
            internal OwnerScope(Dictionary<Entity,Entity> map,Entity owner,Entity before,Entity after):base(map){Owner=owner;BeforePrefab=before;AfterPrefab=after;}
        }
        private static readonly ConditionalWeakTable<Dictionary<Entity,Entity>,HashSet<Entity>> reversed=new ConditionalWeakTable<Dictionary<Entity,Entity>,HashSet<Entity>>();
        internal static void MarkReversed(Dictionary<Entity,Entity> map,IEnumerable<Entity> lanes){var set=reversed.GetOrCreateValue(map);foreach(var lane in lanes)set.Add(lane);}
        internal static bool IsReversed(Dictionary<Entity,Entity> map,Entity lane)=>map!=null&&reversed.TryGetValue(map,out var set)&&set.Contains(lane);
        internal static void CopyDirections(Dictionary<Entity,Entity> source,Dictionary<Entity,Entity> target){if(reversed.TryGetValue(source,out var set))MarkReversed(target,set);}
        private static bool SameDirections(Dictionary<Entity,Entity> a,Dictionary<Entity,Entity> b){var x=reversed.TryGetValue(a,out var left)?left:new HashSet<Entity>();var y=reversed.TryGetValue(b,out var right)?right:new HashSet<Entity>();return x.SetEquals(y);}
        internal static Dictionary<Entity,Entity> WithOwnerPrefab(Dictionary<Entity,Entity> map,Entity owner,Entity before,Entity after){var result=new OwnerScope(map,owner,before,after);CopyDirections(map,result);return result;}
        internal static bool OwnerPrefabMatches(Dictionary<Entity,Entity> map,Entity beforeId,Entity afterId,Entity beforePrefab,Entity afterPrefab)=>map is OwnerScope scope&&beforeId==scope.Owner&&afterId==scope.Owner&&beforePrefab==scope.BeforePrefab&&afterPrefab==scope.AfterPrefab;
        internal static Dictionary<Entity,Entity> Copy(Dictionary<Entity,Entity> map){var result=map is OwnerScope s?WithOwnerPrefab(map,s.Owner,s.BeforePrefab,s.AfterPrefab):new Dictionary<Entity,Entity>(map);CopyDirections(map,result);return result;}
        internal static bool SameScope(Dictionary<Entity,Entity> a,Dictionary<Entity,Entity> b)=>SameDirections(a,b)&&(a is OwnerScope x?b is OwnerScope y&&x.Owner==y.Owner&&x.BeforePrefab==y.BeforePrefab&&x.AfterPrefab==y.AfterPrefab:!(b is OwnerScope));
        internal static bool Add(Dictionary<Entity,Entity> map,Entity source,Entity target){
            if(source==Entity.Null||target==Entity.Null)return false;
            if(map.TryGetValue(source,out var existing))return existing==target;
            if(map.Values.Contains(target))return false;
            map.Add(source,target);return true;
        }
        internal static Entity Resolve(Entity id,Dictionary<Entity,Entity> map)=>map!=null&&map.TryGetValue(id,out var value)?value:id;
    }
}
