using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using MW.EditorTools.History;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private bool areaCapture;
        private int areaCaptureStable,areaCaptureStableFrame;
        private AreaImage[] beforeAreas,afterAreas;
        private Entity deletedArea;
        private Game.Objects.Transform areaOwnerTransform;
        private sealed class AreaImage {
            internal string Metadata=>"flags="+Flags+" kind="+Surface+"/"+Space+" navigation="+Navigation+" hangaround="+Hangaround+" container="+HasContainer+"/"+Container?.name+" scale="+Scale+" intensity="+Intensity+" group="+Group;
            internal AreaImage WithLocal(LocalNodeCache[] local){var copy=(AreaImage)MemberwiseClone();copy.Local=local;return copy;}
            internal Entity Original;
            internal PrefabBase Prefab,Container;
            internal bool Surface,Space,HasContainer,Navigation,Hangaround;
            internal Game.Areas.AreaFlags Flags;
            internal Game.Areas.Node[] Nodes;
            internal LocalNodeCache[] Local;
            internal float3 Scale;
            internal float Intensity;
            internal int Group;
        }
        private bool BeginArea(BulldozeToolSystem tool) {
            var em=world.EntityManager;
            var points=(NativeList<ControlPoint>)controlPoints.GetValue(tool);
            if(!points.IsCreated)return false;
            var targets=new HashSet<Entity>();
            foreach(var point in points)if(point.m_OriginalEntity!=Entity.Null)targets.Add(point.m_OriginalEntity);
            if(!targets.Any(id=>em.Exists(id)&&em.HasComponent<Game.Areas.Area>(id)))return false;
            try {

                if(AutomaticCaptureAllowed?.Invoke()!=true)throw new NotSupportedException("Enable deletion recovery in Options first.");
                if(Busy||NestedTestRunning)throw new NotSupportedException("Wait for the current recovery operation.");
                if(context.DialogBusy)throw new NotSupportedException("Close the open dialog before deleting an area.");
                if(targets.Count!=1)throw new NotSupportedException("Delete one area at a time.");
                var id=targets.Single();
                LogMainEntity("area target",id,true);
                if(!em.HasComponent<Game.Common.Owner>(id)){BeginStandaloneArea(tool,id);return true;}
                if(tool.actualMode!=BulldozeToolSystem.Mode.SubElements&&tool.actualMode!=BulldozeToolSystem.Mode.Everything)throw new NotSupportedException("Use Sub-elements or Everything mode for a bound area.");
                var owner=em.GetComponentData<Game.Common.Owner>(id).m_Owner;
                if(!em.Exists(owner)||!em.HasComponent<PrefabRef>(owner)||em.HasComponent<Game.Common.Owner>(owner))throw new NotSupportedException("Area must belong directly to the edited asset.");
                var asset=world.GetExistingSystemManaged<PrefabSystem>().GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(owner));
                if(asset==null||asset.isReadOnly||asset is AssetStampPrefab||asset.components.Any(c=>c is ActivityLocation)||
                    (Root!=null&&!ReferenceEquals(Root,asset)))throw new NotSupportedException("Select the writable owning asset first.");
                if(!em.HasComponent<Game.Objects.Transform>(owner))throw new NotSupportedException("The area owner must have a stable transform.");
                if(em.HasComponent<SaveInstance>(owner)||((System.Collections.IDictionary)updateMap.GetValue(world.GetExistingSystemManaged<PrefabSystem>())).Contains(asset))throw new NotSupportedException("Wait for the pending native asset edit before deleting an area.");
                var transform=em.GetComponentData<Game.Objects.Transform>(owner);
                if(!math.all(math.isfinite(transform.m_Position))||!math.all(math.isfinite(transform.m_Rotation.value)))throw new NotSupportedException("Nonfinite area owner transform.");
                var image=Capture(asset);var areas=CaptureAreas(owner);
                if(!AreasMatch(areas,owner))throw new NotSupportedException("Area placements are ambiguous before deletion.");
                areaProps=CaptureAreaProps(owner);areaEffects=CaptureAreaEffects(owner);areaNetwork=CaptureAreaNetwork(owner);
                if((asset.GetComponent<ObjectSubObjects>()?.m_SubObjects?.Length??0)!=areaProps.Length||
                    (asset.GetComponent<EffectSource>()?.m_Effects?.Count??0)!=areaEffects.Length)throw new NotSupportedException("Area companion descriptors do not match their placed instances.");
                areaReferences=areaProps.SelectMany(GroupReferences).Concat(areaEffects.Select(e=>e.Container))
                    .Concat(areaNetwork.Items.SelectMany(n=>new[]{n.Prefab,n.Container})).Where(p=>p!=null).Distinct().ToDictionary(p=>p,Capture);
                if(areaReferences.Values.Sum(p=>p.Weight)>250000||areaReferences.Any(p=>!NestedReferenceMatches(p.Key,p.Value)))throw new NotSupportedException("Area companion references exceed budget or changed during capture.");
                var descriptors=asset.GetComponent<ObjectSubAreas>()?.m_SubAreas;
                if(asset.components.Count(c=>c is ObjectSubAreas)!=1||asset.components.Any(c=>c is ObjectSubAreas&&c.GetType()!=typeof(ObjectSubAreas))||descriptors==null||descriptors.Length!=areas.Length||!areas.Any(value=>value.Original==id)||areas.Length==0)throw new NotSupportedException("Area list and native ownership do not match.");
                if(image.Weight>250000)throw new NotSupportedException("Area payload exceeds snapshot budget.");
                if(record!=null&&(!record.Area||!ReferenceEquals(record.Asset,asset)||!OwnerMatches(record)||
                    !(records.Latest?.After??records.NextRedo.Before).Matches(asset)||
                    !AreasMatch(records.Latest?.AfterAreas??records.NextRedo.BeforeAreas,owner)))ClearRecord();
                ClearMain();armed=nestedArmed=nestedForestArmed=lastArmed=false;forestBefore=null;nestedBefore=null;nestedReferences=null;capturedRootTypes=null;areaOwnerTransform=em.GetComponentData<Game.Objects.Transform>(owner);
                areaCaptureStable=0;areaCaptureStableFrame=-1;before=image;after=null;beforeAreas=areas;afterAreas=null;areaCapture=true;deletedArea=id;
                observedOwner=owner;beforeGraph=Array.Empty<GraphValue>();afterGraph=null;
                dispatched=sourceMatched=false;session.Begin(asset,Time.frameCount);
                Status="Capturing area/surface deletion…";
                Mod.Log.Info("Area recovery capture: asset="+asset.name+" areas="+areas.Length+" target="+id);
            }catch(Exception error){ClearAreaCompanions();ClearRecord();areaCapture=false;session.Reset();Status="Area Undo unavailable: "+error.Message;Mod.Log.Info(Status);if(!(error is NotSupportedException))Mod.Log.Warn("Area capture exception: "+error);}
            return true;
        }
        private bool AreaCaptureReady(){
            bool ready=false;
            try{ready=after!=null&&session.Matches(TargetRoot)&&after.Matches(session.Asset)&&OwnerMatches(new Record{Area=true,AreaOwnerTransform=areaOwnerTransform,Asset=session.Asset,Owner=observedOwner})&&AreasMatch(afterAreas,observedOwner)&&AreaLiveCompanionsMatch();}
            catch(NotSupportedException){ready=false;}
            if(!ready){areaCaptureStable=0;return false;}
            if(areaCaptureStableFrame!=Time.frameCount){areaCaptureStableFrame=Time.frameCount;areaCaptureStable++;}
            return areaCaptureStable>=3;
        }
        private AreaImage[] CaptureAreas(Entity owner) {
            var em=world.EntityManager;var result=new List<AreaImage>();
            if(!em.Exists(owner))throw new NotSupportedException("Area owner disappeared.");
            if(!em.HasBuffer<Game.Areas.SubArea>(owner))return result.ToArray();
            var buffer=em.GetBuffer<Game.Areas.SubArea>(owner,true);
            if(buffer.Length>128)throw new NotSupportedException("Area count exceeds128.");
            var seen=new HashSet<Entity>();int nodeCount=0;
            var prefabs=world.GetExistingSystemManaged<PrefabSystem>();
            foreach(var reference in buffer){
                var id=reference.m_Area;
                if(!em.Exists(id)||em.HasComponent<Game.Common.Deleted>(id))continue;
                if(!seen.Add(id)||!em.HasComponent<Game.Common.Owner>(id)||em.GetComponentData<Game.Common.Owner>(id).m_Owner!=owner||
                    !em.HasComponent<Game.Areas.Area>(id)||!em.HasComponent<PrefabRef>(id)||!em.HasBuffer<Game.Areas.Node>(id)||
                    !em.HasBuffer<LocalNodeCache>(id)||em.HasComponent<Game.Areas.Lot>(id)||
                    (em.HasBuffer<Game.Areas.SubArea>(id)&&em.GetBuffer<Game.Areas.SubArea>(id,true).Length!=0))throw new NotSupportedException("Unsupported area schema or ownership.");
                var surface=em.HasComponent<Game.Areas.Surface>(id);var space=em.HasComponent<Game.Areas.Space>(id);
                if(surface==space)throw new NotSupportedException("Only surface and space areas are supported.");
                var nodes=em.GetBuffer<Game.Areas.Node>(id,true).AsNativeArray().ToArray();var local=em.GetBuffer<LocalNodeCache>(id,true).AsNativeArray().ToArray();
                nodeCount+=nodes.Length;
                if(nodes.Length<3||nodes.Length!=local.Length||nodeCount>4096)throw new NotSupportedException("Area node shape exceeds scope.");
                if(nodes.Any(n=>!math.all(math.isfinite(n.m_Position))||!math.isfinite(n.m_Elevation))||local.Any(n=>!math.all(math.isfinite(n.m_Position))))throw new NotSupportedException("Nonfinite area geometry.");
                var item=new AreaImage{Original=id,Prefab=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(id)),Surface=surface,Space=space,
                    Navigation=em.HasComponent<Game.Areas.Navigation>(id),Hangaround=em.HasComponent<Game.Areas.HangaroundLocation>(id),Flags=em.GetComponentData<Game.Areas.Area>(id).m_Flags,Nodes=nodes,Local=local};
                if(em.HasComponent<Game.Tools.EditorContainer>(id)){item.HasContainer=true;var c=em.GetComponentData<Game.Tools.EditorContainer>(id);item.Container=prefabs.GetPrefab<PrefabBase>(c.m_Prefab);item.Scale=c.m_Scale;item.Intensity=c.m_Intensity;item.Group=c.m_GroupIndex;if(!math.all(math.isfinite(item.Scale))||!math.isfinite(item.Intensity))throw new NotSupportedException("Nonfinite area container values.");}
                if(item.Prefab==null)throw new NotSupportedException("Area prefab is unavailable.");
                if(item.HasContainer&&item.Container==null)throw new NotSupportedException("Area container prefab is unavailable.");
                result.Add(item);
            }
            return result.ToArray();
        }
        private static bool AreaEqual(AreaImage a,AreaImage b) => ReferenceEquals(a.Prefab,b.Prefab)&&ReferenceEquals(a.Container,b.Container)&&
            a.Surface==b.Surface&&a.Space==b.Space&&a.Navigation==b.Navigation&&a.Hangaround==b.Hangaround&&a.HasContainer==b.HasContainer&&a.Flags==b.Flags&&a.Group==b.Group&&a.Scale.Equals(b.Scale)&&a.Intensity.Equals(b.Intensity)&&
            AreaGeometry.Matches(a.Nodes,a.Local,b.Nodes,b.Local);
        private bool AreasMatch(AreaImage[] expected,Entity owner){
            if(expected==null)return false;
            var actual=CaptureAreas(owner);if(actual.Length!=expected.Length)return false;var used=new bool[actual.Length];
            foreach(var item in expected){var found=Enumerable.Range(0,actual.Length).Where(i=>!used[i]&&AreaEqual(item,actual[i])).ToArray();if(found.Length!=1)return false;used[found[0]]=true;}
            return true;
        }
        private bool AreaGraphMatches(AreaImage[] expected,Record entry) {
            try{return AreasMatch(expected,entry.Owner)&&CompanionSourcesValid((entry.AreaSources??entry.AfterAreas).Select(a=>a.Original),CaptureAreas(entry.Owner).Select(a=>a.Original));}
            catch(NotSupportedException){return false;}
        }
        private static bool AreaAncillaryMatches(FieldValue field) {
            var current=field.Field.GetValue(field.Owner);
            if(field.Owner is ObjectSubObjects&&field.Field.Name=="m_SubObjects")return DescriptorPermutation.Matches((ObjectSubObjectInfo[])field.Data.Materialize(),(ObjectSubObjectInfo[])current);
            if(field.Owner is ObjectSubNets&&field.Field.Name=="m_SubNets")return AreaNetDescriptorPolicy.MatchesSet((ObjectSubNetInfo[])field.Data.Materialize(),(ObjectSubNetInfo[])current);
            if(field.Owner is EffectSource&&field.Field.Name=="m_Effects")return DescriptorPermutation.Matches(((List<EffectSource.EffectSettings>)field.Data.Materialize())?.ToArray(),((List<EffectSource.EffectSettings>)current)?.ToArray(),(a,b)=>PrefabSnapshot.Take(a).MatchesNativeGeometry(b));
            return field.Data.Matches(current);
        }
        private bool AreaRemovalMatches(Payload original,Payload current,PrefabBase asset){
            const string key="ObjectSubAreas.m_SubAreas";
            if(!original.Values.ContainsKey(key))return false;
            var beforeRows=(ObjectSubAreaInfo[])original.Values[key].Materialize();
            var afterRows=current.Values.ContainsKey(key)?(ObjectSubAreaInfo[])current.Values[key].Materialize():Array.Empty<ObjectSubAreaInfo>();
            var deleted=beforeAreas.SingleOrDefault(a=>a.Original==deletedArea);
            if(deleted==null||!AreaRemovalPolicy.ReferencesAfterRemoval(beforeRows,afterRows,deleted.Prefab as AreaPrefab)){Mod.Log.Info("Area removal refused: descriptor references/counts differ from the captured target; before="+beforeRows.Length+" after="+afterRows.Length);return false;}
            var retained=original.Components.Where(c=>!(c is ObjectSubAreas)).ToArray();
            var actual=current.Components.Where(c=>!(c is ObjectSubAreas)).ToArray();
            var changed=original.Fields.Where(f=>!(f.Owner is ObjectSubAreas)).Where(f=>!AreaAncillaryMatches(f)).Select(f=>f.Owner.GetType().Name+"."+f.Field.Name).ToArray();
            if(!retained.SequenceEqual(actual)||changed.Length!=0){Mod.Log.Info("Area removal refused: componentShape="+retained.SequenceEqual(actual)+" changed="+string.Join(",",changed));return false;}
            if(afterRows.Length>0&&(!original.Shape(asset)||!original.Active.SequenceEqual(current.Active)))return false;
            if(afterRows.Length==0&&current.Components.Any(c=>c is ObjectSubAreas))return false;
            if(original.Components.Where(c=>!(c is ObjectSubAreas)).Any(c=>c.active!=original.Active[Array.IndexOf(original.Components,c)]))return false;
            // Preserve the complete retained before-images as the target. The
            // capture stability window must match these after deferred updates.
            afterAreas=beforeAreas.Where(a=>a.Original!=deletedArea).ToArray();
            return true;
        }
        private static bool AreaTargetShape(Payload target,PrefabBase asset){
            var actual=asset.components.Where(c=>!(c is ObjectSubAreas)).ToArray();var expected=target.Components.Where(c=>!(c is ObjectSubAreas)).ToArray();
            return actual.SequenceEqual(expected)&&actual.All(c=>c!=null)&&target.Components.Count(c=>c is ObjectSubAreas)<=1&&asset.components.Count(c=>c is ObjectSubAreas)<=1;
        }
        private void AlignArea(Payload target,PrefabBase asset){
            if(!AreaTargetShape(target,asset))throw new InvalidOperationException("Area component context changed.");
            int slot=Array.FindIndex(target.Components,c=>c is ObjectSubAreas);var current=asset.components.OfType<ObjectSubAreas>().SingleOrDefault();
            if(slot<0){if(current!=null)ApplyPrefabsSystem.RemoveComponent<ObjectSubAreas>(asset);}
            else if(current==null){
                var previous=target.Components[slot];var created=asset.AddComponent<ObjectSubAreas>();asset.components.Remove(created);asset.components.Insert(slot,created);
                foreach(var payload in records.All.Where(e=>ReferenceEquals(e.Asset,asset)).SelectMany(e=>new[]{e.Before,e.After}).Concat(new[]{target}).Distinct()){
                    for(int i=0;i<payload.Components.Length;i++)if(ReferenceEquals(payload.Components[i],previous))payload.Components[i]=created;
                    foreach(var field in payload.Fields.Where(f=>ReferenceEquals(f.Owner,previous)))field.Owner=created;
                }
            }else if(!ReferenceEquals(current,target.Components[slot]))throw new InvalidOperationException("Area component identity changed.");
            if(!target.Shape(asset))throw new InvalidOperationException("Area component shape mismatch.");
        }
    }
}
