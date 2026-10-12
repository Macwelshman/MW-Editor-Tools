using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private bool areaCapture;
        private int areaCaptureStable,areaCaptureStableFrame;
        private AreaImage[] beforeAreas,afterAreas;
        private Entity deletedArea;
        private Game.Objects.Transform areaOwnerTransform;
        private sealed class AreaImage {
            internal Entity Original;
            internal PrefabBase Prefab,Container;
            internal bool Surface,Space,HasContainer;
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
                if(AutomaticCaptureAllowed?.Invoke()!=true||Busy||NestedTestRunning||context.DialogBusy||targets.Count!=1||
                    (tool.actualMode!=BulldozeToolSystem.Mode.SubElements&&tool.actualMode!=BulldozeToolSystem.Mode.Everything))throw new NotSupportedException("Area capture is unavailable in this context.");
                var id=targets.Single();
                if(!em.HasComponent<Game.Common.Owner>(id))throw new NotSupportedException("Standalone areas are not supported yet.");
                var owner=em.GetComponentData<Game.Common.Owner>(id).m_Owner;
                if(!em.Exists(owner)||!em.HasComponent<PrefabRef>(owner)||em.HasComponent<Game.Common.Owner>(owner))throw new NotSupportedException("Area must belong directly to the edited asset.");
                var asset=world.GetExistingSystemManaged<PrefabSystem>().GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(owner));
                if(asset==null||asset.isReadOnly||asset is AssetStampPrefab||asset.components.Any(c=>c is ActivityLocation)||
                    (Root!=null&&!ReferenceEquals(Root,asset)))throw new NotSupportedException("Select the writable owning asset first.");
                if(!AreaRemovalPolicy.AncillaryOnly(asset.GetComponent<ObjectSubObjects>()?.m_SubObjects?.Length??0,
                    asset.GetComponent<ObjectSubNets>()?.m_SubNets?.Length??0,asset.GetComponent<ObjectSubLanes>()?.m_SubLanes?.Length??0,
                    asset.GetComponent<EffectSource>()?.m_Effects?.Count??0))throw new NotSupportedException("This first area recovery build requires an asset containing areas/surfaces only; mixed props, networks, lanes and effects are not supported yet.");
                if(!em.HasComponent<Game.Objects.Transform>(owner)||(em.HasBuffer<Game.Objects.SubObject>(owner)&&em.GetBuffer<Game.Objects.SubObject>(owner,true).Length!=0))throw new NotSupportedException("The area owner must have a stable transform and no runtime object children.");
                var image=Capture(asset);var areas=CaptureAreas(owner);
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
            }catch(Exception error){ClearRecord();areaCapture=false;session.Reset();Status="Area Undo unavailable: "+error.Message;Mod.Log.Info(Status);}
            return true;
        }
        private bool AreaCaptureReady(){
            bool ready=false;
            try{ready=after!=null&&session.Matches(TargetRoot)&&after.Matches(session.Asset)&&AreasMatch(afterAreas,observedOwner);}
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
                var nodes=em.GetBuffer<Game.Areas.Node>(id,true).ToArray();var local=em.GetBuffer<LocalNodeCache>(id,true).ToArray();
                nodeCount+=nodes.Length;
                if(nodes.Length<3||nodes.Length!=local.Length||nodeCount>4096)throw new NotSupportedException("Area node shape exceeds scope.");
                if(nodes.Any(n=>!math.all(math.isfinite(n.m_Position))||!math.isfinite(n.m_Elevation))||local.Any(n=>!math.all(math.isfinite(n.m_Position))))throw new NotSupportedException("Nonfinite area geometry.");
                var item=new AreaImage{Original=id,Prefab=prefabs.GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(id)),Surface=surface,Space=space,
                    Flags=em.GetComponentData<Game.Areas.Area>(id).m_Flags,Nodes=nodes,Local=local};
                if(em.HasComponent<Game.Tools.EditorContainer>(id)){item.HasContainer=true;var c=em.GetComponentData<Game.Tools.EditorContainer>(id);item.Container=prefabs.GetPrefab<PrefabBase>(c.m_Prefab);item.Scale=c.m_Scale;item.Intensity=c.m_Intensity;item.Group=c.m_GroupIndex;if(!math.all(math.isfinite(item.Scale))||!math.isfinite(item.Intensity))throw new NotSupportedException("Nonfinite area container values.");}
                if(item.Prefab==null)throw new NotSupportedException("Area prefab is unavailable.");
                result.Add(item);
            }
            return result.ToArray();
        }
        private static bool AreaEqual(AreaImage a,AreaImage b) => ReferenceEquals(a.Prefab,b.Prefab)&&ReferenceEquals(a.Container,b.Container)&&
            a.Surface==b.Surface&&a.Space==b.Space&&a.HasContainer==b.HasContainer&&a.Flags==b.Flags&&a.Group==b.Group&&a.Scale.Equals(b.Scale)&&a.Intensity.Equals(b.Intensity)&&
            AreaGeometry.Matches(a.Nodes,a.Local,b.Nodes,b.Local);
        private bool AreasMatch(AreaImage[] expected,Entity owner){
            if(expected==null)return false;
            var actual=CaptureAreas(owner);if(actual.Length!=expected.Length)return false;var used=new bool[actual.Length];
            foreach(var item in expected){var found=Enumerable.Range(0,actual.Length).Where(i=>!used[i]&&AreaEqual(item,actual[i])).ToArray();if(found.Length!=1)return false;used[found[0]]=true;}
            return true;
        }
        private bool AreaRemovalMatches(Payload original,Payload current,PrefabBase asset){
            const string key="ObjectSubAreas.m_SubAreas";
            if(!original.Values.ContainsKey(key))return false;
            var beforeRows=(ObjectSubAreaInfo[])original.Values[key].Materialize();
            var afterRows=current.Values.ContainsKey(key)?(ObjectSubAreaInfo[])current.Values[key].Materialize():Array.Empty<ObjectSubAreaInfo>();
            if(AreaRemovalPolicy.RemovedIndex(beforeRows,afterRows)<0)return false;
            var retained=original.Components.Where(c=>!(c is ObjectSubAreas)).ToArray();
            var actual=current.Components.Where(c=>!(c is ObjectSubAreas)).ToArray();
            if(!retained.SequenceEqual(actual)||original.Fields.Where(f=>!(f.Owner is ObjectSubAreas)).Any(f=>!f.Data.Matches(f.Field.GetValue(f.Owner))))return false;
            if(afterRows.Length>0&&(!original.Shape(asset)||!original.Active.SequenceEqual(current.Active)))return false;
            if(afterRows.Length==0&&current.Components.Any(c=>c is ObjectSubAreas))return false;
            if(original.Components.Where(c=>!(c is ObjectSubAreas)).Any(c=>c.active!=original.Active[Array.IndexOf(original.Components,c)]))return false;
            afterAreas=CaptureAreas(observedOwner);
            var expected=beforeAreas.Where(a=>a.Original!=deletedArea).ToArray();
            if(expected.Length!=afterAreas.Length||!AreasMatch(expected,observedOwner))return false;
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
