using System;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;
using MW.EditorTools.Modules.BulldozerRecovery;
using EditorContainer=Game.Tools.EditorContainer;

namespace MW.EditorTools.Modules.PropReplacer
{
    public sealed partial class PropReplacerSystem
    {
        private sealed class ViewportHitNotReady:InvalidOperationException {
            internal readonly Entity Expected;
            internal readonly DefaultToolSystem Tool;
            internal ViewportHitNotReady(Entity expected,DefaultToolSystem tool):base("Waiting for the native viewport highlight to agree with the cursor."){Expected=expected;Tool=tool;}
        }
        private Entity? requiredViewportHit;
        private Entity ViewportHit()
        {
            var activeTool=World.GetExistingSystemManaged<ToolSystem>()?.activeTool;
            if(activeTool is IViewportTargetSource<Entity> source)
            {
                Entity selected;string reason;bool found;
                if(viewportShortcutIntent){
                    if(!(activeTool is IViewportHoverSource<Entity> hover))throw new InvalidOperationException("This tool has no verified viewport hover source.");
                    found=hover.TryGetHoverTarget(out selected,out reason);
                }else found=source.TryGetTarget(out selected,out reason);
                if(!found)throw new InvalidOperationException(reason);
                if(requiredViewportHit.HasValue&&requiredViewportHit.Value!=selected)throw new InvalidOperationException("The selected target changed; clipboard action cancelled.");
                return selected;
            }
            var tool=World.GetExistingSystemManaged<ToolSystem>()?.activeTool as DefaultToolSystem;
            if(tool==null)throw new InvalidOperationException("Use the default selection tool and hover the destination element.");
            var cached=(Entity)hoverEntity.GetValue(tool);var em=World.EntityManager;
            Game.Common.RaycastResult ray;
            var raycast=World.GetExistingSystemManaged<ToolRaycastSystem>();
            var highlighted=NormalizeViewportEntity(cached);
            if(raycast==null||!raycast.GetRaycastResult(out ray)) {
                if(highlighted==Entity.Null)throw new InvalidOperationException("Hover an element until its native highlight is visible, then use the shortcut.");
                if(requiredViewportHit.HasValue&&requiredViewportHit.Value!=highlighted)throw new InvalidOperationException("The highlighted target changed; the pending clipboard action was cancelled.");
                MW.EditorTools.Mod.Log.Info("Prop Replacer viewport awaiting current ray: highlighted="+cached+" target="+highlighted);
                throw new ViewportHitNotReady(highlighted,tool);
            }
            var owner=NormalizeViewportEntity(ray.m_Owner);
            var hit=NormalizeViewportEntity(ray.m_Hit.m_HitEntity==Entity.Null?ray.m_Owner:ray.m_Hit.m_HitEntity);
            if(requiredViewportHit.HasValue&&requiredViewportHit.Value!=hit)throw new InvalidOperationException("The cursor target changed; the pending clipboard action was cancelled.");
            Func<Entity,bool> live=e=>em.Exists(e)&&!em.HasComponent<Game.Common.Deleted>(e)&&!em.HasComponent<Temp>(e);
            Func<Entity,Tuple<bool,Entity>> parent=e=>em.HasComponent<Game.Common.Owner>(e)?Tuple.Create(true,em.GetComponentData<Game.Common.Owner>(e).m_Owner):Tuple.Create(false,Entity.Null);
            if(!ViewportHitPolicy.MatchesPath(hit,owner,live,parent,OwnerLinks))throw new InvalidOperationException("The actual viewport hit does not have a unique ownership path to its current raycast owner.");
            if(ViewportHitPolicy.Agrees(hit,owner,highlighted,live,parent,OwnerLinks)){
                MW.EditorTools.Mod.Log.Info("Prop Replacer viewport resolved: highlighted="+cached+" rayOwner="+ray.m_Owner+" hit="+hit);
                if(em.Exists(hit)&&!em.HasComponent<Game.Common.Owner>(hit)){
                    using(var types=em.GetComponentTypes(hit,Unity.Collections.Allocator.Temp))MW.EditorTools.Mod.Log.Info("Prop Replacer unowned hit components: "+string.Join(",",types.Select(t=>t.GetManagedType()?.FullName??"unknown")));
                }
                return hit;
            }
            MW.EditorTools.Mod.Log.Info("Prop Replacer viewport awaiting agreement: highlighted="+cached+" normalized="+highlighted+" rayOwner="+ray.m_Owner+" normalizedOwner="+owner+" hit="+hit);
            throw new ViewportHitNotReady(hit,tool);
        }
        private Entity NormalizeViewportEntity(Entity id) {
            var em=World.EntityManager;Entity result;
            return ViewportHitPolicy.Normalize(id,e=>e!=Entity.Null&&em.Exists(e)&&!em.HasComponent<Game.Common.Deleted>(e),e=>{
                if(em.HasComponent<Temp>(e))return Tuple.Create(true,em.GetComponentData<Temp>(e).m_Original);
                if(em.HasComponent<Game.Notifications.Icon>(e)&&!em.HasComponent<Game.Common.Owner>(e)&&em.HasComponent<Game.Common.Target>(e))return Tuple.Create(true,em.GetComponentData<Game.Common.Target>(e).m_Target);
                return Tuple.Create(false,Entity.Null);
            },out result)?result:Entity.Null;
        }
        private int OwnerLinks(Entity owner,Entity id)
        {
            var em=World.EntityManager;if(!em.Exists(owner))return 0;int links=0;
            if(em.HasBuffer<Game.Objects.SubObject>(owner)){var values=em.GetBuffer<Game.Objects.SubObject>(owner,true);for(int i=0;i<values.Length;i++)if(values[i].m_SubObject==id)links++;}
            if(em.HasBuffer<Game.Areas.SubArea>(owner)){var values=em.GetBuffer<Game.Areas.SubArea>(owner,true);for(int i=0;i<values.Length;i++)if(values[i].m_Area==id)links++;}
            if(em.HasBuffer<Game.Net.SubNet>(owner)){var values=em.GetBuffer<Game.Net.SubNet>(owner,true);for(int i=0;i<values.Length;i++)if(values[i].m_SubNet==id)links++;}
            if(em.HasBuffer<Game.Net.SubLane>(owner)){var values=em.GetBuffer<Game.Net.SubLane>(owner,true);for(int i=0;i<values.Length;i++)if(values[i].m_SubLane==id)links++;}
            return links;
        }
        private Row ResolveViewportRow()
        {
            return ResolveViewportRow(ViewportHit(),clipboard.RowType);
        }
        private Row ResolveViewportRow(Entity id,Type rowType,bool presentationOnly=false) {
            var em=World.EntityManager;
            if(id==Entity.Null||!em.Exists(id)||em.HasComponent<Game.Common.Deleted>(id)||em.HasComponent<Temp>(id)||em.HasComponent<Game.Common.Created>(id)||em.HasComponent<Game.Common.Updated>(id))throw new InvalidOperationException("The highlighted destination is missing or still updating.");
            if(!em.HasComponent<Game.Common.Owner>(id))throw new NotSupportedException("Viewport replacement requires an element bound to an editable asset.");
            var owner=em.GetComponentData<Game.Common.Owner>(id).m_Owner;
            if(!em.Exists(owner)||!em.HasComponent<PrefabRef>(owner)||em.HasComponent<Game.Common.Deleted>(owner)||em.HasComponent<Temp>(owner)||em.HasComponent<Game.Common.Created>(owner)||em.HasComponent<Game.Common.Updated>(owner))throw new InvalidOperationException("The destination owner is unavailable or still updating.");
            var prefabs=World.GetExistingSystemManaged<PrefabSystem>();
            var root=prefabs?.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(owner));
            if(root==null||root.isReadOnly)throw new InvalidOperationException("The highlighted element belongs to a read-only or unavailable asset.");
            var ancestors=new System.Collections.Generic.List<PrefabBase>();var chain=new System.Collections.Generic.HashSet<Entity>();var ancestor=owner;
            while(em.HasComponent<Game.Common.Owner>(ancestor))
            {
                if(chain.Count>=8||!chain.Add(ancestor))throw new InvalidOperationException("Nested owner chain is cyclic or too deep.");
                var parent=em.GetComponentData<Game.Common.Owner>(ancestor).m_Owner;
                if(OwnerLinks(parent,ancestor)!=1||!em.HasComponent<PrefabRef>(parent)||!em.HasComponent<LocalTransformCache>(ancestor))throw new InvalidOperationException("Nested owner link or placement is unavailable.");
                var parentPrefab=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(parent));
                var childPrefab=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(ancestor));
                if(parentPrefab==null||parentPrefab.isReadOnly||em.HasComponent<Game.Common.Deleted>(parent)||em.HasComponent<Temp>(parent)||em.HasComponent<Game.Common.Created>(parent)||em.HasComponent<Game.Common.Updated>(parent))throw new InvalidOperationException("A nested parent asset is read-only or still updating.");
                ResolveViewportObjectIndex(parentPrefab,childPrefab,ancestor,parent);
                ancestors.Add(parentPrefab);ancestor=parent;
            }
            if(context.Asset!=null&&!ReferenceEquals(context.Asset,root)&&!ancestors.Any(p=>ReferenceEquals(p,context.Asset)))throw new InvalidOperationException("The highlighted element belongs to a different asset. Inspect its owning asset first.");
            if(OwnerLinks(owner,id)!=1)throw new InvalidOperationException("The highlighted element is not linked exactly once to its owner.");
            if(!em.HasComponent<PrefabRef>(id))throw new InvalidOperationException("The highlighted element has no prefab identity.");
            var prefab=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(id));
            bool effect=em.HasComponent<EditorContainer>(id)&&prefabs.GetPrefab<PrefabBase>(em.GetComponentData<EditorContainer>(id).m_Prefab) is EffectPrefab;
            if(effect)prefab=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<EditorContainer>(id).m_Prefab);
            else if(em.HasComponent<EditorContainer>(id)&&prefabs.GetPrefab<PrefabBase>(em.GetComponentData<EditorContainer>(id).m_Prefab) is NetLanePrefab)
                prefab=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<EditorContainer>(id).m_Prefab);
            var rows=ResolveRows(root).Where(r=>r.Value.GetType()==rowType&&ReferenceEquals(r.ReferenceField.GetValue(r.Value),prefab)).ToArray();
            if(rows.Length==0){
                var container=em.HasComponent<EditorContainer>(id)?prefabs.GetPrefab<PrefabBase>(em.GetComponentData<EditorContainer>(id).m_Prefab):null;
                using(var types=em.GetComponentTypes(id,Unity.Collections.Allocator.Temp))MW.EditorTools.Mod.Log.Info("Prop Replacer target mismatch: copied="+(clipboard?.Replacement?.name??"selection")+" copiedType="+rowType.FullName+" target="+id+" prefab="+(prefab?.name??"null")+" prefabType="+prefab?.GetType().FullName+" container="+(container?.name??"none")+" containerType="+container?.GetType().FullName+" owner="+owner+" root="+root.name+" components="+string.Join(",",types.Select(t=>t.GetManagedType()?.FullName))+" sourceRows="+string.Join(" | ",ResolveRows(root).Select(r=>r.Index+":"+r.Value.GetType().Name+":"+(r.ReferenceField.GetValue(r.Value) as PrefabBase)?.name)));
                throw new InvalidOperationException("Copied "+(clipboard?.Label??"element").ToLowerInvariant()+" "+(clipboard?.Replacement?.name??"selection")+" does not match the highlighted source row ("+(prefab?.name??"unknown")+").");
            }
            Row[] matches;
            if(effect&&rowType==typeof(EffectSource.EffectSettings))
            {
                if(!em.HasComponent<Game.Objects.Transform>(id)||!em.HasComponent<Game.Objects.Transform>(owner))throw new NotSupportedException("Effect placement is unavailable.");
                var c=em.GetComponentData<EditorContainer>(id);var pose=em.GetComponentData<Game.Objects.Transform>(id);
                var image=new AreaCompanionEffect{Original=id,Prefab=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(id)),Container=prefab,HasTransform=true,Position=pose.m_Position,Rotation=pose.m_Rotation,Scale=c.m_Scale,Intensity=c.m_Intensity,Group=c.m_GroupIndex,HasLocal=em.HasComponent<LocalTransformCache>(id)};
                if(image.HasLocal)image.Local=em.GetComponentData<LocalTransformCache>(id);
                var ownerPose=em.GetComponentData<Game.Objects.Transform>(owner);
                matches=rows.Where(r=>EffectEditPolicy.Descriptor((EffectSource.EffectSettings)r.Value,image,ownerPose)).ToArray();
            }
            else if(!effect&&rowType==typeof(ObjectSubObjectInfo)&&em.HasComponent<Game.Objects.Object>(id)&&em.HasComponent<LocalTransformCache>(id))
            {
                int index=ResolveViewportObjectIndex(root,prefab,id,owner);
                matches=rows.Where(r=>r.Index==index).ToArray();
            }
            else if(rowType==typeof(ObjectSubAreaInfo)&&em.HasComponent<Game.Areas.Area>(id)&&em.HasBuffer<LocalNodeCache>(id))
            {
                if(!em.HasBuffer<Game.Areas.Node>(id)||!em.HasComponent<Game.Objects.Transform>(owner))throw new NotSupportedException("Live area geometry relative to its owner is unavailable.");
                var nativeLocal=em.GetBuffer<LocalNodeCache>(id,true);var nativeNodes=em.GetBuffer<Game.Areas.Node>(id,true);
                if(nativeNodes.Length<3||nativeNodes.Length>4096||nativeLocal.Length!=nativeNodes.Length)throw new InvalidOperationException("Area nodes or their placement cache are incomplete.");
                var local=new LocalNodeCache[nativeLocal.Length];var nodes=new Unity.Mathematics.float3[nativeNodes.Length];
                for(int i=0;i<local.Length;i++){local[i]=nativeLocal[i];nodes[i]=nativeNodes[i].m_Position;}
                var positions=ViewportGeometrySource.Area(nodes,local,em.GetComponentData<Game.Objects.Transform>(owner));
                matches=rows.Where(r=>ViewportGeometrySource.AreaMatches((ObjectSubAreaInfo)r.Value,prefab,positions,local)).ToArray();
            }
            else if(rowType==typeof(ObjectSubNetInfo)||rowType==typeof(ObjectSubLaneInfo))
            {
                matches=ResolveViewportNetworkRows(rows,prefab,id,owner,presentationOnly);
            }
            else throw new NotSupportedException("The highlighted element has no supported exact placement cache.");
            if(matches.Length!=1){if(presentationOnly&&(rowType==typeof(ObjectSubNetInfo)||rowType==typeof(ObjectSubLaneInfo)))TraceNetworkRowFailure(rows,id,owner);throw new InvalidOperationException("The highlighted placement matches "+matches.Length+" source rows; viewport replacement refused.");}
            matches[0].Ancestors=ancestors.Distinct().ToArray();return matches[0];
        }
        private int ResolveViewportObjectIndex(PrefabBase root,PrefabBase prefab,Entity id,Entity owner)
        {
            var em=World.EntityManager;
            if(!em.HasComponent<Game.Objects.Transform>(id)||!em.HasComponent<Game.Objects.Transform>(owner))throw new NotSupportedException("Object placement relative to its owner is unavailable.");
            var local=em.GetComponentData<LocalTransformCache>(id);
            var rows=root.GetComponent<ObjectSubObjects>()?.m_SubObjects;
            bool[] groundSources=null;string groundInfo="no compiled source proof";
            var nativeOwner=em.GetComponentData<PrefabRef>(owner).m_Prefab;
            if(rows!=null&&em.HasBuffer<Game.Prefabs.SubObject>(nativeOwner)) {
                var native=em.GetBuffer<Game.Prefabs.SubObject>(nativeOwner,true);
                groundInfo="compiledCount="+native.Length+" sourceCount="+rows.Length;
                var compiled=new Game.Prefabs.SubObject[native.Length];for(int i=0;i<compiled.Length;i++)compiled[i]=native[i];
                var identity=em.GetComponentData<PrefabRef>(id).m_Prefab;
                groundSources=ViewportPropSource.GroundSources(rows,prefab,compiled,identity);
                if(local.m_PrefabSubIndex>=0&&local.m_PrefabSubIndex<rows.Length)groundInfo+=" indexedGroundProof="+groundSources[local.m_PrefabSubIndex];
                if(local.m_PrefabSubIndex<0||local.m_PrefabSubIndex>=rows.Length||!groundSources[local.m_PrefabSubIndex])
                    foreach(var j in Enumerable.Range(0,compiled.Length).Where(i=>compiled[i].m_Prefab==identity).OrderBy(i=>Unity.Mathematics.math.lengthsq(compiled[i].m_Position-local.m_Position)).Take(4)) {
                        var value=compiled[j];groundInfo+=" compiled["+j+"] flags="+value.m_Flags+" position="+value.m_Position+" rotation="+value.m_Rotation+" parent="+value.m_ParentIndex+" probability="+value.m_Probability+" group="+value.m_GroupIndex;
                    }
            }
            int index;
            try{index=ViewportPropSource.Resolve(rows,prefab,local,em.GetComponentData<Game.Objects.Transform>(id),em.GetComponentData<Game.Objects.Transform>(owner),groundSources);}
            catch {
                try{MW.EditorTools.Mod.Log.Info("Prop Replacer source mismatch: target="+id+" owner="+owner+" asset="+root.name+" prefab="+(prefab?.name??"null")+" "+groundInfo+" "+ViewportPropSource.Describe(rows,prefab,local,em.GetComponentData<Game.Objects.Transform>(id),em.GetComponentData<Game.Objects.Transform>(owner)));}catch{}
                throw;
            }
            if(!rows[index].m_Position.Equals(local.m_Position)||!rows[index].m_Rotation.Equals(local.m_Rotation))
                MW.EditorTools.Mod.Log.Info("Prop Replacer native source projection: target="+id+" owner="+owner+" row="+index+" cachePosition="+local.m_Position+" sourcePosition="+rows[index].m_Position+" "+groundInfo);
            return index;
        }
    }
}
