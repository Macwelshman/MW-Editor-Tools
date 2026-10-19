using System;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using EditorContainer=Game.Tools.EditorContainer;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private void EffectComponent<T>(Entity id,bool present,T value)where T:unmanaged,IComponentData {
            if(present)PutMain(id,value);else if(world.EntityManager.HasComponent<T>(id))world.EntityManager.RemoveComponent<T>(id);
        }
        private void WriteEffectValues(Entity id,EffectImage image) {
            var em=world.EntityManager;var prefabs=world.GetExistingSystemManaged<PrefabSystem>();var effect=prefabs.GetEntity(image.Value.Container);
            if(!em.Exists(effect)||!em.HasComponent<EffectData>(effect))throw new NotSupportedException("Effect prefab is no longer available.");
            PutMain(id,new EditorContainer{m_Prefab=effect,m_Scale=image.Value.Scale,m_Intensity=image.Value.Intensity,m_GroupIndex=image.Value.Group});
            PutMain(id,new Game.Objects.Transform{m_Position=image.Value.Position,m_Rotation=image.Value.Rotation});
            EffectComponent(id,image.Value.HasLocal,image.RawLocal);EffectComponent(id,image.Value.HasElevation,image.Value.Elevation);
            EffectComponent(id,image.HasSeed,image.Seed);EffectComponent(id,image.HasSurface,image.Surface);
            EffectComponent(id,image.HasDecoration,new Game.Objects.Decoration());if(image.HasDecoration)em.SetComponentEnabled<Game.Objects.Decoration>(id,image.Decoration);
            PutMain(id,new Game.Common.Updated());PutMain(id,new Game.Common.BatchesUpdated());
            // Native editor light controls use this dedicated render-stage
            // signal for effect containers, independently of object rebuilding.
            PutMain(id,new Game.Common.EffectsUpdated());
            QueueEffectRenderRefresh(id);
        }
        private void CreateStandaloneEffect(EffectEntry e) {
            var em=world.EntityManager;var prefabs=world.GetExistingSystemManaged<PrefabSystem>();var p=prefabs.GetEntity(e.Before.Value.Prefab);
            if(!em.Exists(p)||!em.HasComponent<ObjectData>(p))throw new NotSupportedException("Native editor-container archetype unavailable.");
            var archetype=em.GetComponentData<ObjectData>(p).m_Archetype;
            using(var types=archetype.GetComponentTypes(Allocator.Temp))foreach(var type in types){var name=type.GetManagedType()?.FullName??"unknown";
                if(!EffectKnown.Contains(name)&&name!="Game.Common.Created"&&name!="Game.Common.Updated"&&name!="Game.Common.Applied")throw new NotSupportedException("Unsupported effect creation archetype: "+name);}
            using(var created=new NativeArray<Entity>(1,Allocator.Temp)){em.CreateEntity(archetype,created);e.Live=created[0];}
            PutMain(e.Live,new PrefabRef{m_Prefab=p});if(em.HasComponent<Game.Common.Owner>(e.Live))em.RemoveComponent<Game.Common.Owner>(e.Live);
            // Native CreateContainerObject adds this buffer for effect containers.
            // Recreate an empty registry; allocation indices belong to native systems.
            if(e.Before.HasEnabledBuffer&&!em.HasBuffer<Game.Effects.EnabledEffect>(e.Live))em.AddBuffer<Game.Effects.EnabledEffect>(e.Live);
            WriteEffectValues(e.Live,e.Before);PutMain(e.Live,new Game.Common.Applied());PutMain(e.Live,new Game.Common.Created());
        }
    }
}
