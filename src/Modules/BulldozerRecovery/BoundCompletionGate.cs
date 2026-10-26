using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed class BoundCompletionGate {
        private Dictionary<Entity,Entity> previous;
        private int frame=-1,count;
        internal void Reset(){previous=null;frame=-1;count=0;}
        internal bool Observe(int currentFrame,Dictionary<Entity,Entity> map){
            if(previous==null||!EntityReferenceMap.SameScope(previous,map)||previous.Count!=map.Count||previous.Any(p=>!map.TryGetValue(p.Key,out var value)||value!=p.Value)){previous=EntityReferenceMap.Copy(map);count=0;frame=-1;}
            if(currentFrame!=frame){frame=currentFrame;count++;}
            return count>=3;
        }
    }
    internal static class RetainedValueTransaction {
        internal static bool Apply<T>(IReadOnlyList<T> items,Func<T,bool> preflight,Action<T> write,Action<T> rollback){
            if(!items.All(preflight))return false;
            int last=-1;
            try{for(int i=0;i<items.Count;i++){last=i;write(items[i]);}return true;}
            catch(Exception error){
                var failures=new List<Exception>{error};
                for(int i=last;i>=0;i--)try{rollback(items[i]);}catch(Exception failure){failures.Add(failure);}
                if(failures.Count>1)throw new AggregateException("Retained value restoration and rollback failed.",failures);
                throw;
            }
        }
    }
}
