using System;
using System.Linq;
using Game.Prefabs;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private bool ordinaryNested;
        private PrefabBase[] ordinaryNestedTypes=Array.Empty<PrefabBase>();
        private PrefabBase[] AdoptedNestedTypes=>ordinaryNested?ordinaryNestedTypes:new PrefabBase[]{nestedFixture};
        private int AdoptedRootCount()=>NestedRoots(AdoptedNestedTypes).Length;
        private void LogOrdinarySchema(Unity.Entities.Entity root){
            var em=world.EntityManager;var seen=new System.Collections.Generic.HashSet<Unity.Entities.Entity>();var queue=new System.Collections.Generic.Queue<Unity.Entities.Entity>();queue.Enqueue(root);
            while(queue.Count>0){
                var entity=queue.Dequeue();if(!seen.Add(entity))throw new NotSupportedException("Nested schema inventory found shared nodes or a cycle.");
                if(seen.Count>32)throw new NotSupportedException("Nested schema inventory exceeds 32 nodes.");
                LogMainEntity("ordinary schema",entity,true);
                if(!em.Exists(entity)||!em.HasBuffer<Game.Objects.SubObject>(entity))continue;
                var children=em.GetBuffer<Game.Objects.SubObject>(entity,true);
                if(children.Length+seen.Count+queue.Count>32)throw new NotSupportedException("Nested schema inventory exceeds 32 nodes.");
                foreach(var child in children)queue.Enqueue(child.m_SubObject);
            }
        }
        private bool SelectedAssetUsesNestedGroup()=>context.Asset!=null&&(context.Asset.GetComponent<ObjectSubObjects>()?.m_SubObjects?.Any(value=>value!=null&&ReferenceEquals(value.m_Object,nestedFixture))??false);
        private static bool AllowedNestedTopology(NestedNode[] group)=>NestedPropPolicy.Accept(group.Select(node=>node.Parent).ToArray(),group.Select(node=>node.Depth).ToArray(),group.Select(node=>(object)node.Prefab).ToArray(),group.Select(node=>true).ToArray());
        private static bool AllowedBoundTopology(NestedNode[] group)=>BoundPropPolicy.Accept(group.Select(node=>node.Parent).ToArray(),group.Select(node=>node.Depth).ToArray(),group.Select(node=>(object)node.Prefab).ToArray(),group.Select(node=>(node.Parent==-1?node.Local.m_Probability>=0&&node.Local.m_Probability<=100:node.Local.m_Probability==100)&&node.Local.m_GroupIndex==0).ToArray());
        private static bool OrdinaryGroupSupported(NestedNode[] group,bool areaCompanion=false){
            if(!AllowedBoundTopology(group))throw new NotSupportedException("Live child topology does not match the supported tree limits (nodes="+group.Length+").");
            for(int i=0;i<group.Length;i++){
                var node=group[i];var prefab=node.Prefab;
                var effects=prefab.GetComponent<EffectSource>();
                if(!(prefab is ObjectPrefab)||prefab is BuildingPrefab||prefab.GetComponent<ActivityLocation>()!=null||!NestedPayloadPolicy.Accept(
                    prefab.GetComponent<ObjectSubAreas>()?.m_SubAreas?.Length??0,
                    prefab.GetComponent<ObjectSubNets>()?.m_SubNets?.Length??0,
                    prefab.GetComponent<ObjectSubLanes>()?.m_SubLanes?.Length??0,
                    0,effects?.m_AnimationCurves?.Count??0))throw new NotSupportedException("Unsupported attached area, network, lane, effect, animation or activity data on "+prefab.name+".");
                if(node.Effects.Length>(effects?.m_Effects?.Count??0))throw new NotSupportedException("Effect companion count does not match its definition on "+prefab.name+".");
                var links=prefab.GetComponent<ObjectSubObjects>()?.m_SubObjects;
                var children=group.Where(child=>child.Parent==i).ToArray();
                if((links?.Length??0)!=children.Length)throw new NotSupportedException("Child count mismatch on "+prefab.name+": definition="+(links?.Length??0)+", live="+children.Length+".");
                foreach(var child in children){
                    int index=child.Local.m_PrefabSubIndex;
                    if(index<0||index>=links.Length)throw new NotSupportedException("Child descriptor index out of range on "+prefab.name+": "+index+".");
                    var link=links[index];var local=child.Local;
                    if(link==null)throw new NotSupportedException("Null child descriptor on "+prefab.name+".");
                    if(!ReferenceEquals(link.m_Object,child.Prefab)||link.m_Probability!=100||link.m_GroupIndex!=0||local.m_Probability!=100||local.m_GroupIndex!=0||local.m_ParentMesh!=link.m_ParentMesh||!local.m_Position.Equals(link.m_Position)||!local.m_Rotation.Equals(link.m_Rotation))throw new NotSupportedException("Child descriptor mismatch on "+prefab.name+" index="+index+": prefab="+ReferenceEquals(link.m_Object,child.Prefab)+", probability="+local.m_Probability+"/"+link.m_Probability+", group="+local.m_GroupIndex+"/"+link.m_GroupIndex+", mesh="+local.m_ParentMesh+"/"+link.m_ParentMesh+", position="+local.m_Position.Equals(link.m_Position)+", rotation="+local.m_Rotation.Equals(link.m_Rotation)+".");
                }
                if(children.Select(child=>child.Local.m_PrefabSubIndex).Distinct().Count()!=children.Length)throw new NotSupportedException("Duplicate child descriptor indices on "+prefab.name+".");
            }
            return true;
        }
        private bool UseOrdinaryNestedGroup(PrefabBase targetAsset=null,Unity.Entities.Entity targetOwner=default(Unity.Entities.Entity)){
            var selected=targetAsset??context.Asset;
            if(targetAsset==null?!CanArm:disabled||Busy||context.DialogBusy||!context.EditorAvailable||selected.isReadOnly)return false;
            var direct=selected.GetComponent<ObjectSubObjects>()?.m_SubObjects?.Where(value=>value?.m_Object is ObjectPrefab).Select(value=>value.m_Object).Distinct().ToArray();
            var candidates=BoundPropSelection.Select(direct,prefab=>(prefab.GetComponent<ObjectSubObjects>()?.m_SubObjects?.Length??0)>0);
            if(candidates.Length==0){Status="No bound object props found in this asset.";return false;}
            try{
                    var roots=NestedRoots(candidates,targetOwner);
                    foreach(var root in roots)LogOrdinarySchema(root);
                    var images=roots.Select(root=>CaptureNestedImage(root,false)).ToArray();
                    if(images.Length==0||candidates.Any(candidate=>!images.Any(image=>ReferenceEquals(image[0].Prefab,candidate))))throw new NotSupportedException("A prop type has no live placed group. Adding a list entry alone may not have created a placed instance.");
                    foreach(var image in images)OrdinaryGroupSupported(image);
                    var owner=images[0][0].Owner;
                    if(images.Any(image=>image[0].Owner!=owner)||!OwnerMatches(new Record{Asset=selected,Owner=owner})||MatchForest(images,images)==null)throw new NotSupportedException("Group ownership or root placement is ambiguous or does not match the owning asset.");
                    var ids=images.SelectMany(image=>image.Select(node=>node.Original)).ToArray();if(ids.Distinct().Count()!=ids.Length)throw new NotSupportedException("Groups share live child objects.");
                    var refs=images.SelectMany(GroupReferences).Distinct().ToDictionary(prefab=>prefab,Capture);
                    if(refs.Values.Sum(value=>value.Weight)>250000||refs.Any(pair=>!NestedReferenceMatches(pair.Key,pair.Value)))throw new NotSupportedException("Reference snapshot changed or exceeds the snapshot budget.");
                    ordinaryNestedTypes=NestedRootTypes.Copy(candidates);nestedFixture=(ObjectPrefab)candidates[0];ordinaryNested=true;
                    Mod.Log.Info("Bound props adopted after read-only typed/schema/reference checks; types="+string.Join(",",candidates.Select(candidate=>candidate.name))+" groups="+images.Length+" nodes="+ids.Length+"; recovery remains unarmed.");
            }catch(NotSupportedException error){Status="Cannot enable prop Undo: "+error.Message;Mod.Log.Info(Status);return false;}
            return true;
        }
    }
}
