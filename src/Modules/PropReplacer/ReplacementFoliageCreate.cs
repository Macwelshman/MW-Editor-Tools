using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using MW.EditorTools.Modules.BulldozerRecovery;

namespace MW.EditorTools.Modules.PropReplacer {
 public sealed partial class PropReplacerSystem {
  private sealed class FoliageCandidate {
   internal Entity Owner,Prefab,Created,Target;internal EntityArchetype Archetype;
   internal LocalTransformCache Local;internal Game.Objects.Transform Pose;internal Game.Common.PseudoRandomSeed Seed;
   internal bool Surface,Elevation,Plant,Tree,Color,Decoration,DecorationEnabled;
   internal Game.Objects.Surface SurfaceValue;internal Game.Objects.Elevation ElevationValue;
   internal Game.Objects.Plant PlantValue;internal Game.Objects.Tree TreeValue;internal Game.Objects.Color ColorValue;
   internal Game.Rendering.MeshColor[] Colors;internal Game.Rendering.CustomMeshColor[] Custom;
   internal FoliageWitness[] Witnesses;
   internal bool ExpectMesh,ExpectBounds;
  }
  private void CandidatePut<T>(Entity id,T value) where T:unmanaged,IComponentData {var em=World.EntityManager;if(em.HasComponent<T>(id))em.SetComponentData(id,value);else em.AddComponentData(id,value);}
  private void CreateFoliageCandidates(List<FoliageCandidate> plan){
   var em=World.EntityManager;
   foreach(var c in plan){using(var created=new NativeArray<Entity>(1,Allocator.Temp)){em.CreateEntity(c.Archetype,created);c.Created=created[0];}CandidatePut(c.Created,new PrefabRef{m_Prefab=c.Prefab});CandidatePut(c.Created,new Game.Common.Owner{m_Owner=c.Owner});CandidatePut(c.Created,c.Local);CandidatePut(c.Created,c.Pose);CandidatePut(c.Created,c.Seed);
    if(c.Surface)CandidatePut(c.Created,c.SurfaceValue);else if(em.HasComponent<Game.Objects.Surface>(c.Created))em.RemoveComponent<Game.Objects.Surface>(c.Created);
    if(c.Elevation)CandidatePut(c.Created,c.ElevationValue);if(c.Plant)CandidatePut(c.Created,c.PlantValue);if(c.Tree)CandidatePut(c.Created,c.TreeValue);if(c.Color)CandidatePut(c.Created,c.ColorValue);
    if(c.Decoration){CandidatePut(c.Created,new Game.Objects.Decoration());em.SetComponentEnabled<Game.Objects.Decoration>(c.Created,c.DecorationEnabled);}
    if(c.Colors!=null){var values=em.HasBuffer<Game.Rendering.MeshColor>(c.Created)?em.GetBuffer<Game.Rendering.MeshColor>(c.Created):em.AddBuffer<Game.Rendering.MeshColor>(c.Created);foreach(var v in c.Colors)values.Add(v);}
    if(c.Custom!=null){var values=em.HasBuffer<Game.Rendering.CustomMeshColor>(c.Created)?em.GetBuffer<Game.Rendering.CustomMeshColor>(c.Created):em.AddBuffer<Game.Rendering.CustomMeshColor>(c.Created);foreach(var v in c.Custom)values.Add(v);}
    CandidatePut(c.Created,new Game.Common.Applied());CandidatePut(c.Created,new Game.Common.Created());CandidatePut(c.Created,new Game.Common.Updated());
    em.GetBuffer<Game.Objects.SubObject>(c.Owner).Add(new Game.Objects.SubObject{m_SubObject=c.Created});
    MW.EditorTools.Mod.Log.Info("Replacement foliage candidate supplied at exact destination: "+c.Created+" owner="+c.Owner+" index="+c.Local.m_PrefabSubIndex);
   }
  }
  private void RemoveFoliageCandidates(List<FoliageCandidate> plan){var em=World.EntityManager;foreach(var c in plan){if(c.Created==Entity.Null)continue;if(em.Exists(c.Owner)&&em.HasBuffer<Game.Objects.SubObject>(c.Owner)){var children=em.GetBuffer<Game.Objects.SubObject>(c.Owner);for(int i=children.Length-1;i>=0;i--)if(children[i].m_SubObject==c.Created)children.RemoveAt(i);}if(em.Exists(c.Created))em.DestroyEntity(c.Created);}}
 }
}
