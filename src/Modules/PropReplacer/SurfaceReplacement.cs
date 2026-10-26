using System;
using System.Linq;
using System.Collections.Generic;
using Game.Prefabs;
using Game.Tools;
using MW.EditorTools.History;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace MW.EditorTools.Modules.PropReplacer {
 public sealed partial class PropReplacerSystem {
  private bool surfaceFailed;
  private string surfaceUndoReason="";
  private bool SurfaceUndoBlocked(string reason){if(surfaceUndoReason!=reason){surfaceUndoReason=reason;MW.EditorTools.Mod.Log.Info("Surface replacement Undo blocked: "+reason);}return false;}

  private sealed class SurfacePlan {
   internal Entity Root, From, To;
   internal bool Rendered, FromNavigation, ToNavigation, FromHangaround, ToHangaround, ToNeedsLanes;
   internal bool[] TargetLaneShape;
   internal int[] Indices;
   internal Entity[] Live;
   internal SurfaceWitness[] Witnesses;
  }
  private sealed class SurfaceWitness {
   internal Entity Owner;
   internal Game.Areas.Area Metadata;
   internal Game.Areas.Node[] Nodes;
   internal LocalNodeCache[] Cache;
   internal bool HadLanes;
   internal Game.Net.SubLane[] Lanes;
   internal string Difference(SurfaceWitness other){
    if(Owner!=other.Owner)return "owner reference differs";
    if(!Metadata.Equals(other.Metadata))return "area flags differ: captured="+other.Metadata.m_Flags+" current="+Metadata.m_Flags;
    if(Nodes.Length!=other.Nodes.Length||Cache.Length!=other.Cache.Length)return "boundary/cache count differs";
    for(int i=0;i<Nodes.Length;i++)if(!Nodes[i].Equals(other.Nodes[i]))return "boundary node "+i+" differs: captured="+other.Nodes[i].m_Position+" current="+Nodes[i].m_Position;
    for(int i=0;i<Cache.Length;i++)if(!Cache[i].Equals(other.Cache[i]))return "local cache node "+i+" differs: captured="+other.Cache[i].m_Position+" current="+Cache[i].m_Position;
    if(!Lanes.SequenceEqual(other.Lanes))return "companion lane references differ";
    return null;
   }
  }
  private void SurfaceSettled(Entity id) {
   var em=World.EntityManager;
   if(!em.Exists(id)||em.HasComponent<Game.Common.Created>(id)||em.HasComponent<Game.Common.Updated>(id)||
      em.HasComponent<Game.Common.Deleted>(id)||em.HasComponent<Temp>(id))
    throw new InvalidOperationException("Wait for surface and owner updates to finish before replacing.");
  }
  private SurfacePlan PlanSurfaceReplacement(Row row,PrefabBase from,PrefabBase to,int[] indices) {
   bool surface=from?.GetType()==typeof(SurfacePrefab)&&to?.GetType()==typeof(SurfacePrefab);
   if(!surface && !(from?.GetType()==typeof(SpacePrefab)&&to?.GetType()==typeof(SpacePrefab)))throw new NotSupportedException("Replace a surface with a surface, or an area with an area of the same native layout.");
   if(row.Ancestors?.Length>0)throw new NotSupportedException("Surface replacement inside a nested asset is not supported.");
   var em=World.EntityManager;var registry=World.GetExistingSystemManaged<PrefabSystem>();
   VerifySurfaceApi(registry,row.Root);
   if(!registry.TryGetEntity(row.Root,out var root)||!registry.TryGetEntity(from,out var oldId)||!registry.TryGetEntity(to,out var newId))throw new InvalidOperationException("Surface or owning prefab is no longer registered.");
   bool rendered=em.HasComponent<RenderedAreaData>(oldId);
   if(rendered!=em.HasComponent<RenderedAreaData>(newId)||surface&&!rendered)throw new NotSupportedException("Source and destination area render layouts differ.");
   if(rendered&&!surfaceRendererAvailable)throw new NotSupportedException("Area rendering refresh is unavailable; replacement refused.");
   SurfaceSettled(root);SurfaceSettled(oldId);SurfaceSettled(newId);
   if(!em.HasComponent<AreaData>(oldId)||!em.HasComponent<AreaData>(newId)||!em.HasComponent<AreaGeometryData>(oldId)||!em.HasComponent<AreaGeometryData>(newId))throw new NotSupportedException("Surface prefab data is incomplete.");
   if(rendered&&(!em.HasComponent<RenderedAreaData>(oldId)||!em.HasComponent<RenderedAreaData>(newId)))throw new NotSupportedException("Surface rendering data is incomplete.");
   if(rendered&&(em.GetComponentData<RenderedAreaData>(newId).m_BatchIndex<0||em.GetComponentData<RenderedAreaData>(oldId).m_BatchIndex<0))throw new InvalidOperationException("Wait for surface materials to finish loading before replacing.");
   bool fromNavigation,toNavigation,fromHangaround,toHangaround,fromNeedsLanes,toNeedsLanes;
   using(var a=em.GetComponentData<AreaData>(oldId).m_Archetype.GetComponentTypes(Allocator.Temp))
   using(var b=em.GetComponentData<AreaData>(newId).m_Archetype.GetComponentTypes(Allocator.Temp)) {
    var beforeTypes=a.Select(t=>t.TypeIndex).ToArray();var afterTypes=b.Select(t=>t.TypeIndex).ToArray();
    var navigation=ComponentType.ReadOnly<Game.Areas.Navigation>().TypeIndex;var hangaround=ComponentType.ReadOnly<Game.Areas.HangaroundLocation>().TypeIndex;var lanes=ComponentType.ReadOnly<Game.Net.SubLane>().TypeIndex;
    var allowed=surface?new[]{navigation}.Take(0):new[]{navigation,hangaround,lanes};
    if(beforeTypes.Except(afterTypes).Concat(afterTypes.Except(beforeTypes)).Except(allowed).Any())throw new NotSupportedException("Area native layouts differ beyond supported navigation/activity markers; replacement refused.");
    fromNavigation=beforeTypes.Contains(navigation);toNavigation=afterTypes.Contains(navigation);fromHangaround=beforeTypes.Contains(hangaround);toHangaround=afterTypes.Contains(hangaround);fromNeedsLanes=beforeTypes.Contains(lanes);toNeedsLanes=afterTypes.Contains(lanes);
   }
   var oldGeometry=em.GetComponentData<AreaGeometryData>(oldId);var newGeometry=em.GetComponentData<AreaGeometryData>(newId);
   if(oldGeometry.m_Type!=newGeometry.m_Type||oldGeometry.m_Flags!=newGeometry.m_Flags)throw new NotSupportedException("Surface geometry behavior differs; replacement refused.");
   if(row.Root.components.Count(c=>c is ObjectSubAreas)!=1||!(row.Owner is ObjectSubAreas areas)||!ReferenceEquals(areas.prefab,row.Root)||!ReferenceEquals(areas.m_SubAreas,row.ListField.GetValue(row.Owner))||indices.Length==0||indices.Distinct().Count()!=indices.Length)throw new InvalidOperationException("Surface source ownership or selection is ambiguous.");
   if(!em.HasBuffer<Game.Prefabs.SubArea>(root)||!em.HasBuffer<SubAreaNode>(root))throw new InvalidOperationException("Compiled surface source is unavailable.");
   var compiled=em.GetBuffer<Game.Prefabs.SubArea>(root,true);var nodes=em.GetBuffer<SubAreaNode>(root,true);
   if(compiled.Length!=areas.m_SubAreas.Length||compiled.Length>1024||nodes.Length>65536)throw new NotSupportedException("Surface source list is incomplete or exceeds the replacement budget.");
   int cursor=0;
   for(int i=0;i<compiled.Length;i++) {
    var source=areas.m_SubAreas[i];var item=compiled[i];
    if(source?.m_AreaPrefab==null||source.m_NodePositions==null||source.m_NodePositions.Length<3||source.m_NodePositions.Length>4096||
       (source.m_ParentMeshes!=null&&source.m_ParentMeshes.Length!=0&&source.m_ParentMeshes.Length!=source.m_NodePositions.Length)||
       !registry.TryGetEntity(source.m_AreaPrefab,out var prefab)||item.m_Prefab!=prefab||item.m_NodeRange.x!=cursor||item.m_NodeRange.y!=cursor+source.m_NodePositions.Length||item.m_NodeRange.y>nodes.Length)throw new InvalidOperationException("Compiled surface source differs from its editable rows.");
    for(int n=0;n<source.m_NodePositions.Length;n++)if(!math.all(math.isfinite(source.m_NodePositions[n]))||!nodes[cursor+n].m_Position.Equals(source.m_NodePositions[n])||nodes[cursor+n].m_ParentMesh!=(source.m_ParentMeshes==null||source.m_ParentMeshes.Length==0?-1:source.m_ParentMeshes[n]))throw new InvalidOperationException("Compiled surface boundary or parent mesh differs from its source.");
    cursor=item.m_NodeRange.y;
   }
   if(cursor!=nodes.Length||indices.Any(i=>i<0||i>=compiled.Length||compiled[i].m_Prefab!=oldId))throw new InvalidOperationException("Surface selection or source changed.");
   var live=new List<Entity>();var witnesses=new List<SurfaceWitness>();int owners=0,totalNodes=0,totalLanes=0;
   using(var query=em.CreateEntityQuery(ComponentType.ReadOnly<PrefabRef>(),ComponentType.ReadOnly<Game.Objects.Transform>(),ComponentType.ReadOnly<Game.Areas.SubArea>()))
   using(var entities=query.ToEntityArray(Allocator.Temp))foreach(var owner in entities) {
    if(em.GetComponentData<PrefabRef>(owner).m_Prefab!=root||em.HasComponent<Game.Common.Deleted>(owner)||em.HasComponent<Temp>(owner))continue;
    SurfaceSettled(owner);owners++;
    // Nested instances need ancestor source synchronization and are refused as a group.
    if(em.HasComponent<Game.Common.Owner>(owner))throw new NotSupportedException("Surface replacement has a nested live owner; replacement refused.");
    var children=em.GetBuffer<Game.Areas.SubArea>(owner,true);var candidates=new Dictionary<int,List<Entity>>();foreach(int i in indices)candidates[i]=new List<Entity>();
    if(children.Length>4096)throw new NotSupportedException("Surface owner exceeds the replacement budget.");
    for(int c=0;c<children.Length;c++) {
     var id=children[c].m_Area;
     SurfaceSettled(id);
     if(!em.HasComponent<PrefabRef>(id)||em.GetComponentData<PrefabRef>(id).m_Prefab!=oldId)continue;
     if(rendered&&!em.HasComponent<Game.Areas.Batch>(id))throw new NotSupportedException("Live surface rendering batch is unavailable.");
     if(!em.HasComponent<Game.Common.Owner>(id)||em.GetComponentData<Game.Common.Owner>(id).m_Owner!=owner||!(surface?em.HasComponent<Game.Areas.Surface>(id):em.HasComponent<Game.Areas.Space>(id))||!em.HasBuffer<Game.Areas.Node>(id)||!em.HasBuffer<LocalNodeCache>(id)||!em.HasBuffer<Game.Areas.Triangle>(id)||em.GetBuffer<Game.Areas.Triangle>(id,true).Length==0)throw new NotSupportedException("Live surface geometry or ownership is incomplete.");
     if(em.HasComponent<Game.Areas.Navigation>(id)!=fromNavigation||em.HasComponent<Game.Areas.HangaroundLocation>(id)!=fromHangaround)throw new InvalidOperationException("Area behaviour markers differ from the source prefab.");
     if(fromNeedsLanes&&!em.HasBuffer<Game.Net.SubLane>(id))throw new InvalidOperationException("The source area companion lane buffer is unavailable.");
     var flags=em.GetComponentData<Game.Areas.Area>(id).m_Flags;
     if((flags&Game.Areas.AreaFlags.Complete)==0||(flags&(Game.Areas.AreaFlags.Slave|Game.Areas.AreaFlags.NoTriangles))!=0)throw new NotSupportedException("Surface must have a complete independent boundary.");
     var positions=em.GetBuffer<Game.Areas.Node>(id,true);var cache=em.GetBuffer<LocalNodeCache>(id,true);
     if(positions.Length<3||positions.Length>4096||cache.Length!=positions.Length)throw new NotSupportedException("Live surface boundary or placement cache is incomplete.");
     var points=new float3[positions.Length];var locals=new LocalNodeCache[cache.Length];
     for(int n=0;n<points.Length;n++)points[n]=positions[n].m_Position;for(int n=0;n<locals.Length;n++)locals[n]=cache[n];
     var localPoints=ViewportGeometrySource.Area(points,locals,em.GetComponentData<Game.Objects.Transform>(owner));
     var matches=Enumerable.Range(0,areas.m_SubAreas.Length).Where(i=>ViewportGeometrySource.AreaMatches(areas.m_SubAreas[i],from,localPoints,locals)).ToArray();
     if(matches.Length!=1)throw new InvalidOperationException("Live surface does not map to one unique source boundary.");
     if(candidates.TryGetValue(matches[0],out var selected))selected.Add(id);
    }
    foreach(int i in indices) {
     if(candidates[i].Count!=1)throw new InvalidOperationException("Each replaced surface needs exactly one live destination per owner.");
     var id=candidates[i][0];live.Add(id);
     var boundary=em.GetBuffer<Game.Areas.Node>(id,true);var local=em.GetBuffer<LocalNodeCache>(id,true);
     if((totalNodes+=boundary.Length)>65536)throw new NotSupportedException("Live surface boundaries exceed the replacement budget.");
     var savedNodes=new Game.Areas.Node[boundary.Length];var savedCache=new LocalNodeCache[local.Length];
     for(int n=0;n<savedNodes.Length;n++)savedNodes[n]=boundary[n];for(int n=0;n<savedCache.Length;n++)savedCache[n]=local[n];
     if(em.HasBuffer<Game.Net.SubLane>(id)&&(totalLanes+=em.GetBuffer<Game.Net.SubLane>(id,true).Length)>65536)throw new NotSupportedException("Area companion lanes exceed the replacement budget.");
     var laneRefs=new Game.Net.SubLane[em.HasBuffer<Game.Net.SubLane>(id)?em.GetBuffer<Game.Net.SubLane>(id,true).Length:0];
     if(laneRefs.Length>0){var laneBuffer=em.GetBuffer<Game.Net.SubLane>(id,true);for(int l=0;l<laneRefs.Length;l++)laneRefs[l]=laneBuffer[l];}
     witnesses.Add(new SurfaceWitness{HadLanes=em.HasBuffer<Game.Net.SubLane>(id),Lanes=laneRefs,Owner=owner,Metadata=em.GetComponentData<Game.Areas.Area>(id),Nodes=savedNodes,Cache=savedCache});
    }
   }
   if(owners==0||live.Count>4096||live.Distinct().Count()!=live.Count)throw new InvalidOperationException("Surface replacement requires uniquely owned live destinations.");
   var ordered=Enumerable.Range(0,live.Count).OrderBy(i=>live[i].Index).ToArray();
   return new SurfacePlan{FromNavigation=fromNavigation,ToNavigation=toNavigation,FromHangaround=fromHangaround,ToHangaround=toHangaround,ToNeedsLanes=toNeedsLanes,Rendered=rendered,Root=root,From=oldId,To=newId,Indices=indices,Live=ordered.Select(i=>live[i]).ToArray(),Witnesses=ordered.Select(i=>witnesses[i]).ToArray()};
  }
  private void WriteSurfaceReferences(SurfacePlan plan,bool forward) {
   var em=World.EntityManager;var prefab=forward?plan.To:plan.From;
   // Keep every entity identity, node, cache, owner link and companion reference.
   var compiled=em.GetBuffer<Game.Prefabs.SubArea>(plan.Root);
   foreach(int i in plan.Indices) { var value=compiled[i];value.m_Prefab=prefab;compiled[i]=value; }
   for(int n=0;n<plan.Live.Length;n++) {
    var id=plan.Live[n];
    SetAreaMarker<Game.Areas.Navigation>(id,forward?plan.ToNavigation:plan.FromNavigation);
    SetAreaMarker<Game.Areas.HangaroundLocation>(id,forward?plan.ToHangaround:plan.FromHangaround);
    bool needsLanes=forward?(plan.TargetLaneShape==null?plan.ToNeedsLanes||plan.Witnesses[n].HadLanes:plan.TargetLaneShape[n]):plan.Witnesses[n].HadLanes;
    if(needsLanes&&!em.HasBuffer<Game.Net.SubLane>(id))em.AddBuffer<Game.Net.SubLane>(id);
    else if(!needsLanes&&em.HasBuffer<Game.Net.SubLane>(id)) {
     if(em.GetBuffer<Game.Net.SubLane>(id,true).Length!=0)throw new InvalidOperationException("New companion lanes appeared; area layout restoration refused.");
     em.RemoveComponent<Game.Net.SubLane>(id);
    }
    em.SetComponentData(id,new PrefabRef{m_Prefab=prefab});
   }
  }
  private void SetAreaMarker<T>(Entity id,bool required) where T:unmanaged,IComponentData {
   var em=World.EntityManager;
   if(required&&!em.HasComponent<T>(id))em.AddComponent<T>(id);
   else if(!required&&em.HasComponent<T>(id))em.RemoveComponent<T>(id);
  }
  private void ApplySurfaceReplacement(Row row,SurfacePlan plan,PrefabSnapshot desired,PrefabSnapshot rollback,bool begin=false) {
   bool dirty=row.Root.isDirty;
   ReleaseAreaRender(plan);
   try {
    if(begin)BeforeWrite?.Invoke();
    row.ListField.SetValue(row.Owner,desired.Materialize());WriteSurfaceReferences(plan,true);
    if(!desired.Matches(row.ListField.GetValue(row.Owner)))throw new InvalidOperationException("Surface source did not verify.");
    // The old compiled source already equals the new editable source. Native
    // CompareAreas sees no area-list change and does not destroy/recreate areas.
    row.Root.isDirty=true;Refresh(row.Root);
    if(!plan.Rendered)foreach(var id in plan.Live)if(!World.EntityManager.HasComponent<Game.Common.Updated>(id))World.EntityManager.AddComponent<Game.Common.Updated>(id);
    row.List=row.ListField.GetValue(row.Owner) as System.Collections.IList;selectedValue=null;pendingViewportClipboard=null;
    if(plan.Rendered)QueueSurfaceRender(plan);
   }catch(Exception error) {
    try {
     row.ListField.SetValue(row.Owner,rollback.Materialize());WriteSurfaceReferences(plan,false);row.Root.isDirty=dirty;
     row.List=row.ListField.GetValue(row.Owner) as System.Collections.IList;Refresh(row.Root);
     if(plan.Rendered&&!surfaceFailed)QueueSurfaceRender(new SurfacePlan{Rendered=true,Root=plan.Root,From=plan.To,To=plan.From,Indices=plan.Indices,Live=plan.Live,Witnesses=plan.Witnesses});
    }catch(Exception failure) {
     surfaceFailed=true;MW.EditorTools.Mod.Log.Error(failure,"Surface reference rollback failed; reload without saving.");
     throw new InvalidOperationException("Surface rollback could not complete. Reload without saving before further replacements.",error);
    }
    throw;
   }
  }
  private void ReplaceSurface(Row row,bool batch,PrefabBase original) {
   var replacement=clipboard.Replacement;
   var indices=Enumerable.Range(0,row.List.Count).Where(i=>row.List[i] is ObjectSubAreaInfo area&&ReferenceEquals(area.m_AreaPrefab,original)&&(batch||i==row.Index)).ToArray();
   var plan=PlanSurfaceReplacement(row,original,replacement,indices);
   var before=PrefabSnapshot.Take(row.ListField.GetValue(row.Owner));
   var desired=(ObjectSubAreaInfo[])before.Materialize();foreach(int i in indices)desired[i].m_AreaPrefab=(AreaPrefab)replacement;
   var after=PrefabSnapshot.Take(desired);var selected=context.Asset;
   var guardWeight=ReplacementGuards(row).Sum(g=>g.Values.Sum(v=>v.Weight));
   if(before.Weight+after.Weight+guardWeight+plan.Witnesses.Sum(w=>w.Lanes.Length)>500000)throw new NotSupportedException("Surface replacement exceeds the snapshot budget.");
   ApplySurfaceReplacement(row,plan,after,before,true);
   var guards=ReplacementGuards(row);
   var related=new[]{selected,row.Root,original,replacement};
   Func<bool> valid=()=>context?.EditorAvailable==true&&(context.Asset==null||related.Any(p=>ReferenceEquals(context.Asset,p)));
   Func<bool> ready=()=> {
    if(surfaceFailed)return SurfaceUndoBlocked("Surface replacement/refresh failed; reload without saving.");
    if(surfaceRenderPending!=null)return SurfaceUndoBlocked("Waiting for the surface material refresh.");
    if(!valid())return SurfaceUndoBlocked("The editor or owning asset context changed.");
    if(context.DialogBusy)return SurfaceUndoBlocked("Close the current editor dialog.");
    if(!before.ReferencesValid||!after.ReferencesValid)return SurfaceUndoBlocked("A captured prefab reference is unavailable.");
    if(!after.Matches(row.ListField.GetValue(row.Owner)))return SurfaceUndoBlocked("The replaced source list changed after replacement.");
    foreach(var guard in guards)if(!guard.Matches())return SurfaceUndoBlocked("Owning source/component data changed after replacement.");
    try {
     var reverse=PlanSurfaceReplacement(row,replacement,original,indices);
     if(!reverse.Live.SequenceEqual(plan.Live))return SurfaceUndoBlocked("Live destination identities differ from the retained surface entities.");
     for(int i=0;i<reverse.Witnesses.Length;i++){var difference=reverse.Witnesses[i].Difference(plan.Witnesses[i]);if(difference!=null)return SurfaceUndoBlocked("Surface "+reverse.Live[i]+": "+difference);}
     if(surfaceUndoReason!=""){surfaceUndoReason="";MW.EditorTools.Mod.Log.Info("Surface replacement Undo ready: retained identities, boundaries, source data and references verified.");}
     return true;
    }catch(Exception error){return SurfaceUndoBlocked(error.Message);}
   };
   RecordUndo(new ReplacementUndoAction(original is SurfacePrefab?"surface replacement":"area replacement",ready,()=> {
    if(!ready())return false;
    var reverse=PlanSurfaceReplacement(row,replacement,original,indices);
    reverse.TargetLaneShape=plan.Witnesses.Select(w=>w.HadLanes).ToArray();
    ApplySurfaceReplacement(row,reverse,before,after);
    surfaceRenderUndo=reverse.Rendered;status=reverse.Rendered?"Restoring original surface material. Boundary and settings retained.":"Area replacement undone. Boundary, settings and companion references retained.";MW.EditorTools.Mod.Log.Info(status);return true;
   },valid));
   status=(plan.Rendered?"Updating ":"Replaced ")+indices.Length+(original is SurfacePrefab?(indices.Length==1?" surface":" surfaces"):(indices.Length==1?" area":" areas"))+" to "+replacement.name+". Destination boundaries, settings and companion references retained.";
   MW.EditorTools.Mod.Log.Info("Surface reference replacement: retained "+plan.Live.Length+" live entities; source rows="+string.Join(",",indices));
  }
 }
}
