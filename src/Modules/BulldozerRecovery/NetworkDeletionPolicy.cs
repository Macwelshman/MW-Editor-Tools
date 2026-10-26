using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    // A deletion may remove the selected edge, its generated descendants and
    // newly orphaned endpoints. Every retained value and link remains exact.
    internal static class NetworkDeletionPolicy {
        internal static bool Matches(AreaCompanionNetwork before, Entity target, AreaCompanionNetwork after) => Matches(before,target,after,false,out _);
        internal static string ExplainMismatch(AreaCompanionNetwork before,Entity target,AreaCompanionNetwork after) {
            return Matches(before,target,after,true,out var reason)?"matched":reason;
        }
        private static bool Refused(out string reason,string value) {reason=value;return false;}
        private static bool Matches(AreaCompanionNetwork before, Entity target, AreaCompanionNetwork after,bool explain,out string reason) {
            reason=null;
            if(before==null||after==null||before.Owner!=after.Owner||!before.Items.Any(i=>i.Id==target&&i.HasEdge)||
                after.Items.Any(i=>i.Id==target)||before.Items.Select(i=>i.Id).Distinct().Count()!=before.Items.Length||after.Items.Select(i=>i.Id).Distinct().Count()!=after.Items.Length)return Refused(out reason,"invalid owner, target or duplicate network identity");
            var old=before.Items.ToDictionary(i=>i.Id);
            if(after.Items.Any(i=>!old.ContainsKey(i.Id)))return Refused(out reason,"unexpected new retained network identity");
            var allowed=new HashSet<Entity>{target};
            bool changed;
            do {
                changed=false;
                foreach(var item in before.Items)
                    if(allowed.Contains(item.Owner)||(item.HasLane&&new[]{item.Lane.m_StartNode,item.Lane.m_MiddleNode,item.Lane.m_EndNode}
                        .Any(p=>allowed.Any(id=>id.Index==p.GetOwnerIndex()))))changed|=allowed.Add(item.Id);
            }while(changed);
            var edge=old[target].Edge;
            foreach(var endpoint in new[]{edge.m_Start,edge.m_End}) {
                if(!old.TryGetValue(endpoint,out var node)||!node.HasNode)continue;
                if(!before.Items.Any(i=>!allowed.Contains(i.Id)&&((i.HasEdge&&(i.Edge.m_Start==endpoint||i.Edge.m_End==endpoint))||
                    (i.HasLane&&new[]{i.Lane.m_StartNode,i.Lane.m_MiddleNode,i.Lane.m_EndNode}.Any(p=>p.GetOwnerIndex()==endpoint.Index)))))allowed.Add(endpoint);
            }
            var live=new HashSet<Entity>(after.Items.Select(i=>i.Id));
            if(before.Items.Any(i=>!live.Contains(i.Id)&&!allowed.Contains(i.Id)))return Refused(out reason,"unrelated network companion removed");
            var expected=new AreaCompanionNetwork{Owner=before.Owner,Anchors=before.Anchors.Where(a=>after.Anchors.Any(b=>b.Id==a.Id)).ToArray(),RootNets=before.RootNets.Where(live.Contains).ToArray(),
                RootLanes=before.RootLanes.Where(l=>live.Contains(l.m_SubLane)).ToArray(),Items=before.Items.Where(i=>live.Contains(i.Id)).Select(i=>{
                    // Preserve every captured typed lane field. Only links to the
                    // proved deleted target/descendants/orphan endpoints are pruned.
                    var retained=i.DetachedCopy();
                    retained.Connections=i.Connections.Where(live.Contains).ToArray();
                    retained.SubNets=i.SubNets.Where(live.Contains).ToArray();
                    retained.SubLanes=i.SubLanes.Where(l=>live.Contains(l.m_SubLane)).ToArray();
                    return retained;
                }).ToArray()};
            if(!expected.TryMap(after,true,out var map))return Refused(out reason,explain?expected.ExplainMismatch(after,true):"retained network typed data or links differ");
            if(map.Any(pair=>pair.Key!=pair.Value))return Refused(out reason,"retained network identity remapped");
            return true;
        }
    }
}
