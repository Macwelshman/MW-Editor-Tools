using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Entities;
using Game.Prefabs;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    // References are witnesses, never resources to clone or reconnect by proximity.
    internal sealed partial class SpawnAccessImage {
        internal bool HasSpawn,HasTakeoff,HasAccess,HasRoute,HasMarker;
        internal Game.Objects.SpawnLocation Spawn;
        internal Game.Routes.TakeoffLocation Takeoff;
        internal Game.Routes.AccessLane Access;
        internal Game.Routes.RouteLane Route;
        internal bool HasRegistry;
        internal int[] RegistrationTypes=Array.Empty<int>();
        internal sealed class Reference {
            internal Reference DetachedCopy()=>(Reference)MemberwiseClone();
            internal Entity Id,Owner,Prefab;
            internal bool HasOwner,HasPrefab,HasLane,HasCurve;
            internal Game.Net.Lane Lane;
            internal Game.Net.Curve Curve;
            internal bool Matches(Reference other,Dictionary<Entity,Entity> map)=>EntityReferenceMap.Resolve(Id,map)==other.Id&&HasOwner==other.HasOwner&&EntityReferenceMap.Resolve(Owner,map)==other.Owner&&HasPrefab==other.HasPrefab&&(Prefab==other.Prefab||HasPrefab&&!HasOwner&&!other.HasOwner&&!HasLane&&!other.HasLane&&!HasCurve&&!other.HasCurve&&EntityReferenceMap.OwnerPrefabMatches(map,Id,other.Id,Prefab,other.Prefab))&&HasLane==other.HasLane&&HasCurve==other.HasCurve&&
                (HasLane&&map!=null&&map.ContainsKey(Id)||(Lane.Equals(other.Lane)&&Curve.Equals(other.Curve)));
        }
        internal Reference[] References=Array.Empty<Reference>();
        internal bool Present=>HasSpawn||HasTakeoff||HasAccess||HasRoute;
        internal Entity[] Targets()=>new[]{HasSpawn?Spawn.m_AccessRestriction:Entity.Null,HasSpawn?Spawn.m_ConnectedLane1:Entity.Null,HasSpawn?Spawn.m_ConnectedLane2:Entity.Null,
            HasTakeoff?Takeoff.m_AccessRestriction:Entity.Null,HasAccess?Access.m_Lane:Entity.Null,HasRoute?Route.m_StartLane:Entity.Null,HasRoute?Route.m_EndLane:Entity.Null}.Where(id=>id!=Entity.Null).Distinct().ToArray();
        internal bool Matches(SpawnAccessImage other,Dictionary<Entity,Entity> map=null){
            if(other==null)return false;
            var spawn=Spawn;
            spawn.m_CurvePosition1=MappedPosition(Spawn.m_ConnectedLane1,Spawn.m_CurvePosition1,map);
            spawn.m_CurvePosition2=MappedPosition(Spawn.m_ConnectedLane2,Spawn.m_CurvePosition2,map);
            spawn.m_AccessRestriction=EntityReferenceMap.Resolve(spawn.m_AccessRestriction,map);spawn.m_ConnectedLane1=EntityReferenceMap.Resolve(spawn.m_ConnectedLane1,map);spawn.m_ConnectedLane2=EntityReferenceMap.Resolve(spawn.m_ConnectedLane2,map);
            var takeoff=Takeoff;takeoff.m_AccessRestriction=EntityReferenceMap.Resolve(takeoff.m_AccessRestriction,map);
            var access=Access;access.m_CurvePos=MappedPosition(Access.m_Lane,Access.m_CurvePos,map);access.m_Lane=EntityReferenceMap.Resolve(access.m_Lane,map);
            var route=Route;route.m_StartCurvePos=MappedPosition(Route.m_StartLane,Route.m_StartCurvePos,map);route.m_EndCurvePos=MappedPosition(Route.m_EndLane,Route.m_EndCurvePos,map);route.m_StartLane=EntityReferenceMap.Resolve(route.m_StartLane,map);route.m_EndLane=EntityReferenceMap.Resolve(route.m_EndLane,map);
            return HasMarker==other.HasMarker&&HasRegistry==other.HasRegistry&&RegistrationTypes.SequenceEqual(other.RegistrationTypes)&&HasSpawn==other.HasSpawn&&HasTakeoff==other.HasTakeoff&&HasAccess==other.HasAccess&&HasRoute==other.HasRoute&&
                (!HasSpawn||SameSpawn(spawn,other.Spawn,map))&&(!HasTakeoff||takeoff.Equals(other.Takeoff))&&(!HasAccess||SameAccess(access,other.Access,map))&&(!HasRoute||SameRoute(route,other.Route,map))&&
                References.Length==other.References.Length&&References.Select((reference,i)=>reference.Matches(other.References[i],map)).All(value=>value);
        }
        private static float MappedPosition(Entity lane,float value,Dictionary<Entity,Entity> map)=>EntityReferenceMap.IsReversed(map,lane)?1f-value:value;
        private static bool PositionEqual(float expected,float actual,bool reversed)=>reversed?Finite(expected)&&Finite(actual)&&expected>=0&&expected<=1&&actual>=0&&actual<=1&&Math.Abs(expected-actual)<=0.000001f:expected.Equals(actual);
        private bool SameSpawn(Game.Objects.SpawnLocation expected,Game.Objects.SpawnLocation actual,Dictionary<Entity,Entity> map){
            if(!PositionEqual(expected.m_CurvePosition1,actual.m_CurvePosition1,EntityReferenceMap.IsReversed(map,Spawn.m_ConnectedLane1))||!PositionEqual(expected.m_CurvePosition2,actual.m_CurvePosition2,EntityReferenceMap.IsReversed(map,Spawn.m_ConnectedLane2)))return false;
            expected.m_CurvePosition1=actual.m_CurvePosition1;expected.m_CurvePosition2=actual.m_CurvePosition2;return expected.Equals(actual);
        }
        private bool SameAccess(Game.Routes.AccessLane expected,Game.Routes.AccessLane actual,Dictionary<Entity,Entity> map){
            if(!PositionEqual(expected.m_CurvePos,actual.m_CurvePos,EntityReferenceMap.IsReversed(map,Access.m_Lane)))return false;
            expected.m_CurvePos=actual.m_CurvePos;return expected.Equals(actual);
        }
        private bool SameRoute(Game.Routes.RouteLane expected,Game.Routes.RouteLane actual,Dictionary<Entity,Entity> map){
            if(!PositionEqual(expected.m_StartCurvePos,actual.m_StartCurvePos,EntityReferenceMap.IsReversed(map,Route.m_StartLane))||!PositionEqual(expected.m_EndCurvePos,actual.m_EndCurvePos,EntityReferenceMap.IsReversed(map,Route.m_EndLane)))return false;
            expected.m_StartCurvePos=actual.m_StartCurvePos;expected.m_EndCurvePos=actual.m_EndCurvePos;return expected.Equals(actual);
        }
        internal SpawnAccessImage AfterAreaDeletion(SpawnAccessImage after,HashSet<Entity> removedLanes,HashSet<Entity> retainedLanes,Dictionary<Entity,Entity> map){
            if(after==null)return null;
            bool Position(float value)=>Finite(value)&&value>=0&&value<=1;
            if(HasSpawn&&(!Position(after.Spawn.m_CurvePosition1)||!Position(after.Spawn.m_CurvePosition2))||HasAccess&&!Position(after.Access.m_CurvePos)||HasRoute&&(!Position(after.Route.m_StartCurvePos)||!Position(after.Route.m_EndCurvePos)))return null;
            var copy=(SpawnAccessImage)MemberwiseClone();var changed=new HashSet<Entity>();
            if(HasSpawn&&(Spawn.m_ConnectedLane1!=after.Spawn.m_ConnectedLane1||Spawn.m_CurvePosition1!=after.Spawn.m_CurvePosition1)){
                if(!removedLanes.Contains(Spawn.m_ConnectedLane1)||(after.Spawn.m_ConnectedLane1!=Entity.Null&&!retainedLanes.Contains(after.Spawn.m_ConnectedLane1)))return null;
                changed.Add(Spawn.m_ConnectedLane1);copy.Spawn.m_ConnectedLane1=after.Spawn.m_ConnectedLane1;copy.Spawn.m_CurvePosition1=after.Spawn.m_CurvePosition1;
            }
            if(HasSpawn&&(Spawn.m_ConnectedLane2!=after.Spawn.m_ConnectedLane2||Spawn.m_CurvePosition2!=after.Spawn.m_CurvePosition2)){
                if(!removedLanes.Contains(Spawn.m_ConnectedLane2)||(after.Spawn.m_ConnectedLane2!=Entity.Null&&!retainedLanes.Contains(after.Spawn.m_ConnectedLane2)))return null;
                changed.Add(Spawn.m_ConnectedLane2);copy.Spawn.m_ConnectedLane2=after.Spawn.m_ConnectedLane2;copy.Spawn.m_CurvePosition2=after.Spawn.m_CurvePosition2;
            }
            bool Connection(Entity oldLane,Entity newLane)=>removedLanes.Contains(oldLane)&&(newLane==Entity.Null||retainedLanes.Contains(newLane));
            if(HasAccess&&(Access.m_Lane!=after.Access.m_Lane||Access.m_CurvePos!=after.Access.m_CurvePos)){
                if(!Connection(Access.m_Lane,after.Access.m_Lane))return null;changed.Add(Access.m_Lane);copy.Access.m_Lane=after.Access.m_Lane;copy.Access.m_CurvePos=after.Access.m_CurvePos;
            }
            if(HasRoute&&(Route.m_StartLane!=after.Route.m_StartLane||Route.m_StartCurvePos!=after.Route.m_StartCurvePos)){
                if(!Connection(Route.m_StartLane,after.Route.m_StartLane))return null;changed.Add(Route.m_StartLane);copy.Route.m_StartLane=after.Route.m_StartLane;copy.Route.m_StartCurvePos=after.Route.m_StartCurvePos;
            }
            if(HasRoute&&(Route.m_EndLane!=after.Route.m_EndLane||Route.m_EndCurvePos!=after.Route.m_EndCurvePos)){
                if(!Connection(Route.m_EndLane,after.Route.m_EndLane))return null;changed.Add(Route.m_EndLane);copy.Route.m_EndLane=after.Route.m_EndLane;copy.Route.m_EndCurvePos=after.Route.m_EndCurvePos;
            }
            if(changed.Count==0)return null;
            var refs=new List<Reference>();
            foreach(var target in copy.Targets()){
                var original=References.SingleOrDefault(r=>EntityReferenceMap.Resolve(r.Id,map)==EntityReferenceMap.Resolve(target,map));
                if(original!=null&&!changed.Contains(original.Id)){refs.Add(original);continue;}
                var replacement=after.References.SingleOrDefault(r=>r.Id==target&&r.HasLane&&retainedLanes.Contains(r.Id));
                if(replacement==null)return null;refs.Add(replacement);
            }
            copy.References=refs.ToArray();return copy.Matches(after,map)?copy:null;
        }
        internal string Difference(SpawnAccessImage other,Dictionary<Entity,Entity> map=null){
            if(other==null)return "missing image";
            var differences=new List<string>();
            foreach(var field in typeof(SpawnAccessImage).GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)){
                if(field.Name=="References"||field.Name=="RegistrationTypes")continue;
                var before=field.GetValue(this);var after=field.GetValue(other);
                if(Equals(before,after))continue;
                if(field.FieldType.IsValueType&&!field.FieldType.IsPrimitive){
                    foreach(var value in field.FieldType.GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic)){
                        var left=value.GetValue(before);var right=value.GetValue(after);
                        if(!Equals(left,right))differences.Add(field.Name+"."+value.Name+"="+left+" -> "+right+(left is Entity id?" mapped="+EntityReferenceMap.Resolve(id,map):""));
                    }
                }else differences.Add(field.Name+"="+before+" -> "+after);
            }
            if(!RegistrationTypes.SequenceEqual(other.RegistrationTypes))differences.Add("registrations=["+string.Join(",",RegistrationTypes)+"] -> ["+string.Join(",",other.RegistrationTypes)+"]");
            for(int i=0;i<Math.Max(References.Length,other.References.Length);i++){
                if(i>=References.Length||i>=other.References.Length){differences.Add("reference count="+References.Length+" -> "+other.References.Length);break;}
                var a=References[i];var b=other.References[i];if(!a.Matches(b,map))differences.Add("reference["+i+"] id="+a.Id+" -> "+b.Id+" mapped="+EntityReferenceMap.Resolve(a.Id,map)+" owner="+a.Owner+" -> "+b.Owner+" prefab="+a.Prefab+" -> "+b.Prefab+" laneEqual="+a.Lane.Equals(b.Lane)+" curveEqual="+a.Curve.Equals(b.Curve));
            }
            return differences.Count==0?"equal":string.Join("; ",differences.Take(30));
        }
        internal static SpawnAccessImage Capture(EntityManager em,Entity id) {
            var image=new SpawnAccessImage{HasSpawn=em.HasComponent<Game.Objects.SpawnLocation>(id),HasTakeoff=em.HasComponent<Game.Routes.TakeoffLocation>(id),HasAccess=em.HasComponent<Game.Routes.AccessLane>(id),HasRoute=em.HasComponent<Game.Routes.RouteLane>(id)};
            image.HasMarker=em.HasComponent<Game.Objects.Marker>(id);
            if(image.HasSpawn){image.Spawn=em.GetComponentData<Game.Objects.SpawnLocation>(id);if(!Finite(image.Spawn.m_CurvePosition1)||!Finite(image.Spawn.m_CurvePosition2))throw new NotSupportedException("Nonfinite spawn connection position.");}
            if(image.HasTakeoff)image.Takeoff=em.GetComponentData<Game.Routes.TakeoffLocation>(id);
            if(image.HasAccess){image.Access=em.GetComponentData<Game.Routes.AccessLane>(id);if(!Finite(image.Access.m_CurvePos))throw new NotSupportedException("Nonfinite access lane position.");}
            if(image.HasRoute){image.Route=em.GetComponentData<Game.Routes.RouteLane>(id);if(!Finite(image.Route.m_StartCurvePos)||!Finite(image.Route.m_EndCurvePos))throw new NotSupportedException("Nonfinite route lane position.");}
            if((image.HasAccess||image.HasRoute)&&!image.HasTakeoff)throw new NotSupportedException("Access/route lane without a supported takeoff companion.");
            if(image.HasSpawn&&em.HasComponent<Game.Common.Owner>(id)){
                var owner=em.GetComponentData<Game.Common.Owner>(id).m_Owner;
                if(!em.Exists(owner))throw new NotSupportedException("Spawn companion owner is missing.");
                image.HasRegistry=em.HasBuffer<Game.Buildings.SpawnLocationElement>(owner);
                if(image.HasRegistry){
                    var registry=em.GetBuffer<Game.Buildings.SpawnLocationElement>(owner,true);
                    if(registry.Length>1024)throw new NotSupportedException("Spawn registration buffer exceeds budget.");
                    image.RegistrationTypes=Enumerable.Range(0,registry.Length).Where(i=>registry[i].m_SpawnLocation==id).Select(i=>(int)registry[i].m_Type).OrderBy(type=>type).ToArray();
                }
            }
            image.References=image.Targets().Select(target=>{
                if(target==id||!em.Exists(target)||em.HasComponent<Game.Common.Deleted>(target)||em.HasComponent<Game.Tools.Temp>(target))throw new NotSupportedException("Spawn/access reference is missing, temporary, deleted or self-referencing.");
                var reference=new Reference{Id=target,HasOwner=em.HasComponent<Game.Common.Owner>(target),HasPrefab=em.HasComponent<PrefabRef>(target),HasLane=em.HasComponent<Game.Net.Lane>(target),HasCurve=em.HasComponent<Game.Net.Curve>(target)};
                if(reference.HasOwner)reference.Owner=em.GetComponentData<Game.Common.Owner>(target).m_Owner;
                if(reference.HasPrefab)reference.Prefab=em.GetComponentData<PrefabRef>(target).m_Prefab;
                if(reference.HasLane)reference.Lane=em.GetComponentData<Game.Net.Lane>(target);
                if(reference.HasCurve)reference.Curve=em.GetComponentData<Game.Net.Curve>(target);
                if((image.HasSpawn&&(target==image.Spawn.m_ConnectedLane1||target==image.Spawn.m_ConnectedLane2)||image.HasAccess&&target==image.Access.m_Lane||image.HasRoute&&(target==image.Route.m_StartLane||target==image.Route.m_EndLane))&&!reference.HasLane)throw new NotSupportedException("Spawn/access reference is not a native lane.");
                return reference;
            }).ToArray();
            return image;
        }
        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
