using System;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private string isolatedEffectReason;
        private void AlignIsolatedEffectIndices(IsolatedEffectOwner owner) {
            var em=world.EntityManager;var rows=EffectRows(owner.Source);
            foreach(var effect in CaptureAreaEffects(owner.Owner)){
                var matches=Enumerable.Range(0,rows.Length).Where(i=>EffectEditPolicy.Descriptor(rows[i],effect,owner.Transform,true)).ToArray();
                if(matches.Length!=1||!em.HasComponent<LocalTransformCache>(effect.Original))throw new NotSupportedException("Restored effect has no unique source index.");
                var cache=em.GetComponentData<LocalTransformCache>(effect.Original);
                if(cache.m_PrefabSubIndex!=matches[0]){cache.m_PrefabSubIndex=matches[0];em.SetComponentData(effect.Original,cache);}
            }
        }
        private void RestoreIsolatedEffect(EffectEntry e) {
            if(!CanIsolatedEffectUndo(e))throw new NotSupportedException("Effect parent changed before isolated Undo.");
            var em=world.EntityManager;bool created=false;
            try {
                ApplyIsolatedEffectSource(e.IsolatedBefore);
                if(e.Move)WriteEffectValues(e.Live,e.Before);
                else {
                    CreateStandaloneEffect(e);created=true;
                    PutMain(e.Live,new Game.Common.Owner{m_Owner=e.Before.Owner});
                    LinkIsolatedEffect(e.Before.Owner,e.Live);
                }
                // Native deletion may compact effect source indices. Restore
                // indices on effect containers only, after unique row matching.
                AlignIsolatedEffectIndices(e.IsolatedBefore);
            }catch {
                if(created&&em.Exists(e.Live)){
                    var links=em.GetBuffer<Game.Objects.SubObject>(e.Before.Owner);for(int i=links.Length-1;i>=0;i--)if(links[i].m_SubObject==e.Live)links.RemoveAt(i);
                    em.DestroyEntity(e.Live);
                }
                if(e.Move&&em.Exists(e.Live))WriteEffectValues(e.Live,e.After);
                ApplyIsolatedEffectSource(e.IsolatedAfter);
                AlignIsolatedEffectIndices(e.IsolatedAfter);
                throw;
            }
        }
        private bool IsolatedEffectRestored(EffectEntry e) {
            isolatedEffectReason="native effect initialization pending";
            if(!EffectSettled(e.Live))return false;
            var live=CaptureEffect(e.Live);
            if(!SameEffect(e.Before,live)){isolatedEffectReason="restored target differs. Before: "+EffectDetails(e.Before)+"; Actual: "+EffectDetails(live);return false;}
            var actual=CaptureIsolatedEffectOwner(e.Before.Owner);var before=e.IsolatedBefore;var after=e.IsolatedAfter;
            isolatedEffectReason="unrelated child witnesses or non-effect source fields differ";
            if(!IsolatedChildrenSame(after,actual)||!after.Source.Components.Where(c=>!(c is EffectSource)).SequenceEqual(actual.Source.Components.Where(c=>!(c is EffectSource)))||
                after.Source.Components.Select((c,i)=>new{c,active=after.Source.Active[i]}).Where(x=>!(x.c is EffectSource)).Any(x=>x.c.active!=x.active)||
                after.Source.Fields.Where(f=>!(f.Owner is EffectSource)).Any(f=>!f.Data.ReferencesValid||!f.Data.Matches(f.Field.GetValue(f.Owner))))return false;
            var original=before.Source.Components.OfType<EffectSource>().Single();var source=actual.Asset.GetComponent<EffectSource>();
            isolatedEffectReason="restored EffectSource fields, order, references or enabled state differ";
            if(ReferenceEquals(source,null)||Array.IndexOf(actual.Source.Components,source)!=Array.IndexOf(before.Source.Components,original)||source.active!=before.Source.Active[Array.IndexOf(before.Source.Components,original)]||
                before.Source.Fields.Where(f=>ReferenceEquals(f.Owner,original)).Any(f=>!f.Data.ReferencesValid||!f.Data.Matches(f.Field.GetValue(source))))return false;
            isolatedEffectReason="restored effect count or unique local source descriptors differ";
            if(actual.Effects.Length!=before.Effects.Length||!IsolatedSources(actual))return false;
            var rows=EffectRows(before.Source);var em=world.EntityManager;
            foreach(var effect in actual.Effects){var matches=Enumerable.Range(0,rows.Length).Where(i=>EffectEditPolicy.Descriptor(rows[i],effect,before.Transform,true)).ToArray();
                if(matches.Length!=1||em.GetComponentData<LocalTransformCache>(effect.Original).m_PrefabSubIndex!=matches[0]){isolatedEffectReason="restored effect source cache index differs: "+effect.Original;return false;}}
            isolatedEffectReason="retained effect identity or placement differs";
            return before.Effects.All(v=>actual.Effects.Count(a=>a.Original==(v.Original==e.Before.Original?e.Live:v.Original)&&AreaCompanionEffect.Same(v,a))==1);
        }
    }
}
