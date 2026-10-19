using System;
using System.Linq;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        // Supply the missing leaf before parent regeneration. Native nearest-object
        // matching then has an exact-placement candidate instead of a neighbour.
        private Entity PrepareFoliageCandidate(Record entry,bool undo) {
            var group=entry.Nested;
            if(!undo||entry.Area||entry.ForestFrom==null||group==null||group.Length!=1)return Entity.Null;
            var node=group[0];
            if((!node.HasPlant&&!node.HasTree)||node.HasPlaceholder||node.HasStreetLight||node.Effects.Length!=0||node.EnabledEffects.Length!=0||(node.Prefab.GetComponent<ObjectSubObjects>()?.m_SubObjects?.Length??0)!=0)return Entity.Null;
            if(!entry.ForestFrom.Any(g=>ReferenceEquals(g[0].Prefab,node.Prefab)))return Entity.Null;
            var em=world.EntityManager;
            if(em.Exists(node.Original)||!em.HasBuffer<Game.Objects.SubObject>(entry.Owner))throw new NotSupportedException("Foliage candidate requires a deleted original and stable owner buffer.");
            var prefab=world.GetExistingSystemManaged<PrefabSystem>().GetEntity(node.Prefab);
            if(!em.Exists(prefab)||!em.HasComponent<ObjectData>(prefab))throw new NotSupportedException("Foliage native archetype unavailable.");
            var archetype=em.GetComponentData<ObjectData>(prefab).m_Archetype;
            using(var types=archetype.GetComponentTypes(Allocator.Temp))foreach(var type in types) {
                var name=type.GetManagedType()?.FullName;
                if(!MainKnownTypes.Contains(name??"unknown")&&name!="Game.Objects.Plant"&&name!="Game.Objects.Tree"&&name!="Game.Simulation.UpdateFrame"&&name!="Game.Common.Created"&&name!="Game.Common.Updated"&&name!="Game.Common.Applied")throw new NotSupportedException("Unhandled foliage creation component: "+name);
            }
            var id=Entity.Null;
            try {
                using(var created=new NativeArray<Entity>(1,Allocator.Temp)){em.CreateEntity(archetype,created);id=created[0];}
                PutMain(id,new PrefabRef{m_Prefab=prefab});PutMain(id,new Game.Common.Owner{m_Owner=entry.Owner});
                PutMain(id,node.Local);PutMain(id,node.Data.Transform);PutMain(id,node.Data.Seed);
                if(node.HasSurface)PutMain(id,node.Data.Surface);
                else if(em.HasComponent<Game.Objects.Surface>(id))em.RemoveComponent<Game.Objects.Surface>(id);
                if(node.HasElevation)PutMain(id,node.Elevation);
                if(node.HasColor)PutMain(id,node.Color);
                if(node.HasDecoration){PutMain(id,new Game.Objects.Decoration());em.SetComponentEnabled<Game.Objects.Decoration>(id,node.DecorationEnabled);}
                if(node.HasPlant)PutMain(id,node.Plant);if(node.HasTree)PutMain(id,node.Tree);
                if(node.Data.Colours!=null){var values=em.HasBuffer<Game.Rendering.MeshColor>(id)?em.GetBuffer<Game.Rendering.MeshColor>(id):em.AddBuffer<Game.Rendering.MeshColor>(id);values.Clear();foreach(var value in node.Data.Colours)values.Add(value);}
                if(node.Data.CustomColours!=null){var values=em.HasBuffer<Game.Rendering.CustomMeshColor>(id)?em.GetBuffer<Game.Rendering.CustomMeshColor>(id):em.AddBuffer<Game.Rendering.CustomMeshColor>(id);values.Clear();foreach(var value in node.Data.CustomColours)values.Add(value);}
                PutMain(id,new Game.Common.Applied());PutMain(id,new Game.Common.Created());PutMain(id,new Game.Common.Updated());
                // Structural changes above invalidate previously acquired buffers.
                em.GetBuffer<Game.Objects.SubObject>(entry.Owner).Add(new Game.Objects.SubObject{m_SubObject=id});
                Mod.Log.Info("Bound foliage exact-placement candidate created="+id+"; retained same-prefab identities must survive regeneration.");
                return id;
            }catch{RemoveFoliageCandidate(entry.Owner,id);throw;}
        }
        private void RemoveFoliageCandidate(Entity owner,Entity id) {
            if(id==Entity.Null)return;
            var em=world.EntityManager;
            if(em.Exists(owner)&&em.HasBuffer<Game.Objects.SubObject>(owner)){
                var children=em.GetBuffer<Game.Objects.SubObject>(owner);
                for(int i=children.Length-1;i>=0;i--)if(children[i].m_SubObject==id)children.RemoveAt(i);
            }
            if(em.Exists(id))em.DestroyEntity(id);
        }
        private static bool RetainedFoliageIdentitiesMatch(Record entry,NestedNode[][] actual) {
            if(entry.FoliageCandidate==Entity.Null)return true;
            return entry.ForestFrom.All(group=>actual.Any(current=>SameNestedPlacement(group[0],current[0])&&group.Length==current.Length&&group.Select((node,i)=>node.Original==current[i].Original).All(value=>value)));
        }
    }
}
