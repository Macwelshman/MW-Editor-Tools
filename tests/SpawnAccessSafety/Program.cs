#pragma warning disable CS0649
using System;
using System.Collections.Generic;
using MW.EditorTools.Modules.BulldozerRecovery;
namespace Unity.Entities {
    struct Entity {public int Index,Version;public static Entity Null=>default;public static bool operator ==(Entity a,Entity b)=>a.Equals(b);public static bool operator !=(Entity a,Entity b)=>!a.Equals(b);public override bool Equals(object b)=>b is Entity e&&e.Index==Index&&e.Version==Version;public override int GetHashCode()=>HashCode.Combine(Index,Version);}
    sealed class EntityManager {
        internal Dictionary<Entity,Dictionary<Type,object>> Data=new();
        internal Entity Make(int index){var e=new Entity{Index=index,Version=1};Data[e]=new();return e;}
        public bool Exists(Entity e)=>Data.ContainsKey(e);
        public bool HasComponent<T>(Entity e)=>Data[e].ContainsKey(typeof(T));
        public T GetComponentData<T>(Entity e)=>(T)Data[e][typeof(T)];
        public bool HasBuffer<T>(Entity e)=>Data[e].ContainsKey(typeof(Buffer<T>));
        public Buffer<T> GetBuffer<T>(Entity e,bool readOnly)=>(Buffer<T>)Data[e][typeof(Buffer<T>)];
        internal void Put<T>(Entity e,T value)=>Data[e][typeof(T)]=value;
        internal void Remove<T>(Entity e)=>Data[e].Remove(typeof(T));
    }
    sealed class Buffer<T>:List<T> {public int Length=>Count;}
}
namespace Game.Buildings {struct SpawnLocationElement {public Unity.Entities.Entity m_SpawnLocation;public int m_Type;}}
namespace Game.Common {struct Owner {public Unity.Entities.Entity m_Owner;}struct Deleted {}}
namespace Game.Tools {struct Temp {}}
namespace Game.Prefabs {struct PrefabRef {public Unity.Entities.Entity m_Prefab;}}
namespace Game.Net {struct Lane {public int Endpoints;}struct Curve {public float Length;}}
namespace Game.Objects {struct Marker {}struct SpawnLocation {public Unity.Entities.Entity m_AccessRestriction,m_ConnectedLane1,m_ConnectedLane2;public float m_CurvePosition1,m_CurvePosition2;public int m_GroupIndex,m_Flags;}}
namespace Game.Routes {
    struct TakeoffLocation {public Unity.Entities.Entity m_AccessRestriction;public int m_Flags;}
    struct AccessLane {public Unity.Entities.Entity m_Lane;public float m_CurvePos;}
    struct RouteLane {public Unity.Entities.Entity m_StartLane,m_EndLane;public float m_StartCurvePos,m_EndCurvePos;}
}
class Program {
    static int count;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
    static void Refused(Action action,string message){try{action();}catch(NotSupportedException){count++;return;}throw new Exception(message);}
    static void Main(){
        var em=new Unity.Entities.EntityManager();var marker=em.Make(1);var lane=em.Make(2);var owner=em.Make(3);var restriction=em.Make(4);var prefab=em.Make(5);
        em.Put(lane,new Game.Net.Lane{Endpoints=8});em.Put(lane,new Game.Net.Curve{Length=12});em.Put(lane,new Game.Common.Owner{m_Owner=owner});em.Put(lane,new Game.Prefabs.PrefabRef{m_Prefab=prefab});
        var spawn=new Game.Objects.SpawnLocation{m_ConnectedLane1=lane,m_ConnectedLane2=lane,m_AccessRestriction=restriction,m_CurvePosition1=.25f,m_CurvePosition2=.75f,m_GroupIndex=7,m_Flags=3};em.Put(marker,spawn);
        var takeoff=new Game.Routes.TakeoffLocation{m_AccessRestriction=restriction,m_Flags=5};em.Put(marker,takeoff);
        em.Put(marker,new Game.Routes.AccessLane{m_Lane=lane,m_CurvePos=.4f});em.Put(marker,new Game.Routes.RouteLane{m_StartLane=lane,m_EndLane=lane,m_StartCurvePos=.2f,m_EndCurvePos=.8f});
        var before=SpawnAccessImage.Capture(em,marker);
        Check(before.Matches(SpawnAccessImage.Capture(em,marker)),"unchanged companion");Check(before.References.Length==2,"deduplicate targets");Check(before.Spawn.m_GroupIndex==7&&before.Takeoff.m_Flags==5,"full scalar snapshot");
        var regenerated=em.Make(6);em.Put(regenerated,spawn);em.Put(regenerated,takeoff);em.Put(regenerated,em.GetComponentData<Game.Routes.AccessLane>(marker));em.Put(regenerated,em.GetComponentData<Game.Routes.RouteLane>(marker));
        Check(before.Matches(SpawnAccessImage.Capture(em,regenerated)),"fresh native marker preserves exact references");
        var newLane=em.Make(8);em.Put(newLane,new Game.Net.Lane{Endpoints=8});em.Put(newLane,new Game.Net.Curve{Length=12});em.Put(newLane,new Game.Common.Owner{m_Owner=owner});em.Put(newLane,new Game.Prefabs.PrefabRef{m_Prefab=prefab});
        var mappedSpawn=spawn;mappedSpawn.m_ConnectedLane1=newLane;mappedSpawn.m_ConnectedLane2=newLane;em.Put(regenerated,mappedSpawn);
        em.Put(regenerated,new Game.Routes.AccessLane{m_Lane=newLane,m_CurvePos=.4f});em.Put(regenerated,new Game.Routes.RouteLane{m_StartLane=newLane,m_EndLane=newLane,m_StartCurvePos=.2f,m_EndCurvePos=.8f});
        var laneMap=new Dictionary<Unity.Entities.Entity,Unity.Entities.Entity>{{lane,newLane}};
        Check(!before.Matches(SpawnAccessImage.Capture(em,regenerated))&&before.Matches(SpawnAccessImage.Capture(em,regenerated),laneMap),"only validated lane mapping permits changed reference IDs");
        mappedSpawn.m_Flags++;em.Put(regenerated,mappedSpawn);Check(!before.Matches(SpawnAccessImage.Capture(em,regenerated),laneMap),"mapped references do not hide changed spawn flags");Check(before.Difference(SpawnAccessImage.Capture(em,regenerated),laneMap).Contains("Spawn.m_Flags"),"diagnostic identifies changed scalar without relaxing match");mappedSpawn.m_Flags--;em.Put(regenerated,mappedSpawn);
        em.Put(newLane,new Game.Prefabs.PrefabRef{m_Prefab=owner});Check(!before.Matches(SpawnAccessImage.Capture(em,regenerated),laneMap),"mapped reference retains prefab identity check");
        Check(before.Spawn.m_ConnectedLane1==lane,"reference comparison never mutates detached source");
        em.Put(regenerated,spawn);em.Put(regenerated,em.GetComponentData<Game.Routes.AccessLane>(marker));em.Put(regenerated,em.GetComponentData<Game.Routes.RouteLane>(marker));
        em.Put(marker,new Game.Common.Owner{m_Owner=owner});em.Put(regenerated,new Game.Common.Owner{m_Owner=owner});
        var registry=new Unity.Entities.Buffer<Game.Buildings.SpawnLocationElement>{new(){m_SpawnLocation=marker,m_Type=3}};em.Put(owner,registry);
        var registered=SpawnAccessImage.Capture(em,marker);
        Check(!registered.Matches(SpawnAccessImage.Capture(em,regenerated)),"registry must point to regenerated marker");
        registry[0]=new(){m_SpawnLocation=regenerated,m_Type=3};Check(registered.Matches(SpawnAccessImage.Capture(em,regenerated)),"native registry remap preserves registration");
        registry[0]=new(){m_SpawnLocation=regenerated,m_Type=4};Check(!registered.Matches(SpawnAccessImage.Capture(em,regenerated)),"registration type changed");
        em.Remove<Unity.Entities.Buffer<Game.Buildings.SpawnLocationElement>>(owner);
        spawn.m_GroupIndex++;em.Put(marker,spawn);Check(!before.Matches(SpawnAccessImage.Capture(em,marker)),"group changed");spawn.m_GroupIndex--;em.Put(marker,spawn);
        em.Put(lane,new Game.Common.Owner{m_Owner=restriction});Check(!before.Matches(SpawnAccessImage.Capture(em,marker)),"reference ownership changed");em.Put(lane,new Game.Common.Owner{m_Owner=owner});
        em.Put(lane,new Game.Net.Lane{Endpoints=9});Check(!before.Matches(SpawnAccessImage.Capture(em,marker)),"lane endpoints changed");em.Put(lane,new Game.Net.Lane{Endpoints=8});
        em.Put(lane,new Game.Net.Curve{Length=13});Check(!before.Matches(SpawnAccessImage.Capture(em,marker)),"lane geometry changed");em.Put(lane,new Game.Net.Curve{Length=12});
        em.Put(lane,new Game.Prefabs.PrefabRef{m_Prefab=owner});Check(!before.Matches(SpawnAccessImage.Capture(em,marker)),"reference prefab changed");em.Put(lane,new Game.Prefabs.PrefabRef{m_Prefab=prefab});
        em.Put(lane,new Game.Common.Deleted());Refused(()=>SpawnAccessImage.Capture(em,marker),"pending deletion");em.Remove<Game.Common.Deleted>(lane);
        em.Put(lane,new Game.Tools.Temp());Refused(()=>SpawnAccessImage.Capture(em,marker),"temporary target");em.Remove<Game.Tools.Temp>(lane);
        em.Remove<Game.Net.Lane>(lane);Refused(()=>SpawnAccessImage.Capture(em,marker),"wrong lane type");em.Put(lane,new Game.Net.Lane{Endpoints=8});
        spawn.m_ConnectedLane1=new Unity.Entities.Entity{Index=lane.Index,Version=2};em.Put(marker,spawn);Refused(()=>SpawnAccessImage.Capture(em,marker),"stale version");spawn.m_ConnectedLane1=lane;
        spawn.m_CurvePosition1=float.NaN;em.Put(marker,spawn);Refused(()=>SpawnAccessImage.Capture(em,marker),"nonfinite spawn");spawn.m_CurvePosition1=.25f;em.Put(marker,spawn);
        em.Remove<Game.Routes.TakeoffLocation>(marker);Refused(()=>SpawnAccessImage.Capture(em,marker),"orphan route schema");em.Put(marker,takeoff);
        em.Put(marker,new Game.Routes.AccessLane{m_Lane=lane,m_CurvePos=float.PositiveInfinity});Refused(()=>SpawnAccessImage.Capture(em,marker),"nonfinite access");em.Put(marker,new Game.Routes.AccessLane{m_Lane=lane,m_CurvePos=.4f});
        em.Put(marker,new Game.Routes.RouteLane{m_StartLane=lane,m_EndLane=lane,m_EndCurvePos=float.NaN});Refused(()=>SpawnAccessImage.Capture(em,marker),"nonfinite route");
        // Reproduce the logged transition on the same root access-restriction entity.
        var accessMarker=em.Make(20);var oldRootPrefab=em.Make(21);var editableRootPrefab=em.Make(22);
        em.Put(owner,new Game.Prefabs.PrefabRef{m_Prefab=oldRootPrefab});
        em.Put(accessMarker,new Game.Routes.TakeoffLocation{m_AccessRestriction=owner,m_Flags=5});
        var ownerBefore=SpawnAccessImage.Capture(em,accessMarker);
        em.Put(owner,new Game.Prefabs.PrefabRef{m_Prefab=editableRootPrefab});var ownerAfter=SpawnAccessImage.Capture(em,accessMarker);
        var ownerMap=EntityReferenceMap.WithOwnerPrefab(new Dictionary<Unity.Entities.Entity,Unity.Entities.Entity>(),owner,oldRootPrefab,editableRootPrefab);
        Check(!ownerBefore.Matches(ownerAfter)&&ownerBefore.Matches(ownerAfter,ownerMap),"exact verified owner prefab transition accepted only in scope");
        Check(ownerBefore.References[0].Prefab==oldRootPrefab&&ownerAfter.References[0].Prefab==editableRootPrefab&&em.GetComponentData<Game.Routes.TakeoffLocation>(accessMarker).m_AccessRestriction==owner,"transition comparison preserves snapshots and live references");
        Check(!ownerBefore.Matches(ownerAfter,EntityReferenceMap.WithOwnerPrefab(new Dictionary<Unity.Entities.Entity,Unity.Entities.Entity>(),restriction,oldRootPrefab,editableRootPrefab)),"unrelated target does not inherit owner transition");
        Check(!ownerBefore.Matches(ownerAfter,EntityReferenceMap.WithOwnerPrefab(new Dictionary<Unity.Entities.Entity,Unity.Entities.Entity>(),owner,prefab,editableRootPrefab)),"unexpected original prefab refused");
        em.Put(owner,new Game.Prefabs.PrefabRef{m_Prefab=prefab});Check(!ownerBefore.Matches(SpawnAccessImage.Capture(em,accessMarker),ownerMap),"later third prefab refused");em.Put(owner,new Game.Prefabs.PrefabRef{m_Prefab=editableRootPrefab});
        em.Put(owner,new Game.Common.Owner{m_Owner=restriction});Check(!ownerBefore.Matches(SpawnAccessImage.Capture(em,accessMarker),ownerMap),"owner witness schema change refused");em.Remove<Game.Common.Owner>(owner);
        em.Put(owner,new Game.Net.Lane());Check(!ownerBefore.Matches(SpawnAccessImage.Capture(em,accessMarker),ownerMap),"lane cannot use root prefab exception");em.Remove<Game.Net.Lane>(owner);
        em.Put(accessMarker,new Game.Routes.TakeoffLocation{m_AccessRestriction=owner,m_Flags=6});Check(!ownerBefore.Matches(SpawnAccessImage.Capture(em,accessMarker),ownerMap),"transition does not hide changed companion data");
        var deletedLane=em.Make(30);var remainingLane=em.Make(31);
        var connectionBefore=new SpawnAccessImage{HasSpawn=true,Spawn=new Game.Objects.SpawnLocation{m_ConnectedLane1=deletedLane,m_CurvePosition1=0,m_Flags=3},References=new[]{new SpawnAccessImage.Reference{Id=deletedLane,HasLane=true,Owner=em.Make(32)}}};
        var connectionAfter=new SpawnAccessImage{HasSpawn=true,Spawn=new Game.Objects.SpawnLocation{m_ConnectedLane1=remainingLane,m_CurvePosition1=1,m_Flags=3},References=new[]{new SpawnAccessImage.Reference{Id=remainingLane,HasLane=true,Owner=em.Make(33)}}};
        var removedLanes=new HashSet<Unity.Entities.Entity>{deletedLane};var retainedLanes=new HashSet<Unity.Entities.Entity>{remainingLane};
        var adapted=connectionBefore.AfterAreaDeletion(connectionAfter,removedLanes,retainedLanes,null);
        Check(adapted!=null&&adapted.Matches(connectionAfter),"deleted area lane connection adopts independently captured native after-image");
        Check(connectionBefore.Spawn.m_ConnectedLane1==deletedLane&&connectionBefore.Spawn.m_CurvePosition1==0,"area connection before-image remains detached and unchanged");
        var laterLane=em.Make(34);
        var laterConnection=connectionAfter.DetachedCopy();laterConnection.Spawn.m_ConnectedLane1=laterLane;laterConnection.Spawn.m_CurvePosition1=.75f;
        laterConnection.References=new[]{new SpawnAccessImage.Reference{Id=laterLane,HasLane=true,Owner=em.Make(35)}};
        var laterRetained=new HashSet<Unity.Entities.Entity>(retainedLanes){laterLane};
        Check(adapted.AfterAreaDeletion(laterConnection,removedLanes,laterRetained,null)==null,"frozen early after-image cannot justify late retained-to-retained reconnection");
        var lateProjection=connectionBefore.AfterAreaDeletion(laterConnection,removedLanes,laterRetained,null);
        Check(lateProjection!=null&&lateProjection.Matches(laterConnection),"immutable original deleted-lane witness validates later native reconnection");
        Check(connectionBefore.Spawn.m_ConnectedLane1==deletedLane&&adapted.Spawn.m_ConnectedLane1==remainingLane,"late readiness projection changes neither original nor recorded early snapshots");
        Check(connectionBefore.AfterAreaDeletion(laterConnection,removedLanes,retainedLanes,null)==null,"late connection outside verified retained graph is refused");
        laterConnection.Spawn.m_Flags++;
        Check(connectionBefore.AfterAreaDeletion(laterConnection,removedLanes,laterRetained,null)==null,"late readiness projection cannot hide changed marker settings");
        Check(connectionBefore.AfterAreaDeletion(connectionAfter,new HashSet<Unity.Entities.Entity>(),retainedLanes,null)==null,"retained lane connection may not change");
        Check(connectionBefore.AfterAreaDeletion(connectionAfter,removedLanes,new HashSet<Unity.Entities.Entity>(),null)==null,"new connection must be in verified retained area graph");
        connectionAfter.Spawn.m_Flags++;Check(connectionBefore.AfterAreaDeletion(connectionAfter,removedLanes,retainedLanes,null)==null,"area reconnection does not hide changed spawn flags");connectionAfter.Spawn.m_Flags--;
        connectionAfter.Takeoff.m_Flags=9;connectionAfter.HasTakeoff=true;Check(connectionBefore.AfterAreaDeletion(connectionAfter,removedLanes,retainedLanes,null)==null,"area reconnection preserves all other companion shape and data");
        var routeBefore=new SpawnAccessImage{HasAccess=true,HasRoute=true,HasTakeoff=true,Access=new Game.Routes.AccessLane{m_Lane=deletedLane,m_CurvePos=0},Route=new Game.Routes.RouteLane{m_StartLane=deletedLane,m_EndLane=deletedLane,m_StartCurvePos=0,m_EndCurvePos=1},References=new[]{new SpawnAccessImage.Reference{Id=deletedLane,HasLane=true}}};
        var routeAfter=new SpawnAccessImage{HasAccess=true,HasRoute=true,HasTakeoff=true,Access=new Game.Routes.AccessLane{m_Lane=remainingLane,m_CurvePos=1},Route=new Game.Routes.RouteLane{m_StartLane=remainingLane,m_EndLane=remainingLane,m_StartCurvePos=1,m_EndCurvePos=0},References=new[]{new SpawnAccessImage.Reference{Id=remainingLane,HasLane=true}}};
        Check(routeBefore.AfterAreaDeletion(routeAfter,removedLanes,retainedLanes,null)?.Matches(routeAfter)==true,"native access and route after-image reconnects only deleted lanes");
        Check(routeBefore.Access.m_Lane==deletedLane&&routeBefore.Route.m_EndLane==deletedLane,"route reconnection leaves original references intact");
        routeAfter.Takeoff.m_Flags=8;Check(routeBefore.AfterAreaDeletion(routeAfter,removedLanes,retainedLanes,null)==null,"route reconnection keeps takeoff data strict");routeAfter.Takeoff.m_Flags=0;
        routeAfter.Access.m_CurvePos=float.NaN;Check(routeBefore.AfterAreaDeletion(routeAfter,removedLanes,retainedLanes,null)==null,"native connection after-image cannot admit nonfinite positions");
        var empty=em.Make(7);Check(!SpawnAccessImage.Capture(em,empty).Present,"ordinary props unchanged");
        em.Put(empty,new Game.Objects.SpawnLocation());Check(SpawnAccessImage.Capture(em,empty).References.Length==0,"null native links supported");
        em.Put(empty,new Game.Objects.SpawnLocation{m_AccessRestriction=empty});Refused(()=>SpawnAccessImage.Capture(em,empty),"self reference");
        Check(before.Spawn.m_GroupIndex==7&&before.References[1].Id==lane,"detached snapshot untouched by live mutations");
        var reverseMap=new Dictionary<Unity.Entities.Entity,Unity.Entities.Entity>{{deletedLane,remainingLane}};
        reverseMap[connectionBefore.References[0].Owner]=connectionAfter.References[0].Owner;
        EntityReferenceMap.MarkReversed(reverseMap,new[]{deletedLane});
        routeAfter.Access.m_CurvePos=1;
        Check(routeBefore.Matches(routeAfter,reverseMap),"reversed access and route positions preserve same physical attachment");
        Check(!routeBefore.Matches(routeAfter,new Dictionary<Unity.Entities.Entity,Unity.Entities.Entity>(reverseMap)),"plain entity map cannot bypass directed position validation");
        routeAfter.Route.m_EndCurvePos=.2f;Check(!routeBefore.Matches(routeAfter,reverseMap),"reversal refuses moved route attachment");routeAfter.Route.m_EndCurvePos=0;
        routeAfter.Takeoff.m_Flags=1;Check(!routeBefore.Matches(routeAfter,reverseMap),"reversal keeps takeoff flags strict");routeAfter.Takeoff.m_Flags=0;
        connectionAfter.HasTakeoff=false;connectionAfter.Takeoff=default;
        Check(connectionBefore.Matches(connectionAfter,reverseMap),"reversed spawn lane complements connection position");
        connectionAfter.Spawn.m_CurvePosition1=.8f;Check(!connectionBefore.Matches(connectionAfter,reverseMap),"reversal refuses changed spawn attachment");
        var combined=EntityReferenceMap.WithOwnerPrefab(new Dictionary<Unity.Entities.Entity,Unity.Entities.Entity>(reverseMap),owner,oldRootPrefab,editableRootPrefab);
        EntityReferenceMap.CopyDirections(reverseMap,combined);var reverseCopy=EntityReferenceMap.Copy(combined);
        Check(EntityReferenceMap.IsReversed(reverseCopy,deletedLane)&&EntityReferenceMap.OwnerPrefabMatches(reverseCopy,owner,owner,oldRootPrefab,editableRootPrefab),"direction copy preserves owner prefab and reversal scopes together");
        Check(connectionBefore.Spawn.m_CurvePosition1==0&&routeBefore.Route.m_EndCurvePos==1,"direction validation never mutates saved companion images");
        Console.WriteLine($"Spawn/access safety: {count} passed.");
    }
}

