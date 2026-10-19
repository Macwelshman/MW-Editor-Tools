using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;
using Unity.Mathematics;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private sealed class IsolatedEffectOwner {
            internal Entity Owner;
            internal PrefabBase Asset;
            internal Game.Objects.Transform Transform;
            internal Payload Source;
            internal AreaCompanionEffect[] Effects;
            internal EffectChild[] Children;
        }
        private IsolatedEffectOwner CaptureIsolatedEffectOwner(Entity owner) {
            var em=world.EntityManager;var prefabs=world.GetExistingSystemManaged<PrefabSystem>();
            if(!em.Exists(owner)||em.HasComponent<Game.Common.Owner>(owner)||!em.HasComponent<PrefabRef>(owner)||!em.HasComponent<Game.Objects.Transform>(owner)||!em.HasBuffer<Game.Objects.SubObject>(owner)||
                em.HasComponent<Game.Common.Deleted>(owner)||em.HasComponent<Game.Common.Created>(owner)||em.HasComponent<Game.Common.Updated>(owner)||em.HasComponent<Temp>(owner))throw new NotSupportedException("Effect parent is missing, nested or updating.");
            var asset=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(owner));
            if(asset==null||asset.isReadOnly||asset is AssetStampPrefab||(Root!=null&&!ReferenceEquals(Root,asset)))throw new NotSupportedException("Effect parent is not the writable edited asset.");
            var snapshot=new IsolatedEffectOwner{Owner=owner,Asset=asset,Transform=em.GetComponentData<Game.Objects.Transform>(owner),Source=Capture(asset),Effects=CaptureAreaEffects(owner)};
            if(snapshot.Source.Weight>250000||!snapshot.Source.Matches(asset)||snapshot.Effects.Length>128||!math.all(math.isfinite(snapshot.Transform.m_Position))||!math.all(math.isfinite(snapshot.Transform.m_Rotation.value)))throw new NotSupportedException("Effect parent source/pose changed, is nonfinite or exceeds capture budget.");
            // Read the owned tree, including markers and generated utilities.
            // These entities are witnesses only: Undo never writes or recreates them.
            var nodes=new List<EffectChild>();var seen=new HashSet<Entity>{owner};var queue=new Queue<Tuple<Entity,int>>();queue.Enqueue(Tuple.Create(owner,0));
            while(queue.Count>0){var parent=queue.Dequeue();if(!em.HasBuffer<Game.Objects.SubObject>(parent.Item1))continue;
                foreach(var link in em.GetBuffer<Game.Objects.SubObject>(parent.Item1,true)){
                    var id=link.m_SubObject;if(nodes.Count>=2048||parent.Item2>=8||!seen.Add(id)||!em.Exists(id)||em.HasComponent<Game.Common.Deleted>(id)||em.HasComponent<Game.Common.Created>(id)||em.HasComponent<Game.Common.Updated>(id)||em.HasComponent<Temp>(id)||
                        !em.HasComponent<Game.Common.Owner>(id)||em.GetComponentData<Game.Common.Owner>(id).m_Owner!=parent.Item1||!em.HasComponent<PrefabRef>(id)||!em.HasComponent<Game.Objects.Transform>(id))throw new NotSupportedException("Effect parent child witnesses are pending, cyclic, missing or exceed budget.");
                    if(IsEffectContainer(id)){if(parent.Item1!=owner)throw new NotSupportedException("Nested companion effects are not supported by isolated Undo.");continue;}
                    var child=new EffectChild{Id=id,Owner=parent.Item1,Prefab=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(id)),Transform=em.GetComponentData<Game.Objects.Transform>(id),HasLocal=em.HasComponent<LocalTransformCache>(id)};
                    if(child.Prefab==null||!math.all(math.isfinite(child.Transform.m_Position))||!math.all(math.isfinite(child.Transform.m_Rotation.value)))throw new NotSupportedException("Effect parent child prefab/pose unavailable or nonfinite.");
                    if(child.HasLocal){child.Local=AreaCompanionEffect.DetachedLocal(em.GetComponentData<LocalTransformCache>(id));if(!math.all(math.isfinite(child.Local.m_Position))||!math.all(math.isfinite(child.Local.m_Rotation.value)))throw new NotSupportedException("Effect parent child cache is nonfinite.");}
                    nodes.Add(child);queue.Enqueue(Tuple.Create(id,parent.Item2+1));
                }
            }
            snapshot.Children=nodes.ToArray();return snapshot;
        }
        private static bool IsolatedChildrenSame(IsolatedEffectOwner a,IsolatedEffectOwner b)=>a.Owner==b.Owner&&ReferenceEquals(a.Asset,b.Asset)&&a.Transform.Equals(b.Transform)&&EffectChild.Matches(a.Children,b.Children);
        private static bool IsolatedSources(IsolatedEffectOwner a) {
            var rows=EffectRows(a.Source);if(rows.Length!=a.Effects.Length)return false;
            var used=new HashSet<int>();foreach(var effect in a.Effects){
                var matches=Enumerable.Range(0,rows.Length).Where(i=>!used.Contains(i)&&EffectEditPolicy.Descriptor(rows[i],effect,a.Transform,true)).ToArray();
                if(matches.Length!=1)return false;used.Add(matches[0]);
            }return true;
        }
        private static bool IsolatedSourceShape(IsolatedEffectOwner before,IsolatedEffectOwner after) {
            var a=before.Source;var b=after.Source;
            if(!a.Components.Where(c=>!(c is EffectSource)).SequenceEqual(b.Components.Where(c=>!(c is EffectSource))))return false;
            if(a.Components.Select((c,i)=>new{c,active=a.Active[i]}).Where(x=>!(x.c is EffectSource)).Any(x=>x.c.active!=x.active))return false;
            var original=a.Components.OfType<EffectSource>().SingleOrDefault();var current=b.Components.OfType<EffectSource>().SingleOrDefault();
            if(ReferenceEquals(original,null)||(!ReferenceEquals(current,null)&&!ReferenceEquals(original,current)))return false;
            return ReferenceEquals(current,null)||Array.IndexOf(a.Components,original)==Array.IndexOf(b.Components,current)&&a.Fields.Where(f=>ReferenceEquals(f.Owner,original)&&f.Field.Name!="m_Effects").All(f=>f.Data.Matches(f.Field.GetValue(current)))&&current.active==a.Active[Array.IndexOf(a.Components,original)];
        }
        private bool IsolatedAfter(EffectEntry e,IsolatedEffectOwner after,EffectImage target) {
            var before=e.IsolatedBefore;
            return before.Owner==after.Owner&&ReferenceEquals(before.Asset,after.Asset)&&before.Transform.Equals(after.Transform)&&EffectChild.NativeSettlement(before.Children,after.Children)&&IsolatedSourceShape(before,after)&&
                EffectEditPolicy.Retained(before.Effects,after.Effects.Where(v=>!e.Move||v.Original!=e.Live).ToArray(),e.Before.Original)&&
                before.Effects.Where(v=>v.Original!=e.Before.Original).All(v=>after.Effects.Any(a=>a.Original==v.Original&&AreaCompanionEffect.Same(v,a)))&&
                after.Effects.Length==before.Effects.Length-(e.Move?0:1)&&(!e.Move||after.Effects.Count(v=>v.Original==e.Live&&AreaCompanionEffect.Same(v,target.Value))==1)&&IsolatedSources(after);
        }
        private bool CanIsolatedEffectUndo(EffectEntry e) {
            if(e.IsolatedBefore.Source.Fields.Any(f=>!f.Data.ReferencesValid))return EffectUndoBlocked("original isolated source references unavailable");
            var after=CaptureIsolatedEffectOwner(e.Before.Owner);
            if(!e.IsolatedAfter.Source.Matches(after.Asset))return EffectUndoBlocked("isolated parent source differs; shape="+e.IsolatedAfter.Source.Shape(after.Asset)+" active="+string.Join(",",e.IsolatedAfter.Source.Components.Select((c,i)=>new{c,i}).Where(x=>x.c.active!=e.IsolatedAfter.Source.Active[x.i]).Select(x=>x.c.GetType().Name))+" fields="+string.Join(",",e.IsolatedAfter.Source.Fields.Where(f=>!f.Data.Matches(f.Field.GetValue(f.Owner))).Take(8).Select(f=>f.Owner.GetType().Name+"."+f.Field.Name)));
            if(!IsolatedChildrenSame(e.IsolatedAfter,after))return EffectUndoBlocked("isolated child witness differs; counts="+e.IsolatedAfter.Children.Length+"/"+after.Children.Length+" changed="+string.Join(",",e.IsolatedAfter.Children.Where(c=>!EffectChild.Matches(new[]{c},after.Children.Where(a=>a.Id==c.Id).ToArray())).Take(4).Select(c=>c.Prefab.name+" "+c.Id)));
            if(!AreaCompanionEffect.Matches(e.IsolatedAfter.Effects,after.Effects))return EffectUndoBlocked("isolated effect values differ; recorded target: "+EffectDetails(e.After??e.Before)+"; current target: "+(e.Move?EffectDetails(CaptureEffect(e.Live)):"deleted"));
            if(!e.IsolatedAfter.Effects.All(v=>after.Effects.Any(a=>a.Original==v.Original&&AreaCompanionEffect.Same(v,a))))return EffectUndoBlocked("isolated retained effect identities differ");
            return IsolatedSources(after)||EffectUndoBlocked("isolated local source descriptors no longer match");
        }
    }
}
