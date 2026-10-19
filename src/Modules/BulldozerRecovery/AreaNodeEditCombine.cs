using System.Linq;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private static bool SameAreaEditPayload(Payload a,Payload b)=>a.Components.SequenceEqual(b.Components)&&a.Active.SequenceEqual(b.Active)&&a.Values.Count==b.Values.Count&&
            a.Values.All(v=>b.Values.TryGetValue(v.Key,out var other)&&v.Value.Matches(other.Materialize()));
        private AreaMoveEntry CombineAddedNodeMove(AreaMoveEntry next) {
            try{return CombineAddedNodeMoveCore(next);}
            catch(System.Exception error){Mod.Log.Info("Node addition/move combination unavailable; retaining separate edit: "+error.Message);return next;}
        }
        private AreaMoveEntry CombineAddedNodeMoveCore(AreaMoveEntry next) {
            var prior=areaMoveHistory.Latest;
            if(prior==null||prior.AddedNode<0||prior.Bound!=next.Bound||prior.Live!=next.Target||prior.Owner!=next.Owner||
                !prior.Before.Valid||!prior.After.Valid||!StandaloneAreaMatches(prior.After,next.Before)||
                !prior.After.Network.TryMap(next.Before.Network,true,out _)||
                !AreaNodeEditPolicy.OnlyAddedNodeMoved(next.Before.Data,next.After.Data,prior.AddedNode,next.Bound))return next;
            if(next.Bound) {
                var a=prior.BoundAfter;var b=next.BoundBefore;
                if(!ReferenceEquals(a.Asset,b.Asset)||a.Owner!=b.Owner||!a.Transform.Equals(b.Transform)||!SameAreaEditPayload(a.Before,b.Before)||
                    prior.BoundBefore.References.Any(p=>!NestedReferenceMatches(p.Key,p.Value))||!a.Network.TryMap(b.Network,true,out _)||
                    !AreaCompanionEffect.Matches(a.Effects,b.Effects)||a.Areas.Length!=b.Areas.Length||
                    a.Areas.Any(old=>b.Areas.Count(n=>n.Original==old.Original&&AreaEqual(old,n))!=1))return next;
                var props=MatchForest(a.Props,b.Props);
                if(props==null||a.Props.Where((g,i)=>!ExactNestedGroup(g,props[i])||!GroupSources(g).SequenceEqual(GroupSources(props[i]))).Any())return next;
            }
            Mod.Log.Info("Combined node addition and move of the same added node; index="+prior.AddedNode);
            return new AreaMoveEntry{Target=prior.Target,Owner=prior.Owner,Live=next.Live,Before=prior.Before,After=next.After,
                BoundBefore=prior.BoundBefore,BoundAfter=next.BoundAfter,AddedNode=prior.AddedNode};
        }
    }
}
