using System;
using System.Linq;
using System.Collections.Generic;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private static EffectSource.EffectSettings[] EffectRows(Payload source)=>source.Values.TryGetValue("EffectSource.m_Effects",out var rows)?((List<EffectSource.EffectSettings>)rows.Materialize())?.ToArray()??Array.Empty<EffectSource.EffectSettings>():Array.Empty<EffectSource.EffectSettings>();
        private bool EffectOwnerAfter(EffectEntry e,NetworkEntry after,EffectImage target) {
            var before=e.BoundBefore;
            if(!ReferenceEquals(before.Asset,after.Asset)||!before.Transform.Equals(after.Transform)||before.References.Any(p=>!NestedReferenceMatches(p.Key,p.Value))||
                !before.Before.Components.Where(c=>!(c is EffectSource)).SequenceEqual(after.Before.Components.Where(c=>!(c is EffectSource)))||
                before.Before.Components.Select((c,i)=>new{c,active=before.Before.Active[i]}).Where(p=>!(p.c is EffectSource)).Any(p=>p.c.active!=p.active))return false;
            var oldSource=before.Before.Components.OfType<EffectSource>().SingleOrDefault();var newSource=after.Before.Components.OfType<EffectSource>().SingleOrDefault();
            if(ReferenceEquals(oldSource,null)||(!ReferenceEquals(newSource,null)&&!ReferenceEquals(oldSource,newSource))||(e.Move&&ReferenceEquals(newSource,null)))return false;
            if(!ReferenceEquals(newSource,null)&&before.Before.Active[Array.IndexOf(before.Before.Components,oldSource)]!=newSource.active)return false;
            var ancillary=new[]{"ObjectSubObjects.m_SubObjects","ObjectSubAreas.m_SubAreas","ObjectSubNets.m_SubNets","ObjectSubLanes.m_SubLanes"};
            if(before.Before.Fields.Where(f=>f.Owner.GetType().Name+"."+f.Field.Name!="EffectSource.m_Effects"&&!ancillary.Contains(f.Owner.GetType().Name+"."+f.Field.Name)).Any(f=>!f.Data.Matches(f.Field.GetValue(f.Owner))))return false;
            var props=MatchForest(before.Props,after.Props);
            var referenceMap=EntityReferenceMap.WithOwnerPrefab(new Dictionary<Entity,Entity>(),before.Owner,before.OwnerPrefab,after.OwnerPrefab);
            if(props==null||!NetworkOwnerValid(after)||!before.Network.TryMap(after.Network,true,out var networkMap))return false;
            foreach(var pair in networkMap)if(!EntityReferenceMap.Add(referenceMap,pair.Key,pair.Value))return false;
            if(before.CompanionMap!=null&&(after.CompanionMap==null||!MapAreaLanes(before.CompanionMap,after.CompanionMap,referenceMap,true)))return false;
            if(!AddForestMap(before.Props,props,referenceMap))return false;
            if(props==null||!before.Props.Select((g,i)=>NestedGroupAtIndex(g,props[i],BoundDescriptorIndex.Resolve(after.Asset.GetComponent<ObjectSubObjects>()?.m_SubObjects,g[0].Prefab,g[0].Local),referenceMap)).All(v=>v)||
                !AreasMatch(before.Areas,e.Before.Owner)||!before.Network.TryMap(after.Network,true,out _)||!NetworkRowsMatchGraph(after.Before,after.Network))return false;
            var retained=after.Effects.Where(f=>!e.Move||f.Original!=e.Live).ToArray();
            if(!EffectEditPolicy.Retained(before.Effects,retained,e.Before.Original)||!CompanionSourcesValid(before.Effects.Where(f=>f.Original!=e.Before.Original).Select(f=>f.Original),retained.Select(f=>f.Original)))return false;
            if(e.Move&&after.Effects.Count(f=>f.Original==e.Live&&AreaCompanionEffect.Same(f,target.Value))!=1)return false;
            return after.Effects.Length==before.Effects.Length-(e.Move?0:1)&&EffectEditPolicy.Sources(EffectRows(after.Before),after.Effects,after.Transform,e.Move&&EffectEditPolicy.GroundDetach(e.Before.Value,target.Value)?e.Live:Entity.Null);
        }
        private void PrepareEffectBoundEntry(EffectEntry e) {
            foreach(var key in new[]{"ObjectSubObjects.m_SubObjects","ObjectSubAreas.m_SubAreas","ObjectSubNets.m_SubNets","ObjectSubLanes.m_SubLanes"})
                if(e.BoundBefore.Before.Values.ContainsKey(key)&&e.BoundAfter.Before.Values.TryGetValue(key,out var value)){
                    e.BoundBefore.Before.Values[key]=value;foreach(var f in e.BoundBefore.Before.Fields.Where(f=>f.Owner.GetType().Name+"."+f.Field.Name==key))f.Data=value;
                }
            var rows=e.BoundAfter.Asset.GetComponent<ObjectSubObjects>()?.m_SubObjects;
            var roots=e.BoundBefore.Props.Select(g=>BoundDescriptorIndex.ForSource(rows,g[0].Prefab,g[0].Local)).ToArray();for(int i=0;i<roots.Length;i++)e.BoundBefore.Props[i][0].Local=roots[i];
            e.BoundBefore.After=e.BoundAfter.Before;e.BoundBefore.Target=e.Before.Original;e.BoundBefore.TargetPrefab=e.Before.Value.Container;e.BoundBefore.EffectEntry=e;
        }
        private bool EffectBoundRestored(EffectEntry e) {
            var matches=CaptureAreaEffects(e.Before.Owner).Where(v=>AreaCompanionEffect.Same(v,e.Before.Value)).ToArray();if(matches.Length!=1||!EffectSettled(matches[0].Original))return false;
            var actual=CaptureEffect(matches[0].Original);
            if(!actual.Valid||actual.HasSeed!=e.Before.HasSeed||actual.HasSurface!=e.Before.HasSurface||actual.HasDecoration!=e.Before.HasDecoration)return false;
            bool changed=false;var em=world.EntityManager;
            if(actual.HasSeed&&!actual.Seed.Equals(e.Before.Seed)){em.SetComponentData(actual.Original,e.Before.Seed);changed=true;}
            if(actual.HasSurface&&!actual.Surface.Equals(e.Before.Surface)){em.SetComponentData(actual.Original,e.Before.Surface);changed=true;}
            if(actual.HasDecoration&&actual.Decoration!=e.Before.Decoration){em.SetComponentEnabled<Game.Objects.Decoration>(actual.Original,e.Before.Decoration);changed=true;}
            bool fresh=e.Live!=actual.Original;e.Live=actual.Original;
            if(fresh){QueueEffectRenderRefresh(e.Live);InvalidateEffectHover(e.Live,e.Before.Owner);return false;}
            return !changed&&EffectRenderReady(e.Live)&&SameEffect(e.Before,actual);
        }
    }
}
