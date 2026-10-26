using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Game.Tools;
using Game.Prefabs;
using Game.Common;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private sealed class CacheWrite {
            internal Entity Id;internal bool Local,Seed;
            internal LocalCurveCache BeforeLocal,AfterLocal;
            internal PseudoRandomSeed BeforeSeed,AfterSeed;
        }
        private bool RepairNetworkCaches(NetworkEntry entry,AreaCompanionNetwork actual,Dictionary<Entity,Entity> rootMap){
            var em=world.EntityManager;var mapping=new Dictionary<Entity,Entity>(rootMap);
            var expected=entry.Network.Items.AsEnumerable();var live=actual.Items.AsEnumerable();
            BoundCompanions areas=null;
            if(entry.CompanionMap!=null){
                areas=CaptureBoundCompanions(entry.Owner,true,true);
                if(!AreasMatch(entry.Areas,entry.Owner)||!MapAreaLanes(entry.CompanionMap,areas,mapping,false,true,false))return false;
                expected=expected.Concat(entry.CompanionMap.AreaLanes.Values.SelectMany(g=>g.Items));
                live=live.Concat(areas.AreaLanes.Values.SelectMany(g=>g.Items));
            }
            var targets=live.ToDictionary(i=>i.Id);var writes=new List<CacheWrite>();
            foreach(var item in expected){
                if(!mapping.TryGetValue(item.Id,out var id)||!targets.TryGetValue(id,out var target))return false;
                if(item.HasLocal&&!AreaCompanionNetwork.CanRestoreLocal(item.Local,target.Local))return false;
                bool local=item.HasLocal&&!FiniteCurve(target.Local.m_Curve),seed=item.HasSeed&&!item.Seed.Equals(target.Seed);
                if(local||seed)writes.Add(new CacheWrite{Id=id,Local=local,Seed=seed,BeforeLocal=target.Local,AfterLocal=item.Local,BeforeSeed=target.Seed,AfterSeed=item.Seed});
            }
            bool applied=RetainedValueTransaction.Apply(writes,
                w=>(!w.Local||AreaCompanionNetwork.LocalSnapshotEqual(em.GetComponentData<LocalCurveCache>(w.Id),w.BeforeLocal))&&(!w.Seed||em.GetComponentData<PseudoRandomSeed>(w.Id).Equals(w.BeforeSeed)),
                w=>{if(w.Local)em.SetComponentData(w.Id,w.AfterLocal);if(w.Seed)em.SetComponentData(w.Id,w.AfterSeed);},
                w=>{if(w.Local)em.SetComponentData(w.Id,w.BeforeLocal);if(w.Seed)em.SetComponentData(w.Id,w.BeforeSeed);});
            if(!applied)return false;
            if(entry.CompanionMap!=null&&!MapAreaLanes(entry.CompanionMap,CaptureBoundCompanions(entry.Owner,true),new Dictionary<Entity,Entity>(rootMap),true,true,true))return false;
            if(writes.Any(w=>w.Local))Mod.Log.Info("Bound lane/network finite local caches restored after complete live graph preflight: "+writes.Count(w=>w.Local));
            return true;
        }
        private bool RollbackPropsUnchanged(NetworkEntry entry){
            var actual=MatchForest(entry.Props,CaptureAreaProps(entry.Owner));if(actual==null)return false;
            for(int g=0;g<entry.Props.Length;g++){
                if(entry.Props[g].Length!=actual[g].Length)return false;
                for(int n=0;n<entry.Props[g].Length;n++){
                    var e=entry.Props[g][n];var a=actual[g][n];var local=e.Local;
                    if(n==0)local.m_PrefabSubIndex=BoundDescriptorIndex.ResolveNative(entry.Asset.GetComponent<ObjectSubObjects>()?.m_SubObjects,e.Prefab,e.Local,entry.Transform,e.Data.Transform);
                    if(e.Parent!=a.Parent||e.Depth!=a.Depth||!ReferenceEquals(e.Prefab,a.Prefab)||!SameNestedPlacement(e,a)||!local.Equals(a.Local)||e.HasSurface!=a.HasSurface||e.HasColor!=a.HasColor||e.HasDecoration!=a.HasDecoration||e.HasPlant!=a.HasPlant||e.HasTree!=a.HasTree||!e.Data.Matches(new MainPropData(e.Data.Transform,e.Data.Seed,a.Data.Surface,e.Data.Colours,a.Data.CustomColours)))return false;
                }
            }
            return true;
        }
        private bool TryRollbackFence(string reason){
            var entry=networkPending;
            if(!networkRestoring||entry==null||entry.RollbackOf!=null||entry.BoundRecord!=null||entry.Move||entry.AreaMove||entry.EffectEntry!=null||entry.AfterNetwork==null)return false;
            try{
                if(!NetworkOwnerValid(entry)||!entry.Before.Matches(entry.Asset)||entry.References.Any(p=>!NestedReferenceMatches(p.Key,p.Value)))return false;
                if(!RollbackPropsUnchanged(entry))return false;
                var rollback=new NetworkEntry{RollbackOf=entry,Asset=entry.Asset,Owner=entry.Owner,OwnerPrefab=entry.OwnerPrefab,Target=entry.Target,TargetPrefab=entry.TargetPrefab,Lane=entry.Lane,
                    Before=entry.After,After=entry.Before,Network=entry.AfterNetwork,Props=entry.AfterProps??entry.Props,Areas=entry.Areas,Effects=entry.Effects,References=entry.References,Transform=entry.Transform,CompanionMap=entry.CompanionMap};
                Mod.Log.Warn("Fence Undo failed; attempting guarded recovery of the recorded deleted-fence state: "+reason);
                StartBoundRestoration(rollback,true);return networkPending==rollback;
            }catch(Exception error){Mod.Log.Warn("Fence rollback could not start: "+error.Message);return false;}
        }
    }
}
