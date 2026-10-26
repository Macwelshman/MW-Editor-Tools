using System;
using System.Linq;
using System.Collections.Generic;
namespace Unity.Entities {
 struct Entity {internal int Id;internal static Entity Null=>default;public override bool Equals(object o)=>o is Entity e&&e.Id==Id;public override int GetHashCode()=>Id;public static bool operator ==(Entity a,Entity b)=>a.Id==b.Id;public static bool operator !=(Entity a,Entity b)=>a.Id!=b.Id;}
 class Buffer<T>:List<T>{internal int Length=>Count;}
 class Manager {
  internal Dictionary<Entity,Dictionary<Type,object>> Data=new();internal Dictionary<Entity,Buffer<Game.Rendering.MeshColor>> Colours=new();internal int Writes,FailAt=-1;
  internal bool HasBuffer<T>(Entity id)=>false;
  internal bool Exists(Entity id)=>Data.ContainsKey(id);internal bool HasComponent<T>(Entity id)=>Data[id].ContainsKey(typeof(T));
  internal T GetComponentData<T>(Entity id)=>(T)Data[id][typeof(T)];
  internal void SetComponentData<T>(Entity id,T value){Data[id][typeof(T)]=value;Writes++;if(Writes==FailAt){FailAt=-1;throw new InvalidOperationException("injected partial write");}}
  internal void SetComponentEnabled<T>(Entity id,bool enabled){}
  internal Buffer<T> GetBuffer<T>(Entity id,bool readOnly=false)=>(Buffer<T>)(object)Colours[id];
 }
 class World {internal Manager EntityManager=new();}
}
namespace UnityEngine {static class Time {internal static int frameCount;}}
namespace Game.Common {struct Deleted{}struct Created{}struct Updated{}struct PseudoRandomSeed{internal int Value;}}
namespace Game.Tools {struct Temp{}struct LocalCurveCache{internal int m_Curve;public static implicit operator LocalCurveCache(int value)=>new(){m_Curve=value};public static implicit operator int(LocalCurveCache value)=>value.m_Curve;}}
namespace Game.Objects {struct Transform{internal int Value;}struct Color{}struct Decoration{}struct Plant{}struct Tree{}}
namespace Game.Net {struct SubLane{}}
namespace Game.Rendering {struct MeshColor {internal int Value;}}
namespace Game.Prefabs {
 struct PrefabRef{internal Unity.Entities.Entity m_Prefab;}
 class PrefabBase {internal string name="Prefab";internal ObjectSubObjects Rows;internal T GetComponent<T>() where T:class=>Rows as T;}
 class ObjectSubObjects {internal ObjectSubObjectInfo[] m_SubObjects;}
 class ObjectSubObjectInfo {internal PrefabBase Prefab;internal int Index;}
}
namespace MW.EditorTools {static class Mod {internal static class Log {internal static void Info(string text){}internal static void Warn(string text){}}}}
namespace MW.EditorTools.Modules.BulldozerRecovery {
 using Unity.Entities;using Game.Prefabs;
 class AreaCompanionEffect {internal Entity Original;internal PrefabBase Prefab,Container;internal static bool Matches(AreaCompanionEffect[] a,AreaCompanionEffect[] b)=>a.Length==b.Length;}
 class AreaCompanionNetwork {
  internal class Item {internal bool HasLocal,HasLane,HasSeed;internal Game.Tools.LocalCurveCache Local;internal Game.Common.PseudoRandomSeed Seed;internal Entity Id;internal PrefabBase Prefab,Container;internal Entity[] Connections=Array.Empty<Entity>(),SubNets=Array.Empty<Entity>();internal int[] SubLanes=Array.Empty<int>();}
  internal class Anchor {internal Entity Id;internal PrefabBase Prefab;}
  internal static bool CanRestoreLocal(Game.Tools.LocalCurveCache expected,Game.Tools.LocalCurveCache actual)=>expected.m_Curve>=0&&(actual.m_Curve<0||expected.m_Curve==actual.m_Curve);
  internal static bool LocalSnapshotEqual(Game.Tools.LocalCurveCache a,Game.Tools.LocalCurveCache b)=>a.m_Curve==b.m_Curve;
  internal string ExplainMismatch(AreaCompanionNetwork actual,bool seeds,bool compareLocal=true,bool nativeRounding=false,bool rebuiltObjects=false,Game.Objects.Transform? markerOwner=null)=>"test graph mismatch";
  internal Item[] Items=Array.Empty<Item>();internal Anchor[] Anchors=Array.Empty<Anchor>();internal Entity Owner;internal bool Valid=true,RequireRebuild;internal int Kind;
  internal bool TryMap(AreaCompanionNetwork other,bool seeds,out Dictionary<Entity,Entity> map,bool compareLocal=true,bool nativeRounding=false,bool rebuiltObjects=false,Game.Objects.Transform? markerOwner=null){map=new(){{Owner,other.Owner}};if(Items.Length!=other.Items.Length)return false;for(int i=0;i<Items.Length;i++)map[Items[i].Id]=other.Items[i].Id;return (!RequireRebuild||rebuiltObjects)&&Valid&&other.Valid&&Kind==other.Kind&&Items.Select((item,i)=>item.HasLocal==other.Items[i].HasLocal&&(!compareLocal||!item.HasLocal||item.Local.m_Curve==other.Items[i].Local.m_Curve)&&item.HasSeed==other.Items[i].HasSeed&&(!seeds||item.Seed.Equals(other.Items[i].Seed))).All(v=>v);}
 }
 class MainPropData {
  internal Game.Objects.Transform Transform;internal Game.Common.PseudoRandomSeed Seed;internal int Surface;internal Game.Rendering.MeshColor[] Colours;internal int[] CustomColours;
  internal MainPropData(Game.Objects.Transform transform,Game.Common.PseudoRandomSeed seed,int surface,Game.Rendering.MeshColor[] colours,int[] custom){Transform=transform;Seed=seed;Surface=surface;Colours=colours.ToArray();CustomColours=custom.ToArray();}
  internal bool Matches(MainPropData other)=>Transform.Equals(other.Transform)&&Seed.Equals(other.Seed)&&Surface==other.Surface&&Colours.SequenceEqual(other.Colours)&&CustomColours.SequenceEqual(other.CustomColours);
 }
 static class BoundDescriptorIndex {internal static int ResolveNative(ObjectSubObjectInfo[] rows,PrefabBase prefab,Local local,Game.Objects.Transform owner,Game.Objects.Transform world)=>Resolve(rows,prefab,local);internal static int Resolve(ObjectSubObjectInfo[] rows,PrefabBase prefab,Local local)=>rows.Single(r=>ReferenceEquals(r.Prefab,prefab)).Index;}
 struct Local {internal int m_PrefabSubIndex;}
 class SpawnAccessImage {internal bool Present;internal SpawnAccessImage AfterAreaDeletion(SpawnAccessImage after,HashSet<Entity> removed,HashSet<Entity> retained,Dictionary<Entity,Entity> map)=>null;internal bool Matches(SpawnAccessImage other,Dictionary<Entity,Entity> map=null)=>true;}
 static class OwnedConnectionPolicy {internal static Func<AreaCompanionNetwork,AreaCompanionNetwork,IEnumerable<Entity>,AreaCompanionNetwork> Projection;internal static AreaCompanionNetwork AfterObjectDeletion(AreaCompanionNetwork before,AreaCompanionNetwork after,IEnumerable<Entity> deleted)=>Projection==null?null:Projection(before,after,deleted);}
 class LaneImage {internal AreaCompanionNetwork Graph;}
 internal sealed partial class BulldozerTrace {
  private string spawnDeletionRefusal;
  private void ClearSpawnDeletionCapture(){spawnDeletionRefusal=null;}
  private AreaCompanionNetwork ProjectSpawnDeletion(AreaCompanionNetwork before,AreaCompanionNetwork after,IEnumerable<Entity> deleted)=>OwnedConnectionPolicy.AfterObjectDeletion(before,after,deleted);
  private NestedNode[][] AdaptSpawnDeletionConnections(NestedNode[][] before,NestedNode[][] actual,BoundCompanions current,Dictionary<Entity,Entity> map){var kept=new HashSet<Entity>(current.Network.Items.Where(i=>i.HasLane).Select(i=>i.Id));var removed=new HashSet<Entity>(boundBefore.Network.Items.Where(i=>i.HasLane&&!kept.Contains(i.Id)).Select(i=>i.Id));var allowed=new HashSet<Entity>(kept.Concat(current.AreaLanes.Values.SelectMany(g=>g.Items).Where(i=>i.HasLane).Select(i=>i.Id)));return AdaptConnectionImages(before,actual,removed,allowed,map);}
  private class Payload {internal bool Valid=true;internal int Weight=>1;internal bool Matches(PrefabBase asset)=>Valid;}
  private class AreaImage {internal Entity Original;internal PrefabBase Prefab,Container;internal int Geometry;}
  private class Record {internal Game.Objects.Transform? ReferenceMarkerOwner;internal bool Area,ReferenceRebuiltObjects;internal NestedNode[][] ForestBefore,ForestAfter;internal NestedNode[] Nested;internal Payload Before,After;internal Dictionary<PrefabBase,Payload> NestedReferences;internal PrefabBase Asset;internal Entity Owner,OwnerPrefab;internal BoundCompanions BoundBefore,BoundAfter,ReferenceAreas;internal AreaCompanionNetwork ReferenceNetwork;}
  private class NestedNode {
   internal NestedNode DetachedCopy()=>(NestedNode)MemberwiseClone();internal SpawnAccessImage SpawnAccess=new();internal Entity Original;internal int Parent=-1,Depth;internal PrefabBase Prefab;internal MainPropData Data;internal Local Local;internal LaneImage Lanes=new();internal int Companion;
   internal bool HasSurface=true,HasColor,HasDecoration,HasPlant,HasTree,DecorationEnabled;internal Game.Objects.Color Color;internal Game.Objects.Plant Plant;internal Game.Objects.Tree Tree;
  }
  private class Session {internal PrefabBase Asset;internal bool Valid=true;internal bool Matches(PrefabBase asset)=>Valid&&ReferenceEquals(asset,Asset);}
  private World world=new();private Session session=new();private Payload after=new();private Entity observedOwner;private PrefabBase TargetRoot=>session.Asset;private bool areaCapture;private string Status;
  private Dictionary<PrefabBase,Payload> nestedReferences=new(),areaReferences=new();private NestedNode[][] forestBefore,areaProps;private NestedNode[] nestedBefore;private PrefabBase[] capturedRootTypes;private Entity deletedArea;
  private AreaImage[] liveAreas=Array.Empty<AreaImage>();private AreaCompanionEffect[] liveEffects=Array.Empty<AreaCompanionEffect>();private AreaCompanionNetwork liveNetwork;private NestedNode[][] liveGroups;private bool ownerValid=true;private bool invalidNetworkLocal;
  private class NetworkEntry {
   internal NetworkEntry RollbackOf;internal Record BoundRecord;internal PrefabBase Asset,TargetPrefab;internal Entity Owner,OwnerPrefab,Target;internal bool Move,AreaMove,Lane;internal object EffectEntry;
   internal Payload Before,After;internal AreaCompanionNetwork Network,AfterNetwork;internal NestedNode[][] Props,AfterProps;internal AreaImage[] Areas;internal AreaCompanionEffect[] Effects;internal Dictionary<PrefabBase,Payload> References;internal Game.Objects.Transform Transform;internal BoundCompanions CompanionMap;
  }
  private readonly MW.EditorTools.History.LastDeletion<Record> records=new();
  private NetworkEntry networkPending;private bool networkRestoring;private int rollbackStarts;
  private bool NetworkOwnerValid(NetworkEntry e)=>ownerValid;
  private bool AreaReferenceMap(BoundCompanions expected,Entity owner,Dictionary<Entity,Entity> map,bool native,bool strictLocal)=>AreasMatch(expected.Areas,owner)&&MapAreaLanes(expected,CaptureBoundCompanions(owner,true,native),map,false,native,strictLocal);
  private void StartBoundRestoration(NetworkEntry e,bool repairRead=false){networkPending=e;rollbackStarts++;}
  private bool NetworkSettled(AreaCompanionNetwork graph)=>graph.Items.All(i=>world.EntityManager.Exists(i.Id)&&!world.EntityManager.HasComponent<Game.Common.Created>(i.Id)&&!world.EntityManager.HasComponent<Game.Common.Updated>(i.Id));
  private static bool FiniteCurve(int curve)=>curve>=0;
  private static int AreaWeight(AreaImage[] areas)=>areas.Length;
  private bool OwnerMatches(Record entry)=>ownerValid;
  private static Payload Capture(PrefabBase prefab)=>new();
  private static bool NestedReferenceMatches(PrefabBase prefab,Payload image)=>image.Valid;
  private AreaImage[] CaptureAreas(Entity owner)=>liveAreas;
  private AreaCompanionEffect[] CaptureAreaEffects(Entity owner)=>liveEffects;
  private AreaCompanionNetwork CaptureAreaNetwork(Entity owner,bool allowInvalidLocal=false,bool objectAnchors=true){if(invalidNetworkLocal&&!allowInvalidLocal)throw new NotSupportedException("Nonfinite local network curve.");return new(){Owner=liveNetwork.Owner,Valid=liveNetwork.Valid,Kind=liveNetwork.Kind,Anchors=liveNetwork.Anchors,Items=liveNetwork.Items.Select(i=>new AreaCompanionNetwork.Item{Id=i.Id,HasLocal=i.HasLocal,Local=world.EntityManager.Exists(i.Id)&&world.EntityManager.HasComponent<Game.Tools.LocalCurveCache>(i.Id)?world.EntityManager.GetComponentData<Game.Tools.LocalCurveCache>(i.Id):invalidNetworkLocal?-1:i.Local,HasSeed=i.HasSeed,Seed=i.HasSeed?world.EntityManager.GetComponentData<Game.Common.PseudoRandomSeed>(i.Id):i.Seed,Prefab=i.Prefab,Container=i.Container}).ToArray()};}
  private static bool AreaEqual(AreaImage a,AreaImage b)=>a.Geometry==b.Geometry&&ReferenceEquals(a.Prefab,b.Prefab);
  private bool AreasMatch(AreaImage[] expected,Entity owner)=>expected.Length==liveAreas.Length&&expected.All(e=>liveAreas.Any(a=>a.Original==e.Original&&a.Geometry==e.Geometry));
  private bool CompanionSourcesValid(IEnumerable<Entity> expected,IEnumerable<Entity> actual)=>expected.All(e=>!world.EntityManager.Exists(e)||actual.Contains(e));
  private static IEnumerable<Entity> GroupSources(NestedNode[] group)=>group.Select(n=>n.Original);
  private static bool SameNestedPlacement(NestedNode a,NestedNode b)=>a.Data.Transform.Equals(b.Data.Transform);
  private static string NestedCompanionDifference(NestedNode a,NestedNode b,Dictionary<Entity,Entity> map)=>" stub companion="+(a.Companion==b.Companion);
  private static bool SameNestedElevation(NestedNode a,NestedNode b,bool values=true,Dictionary<Entity,Entity> referenceMap=null)=>a.Companion==b.Companion;
  private static NestedNode[][] MatchForest(NestedNode[][] expected,NestedNode[][] actual)=>expected.Length==actual.Length?expected.Select(e=>actual.Single(g=>ReferenceEquals(e[0].Prefab,g[0].Prefab))).ToArray():null;
  private static bool NestedGroupAtIndex(NestedNode[] e,NestedNode[] a,int index,Dictionary<Entity,Entity> map)=>e.Length==a.Length&&e.Select((n,i)=>n.Companion==a[i].Companion&&n.Data.Matches(a[i].Data)&&a[i].Local.m_PrefabSubIndex==index).All(v=>v);
  private Entity[] NestedRoots(PrefabBase[] types,Entity owner)=>liveGroups.Select(g=>g[0].Original).ToArray();
  private NestedNode[] CaptureNestedImage(Entity id,bool log){var source=liveGroups.Single(g=>g[0].Original==id)[0];return new[]{new NestedNode{Original=id,Prefab=source.Prefab,Local=source.Local,Companion=source.Companion,Data=new MainPropData(world.EntityManager.GetComponentData<Game.Objects.Transform>(id),world.EntityManager.GetComponentData<Game.Common.PseudoRandomSeed>(id),source.Data.Surface,world.EntityManager.GetBuffer<Game.Rendering.MeshColor>(id).ToArray(),source.Data.CustomColours)}};}
  private NestedNode[][] CaptureAreaProps(Entity owner)=>liveGroups.Select(g=>CaptureNestedImage(g[0].Original,false)).ToArray();
  private static Entity Id(int id)=>new(){Id=id};
  private void Setup(bool area=false){
   OwnedConnectionPolicy.Projection=null;
   observedOwner=Id(1);session.Asset=new PrefabBase();world.EntityManager.Data[observedOwner]=new(){{typeof(Game.Objects.Transform),new Game.Objects.Transform{Value=10}}};liveNetwork=new(){Owner=observedOwner};
   NestedNode Node(int id,int index,int pose){var prefab=new PrefabBase{name="Node"+id};var node=new NestedNode{Original=Id(id),Prefab=prefab,Local=new(){m_PrefabSubIndex=index},Data=new(new(){Value=pose},new(){Value=id},1,new[]{new Game.Rendering.MeshColor{Value=id}},new[]{id})};world.EntityManager.Data[node.Original]=new(){{typeof(Game.Objects.Transform),node.Data.Transform},{typeof(Game.Common.PseudoRandomSeed),node.Data.Seed}};world.EntityManager.Colours[node.Original]=new(){new(){Value=id}};return node;}
   var target=Node(10,0,100);var a=Node(11,1,110);var b=Node(12,2,120);nestedBefore=new[]{target};forestBefore=new[]{new[]{target},new[]{a},new[]{b}};areaProps=forestBefore.Skip(1).ToArray();capturedRootTypes=forestBefore.Select(g=>g[0].Prefab).ToArray();
   var liveA=Node(21,0,110);liveA.Prefab=a.Prefab;liveA.Data=a.Data;world.EntityManager.Data[liveA.Original][typeof(Game.Common.PseudoRandomSeed)]=a.Data.Seed;world.EntityManager.Colours[liveA.Original][0]=a.Data.Colours[0];
   var liveB=Node(22,1,120);liveB.Prefab=b.Prefab;liveB.Data=b.Data;world.EntityManager.Data[liveB.Original][typeof(Game.Common.PseudoRandomSeed)]=b.Data.Seed;world.EntityManager.Colours[liveB.Original][0]=b.Data.Colours[0];
   foreach(var id in new[]{target.Original,a.Original,b.Original})world.EntityManager.Data.Remove(id);
   liveGroups=new[]{new[]{liveA},new[]{liveB}};session.Asset.Rows=new(){m_SubObjects=new[]{new ObjectSubObjectInfo{Prefab=a.Prefab,Index=0},new ObjectSubObjectInfo{Prefab=b.Prefab,Index=1}}};
   areaCapture=area;deletedArea=Id(5);if(area)liveAreas=new[]{new AreaImage{Original=deletedArea,Geometry=5},new AreaImage{Original=Id(6),Geometry=6}};
   foreach(var row in liveAreas)world.EntityManager.Data[row.Original]=new();
   world.EntityManager.Data[observedOwner][typeof(PrefabRef)]=new PrefabRef{m_Prefab=Id(99)};boundBefore=CaptureBoundCompanions(observedOwner);if(area){liveAreas=liveAreas.Skip(1).ToArray();world.EntityManager.Data.Remove(deletedArea);}
  }
  internal static void Main(){
   int checks=0;void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
   var a=new BulldozerTrace();a.Setup();UnityEngine.Time.frameCount=1;Check(!a.BoundCaptureReady()&&!a.BoundCaptureReady(),"same-frame callbacks do not record");UnityEngine.Time.frameCount=2;Check(!a.BoundCaptureReady(),"second stable frame waits");UnityEngine.Time.frameCount=3;Check(a.BoundCaptureReady()&&a.boundAfter!=null&&a.boundForestAfter.Length==2&&a.world.EntityManager.Writes==0,"exact retained forest records after stable window without writes");
   a=new();a.Setup();var id=a.liveGroups[0][0].Original;a.world.EntityManager.Data[id][typeof(Game.Common.PseudoRandomSeed)]=new Game.Common.PseudoRandomSeed{Value=99};UnityEngine.Time.frameCount=10;Check(!a.BoundCaptureReady()&&a.world.EntityManager.GetComponentData<Game.Common.PseudoRandomSeed>(id).Value==11,"native reseed restored after complete preflight");for(int f=11;f<=13;f++){UnityEngine.Time.frameCount=f;bool ready=a.BoundCaptureReady();Check(ready==(f==13),"restored values require a fresh stable window");}
   a=new();a.Setup();id=a.liveGroups[0][0].Original;a.world.EntityManager.Data[id][typeof(Game.Common.PseudoRandomSeed)]=new Game.Common.PseudoRandomSeed{Value=99};a.liveGroups[1][0].Companion=1;Check(!a.BoundCaptureReady()&&a.world.EntityManager.Writes==0,"invalid later companion prevents every write");
   a=new();a.Setup();a.liveNetwork=new(){Owner=a.observedOwner,Kind=7};Check(!a.BoundCaptureReady()&&a.world.EntityManager.Writes==0,"changed network prevents writes");
   a=new();a.Setup();a.ownerValid=false;Check(!a.BoundCaptureReady()&&a.world.EntityManager.Writes==0,"changed owner prevents writes");
   a=new();a.Setup();a.after.Valid=false;Check(!a.BoundCaptureReady()&&a.world.EntityManager.Writes==0,"changed source payload prevents writes");
   a=new();a.Setup();id=a.liveGroups[0][0].Original;a.world.EntityManager.Data[id][typeof(Game.Common.PseudoRandomSeed)]=new Game.Common.PseudoRandomSeed{Value=99};a.world.EntityManager.FailAt=3;bool failed=false;try{a.BoundCaptureReady();}catch(InvalidOperationException){failed=true;}Check(failed&&a.world.EntityManager.GetComponentData<Game.Common.PseudoRandomSeed>(id).Value==99&&a.boundAfter==null,"failed partial write rolls back to native after-image and cannot record");
   a=new();a.Setup(true);for(int f=20;f<=22;f++){UnityEngine.Time.frameCount=f;bool ready=a.BoundCaptureReady();Check(ready==(f==22),"bound area deletion uses the same settling window");}Check(a.boundBefore.Areas.Length==2&&a.boundAfter.Areas.Length==1,"area target remains in before-image only");
   a=new();a.Setup(true);a.liveAreas=new[]{new AreaImage{Original=Id(6),Geometry=99}};Check(!a.BoundCaptureReady()&&a.world.EntityManager.Writes==0,"changed retained area geometry refuses recording");
   a=new();a.Setup();a.world.EntityManager.Data[a.observedOwner][typeof(Game.Common.Updated)]=new Game.Common.Updated();Check(!a.BoundCaptureReady()&&a.world.EntityManager.Writes==0,"pending owner initialization blocks completion");
   a=new();a.Setup(true);for(int f=30;f<=32;f++){UnityEngine.Time.frameCount=f;a.BoundCaptureReady();}var record=new Record{Owner=a.observedOwner,Asset=a.session.Asset,BoundBefore=a.boundBefore,BoundAfter=a.boundAfter};
   Check(a.BoundRecordCompanionsMatch(record,false)&&!a.BoundRecordCompanionsMatch(record,true),"pre-Undo validation selects the complete after-image");
   var actual=a.CaptureAreaProps(a.observedOwner);Check(a.BoundRecordMap(record,false,a.boundForestAfter,actual,out var mapping)&&mapping[actual[0][0].Original]==actual[0][0].Original,"record validation includes mapped retained graph");
   a.liveNetwork=new(){Owner=a.observedOwner,Kind=8};Check(!a.BoundRecordCompanionsMatch(record,false),"later network edit prevents Undo");
   void SeedNetwork(BulldozerTrace trace){var seedId=Id(30);trace.world.EntityManager.Data[seedId]=new(){{typeof(Game.Common.PseudoRandomSeed),new Game.Common.PseudoRandomSeed{Value=5}}};trace.liveNetwork.Items=new[]{new AreaCompanionNetwork.Item{Id=seedId,HasSeed=true}};trace.boundBefore=trace.CaptureBoundCompanions(trace.observedOwner);trace.world.EntityManager.Data[seedId][typeof(Game.Common.PseudoRandomSeed)]=new Game.Common.PseudoRandomSeed{Value=9};}
   a=new();a.Setup();SeedNetwork(a);UnityEngine.Time.frameCount=40;Check(!a.BoundCaptureReady()&&a.world.EntityManager.GetComponentData<Game.Common.PseudoRandomSeed>(Id(30)).Value==5,"companion network seed restored only after complete retained preflight");
   for(int f=41;f<=43;f++){UnityEngine.Time.frameCount=f;bool ready=a.BoundCaptureReady();Check(ready==(f==43),"network seed preservation requires exact post-write stable frames");}
   record=new Record{Owner=a.observedOwner,BoundBefore=a.boundBefore,BoundAfter=a.boundAfter};a.world.EntityManager.Data[Id(30)][typeof(Game.Common.PseudoRandomSeed)]=new Game.Common.PseudoRandomSeed{Value=19};
   Check(a.RestoreBoundNetworkSeeds(record,true)&&a.world.EntityManager.GetComponentData<Game.Common.PseudoRandomSeed>(Id(30)).Value==5,"Undo preserves companion network seed after native regeneration");
   a.world.EntityManager.Data[Id(30)][typeof(Game.Common.PseudoRandomSeed)]=new Game.Common.PseudoRandomSeed{Value=29};a.liveNetwork.Kind=9;Check(!a.RestoreBoundNetworkSeeds(record,true)&&a.world.EntityManager.GetComponentData<Game.Common.PseudoRandomSeed>(Id(30)).Value==29,"changed graph blocks seed writes during Undo");
   a=new();a.Setup();SeedNetwork(a);a.liveGroups[1][0].Companion=1;Check(!a.BoundCaptureReady()&&a.world.EntityManager.GetComponentData<Game.Common.PseudoRandomSeed>(Id(30)).Value==9&&a.world.EntityManager.Writes==0,"invalid retained companion blocks network and prop writes together");
   a=new();a.Setup();SeedNetwork(a);a.world.EntityManager.FailAt=5;failed=false;try{a.BoundCaptureReady();}catch(InvalidOperationException){failed=true;}Check(failed&&a.world.EntityManager.GetComponentData<Game.Common.PseudoRandomSeed>(Id(30)).Value==9&&a.boundAfter==null,"failed network write rolls back the complete typed-value transaction");
   a=new();a.Setup();a.world.EntityManager.Data[a.observedOwner][typeof(PrefabRef)]=new PrefabRef{m_Prefab=Id(100)};
   var transitioned=a.CaptureBoundCompanions(a.observedOwner);
   Check(a.BoundCompanionsMap(a.boundBefore,transitioned,a.observedOwner,out var ownerMap,a.session.Asset)&&EntityReferenceMap.OwnerPrefabMatches(ownerMap,a.observedOwner,a.observedOwner,Id(99),Id(100)),"validated owner transition reaches companion reference comparison");
   Check(a.boundBefore.OwnerPrefab==Id(99)&&a.world.EntityManager.Writes==0,"owner transition leaves captured and live data untouched");
   a.ownerValid=false;Check(!a.BoundCompanionsMap(a.boundBefore,transitioned,a.observedOwner,out ownerMap,a.session.Asset)&&ownerMap==null,"changed editable owner cannot authorize transition");
   a.ownerValid=true;a.boundBefore.Owner=Id(777);Check(!a.BoundCompanionsMap(a.boundBefore,transitioned,a.observedOwner,out ownerMap,a.session.Asset),"different source owner cannot authorize transition");
   a=new();a.Setup();var sourceArea=new AreaImage{Original=Id(60),Geometry=77};var liveArea=new AreaImage{Original=Id(61),Geometry=77};
   var oldAreas=new BoundCompanions{Areas=new[]{sourceArea},AreaLanes=new(){{Id(60),new AreaCompanionNetwork{Owner=Id(60),Items=new[]{new AreaCompanionNetwork.Item{Id=Id(70),HasLane=true}}}}}};
   var newAreas=new BoundCompanions{Areas=new[]{liveArea},AreaLanes=new(){{Id(61),new AreaCompanionNetwork{Owner=Id(61),Items=new[]{new AreaCompanionNetwork.Item{Id=Id(71),HasLane=true}}}}}};
   var areaMap=new Dictionary<Entity,Entity>();Check(a.MapAreaLanes(oldAreas,newAreas,areaMap,true)&&areaMap[Id(60)]==Id(61)&&areaMap[Id(70)]==Id(71),"actual area graph mapper maps regenerated area and lane identities");
   liveArea.Geometry++;Check(!a.MapAreaLanes(oldAreas,newAreas,new(),true),"changed area geometry cannot authorize lane mapping");liveArea.Geometry--;
   a.world.EntityManager.Data[Id(70)]=new();Check(!a.MapAreaLanes(oldAreas,newAreas,new(),true),"old lane surviving outside mapped graph refuses");a.world.EntityManager.Data.Remove(Id(70));
   Check(!a.MapAreaLanes(oldAreas,newAreas,new(){{Id(99),Id(71)}},true),"noninjective area lane mapping refuses");
   a=new();a.Setup();a.invalidNetworkLocal=true;bool strictRefused=false;
   try{a.CaptureBoundCompanions(a.observedOwner,true);}catch(NotSupportedException){strictRefused=true;}
   Check(strictRefused,"ordinary companion capture still refuses nonfinite native local cache");
   var repairImage=a.CaptureBoundCompanions(a.observedOwner,true,true);Check(repairImage!=null&&a.world.EntityManager.Writes==0,"repair preflight can inspect typed graph without writing native cache");
   var repairRecord=new Record{Owner=a.observedOwner,Asset=a.session.Asset,OwnerPrefab=Id(99),ReferenceNetwork=a.boundBefore.Network,ReferenceAreas=a.boundBefore};
   var repairActual=a.CaptureAreaProps(a.observedOwner);
   Check(a.BoundRecordMap(repairRecord,true,repairActual,repairActual,out _)&&a.world.EntityManager.Writes==0,"actual retained-reference preflight reaches guarded network repair with nonfinite cache");
   a.ownerValid=false;Check(!a.BoundRecordMap(repairRecord,true,repairActual,repairActual,out _),"cache preflight still refuses changed owner");
   a=new();a.Setup();a.liveNetwork.Items=new[]{new AreaCompanionNetwork.Item{Id=Id(88),HasLocal=true,Local=5}};a.world.EntityManager.Data[Id(88)]=new();
   var guardedNetwork=a.CaptureAreaNetwork(a.observedOwner);a.liveNetwork.Items[0].Local=12;
   repairRecord=new Record{Owner=a.observedOwner,Asset=a.session.Asset,OwnerPrefab=Id(99),ReferenceNetwork=guardedNetwork,ReferenceAreas=a.boundBefore};repairActual=a.CaptureAreaProps(a.observedOwner);
   Check(!a.BoundRecordMap(repairRecord,true,repairActual,repairActual,out _)&&a.world.EntityManager.Writes==0,"changed finite cache refuses before retained companion writes");
   a=new();a.Setup();a.nestedBefore[0].SpawnAccess.Present=true;
   var deletedConnection=new AreaCompanionNetwork{Owner=a.observedOwner,Items=new[]{new AreaCompanionNetwork.Item{Id=Id(90),HasLane=true}}};
   a.boundBefore.Network=deletedConnection;int projectedCalls=0;
   OwnedConnectionPolicy.Projection=(before,live,deleted)=>{projectedCalls++;if(!deleted.SequenceEqual(new[]{Id(10)}))throw new Exception("wrong deletion ownership scope");return live;};
   for(int frame=200;frame<=202;frame++){UnityEngine.Time.frameCount=frame;Check(a.BoundCaptureReady()==(frame==202),"spawn deletion invokes ownership projection then still requires three stable frames");}
   Check(projectedCalls==3&&a.boundBefore.Network.Items.Length==1&&a.boundAfter.Network.Items.Length==0&&a.world.EntityManager.Writes==0,"spawn after-image uses projected closure without modifying full original recovery image or entity values");
   a=new();a.Setup();a.nestedBefore[0].SpawnAccess.Present=true;
   Check(!a.BoundCaptureReady()&&a.world.EntityManager.Writes==0,"failed spawn connection proof prevents retained writes and recording");
   a=new();a.Setup();a.nestedBefore[0].SpawnAccess.Present=true;OwnedConnectionPolicy.Projection=(before,live,deleted)=>live;a.liveGroups[1][0].Companion=1;
   Check(!a.BoundCaptureReady()&&a.world.EntityManager.Writes==0,"valid spawn closure cannot bypass changed retained companion data");
   a=new();a.Setup();a.nestedBefore[0].SpawnAccess.Present=true;
   var spawnRecord=new Record{Asset=a.session.Asset,Owner=a.observedOwner,BoundBefore=a.boundBefore,BoundAfter=a.CaptureBoundCompanions(a.observedOwner),Before=new(),After=new(),ForestBefore=a.forestBefore,ForestAfter=a.liveGroups,Nested=a.nestedBefore,NestedReferences=a.nestedReferences};
   a.records.Record(spawnRecord);
   Check(a.TryRestoreSpawnDeletion(spawnRecord,true)&&a.rollbackStarts==1&&ReferenceEquals(a.networkPending.BoundRecord,spawnRecord),"actual spawn dispatch uses existing complete bound regeneration pipeline and original history identity");
   Check(ReferenceEquals(a.networkPending.Network,spawnRecord.BoundBefore.Network)&&ReferenceEquals(a.networkPending.AfterNetwork,spawnRecord.BoundAfter.Network)&&ReferenceEquals(a.networkPending.Props,spawnRecord.ForestBefore)&&ReferenceEquals(a.networkPending.CompanionMap,spawnRecord.BoundBefore),"spawn dispatch retains complete original graph, companion and typed forest images");
   Check(a.world.EntityManager.Writes==0,"spawn adapter performs no direct entity/reference writes");
   Check(ReferenceEquals(a.records.Latest,spawnRecord),"spawn dispatch cannot consume original Undo slot before validated completion");
   Check(a.CompleteSpawnDeletion(a.networkPending)&&a.records.UndoCount==0,"actual completion consumes only the original spawn record");
   var newerRecord=new Record();a.records.Record(newerRecord);
   Check(!a.CompleteSpawnDeletion(a.networkPending)&&ReferenceEquals(a.records.Latest,newerRecord),"stale spawn completion cannot consume a newer action");
   Check(!a.CompleteSpawnDeletion(new NetworkEntry())&&ReferenceEquals(a.records.Latest,newerRecord),"ordinary network completion cannot consume spawn history");
   a.rollbackStarts=0;Check(!a.TryRestoreSpawnDeletion(spawnRecord,false)&&a.rollbackStarts==0,"adapter does not extend Redo behavior");
   spawnRecord.Nested[0].SpawnAccess.Present=false;Check(!a.TryRestoreSpawnDeletion(spawnRecord,true)&&a.rollbackStarts==0,"ordinary prop Undo stays on accepted path");spawnRecord.Nested[0].SpawnAccess.Present=true;
   spawnRecord.Area=true;Check(!a.TryRestoreSpawnDeletion(spawnRecord,true),"area deletion stays on accepted path");spawnRecord.Area=false;
   spawnRecord.After.Valid=false;bool sourceRefused=false;try{a.TryRestoreSpawnDeletion(spawnRecord,true);}catch(NotSupportedException){sourceRefused=true;}Check(sourceRefused&&a.rollbackStarts==0,"changed after-source refuses spawn dispatch without writes");spawnRecord.After.Valid=true;
   a.ownerValid=false;bool ownerRefused=false;try{a.TryRestoreSpawnDeletion(spawnRecord,true);}catch(NotSupportedException){ownerRefused=true;}Check(ownerRefused&&a.rollbackStarts==0,"changed owner refuses spawn dispatch");a.ownerValid=true;
   var guardedPrefab=new PrefabBase();spawnRecord.NestedReferences=new(){{guardedPrefab,new Payload{Valid=false}}};bool refsRefused=false;try{a.TryRestoreSpawnDeletion(spawnRecord,true);}catch(NotSupportedException){refsRefused=true;}Check(refsRefused&&a.rollbackStarts==0,"changed referenced prefab refuses spawn dispatch");
   a=new();a.Setup();var rebuiltGraph=a.CaptureAreaNetwork(a.observedOwner);rebuiltGraph.RequireRebuild=true;
   var anchorRecord=new Record{Owner=a.observedOwner,OwnerPrefab=Id(99),Asset=a.session.Asset,ReferenceNetwork=rebuiltGraph};var anchorForest=a.CaptureAreaProps(a.observedOwner);
   Check(!a.BoundRecordMap(anchorRecord,true,anchorForest,anchorForest,out _),"ordinary retained reference mapping cannot opt into rebuilt anchor identities");
   anchorRecord.ReferenceRebuiltObjects=true;
   Check(a.BoundRecordMap(anchorRecord,true,anchorForest,anchorForest,out _)&&a.world.EntityManager.Writes==0,"actual reference preflight forwards spawn-only rebuilt identity proof without writes");
   a.liveNetwork.Kind=7;Check(!a.BoundRecordMap(anchorRecord,true,anchorForest,anchorForest,out _),"rebuilt identity scope cannot bypass different network metadata");a.liveNetwork.Kind=0;
   a.ownerValid=false;Check(!a.BoundRecordMap(anchorRecord,true,anchorForest,anchorForest,out _),"rebuilt identity scope cannot bypass owner validation");
   void CacheCase(out BulldozerTrace trace,out NetworkEntry entry,out AreaCompanionNetwork live,out Dictionary<Entity,Entity> map){
    trace=new();trace.Setup();var cache=Id(90);trace.world.EntityManager.Data[cache]=new(){{typeof(Game.Tools.LocalCurveCache),(Game.Tools.LocalCurveCache)(-1)},{typeof(Game.Common.PseudoRandomSeed),new Game.Common.PseudoRandomSeed{Value=99}}};
    var expectedGraph=new AreaCompanionNetwork{Owner=trace.observedOwner,Items=new[]{new AreaCompanionNetwork.Item{Id=cache,HasLocal=true,Local=5,HasSeed=true,Seed=new(){Value=7}}}};
    trace.liveNetwork=new(){Owner=trace.observedOwner,Items=new[]{new AreaCompanionNetwork.Item{Id=cache,HasLocal=true,Local=-1,HasSeed=true,Seed=new(){Value=99}}}};
    live=trace.CaptureAreaNetwork(trace.observedOwner,true);entry=new(){Asset=trace.session.Asset,Owner=trace.observedOwner,OwnerPrefab=Id(99),Before=new(),After=new(),Network=expectedGraph,AfterNetwork=trace.liveNetwork,Props=trace.liveGroups,Areas=Array.Empty<AreaImage>(),Effects=Array.Empty<AreaCompanionEffect>(),References=new()};map=new(){{trace.observedOwner,trace.observedOwner},{cache,cache}};
   }
   CacheCase(out a,out var cacheEntry,out var cacheLive,out var cacheMap);
   Check(a.RepairNetworkCaches(cacheEntry,cacheLive,cacheMap)&&a.world.EntityManager.GetComponentData<Game.Tools.LocalCurveCache>(Id(90)).m_Curve==5&&a.world.EntityManager.GetComponentData<Game.Common.PseudoRandomSeed>(Id(90)).Value==7,"actual cache repair restores finite captured cache and seed after typed mapping");
   CacheCase(out a,out cacheEntry,out cacheLive,out cacheMap);cacheLive.Items[0].Local=12;
   Check(!a.RepairNetworkCaches(cacheEntry,cacheLive,cacheMap)&&a.world.EntityManager.Writes==0,"actual cache repair refuses changed finite cache without writes");
   CacheCase(out a,out cacheEntry,out cacheLive,out cacheMap);a.world.EntityManager.FailAt=2;bool cacheFailed=false;
   try{a.RepairNetworkCaches(cacheEntry,cacheLive,cacheMap);}catch(InvalidOperationException){cacheFailed=true;}
   Check(cacheFailed&&a.world.EntityManager.GetComponentData<Game.Tools.LocalCurveCache>(Id(90)).m_Curve==-1&&a.world.EntityManager.GetComponentData<Game.Common.PseudoRandomSeed>(Id(90)).Value==99,"actual cache transaction rolls back cache and seed after partial write failure");
   CacheCase(out a,out cacheEntry,out cacheLive,out cacheMap);a.world.EntityManager.Data[Id(90)][typeof(Game.Common.Created)]=new Game.Common.Created();
   var pendingRecord=new Record{Owner=a.observedOwner,Asset=a.session.Asset,OwnerPrefab=Id(99),ReferenceNetwork=cacheEntry.Network};var pendingForest=a.CaptureAreaProps(a.observedOwner);
   Check(!a.BoundRecordMap(pendingRecord,true,pendingForest,pendingForest,out _)&&a.world.EntityManager.Writes==0,"pending native graph initialization prevents retained writes");
   CacheCase(out a,out cacheEntry,out cacheLive,out cacheMap);cacheEntry.CompanionMap=a.boundBefore;cacheEntry.Areas=new[]{new AreaImage{Original=Id(777),Geometry=9}};
   Check(!a.RepairNetworkCaches(cacheEntry,cacheLive,cacheMap)&&a.world.EntityManager.Writes==0,"changed retained area prevents cache repair writes");
   CacheCase(out a,out cacheEntry,out cacheLive,out cacheMap);a.networkPending=cacheEntry;a.networkRestoring=true;
   Check(a.TryRollbackFence("test failed restoration")&&a.rollbackStarts==1&&a.networkPending.RollbackOf==cacheEntry&&ReferenceEquals(a.networkPending.Before,cacheEntry.After)&&ReferenceEquals(a.networkPending.Network,cacheEntry.AfterNetwork),"actual fence failure dispatches recorded after-image through guarded native restoration");
   Check(!a.TryRollbackFence("rollback failure")&&a.rollbackStarts==1,"rollback cannot recurse or claim completion");
   CacheCase(out a,out cacheEntry,out cacheLive,out cacheMap);a.networkPending=cacheEntry;a.networkRestoring=true;cacheEntry.Before.Valid=false;
   Check(!a.TryRollbackFence("source changed")&&a.rollbackStarts==0,"changed source refuses rollback without dispatch");
   cacheEntry.Before.Valid=true;a.ownerValid=false;Check(!a.TryRollbackFence("owner changed")&&a.rollbackStarts==0,"changed owner refuses rollback without dispatch");
   a.ownerValid=true;cacheEntry.Props=cacheEntry.Props.Select(g=>g.Select(n=>{var copy=n.DetachedCopy();copy.Data=new MainPropData(n.Data.Transform,n.Data.Seed,n.Data.Surface,n.Data.Colours,n.Data.CustomColours);return copy;}).ToArray()).ToArray();a.liveGroups[0][0].Data.Transform=new Game.Objects.Transform{Value=999};a.world.EntityManager.Data[a.liveGroups[0][0].Original][typeof(Game.Objects.Transform)]=new Game.Objects.Transform{Value=999};
   Check(!a.TryRollbackFence("retained placement changed")&&a.rollbackStarts==0,"changed retained prop placement refuses rollback without dispatch");
   a.liveGroups[0][0].Data.Transform=a.world.EntityManager.GetComponentData<Game.Objects.Transform>(a.liveGroups[0][0].Original);
   a.ownerValid=true;cacheEntry.EffectEntry=new();Check(!a.TryRollbackFence("effect")&&a.rollbackStarts==0,"accepted effect restore path is excluded from fence rollback");
   CacheCase(out a,out cacheEntry,out cacheLive,out cacheMap);a.networkPending=cacheEntry;a.networkRestoring=true;cacheEntry.BoundRecord=new();
   Check(!a.TryRollbackFence("spawn failure")&&a.rollbackStarts==0,"spawn recovery cannot enter fence after-image fallback or consume a network history slot");
   Console.WriteLine($"PASS: {checks} actual-source bound completion checks; native scheduling remains unproven.");
  }
 }
}


