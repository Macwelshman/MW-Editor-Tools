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
        private Entity ViewportHit()
        {
            var tool=World.GetExistingSystemManaged<ToolSystem>()?.activeTool as DefaultToolSystem;
            if(tool==null)throw new InvalidOperationException("Use the default selection tool and hover the destination element.");
            var cached=(Entity)hoverEntity.GetValue(tool);var em=World.EntityManager;
            Game.Common.RaycastResult ray;
            var raycast=World.GetExistingSystemManaged<ToolRaycastSystem>();
            if(raycast==null||!raycast.GetRaycastResult(out ray)||ray.m_Owner!=cached)throw new InvalidOperationException("No current viewport hit agrees with the highlighted owner.");
            var hit=ray.m_Hit.m_HitEntity;
            if(hit==Entity.Null)hit=cached;
            bool direct=hit==cached;
            if(em.Exists(hit)&&em.HasComponent<Temp>(hit)){var original=em.GetComponentData<Temp>(hit).m_Original;if(original==Entity.Null)throw new InvalidOperationException("The highlighted marker has no original element.");hit=original;}
            if(direct||ViewportHitPolicy.MatchesPath(hit,cached,e=>em.Exists(e)&&!em.HasComponent<Game.Common.Deleted>(e),e=>em.HasComponent<Game.Common.Owner>(e)?Tuple.Create(true,em.GetComponentData<Game.Common.Owner>(e).m_Owner):Tuple.Create(false,Entity.Null),OwnerLinks)){
                if(em.Exists(hit)&&em.HasComponent<Game.Notifications.Icon>(hit)&&!em.HasComponent<Game.Common.Owner>(hit)&&em.HasComponent<Game.Common.Target>(hit)){
                    var marker=hit;hit=em.GetComponentData<Game.Common.Target>(hit).m_Target;
                    if(em.Exists(hit)&&em.HasComponent<Temp>(hit))hit=em.GetComponentData<Temp>(hit).m_Original;
                    if(hit==Entity.Null||!em.Exists(hit)||em.HasComponent<Game.Common.Deleted>(hit)||em.HasComponent<Temp>(hit))throw new InvalidOperationException("The highlighted icon has no live original target.");
                    MW.EditorTools.Mod.Log.Info("Prop Replacer icon target resolved: marker="+marker+" target="+hit);
                }
                MW.EditorTools.Mod.Log.Info("Prop Replacer viewport resolved: owner="+cached+" hit="+hit);
                if(em.Exists(hit)&&!em.HasComponent<Game.Common.Owner>(hit)){
                    using(var types=em.GetComponentTypes(hit,Unity.Collections.Allocator.Temp))MW.EditorTools.Mod.Log.Info("Prop Replacer unowned hit components: "+string.Join(",",types.Select(t=>t.GetManagedType()?.FullName??"unknown")));
                }
                return hit;
            }
            throw new InvalidOperationException("The actual viewport hit does not have a unique ownership path to its highlighted owner.");
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
            var id=ViewportHit();var em=World.EntityManager;
            if(id==Entity.Null||!em.Exists(id)||em.HasComponent<Game.Common.Deleted>(id)||em.HasComponent<Temp>(id)||em.HasComponent<Game.Common.Created>(id)||em.HasComponent<Game.Common.Updated>(id))throw new InvalidOperationException("The highlighted destination is missing or still updating.");
            if(!em.HasComponent<Game.Common.Owner>(id))throw new NotSupportedException("Viewport replacement requires an element bound to an editable asset.");
            var owner=em.GetComponentData<Game.Common.Owner>(id).m_Owner;
            if(!em.Exists(owner)||!em.HasComponent<PrefabRef>(owner)||em.HasComponent<Game.Common.Deleted>(owner)||em.HasComponent<Temp>(owner))throw new InvalidOperationException("The destination owner is unavailable.");
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
                if(parentPrefab==null||parentPrefab.isReadOnly)throw new InvalidOperationException("A nested parent asset is read-only.");
                BoundDescriptorIndex.Resolve(parentPrefab.GetComponent<ObjectSubObjects>()?.m_SubObjects,childPrefab,em.GetComponentData<LocalTransformCache>(ancestor));
                ancestors.Add(parentPrefab);ancestor=parent;
            }
            if(context.Asset!=null&&!ReferenceEquals(context.Asset,root)&&!ancestors.Any(p=>ReferenceEquals(p,context.Asset)))throw new InvalidOperationException("The highlighted element belongs to a different asset. Inspect its owning asset first.");
            if(OwnerLinks(owner,id)!=1)throw new InvalidOperationException("The highlighted element is not linked exactly once to its owner.");
            if(!em.HasComponent<PrefabRef>(id))throw new InvalidOperationException("The highlighted element has no prefab identity.");
            var prefab=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(id));
            bool effect=em.HasComponent<EditorContainer>(id)&&prefabs.GetPrefab<PrefabBase>(em.GetComponentData<EditorContainer>(id).m_Prefab) is EffectPrefab;
            if(effect)prefab=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<EditorContainer>(id).m_Prefab);
            var rows=ResolveRows(root).Where(r=>r.Value.GetType()==clipboard.RowType&&ReferenceEquals(r.ReferenceField.GetValue(r.Value),prefab)).ToArray();
            if(rows.Length==0){
                var container=em.HasComponent<EditorContainer>(id)?prefabs.GetPrefab<PrefabBase>(em.GetComponentData<EditorContainer>(id).m_Prefab):null;
                using(var types=em.GetComponentTypes(id,Unity.Collections.Allocator.Temp))MW.EditorTools.Mod.Log.Info("Prop Replacer target mismatch: copied="+clipboard.Replacement.name+" copiedType="+clipboard.RowType.FullName+" target="+id+" prefab="+(prefab?.name??"null")+" prefabType="+prefab?.GetType().FullName+" container="+(container?.name??"none")+" containerType="+container?.GetType().FullName+" owner="+owner+" root="+root.name+" components="+string.Join(",",types.Select(t=>t.GetManagedType()?.FullName))+" sourceRows="+string.Join(" | ",ResolveRows(root).Select(r=>r.Index+":"+r.Value.GetType().Name+":"+(r.ReferenceField.GetValue(r.Value) as PrefabBase)?.name)));
                throw new InvalidOperationException("Copied "+clipboard.Label.ToLowerInvariant()+" "+clipboard.Replacement.name+" does not match the highlighted source row ("+(prefab?.name??"unknown")+").");
            }
            Row[] matches;
            if(effect&&clipboard.RowType==typeof(EffectSource.EffectSettings))
            {
                if(!em.HasComponent<Game.Objects.Transform>(id)||!em.HasComponent<Game.Objects.Transform>(owner))throw new NotSupportedException("Effect placement is unavailable.");
                var c=em.GetComponentData<EditorContainer>(id);var pose=em.GetComponentData<Game.Objects.Transform>(id);
                var image=new AreaCompanionEffect{Original=id,Prefab=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(id)),Container=prefab,HasTransform=true,Position=pose.m_Position,Rotation=pose.m_Rotation,Scale=c.m_Scale,Intensity=c.m_Intensity,Group=c.m_GroupIndex,HasLocal=em.HasComponent<LocalTransformCache>(id)};
                if(image.HasLocal)image.Local=em.GetComponentData<LocalTransformCache>(id);
                var ownerPose=em.GetComponentData<Game.Objects.Transform>(owner);
                matches=rows.Where(r=>EffectEditPolicy.Descriptor((EffectSource.EffectSettings)r.Value,image,ownerPose)).ToArray();
            }
            else if(!effect&&clipboard.RowType==typeof(ObjectSubObjectInfo)&&em.HasComponent<Game.Objects.Object>(id)&&em.HasComponent<LocalTransformCache>(id))
            {
                var local=em.GetComponentData<LocalTransformCache>(id);
                var source=root.GetComponent<ObjectSubObjects>();
                int index=BoundDescriptorIndex.Resolve(source?.m_SubObjects,prefab,local);
                matches=rows.Where(r=>r.Index==index).ToArray();
            }
            else if(clipboard.RowType==typeof(ObjectSubAreaInfo)&&em.HasComponent<Game.Areas.Area>(id)&&em.HasBuffer<LocalNodeCache>(id))
            {
                var local=em.GetBuffer<LocalNodeCache>(id,true);
                matches=rows.Where(r=>{var row=(ObjectSubAreaInfo)r.Value;return row.m_NodePositions!=null&&row.m_NodePositions.Length==local.Length&&(row.m_ParentMeshes==null||row.m_ParentMeshes.Length==local.Length)&&Enumerable.Range(0,local.Length).All(i=>row.m_NodePositions[i].Equals(local[i].m_Position)&&(row.m_ParentMeshes==null?-1:row.m_ParentMeshes[i])==local[i].m_ParentMesh);}).ToArray();
            }
            else if(em.HasComponent<LocalCurveCache>(id)&&((clipboard.RowType==typeof(ObjectSubNetInfo)&&em.HasComponent<Game.Net.Edge>(id))||(clipboard.RowType==typeof(ObjectSubLaneInfo)&&em.HasComponent<Game.Net.Lane>(id))))
            {
                var curve=em.GetComponentData<LocalCurveCache>(id).m_Curve;
                matches=rows.Where(r=>r.Value is ObjectSubNetInfo net?net.m_BezierCurve.Equals(curve):((ObjectSubLaneInfo)r.Value).m_BezierCurve.Equals(curve)).ToArray();
            }
            else throw new NotSupportedException("The highlighted element has no supported exact placement cache.");
            if(matches.Length!=1)throw new InvalidOperationException("The highlighted placement matches "+matches.Length+" source rows; viewport replacement refused.");
            matches[0].Ancestors=ancestors.Distinct().ToArray();return matches[0];
        }
    }
}
