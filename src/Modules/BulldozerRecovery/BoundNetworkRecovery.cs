using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using MW.EditorTools.History;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private sealed class NetworkEntry {
            internal NetworkEntry RollbackOf;
            internal Record BoundRecord;
            internal PrefabBase Asset,TargetPrefab;
            internal Entity Owner,Target,OwnerPrefab;
            internal bool Lane,Move,AreaMove,MarkerMove;
            internal AreaMoveEntry AreaMoveEntry;
            internal EffectEntry EffectEntry;
            internal Game.Objects.Transform Transform;
            internal Payload Before,After;
            internal AreaCompanionNetwork Network,AfterNetwork,RestoreFrom;
            internal NestedNode[][] Props,AfterProps;
            internal Record PropRestore;
            internal BoundCompanions CompanionMap;
            internal AreaImage[] Areas;
            internal AreaCompanionEffect[] Effects;
            internal Dictionary<PrefabBase,Payload> References;
        }
        private readonly LastDeletion<NetworkEntry> networkHistory=new LastDeletion<NetworkEntry>();
        private NetworkEntry networkPending;
        private bool networkInApply,networkAccepted,networkRestoring,networkDisabled;
        private int networkFrame,networkStable,networkLastFrame=-1;
        private bool NetworkBusy=>networkPending!=null;
        private void ClearNetwork(){if(networkPending!=null)FinishBikeObservation("network cleared",networkPending);networkPending=null;networkHistory.Clear();networkInApply=networkAccepted=networkRestoring=false;networkStable=0;}
        private void FailNetwork(string reason){if(TryRollbackFence(reason))return;if(networkPending?.BoundRecord!=null){disabled|=networkRestoring;ClearRecord();}else if(networkPending?.EffectEntry!=null){effectDisabled|=networkRestoring;effectHistory.Clear();}else if(networkPending?.AreaMove==true){areaMoveDisabled|=networkRestoring;areaMoveHistory.Clear();}else if(networkPending?.Move==true){moveDisabled|=networkRestoring;moveHistory.Clear();}networkDisabled|=networkRestoring;ClearNetwork();Status=reason;Mod.Log.Warn(reason);}
        private bool NetworkOwnerValid(NetworkEntry entry,bool settled=true) {
            var em=world.EntityManager;
            return context.EditorAvailable&&entry.Asset!=null&&!entry.Asset.isReadOnly&&(Root==null||ReferenceEquals(Root,entry.Asset))&&
                em.Exists(entry.Owner)&&!em.HasComponent<Game.Common.Deleted>(entry.Owner)&&!em.HasComponent<Game.Common.Owner>(entry.Owner)&&
                em.HasComponent<PrefabRef>(entry.Owner)&&ReferenceEquals(world.GetExistingSystemManaged<PrefabSystem>().GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(entry.Owner)),entry.Asset)&&
                em.HasComponent<Game.Objects.Transform>(entry.Owner)&&em.GetComponentData<Game.Objects.Transform>(entry.Owner).Equals(entry.Transform)&&
                (!settled||(!em.HasComponent<SaveInstance>(entry.Owner)&&!((IDictionary)updateMap.GetValue(world.GetExistingSystemManaged<PrefabSystem>())).Contains(entry.Asset)))&&
                entry.References.All(p=>NestedReferenceMatches(p.Key,p.Value));
        }
        private bool NetworkCompanionsMatch(NetworkEntry entry,bool sources) {
            return AreasMatch(entry.Areas,entry.Owner)&&NetworkNonAreaCompanionsMatch(entry,sources);
        }
        private bool NetworkNonAreaCompanionsMatch(NetworkEntry entry,bool sources) {
            if(!AreaCompanionEffect.Matches(entry.Effects,CaptureAreaEffects(entry.Owner)))return false;
            // Always derive the after-image from the immutable original capture.
            // Native marker reconnection can finish after the three-frame record gate.
            bool directDeletion=entry.BoundRecord==null&&!entry.Move&&!entry.AreaMove&&entry.EffectEntry==null;
            var expected=networkRestoring||directDeletion?entry.Props:entry.AfterProps??entry.Props;
            bool projectedDeletion=false;
            var actual=CaptureAreaProps(entry.Owner);var mapped=MatchForest(expected,actual);
            if(mapped==null||!NetworkOwnerValid(entry))return false;
            var referenceMap=EntityReferenceMap.WithOwnerPrefab(new Dictionary<Entity,Entity>(),entry.Owner,entry.OwnerPrefab,world.EntityManager.GetComponentData<PrefabRef>(entry.Owner).m_Prefab);
            if(networkRestoring||entry.AfterNetwork!=null){
                if(!(networkRestoring?entry.Network:entry.AfterNetwork).TryMap(CaptureAreaNetwork(entry.Owner,networkRestoring,entry.Network.Anchors.Length>0||entry.EffectEntry!=null),false,out var links,false,true,networkRestoring&&(entry.BoundRecord!=null||entry.MarkerMove),networkRestoring?NetworkCurveWitness(entry):null))return false;
                foreach(var link in links)if(!EntityReferenceMap.Add(referenceMap,link.Key,link.Value))return false;
                EntityReferenceMap.CopyDirections(links,referenceMap);
            }
            var areaImages=entry.CompanionMap!=null?CaptureAreaReferenceView(entry.CompanionMap,entry.Owner,networkRestoring):null;
            if(entry.CompanionMap!=null&&!MapAreaLanes(entry.CompanionMap,areaImages,referenceMap,false,networkRestoring,!networkRestoring))return false;
            if(!networkRestoring&&directDeletion&&!world.EntityManager.Exists(entry.Target)){
                var live=CaptureAreaNetwork(entry.Owner,false,entry.Network.Anchors.Length>0);
                if(!NetworkSettled(live)||!NetworkDeletionPolicy.Matches(entry.Network,entry.Target,live))return false;
                {
                    var removed=new HashSet<Entity>(entry.Network.Items.Where(i=>i.HasLane&&!live.Items.Any(a=>a.Id==i.Id)).Select(i=>i.Id));
                    var retained=new HashSet<Entity>(live.Items.Where(i=>i.HasLane).Select(i=>i.Id).Concat(areaImages?.AreaLanes.Values.SelectMany(g=>g.Items).Where(i=>i.HasLane).Select(i=>i.Id)??Enumerable.Empty<Entity>()));
                    expected=AdaptConnectionImages(expected,mapped,removed,retained,referenceMap);
                    if(expected.Where((g,i)=>!NestedGroupAtIndex(g,mapped[i],BoundDescriptorIndex.ResolveNative(entry.Asset.GetComponent<ObjectSubObjects>()?.m_SubObjects,g[0].Prefab,g[0].Local,entry.Transform,g[0].Data.Transform),referenceMap)).Any()) {
                        var detail=string.Join("; ",expected.SelectMany((group,g)=>group.Select((node,n)=>node.SpawnAccess.Difference(mapped[g][n].SpawnAccess,referenceMap))).Where(d=>d!="equal").Take(4));
                        return ReferenceMapBlocked("retained marker after-image differs: "+detail);
                    }
                    projectedDeletion=true;
                }
            }
            if(!AddForestMap(expected,mapped,referenceMap))return false;
            bool valid=expected.Select((g,i)=>NestedGroupAtIndex(g,mapped[i],BoundDescriptorIndex.ResolveNative(entry.Asset.GetComponent<ObjectSubObjects>()?.m_SubObjects,g[0].Prefab,g[0].Local,entry.Transform,g[0].Data.Transform),referenceMap)).All(v=>v)&&
                (!sources||(CompanionSourcesValid(entry.Props.SelectMany(GroupSources),actual.SelectMany(GroupSources))&&
                    CompanionSourcesValid(entry.Areas.Select(a=>a.Original),CaptureAreas(entry.Owner).Select(a=>a.Original))&&
                    CompanionSourcesValid(entry.Effects.Select(e=>e.Original),CaptureAreaEffects(entry.Owner).Select(e=>e.Original))));
            if(valid&&projectedDeletion)entry.AfterProps=expected;
            return valid;
        }
        private bool BeginNetwork(BulldozeToolSystem tool) {
            var em=world.EntityManager;var points=(NativeList<ControlPoint>)controlPoints.GetValue(tool);
            if(!points.IsCreated)return false;
            var targets=points.AsArray().ToArray().Select(p=>p.m_OriginalEntity).Where(id=>id!=Entity.Null).Distinct().ToArray();
            if(!targets.Any(id=>em.Exists(id)&&(em.HasComponent<Game.Net.Edge>(id)||em.HasComponent<Game.Net.Node>(id)||em.HasComponent<Game.Net.Lane>(id))))return false;
            try {
                if(AutomaticCaptureAllowed?.Invoke()!=true||networkDisabled||Busy||context.DialogBusy||NestedTestRunning)throw new NotSupportedException("Enable recovery and wait for the current operation.");
                if(tool.actualMode!=BulldozeToolSystem.Mode.SubElements&&tool.actualMode!=BulldozeToolSystem.Mode.Everything)throw new NotSupportedException("Use Sub-elements or Everything for bound lanes/networks.");
                Mod.Log.Info("Network control-point originals: "+string.Join(",",targets.Select(t=>t.ToString())));
                if(targets.Length==0||targets.Any(id=>!em.Exists(id)||!em.HasComponent<Game.Common.Owner>(id)))throw new NotSupportedException("Only directly bound network/lane segments are supported; standalone networks, junction-only and generated-lane deletion are unavailable.");
                var owner=em.GetComponentData<Game.Common.Owner>(targets[0]).m_Owner;
                if(targets.Any(id=>em.GetComponentData<Game.Common.Owner>(id).m_Owner!=owner))throw new NotSupportedException("Network control points belong to different owners.");
                if(!em.Exists(owner)||em.HasComponent<Game.Common.Owner>(owner)||!em.HasComponent<PrefabRef>(owner)||!em.HasComponent<Game.Objects.Transform>(owner))throw new NotSupportedException("The segment must belong directly to the edited asset.");
                var asset=world.GetExistingSystemManaged<PrefabSystem>().GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(owner));
                if(asset==null||asset.isReadOnly||asset is AssetStampPrefab||asset.components.Any(c=>c is ActivityLocation)||
                    asset.components.Any(c=>(c is ObjectSubNets&&c.GetType()!=typeof(ObjectSubNets))||(c is ObjectSubLanes&&c.GetType()!=typeof(ObjectSubLanes)))||
                    asset.components.Count(c=>c is ObjectSubNets)>1||asset.components.Count(c=>c is ObjectSubLanes)>1)throw new NotSupportedException("Unsupported network asset/component shape.");
                var network=CaptureAreaNetwork(owner);if(!NetworkSettled(network))throw new NotSupportedException("Wait for native network updates to finish.");
                var id=NetworkTargetSelection.Resolve(targets,network);
                if(id==Entity.Null)throw new NotSupportedException("Selection does not resolve to one directly bound segment; multi-segment and junction-only deletion are unavailable.");
                LogMainEntity("network target",id,true);var target=network.Items.SingleOrDefault(i=>i.Id==id);
                if(target==null||!network.RootNets.Contains(id)||!target.HasCurve)throw new NotSupportedException("The segment has no stable editable source curve.");
                var lane=target.Container is NetLanePrefab;var prefab=lane?target.Container:target.Prefab;
                if(target.HasContainer&&!lane)throw new NotSupportedException("Unsupported network editor container.");
                if(!lane&&!(prefab is NetPrefab))throw new NotSupportedException("Network prefab is not serializable by the editor.");
                var entry=new NetworkEntry{Asset=asset,Owner=owner,OwnerPrefab=em.GetComponentData<PrefabRef>(owner).m_Prefab,Target=id,TargetPrefab=prefab,Lane=lane,Transform=em.GetComponentData<Game.Objects.Transform>(owner),
                    Before=Capture(asset),CompanionMap=CaptureBoundCompanions(owner,true),Network=network,Props=CaptureAreaProps(owner),Areas=CaptureAreas(owner),Effects=CaptureAreaEffects(owner)};
                entry.References=entry.Props.SelectMany(GroupReferences).Concat(entry.Areas.SelectMany(a=>new[]{a.Prefab,a.Container})).Concat(entry.Effects.SelectMany(e=>new[]{e.Prefab,e.Container}))
                    .Concat(network.Items.SelectMany(i=>new[]{i.Prefab,i.Container})).Where(p=>p!=null).Distinct().ToDictionary(p=>p,Capture);
                if(entry.Before.Weight+entry.References.Values.Sum(p=>p.Weight)>250000||!entry.Before.Matches(asset)||!NetworkOwnerValid(entry)||!NetworkCompanionsMatch(entry,true)||!NetworkRowsMatchGraph(entry.Before,network))throw new NotSupportedException("Network/companion source does not match the writable asset or exceeds the capture budget.");
                ClearMain();ClearRecord();ClearStandaloneArea();session.Reset();armed=nestedArmed=nestedForestArmed=lastArmed=false;armedAsset=null;areaCapture=false;
                networkPending=entry;networkFrame=Time.frameCount;networkStable=0;networkLastFrame=-1;networkInApply=true;networkAccepted=networkRestoring=false;
                Status="Capturing bound "+(lane?"lane":"network")+" deletionâ€¦";Mod.Log.Info(Status+" target="+id+" prefab="+prefab.name+" graph="+network.Items.Length);
            }catch(Exception error){FailNetwork("Lane/network Undo unavailable: "+error.Message);if(!(error is NotSupportedException))Mod.Log.Warn(error.ToString());}
            return true;
        }
        private static Game.Objects.Transform? NetworkCurveWitness(NetworkEntry entry) {
            if(entry.MarkerMove)return entry.Transform;
            // Direct fence/network deletion restoration has the same guarded owner
            // pose and exact source image. Other recovery stages keep their policies.
            return entry.BoundRecord==null&&!entry.Move&&!entry.AreaMove&&entry.EffectEntry==null&&
                entry.TargetPrefab is NetPrefab&&entry.Before.Matches(entry.Asset)?(Game.Objects.Transform?)entry.Transform:null;
        }
        private static T[] NetworkRows<T>(Payload image,string key)=>image.Values.TryGetValue(key,out var rows)?(T[])rows.Materialize():Array.Empty<T>();
        private static bool NetworkRowsMatchGraph(Payload image,AreaCompanionNetwork graph) {
            var roots=graph.Items.Where(i=>i.Owner==graph.Owner&&i.HasEdge&&graph.RootNets.Contains(i.Id)).ToArray();
            var lanes=roots.Where(i=>i.Container is NetLanePrefab).ToArray();var nets=roots.Where(i=>!(i.Container is NetLanePrefab)).ToArray();
            var laneRows=NetworkRows<ObjectSubLaneInfo>(image,"ObjectSubLanes.m_SubLanes");var netRows=NetworkRows<ObjectSubNetInfo>(image,"ObjectSubNets.m_SubNets");
            return laneRows!=null&&netRows!=null&&laneRows.Length==lanes.Length&&netRows.Length==nets.Length&&
                laneRows.All(r=>NetworkDescriptorPolicy.LaneMatches(r,r))&&netRows.All(r=>r!=null&&!ReferenceEquals(r.m_NetPrefab,null)&&AreaNetDescriptorPolicy.Matches(r,r))&&
                NetworkDescriptorPolicy.Set(laneRows.Select(r=>r?.m_LanePrefab).ToArray(),lanes.Select(i=>i.Container as NetLanePrefab).ToArray(),ReferenceEquals)&&
                NetworkDescriptorPolicy.Set(netRows.Select(r=>r?.m_NetPrefab).ToArray(),nets.Select(i=>i.Prefab as NetPrefab).ToArray(),ReferenceEquals);
        }
        private static bool NetworkPayloadRemoval(NetworkEntry entry,Payload after) {
            var before=entry.Before;
            if(!before.Components.Where(c=>!NetworkComponent(c)).SequenceEqual(after.Components.Where(c=>!NetworkComponent(c))))return false;
            if(before.Components.Where(c=>!NetworkComponent(c)).Any(c=>c.active!=before.Active[Array.IndexOf(before.Components,c)]))return false;
            foreach(var c in after.Components.Where(NetworkComponent))if(!before.Components.Contains(c)||c.active!=before.Active[Array.IndexOf(before.Components,c)])return false;
            if(before.Fields.Where(f=>!NetworkComponent(f.Owner)).Any(f=>!NetworkAncillaryMatches(entry,f)))return false;
            if(before.Fields.Where(f=>f.Owner is ObjectSubNets&&f.Field.Name!="m_SubNets").Any(f=>!f.Data.Matches(f.Field.GetValue(f.Owner))))return false;
            var bn=NetworkRows<ObjectSubNetInfo>(before,"ObjectSubNets.m_SubNets");var an=NetworkRows<ObjectSubNetInfo>(after,"ObjectSubNets.m_SubNets");
            var bl=NetworkRows<ObjectSubLaneInfo>(before,"ObjectSubLanes.m_SubLanes");var al=NetworkRows<ObjectSubLaneInfo>(after,"ObjectSubLanes.m_SubLanes");
            return entry.Lane?
                AreaNetDescriptorPolicy.MatchesSet(bn,an)&&NetworkDescriptorPolicy.Removed(bl,al,r=>r!=null&&ReferenceEquals(r.m_LanePrefab,entry.TargetPrefab),NetworkDescriptorPolicy.LaneMatches):
                NetworkDescriptorPolicy.Set(bl,al,NetworkDescriptorPolicy.LaneMatches)&&NetworkDescriptorPolicy.Removed(bn,an,r=>r!=null&&ReferenceEquals(r.m_NetPrefab,entry.TargetPrefab),AreaNetDescriptorPolicy.Matches);
        }
        private static bool NetworkComponent(ComponentBase c)=>c is ObjectSubNets||c is ObjectSubLanes;
        private static bool NetworkAncillaryMatches(NetworkEntry entry,FieldValue field) {
            if(field.Owner is ObjectSubObjects&&field.Field.Name=="m_SubObjects")return BoundDescriptorIndex.NativeRows(
                (ObjectSubObjectInfo[])field.Data.Materialize(),(ObjectSubObjectInfo[])field.Field.GetValue(field.Owner),
                entry.Props.Select(g=>g[0].Prefab).ToArray(),entry.Props.Select(g=>g[0].Local).ToArray(),entry.Props.Select(g=>g[0].Data.Transform).ToArray(),entry.Transform);
            if(field.Owner is ObjectSubAreas&&field.Field.Name=="m_SubAreas")return NetworkDescriptorPolicy.Set((ObjectSubAreaInfo[])field.Data.Materialize(),(ObjectSubAreaInfo[])field.Field.GetValue(field.Owner),
                (a,b)=>AreaRemovalPolicy.ValidRow(a)&&AreaRemovalPolicy.ValidRow(b)&&ReferenceEquals(a.m_AreaPrefab,b.m_AreaPrefab));
            return AreaAncillaryMatches(field);
        }
        private bool NetworkSettled(AreaCompanionNetwork graph) {
            var em=world.EntityManager;
            return graph.Items.All(i=>em.Exists(i.Id)&&!em.HasComponent<Temp>(i.Id)&&!em.HasComponent<Game.Common.Created>(i.Id)&&!em.HasComponent<Game.Common.Updated>(i.Id)&&!em.HasComponent<Game.Common.Deleted>(i.Id));
        }
        private bool CanNetworkUndo {
            get{try{
                var e=networkHistory.Latest;if(e==null)return false;
                if(networkDisabled||Busy||context.DialogBusy)return NetworkBlocked("recovery busy, disabled or dialog open");
                if(!NetworkOwnerValid(e))return NetworkBlocked("owner, referenced prefab or pending native update differs");
                if(!NetworkCompanionsMatch(e,true))return NetworkBlocked("recorded retained companion graph or typed values differ");
                var actual=CaptureAreaNetwork(e.Owner);
                if(!NetworkSettled(actual))return NetworkBlocked("retained network graph is still updating");
                if(!e.AfterNetwork.TryMap(actual,true,out _))return NetworkBlocked("recorded after graph: "+e.AfterNetwork.ExplainMismatch(actual,true));
                if(world.EntityManager.Exists(e.Target))return NetworkBlocked("deleted target identity still exists");
                if(!e.After.Matches(e.Asset)){
                    // Late SaveInstance normalization is admissible only under the
                    // same complete deleted-target/retained-live-state proof as capture.
                    var after=Capture(e.Asset);
                    if(!NetworkPayloadRemoval(e,after)||!NetworkRowsMatchGraph(after,actual))return NetworkBlocked("recorded source after-image changed beyond native normalization");
                    AdoptNetworkAncillary(e,after);e.After=after;
                }
                networkBlockReason=null;return true;
            }catch(Exception error){return NetworkBlocked(error.Message);}}
        }
        private static void AdoptNetworkAncillary(NetworkEntry entry,Payload after){
            // Caller has already proved unchanged live companions and exact native
            // source normalization. Never adopt the deleted segment's own list.
            foreach(var key in new[]{"ObjectSubObjects.m_SubObjects","ObjectSubAreas.m_SubAreas","EffectSource.m_Effects",entry.Lane?"ObjectSubNets.m_SubNets":"ObjectSubLanes.m_SubLanes"})
                if(entry.Before.Values.ContainsKey(key)&&after.Values.TryGetValue(key,out var value)){
                    entry.Before.Values[key]=value;foreach(var field in entry.Before.Fields.Where(f=>f.Owner.GetType().Name+"."+f.Field.Name==key))field.Data=value;
                }
        }
        private string networkBlockReason;
        private bool NetworkBlocked(string reason){if(reason!=networkBlockReason){networkBlockReason=reason;Mod.Log.Info("Network Undo blocked: "+reason);}return false;}
        private void EndNetworkApply(){if(!networkInApply)return;networkInApply=false;if(!networkAccepted)FailNetwork("Native lane/network deletion denied; no Undo entry.");}
        private void AlignNetwork(Payload target,NetworkEntry entry) {
            AlignEffectSource(target,entry);
            // Remove absent types first, then insert missing types in target order.
            // Native assets need not list ObjectSubNets before ObjectSubLanes.
            foreach(var type in new[]{typeof(ObjectSubNets),typeof(ObjectSubLanes)}.OrderBy(t=>Array.FindIndex(target.Components,c=>c.GetType()==t))) {
                var expected=target.Components.SingleOrDefault(c=>c.GetType()==type);var current=entry.Asset.components.SingleOrDefault(c=>c.GetType()==type);
                if(ReferenceEquals(expected,null)){if(current is ObjectSubNets)ApplyPrefabsSystem.RemoveComponent<ObjectSubNets>(entry.Asset);else if(current is ObjectSubLanes)ApplyPrefabsSystem.RemoveComponent<ObjectSubLanes>(entry.Asset);continue;}
                if(current==null){current=type==typeof(ObjectSubNets)?(ComponentBase)entry.Asset.AddComponent<ObjectSubNets>():entry.Asset.AddComponent<ObjectSubLanes>();
                    entry.Asset.components.Remove(current);entry.Asset.components.Insert(Math.Min(Array.IndexOf(target.Components,expected),entry.Asset.components.Count),current);
                    foreach(var image in new[]{entry.Before,entry.After}){for(int i=0;i<image.Components.Length;i++)if(ReferenceEquals(image.Components[i],expected))image.Components[i]=current;foreach(var field in image.Fields.Where(f=>ReferenceEquals(f.Owner,expected)))field.Owner=current;}
                }else if(!ReferenceEquals(current,expected))throw new InvalidOperationException("Network component identity changed.");
            }
            if(!target.Shape(entry.Asset))throw new InvalidOperationException("Network component order changed.");
        }
        private void UndoNetwork() {
            if(!CanNetworkUndo)return;StartBoundRestoration(networkHistory.Latest);
        }
        private void StartBoundRestoration(NetworkEntry entry,bool repairRead=false) {
            BeginBikeUndo(entry);
            using var preparationTiming=ObserveBikeRestore(entry,"undo-prepare");
            retainedReferenceRefusal=null;
            var prepared=TimeBikeRestore(entry,"prepare-before",()=>entry.Before.Prepare());var rollback=TimeBikeRestore(entry,"prepare-rollback",()=>entry.After.Prepare());entry.RestoreFrom=TimeBikeRestore(entry,"capture-network",()=>CaptureAreaNetwork(entry.Owner,repairRead,(entry.Network.Anchors.Length>0||entry.EffectEntry!=null)));
            if(entry.EffectEntry==null&&!entry.AreaMove){
                using var alignmentTiming=ObserveBikeRestore(entry,"align-source-roots");
                var originalProps=entry.Props;
                // Materialize the same immutable source image once for the entire
                // forest. Each root still resolves uniquely against the full rows.
                var targetRows=NetworkRows<ObjectSubObjectInfo>(entry.Before,"ObjectSubObjects.m_SubObjects");
                entry.Props=entry.Props.Select(group=>group.Select((node,index)=>{
                    if(index!=0)return node;var copy=node.DetachedCopy();
                    // The target source may differ from the currently installed after-source
                    // for a moved root, so read the prepared target payload's rows.
                    copy.Local=BoundDescriptorIndex.ForNativeSource(targetRows,copy.Prefab,copy.Local,entry.Transform,copy.Data.Transform);return copy;
                }).ToArray()).ToArray();
                AlignSpawnAnchorExpectations(entry,originalProps);
            }
            entry.PropRestore=new Record{Area=true,Asset=entry.Asset,Owner=entry.Owner,OwnerPrefab=entry.OwnerPrefab,ReferenceNetwork=entry.Network,ReferenceRebuiltObjects=(entry.BoundRecord!=null||entry.MarkerMove),ReferenceMarkerOwner=NetworkCurveWitness(entry),ReferenceAreas=entry.CompanionMap,ForestBefore=entry.Props,ForestFrom=TimeBikeRestore(entry,"capture-retained-props",()=>CaptureAreaProps(entry.Owner)),Nested=entry.Props.FirstOrDefault(),NestedReferences=entry.References};
            nestedSeedsRestored=false;restoringUndo=true;restoreFrame=Time.frameCount;
            networkPending=entry;networkRestoring=true;networkFrame=Time.frameCount;networkStable=0;networkLastFrame=-1;
            try {
                if(entry.BoundRecord?.Last==true)AlignLast(entry.Before,entry.Asset);
                AlignNetwork(entry.Before,entry);entry.Before.Apply(prepared);
                if(!entry.Before.Matches(entry.Asset))throw new InvalidOperationException("Restored lane/network source differs.");
                entry.PropRestore.ReferenceMarkerOwner=NetworkCurveWitness(entry);
                world.GetExistingSystemManaged<PrefabSystem>().UpdatePrefab(entry.Asset,Entity.Null);entry.Asset.asset?.MarkDirty();
                Status=entry.MarkerMove?(entry.TargetPrefab?.name=="BikeRack01"?"Restoring bound bike rack move…":"Restoring bound marker move…"):entry.BoundRecord!=null?"Restoring bound spawn/access deletionâ€¦":entry.EffectEntry!=null?"Restoring bound effect "+(entry.EffectEntry.Move?"move":"deletion")+"â€¦":entry.AreaMove?"Restoring bound area/surface editâ€¦":entry.Move?"Restoring bound prop moveâ€¦":"Restoring bound "+(entry.Lane?"lane":"network")+"â€¦";Mod.Log.Info(Status);
            }catch(Exception error){
                try{if(entry.BoundRecord?.Last==true)AlignLast(entry.After,entry.Asset);AlignNetwork(entry.After,entry);entry.After.Apply(rollback);world.GetExistingSystemManaged<PrefabSystem>().UpdatePrefab(entry.Asset,Entity.Null);}catch(Exception rollbackError){Mod.Log.Warn("Network source rollback failed: "+rollbackError.Message);}
                FailNetwork("Lane/network Undo failed; reload the disposable editor session without saving: "+error.Message);
            }
        }
        private void TickNetworkRecovery() {
            if(!context.EditorAvailable){ClearNetwork();return;}
            if(networkPending==null){
                if(networkHistory.Latest!=null&&!NetworkOwnerValid(networkHistory.Latest,false)){
                    Mod.Log.Info("Network Undo entry cleared: owner identity, transform, referenced prefab or editor context changed; inspector="+(Root?.name??"none")+" asset="+networkHistory.Latest.Asset.name);
                    networkHistory.Clear();
                }
                return;
            }
            if(networkInApply)return;var entry=networkPending;using var tickTiming=ObserveBikeRestore(entry,"restore-tick");var em=world.EntityManager;bool valid=false;string reason="owner, pending native edit, or companion graph differs";
            try {
                if(networkRestoring&&entry.AreaMove&&NetworkOwnerValid(entry)&&(!entry.Props.Any()||nestedSeedsRestored||RestoreForestValues(entry.PropRestore)))RestoreAreaMoveCaches(entry);
                // LocalCurveCache may be nonfinite after native reconstruction.
                // Companion reads must reach the existing complete graph proof
                // and finite-cache repair below before demanding strict caches.
                if(!NetworkOwnerValid(entry))reason="owner or native prefab finalization is pending";
                else if(networkRestoring&&entry.Props.Length>0&&!nestedSeedsRestored&&!RestoreForestValues(entry.PropRestore))reason="retained prop/reference preflight did not validate";
                else if(!NetworkCompanionsMatch(entry,true))reason="retained area/effect/prop companions did not validate";
                else {
                    var actual=CaptureAreaNetwork(entry.Owner,networkRestoring,(entry.Network.Anchors.Length>0||entry.EffectEntry!=null));
                    if(!NetworkSettled(actual))throw new NotSupportedException("Native network graph is updating.");
                    reason="network graph or source descriptors differ";
                    if(networkRestoring) {
                        bool source=entry.Before.Matches(entry.Asset);
                        bool mapped=entry.Network.TryMap(actual,false,out var map,false,true,(entry.BoundRecord!=null||entry.MarkerMove),NetworkCurveWitness(entry));
                        var badCache=mapped?entry.Network.Items.FirstOrDefault(i=>i.HasLocal&&!AreaCompanionNetwork.CanRestoreLocal(i.Local,actual.Items.Single(a=>a.Id==map[i.Id]).Local)):null;
                        bool contained=CompanionSourcesValid(entry.RestoreFrom.Items.Select(i=>i.Id),actual.Items.Select(i=>i.Id));
                        bool explain=Time.frameCount-networkFrame>=120;
                        if(!source)reason="source fields differ: "+string.Join(",",entry.Before.Fields.Where(f=>!f.Data.Matches(f.Field.GetValue(f.Owner))).Select(f=>f.Owner.GetType().Name+"."+f.Field.Name));
                        else if(!mapped)reason=explain?"restoration preflight: "+entry.Network.ExplainMismatch(actual,false,false,true,(entry.BoundRecord!=null||entry.MarkerMove),NetworkCurveWitness(entry)):"restoration topology/typed mapping differs";
                        else if(badCache!=null)reason=explain?"finite local cache differs: expected "+AreaCompanionNetwork.Describe(badCache)+" actual "+AreaCompanionNetwork.Describe(actual.Items.Single(a=>a.Id==map[badCache.Id])):"finite local cache differs";
                        else if(!contained)reason="surviving original entity is outside the regenerated graph";
                        if(source&&mapped&&badCache==null&&contained) {
                            if(!RepairNetworkCaches(entry,actual,map))throw new NotSupportedException("Complete network/area cache repair preflight differs.");
                            var final=CaptureAreaNetwork(entry.Owner,false,(entry.Network.Anchors.Length>0||entry.EffectEntry!=null));bool networkExact=entry.Network.TryMap(final,true,out _,true,true,(entry.BoundRecord!=null||entry.MarkerMove),NetworkCurveWitness(entry));valid=networkExact&&(!entry.AreaMove||AreaMoveRestored(entry.AreaMoveEntry))&&(entry.EffectEntry==null||EffectBoundRestored(entry.EffectEntry));
                            if(!valid&&explain)reason=networkExact?"restored area geometry/metadata or its generated companions differ":"strict final graph: "+entry.Network.ExplainMismatch(final,true,true,true,(entry.BoundRecord!=null||entry.MarkerMove),NetworkCurveWitness(entry));
                        }
                    }else if(em.Exists(entry.Target)) reason="deleted network target identity still exists";
                    else if(!NetworkDeletionPolicy.Matches(entry.Network,entry.Target,actual)) {
                        reason=Time.frameCount-networkFrame>=120?"deletion graph: "+NetworkDeletionPolicy.ExplainMismatch(entry.Network,entry.Target,actual):"retained deletion graph differs";
                    }else {
                        var after=Capture(entry.Asset);
                        if(!NetworkPayloadRemoval(entry,after))reason="deleted network source row or ancillary fields differ";
                        else if(!NetworkRowsMatchGraph(after,actual))reason="remaining network source rows do not match the live graph";
                        else {
                            if(entry.After==null||!entry.After.Matches(entry.Asset)){entry.After=after;entry.AfterNetwork=actual;networkStable=0;}
                            valid=entry.AfterNetwork.TryMap(actual,true,out _);
                            if(!valid)reason="recorded deletion after-graph has not settled";
                        }
                    }
                }
            }catch(NotSupportedException error){valid=false;reason=error.Message;}
            catch(Exception error){if(networkPending?.EffectEntry!=null){FailNetwork("Effect recovery failed; reload without saving: "+error.Message);return;}else if(networkPending?.AreaMove==true){FailNetwork("Area move recovery failed; reload without saving: "+error.Message);return;}FailNetwork("Lane/network recovery failed; reload without saving: "+error.Message);Mod.Log.Warn(error.ToString());return;}
            if(!valid)networkStable=0;else if(networkLastFrame!=Time.frameCount){networkLastFrame=Time.frameCount;networkStable++;}
            if(networkStable>=3) {
                if(networkRestoring&&entry.RollbackOf!=null){
                    networkHistory.Clear();networkPending=null;networkRestoring=false;networkStable=0;
                    Status="Fence Undo failed; the recorded deleted-fence state and companions were recovered. Prop Undo is available for new actions.";Mod.Log.Warn(Status);return;
                }
                if(networkRestoring){bool consumed=entry.BoundRecord!=null?CompleteSpawnDeletion(entry):entry.EffectEntry!=null?effectHistory.Undo(e=>ReferenceEquals(e.BoundBefore,entry)):entry.AreaMove?areaMoveHistory.Undo(e=>ReferenceEquals(e.BoundBefore,entry)):entry.Move?moveHistory.Undo(e=>ReferenceEquals(e.BoundBefore,entry)):networkHistory.Undo(e=>ReferenceEquals(e,entry));if(!consumed){FailNetwork("Recovery slot changed; reload without saving.");return;}Status=entry.MarkerMove?(entry.TargetPrefab?.name=="BikeRack01"?"Undid bound bike rack move; source, graph and companions verified.":"Undid bound marker move; source, graph and companions verified."):entry.BoundRecord!=null?"Undid bound spawn/access deletion; source, graph and companions verified.":entry.EffectEntry!=null?"Undid bound effect "+(entry.EffectEntry.Move?"move":"deletion")+"; source, placement and companions verified.":entry.AreaMove?"Undid bound area/surface edit; source, geometry and companions verified.":entry.Move?"Undid bound prop move; source, graph and companions verified.":"Undid bound "+(entry.Lane?"lane":"network")+" deletion; graph and companions verified.";}
                else{
                    // Only ancillary lists proved unchanged in the complete live
                    // graph may adopt SaveInstance's normalized representation.
                    // The deleted segment's source list stays in its before-image.
                    AdoptNetworkAncillary(entry,entry.After);
                    networkHistory.Record(entry);Status="Bound "+(entry.Lane?"lane":"network")+" deletion ready to Undo.";
                }
                Mod.Log.Info(Status+" asset="+entry.Asset.name);tickTiming?.Dispose();FinishBikeObservation("completed",entry);networkPending=null;networkRestoring=false;networkStable=0;
            }else if(Time.frameCount-networkFrame>120){var kind=entry.BoundRecord!=null?"Bound spawn/access":entry.EffectEntry!=null?"Bound effect":entry.AreaMove?"Bound area/surface move":entry.Move?"Bound prop move":"Lane/network";Mod.Log.Info(kind+" validation timeout: "+reason+" target="+entry.Target+" source="+entry.Asset.name);FailNetwork(networkRestoring?kind+" Undo did not validate; reload the disposable editor session without saving.":kind+" deletion did not validate; no Undo entry recorded.");}
        }
    }
}
