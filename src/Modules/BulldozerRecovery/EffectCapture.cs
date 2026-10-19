using System;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using EditorContainer=Game.Tools.EditorContainer;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private sealed class EffectImage {
            internal Entity Original,Owner;
            internal AreaCompanionEffect Value;
            internal LocalTransformCache RawLocal;
            internal bool HasSeed,HasSurface,HasDecoration,Decoration,HasEnabledBuffer;
            internal Game.Common.PseudoRandomSeed Seed;
            internal Game.Objects.Surface Surface;
            internal System.Collections.Generic.Dictionary<PrefabBase,Payload> References;
            internal bool Valid=>Value?.Container!=null&&Value.Prefab!=null&&References!=null&&References.All(p=>NestedReferenceMatches(p.Key,p.Value));
        }
        private static readonly System.Collections.Generic.HashSet<string> EffectKnown=new System.Collections.Generic.HashSet<string>{
            "Game.Tools.EditorContainer","Game.Tools.LocalTransformCache","Game.Prefabs.PrefabRef","Game.Objects.Transform","Game.Objects.Static","Game.Objects.Object","Game.Objects.ObjectGeometry",
            "Game.Objects.Surface","Game.Objects.Decoration","Game.Objects.Elevation","Game.Common.Owner","Game.Common.PseudoRandomSeed","Game.Common.BatchesUpdated","Game.Common.EffectsUpdated",
            "Game.Rendering.CullingInfo","Game.Rendering.MeshBatch","Game.Tools.Hidden","Game.Effects.EnabledEffect","Game.Objects.SubObject","Game.Net.SubNet","Game.Net.SubLane","Game.Areas.SubArea","Unity.Entities.Simulate"
        };
        private bool EffectSettled(Entity id){var em=world.EntityManager;return em.Exists(id)&&!em.HasComponent<Game.Common.Deleted>(id)&&!em.HasComponent<Game.Common.Created>(id)&&!em.HasComponent<Game.Common.Updated>(id)&&!em.HasComponent<Temp>(id);}
        private EffectImage CaptureEffect(Entity id) {
            var em=world.EntityManager;if(!EffectSettled(id)||!IsEffectContainer(id)||!em.HasComponent<PrefabRef>(id)||!em.HasComponent<Game.Objects.Transform>(id))throw new NotSupportedException("Not a stable native effect editor container.");
            using(var types=em.GetComponentTypes(id,Allocator.Temp))foreach(var t in types)if(!EffectKnown.Contains(t.GetManagedType()?.FullName??"unknown"))throw new NotSupportedException("Unhandled effect component: "+t.GetManagedType()?.FullName);
            if((em.HasBuffer<Game.Objects.SubObject>(id)&&em.GetBuffer<Game.Objects.SubObject>(id,true).Length!=0)||
                (em.HasBuffer<Game.Net.SubNet>(id)&&em.GetBuffer<Game.Net.SubNet>(id,true).Length!=0)||(em.HasBuffer<Game.Net.SubLane>(id)&&em.GetBuffer<Game.Net.SubLane>(id,true).Length!=0)||(em.HasBuffer<Game.Areas.SubArea>(id)&&em.GetBuffer<Game.Areas.SubArea>(id,true).Length!=0))throw new NotSupportedException("Effect container has unsupported child/runtime allocations.");
            var prefabs=world.GetExistingSystemManaged<PrefabSystem>();var c=em.GetComponentData<EditorContainer>(id);var tpose=em.GetComponentData<Game.Objects.Transform>(id);
            var e=new EffectImage{Original=id,Owner=em.HasComponent<Game.Common.Owner>(id)?em.GetComponentData<Game.Common.Owner>(id).m_Owner:Entity.Null,
                Value=new AreaCompanionEffect{Original=id,Prefab=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(id)),Container=prefabs.GetPrefab<PrefabBase>(c.m_Prefab),HasTransform=true,Position=tpose.m_Position,Rotation=tpose.m_Rotation,Scale=c.m_Scale,Intensity=c.m_Intensity,Group=c.m_GroupIndex}};
            e.HasEnabledBuffer=em.HasBuffer<Game.Effects.EnabledEffect>(id);
            if(e.HasEnabledBuffer&&em.GetBuffer<Game.Effects.EnabledEffect>(id,true).Length>256)throw new NotSupportedException("Native effect allocation buffer exceeds budget.");
            if(e.Value.Prefab==null||!(e.Value.Container is EffectPrefab))throw new NotSupportedException("Effect prefab is unavailable.");
            e.Value.HasLocal=em.HasComponent<LocalTransformCache>(id);if(e.Value.HasLocal){e.RawLocal=em.GetComponentData<LocalTransformCache>(id);e.Value.Local=e.Owner==Entity.Null?e.RawLocal:AreaCompanionEffect.DetachedLocal(e.RawLocal);}
            e.Value.HasElevation=em.HasComponent<Game.Objects.Elevation>(id);if(e.Value.HasElevation)e.Value.Elevation=em.GetComponentData<Game.Objects.Elevation>(id);
            e.HasSeed=em.HasComponent<Game.Common.PseudoRandomSeed>(id);if(e.HasSeed)e.Seed=em.GetComponentData<Game.Common.PseudoRandomSeed>(id);
            e.HasSurface=em.HasComponent<Game.Objects.Surface>(id);if(e.HasSurface)e.Surface=em.GetComponentData<Game.Objects.Surface>(id);
            e.HasDecoration=em.HasComponent<Game.Objects.Decoration>(id);if(e.HasDecoration)e.Decoration=em.IsComponentEnabled<Game.Objects.Decoration>(id);
            e.References=new[]{e.Value.Prefab,e.Value.Container}.Distinct().ToDictionary(p=>p,Capture);
            if(!e.Valid||!AreaCompanionEffect.Same(e.Value,e.Value)||e.References.Values.Sum(p=>p.Weight)>250000)throw new NotSupportedException("Effect values/references are nonfinite, changed or exceed budget.");
            var placeholder=prefabs.GetEntity(e.Value.Prefab);if(!em.HasComponent<ObjectData>(placeholder))throw new NotSupportedException("Effect editor container has no supported native object archetype.");
            return e;
        }
        private string EffectReferenceDifference(EffectImage e)=>string.Join("; ",e.References.Where(p=>!NestedReferenceMatches(p.Key,p.Value)).Take(4).Select(p=>p.Key==null?"missing prefab":p.Key.name+" fields="+string.Join(",",p.Value.Fields.Where(f=>!f.Data.Matches(f.Field.GetValue(f.Owner))).Take(8).Select(f=>f.Owner.GetType().Name+"."+f.Field.Name))));
        private string EffectDetails(EffectImage e) {
            var v=e.Value;return "entity="+e.Original+" owner="+e.Owner+" inspector="+(Root?.name??"none")+" effect="+v.Container.name+" placeholder="+v.Prefab.name+
                " position="+v.Position+" rotation="+v.Rotation+" scale="+v.Scale+" intensity="+v.Intensity+" group="+v.Group+
                " local="+v.HasLocal+(v.HasLocal?" [position="+v.Local.m_Position+" rotation="+v.Local.m_Rotation+" mesh="+v.Local.m_ParentMesh+" probability="+v.Local.m_Probability+" group="+v.Local.m_GroupIndex+" rawIndex="+e.RawLocal.m_PrefabSubIndex+"]":"")+
                " elevation="+v.HasElevation+(v.HasElevation?" [value="+v.Elevation.m_Elevation+" flags="+v.Elevation.m_Flags+"]":"")+" seed="+e.HasSeed+" surface="+e.HasSurface+" decoration="+e.HasDecoration+" enabledBuffer="+e.HasEnabledBuffer;
        }
        private string EffectSourceDetails(NetworkEntry e) {
            var rows=EffectRows(e.Before);return "asset="+e.Asset.name+" owner="+e.Owner+" rows="+rows.Length+" live="+e.Effects.Length+"; "+string.Join("; ",rows.Take(8).Select((r,i)=>"row["+i+"] effect="+(r?.m_Effect?.name??"none")+" position="+r?.m_PositionOffset+" rotation="+r?.m_Rotation+" scale="+r?.m_Scale+" intensity="+r?.m_Intensity+" mesh="+r?.m_ParentMesh+" animation="+r?.m_AnimationIndex))+"; "+string.Join("; ",e.Effects.Take(8).Select((v,i)=>"live["+i+"] entity="+v.Original+" effect="+v.Container.name+" position="+v.Position+" rotation="+v.Rotation+" local="+v.HasLocal+" localPosition="+v.Local.m_Position+" localRotation="+v.Local.m_Rotation+" mesh="+v.Local.m_ParentMesh+" matches="+string.Join(",",Enumerable.Range(0,rows.Length).Where(j=>EffectEditPolicy.Descriptor(rows[j],v,e.Transform)))));
        }
        private static bool SameEffect(EffectImage a,EffectImage b)=>a.Owner==b.Owner&&a.HasEnabledBuffer==b.HasEnabledBuffer&&AreaCompanionEffect.Same(a.Value,b.Value)&&a.HasSeed==b.HasSeed&&(!a.HasSeed||a.Seed.Equals(b.Seed))&&a.HasSurface==b.HasSurface&&(!a.HasSurface||a.Surface.Equals(b.Surface))&&a.HasDecoration==b.HasDecoration&&(!a.HasDecoration||a.Decoration==b.Decoration);
    }
}
