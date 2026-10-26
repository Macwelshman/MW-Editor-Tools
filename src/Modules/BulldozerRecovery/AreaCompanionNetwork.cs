using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed class AreaCompanionNetwork {
        internal sealed class Anchor {
            internal Anchor DetachedCopy()=>(Anchor)MemberwiseClone();
            internal Entity Id,Owner;
            internal PrefabBase Prefab;
            internal Game.Objects.Transform Transform;
            internal bool HasLocal;
            internal Game.Tools.LocalTransformCache Local;
        }
        internal Anchor[] Anchors=Array.Empty<Anchor>();
        internal sealed class Item {
            internal Item DetachedCopy()=>(Item)MemberwiseClone();
            internal Entity Id,Owner;
            internal PrefabBase Prefab,Container;
            internal bool HasNode,HasEdge,HasLane,HasCurve,HasLocal,HasContainer,HasSeed,HasConnection,HasSecondary,HasParking;
            internal Game.Net.ParkingLane Parking;
            internal bool HasGarage,HasCar,HasPedestrian,HasSlave;
            internal Game.Net.GarageLane Garage;
            internal Game.Net.CarLane Car;
            internal Game.Net.PedestrianLane Pedestrian;
            internal Game.Net.SlaveLane Slave;
            internal Game.Net.Node Node;
            internal Game.Net.Edge Edge;
            internal Game.Net.Lane Lane;
            internal Game.Net.ConnectionLane Connection;
            internal Game.Net.Curve Curve;
            internal Game.Tools.LocalCurveCache Local;
            internal Game.Tools.EditorContainer ContainerData;
            internal Game.Common.PseudoRandomSeed Seed;
            internal Entity[] Connections=Array.Empty<Entity>();
            internal Entity[] SubNets=Array.Empty<Entity>();
            internal Game.Net.SubLane[] SubLanes=Array.Empty<Game.Net.SubLane>();
        }
        internal Entity Owner;
        internal Item[] Items=Array.Empty<Item>();
        internal Entity[] RootNets=Array.Empty<Entity>();
        internal Game.Net.SubLane[] RootLanes=Array.Empty<Game.Net.SubLane>();
        internal static bool SourcesValid(IEnumerable<Entity> sources,IEnumerable<Entity> live,Func<Entity,bool> exists) {
            var ids=new HashSet<Entity>(live);return sources.All(id=>!exists(id)||ids.Contains(id));
        }
        internal bool TryMap(AreaCompanionNetwork actual,bool seeds,out Dictionary<Entity,Entity> map,bool compareLocal=true,bool nativeRounding=false,bool rebuiltObjects=false,Game.Objects.Transform? markerOwner=null)=>TryMapCore(markerOwner.HasValue?MarkerHeightWitness(actual,markerOwner.Value,!compareLocal):actual,seeds,out map,out _,compareLocal,nativeRounding,rebuiltObjects);
        // Marker moves can make an existing fence recompute its interior heights.
        // Prove both world images against an identical finite local curve and the
        // guarded owner pose. Only comparison copies change; snapshots/ECS do not.
        internal AreaCompanionNetwork MarkerHeightWitness(AreaCompanionNetwork actual,Game.Objects.Transform owner,bool allowInvalidLocal=false){
            if(actual==null||Owner!=actual.Owner||!math.all(math.isfinite(owner.m_Position))||!math.all(math.isfinite(owner.m_Rotation.value))||math.abs(math.lengthsq(owner.m_Rotation.value)-1)>1e-5f)return actual;
            bool SameSource(Item old,Item live)=>LocalSnapshotEqual(old.Local,live.Local)||
                (allowInvalidLocal&&old.HasCurve&&live.HasCurve&&Finite(old.Local.m_Curve)&&!Finite(live.Local.m_Curve)&&
                 Near(old.Curve.m_Bezier.a,live.Curve.m_Bezier.a,true)&&Near(old.Curve.m_Bezier.d,live.Curve.m_Bezier.d,true));
            var items=actual.Items.Select(live=>{
                var candidates=Items.Where(i=>i.Id==live.Id).ToArray();
                if(candidates.Length==0)candidates=Items.Where(i=>i.HasEdge&&!i.HasLane&&i.HasLocal&&live.HasLocal&&ReferenceEquals(i.Prefab,live.Prefab)&&SameSource(i,live)).ToArray();
                if(candidates.Length!=1)return live;var old=candidates[0];
                if(old.Owner!=Owner||live.Owner!=Owner||!old.HasEdge||old.HasLane||!old.HasCurve||!old.HasLocal||!live.HasLocal||!Finite(old.Local.m_Curve)||!SameSource(old,live))return live;
                var local=old.Local.m_Curve;var world=old.Curve.m_Bezier;var next=live.Curve.m_Bezier;
                float3 Project(float3 p)=>math.mul(owner.m_Rotation,p)+owner.m_Position;
                bool Height(float3 p,float3 q,float3 saved){
                    var projected=Project(saved);if(!math.all(math.isfinite(p))||!math.all(math.isfinite(q))||!math.all(math.isfinite(projected)))return false;
                    // ListObjectSubNets IL_0291..02ea applies CheckCachedValue
                    // independently to all four points. Its exact predicate is
                    // local-space distance < .01 (IL_0007..0015). Both images
                    // must serialize to the identical captured local point.
                    var inverse=math.inverse(owner.m_Rotation);
                    return Near(new float3(p.x,0,p.z),new float3(projected.x,0,projected.z),true)&&Near(new float3(q.x,0,q.z),new float3(projected.x,0,projected.z),true)&&
                        math.distance(math.mul(inverse,p-owner.m_Position),saved)<.01f&&math.distance(math.mul(inverse,q-owner.m_Position),saved)<.01f;
                }
                if(!Near(world.a,Project(local.a),true)||!Near(next.a,Project(local.a),true)||!Near(world.d,Project(local.d),true)||!Near(next.d,Project(local.d),true)||!Height(world.b,next.b,local.b)||!Height(world.c,next.c,local.c))return live;
                var copy=live.DetachedCopy();next.b.y=world.b.y;next.c.y=world.c.y;copy.Curve.m_Bezier=next;return copy;
            }).ToArray();
            // A source-free fence centre lane has no local cache of its own.
            // Establish its geometry independently from its exact owning edge in
            // BOTH raw images, then use that edge's already proved source witness.
            var edges=items;
            items=actual.Items.Select(live=>{
                if(!live.HasLane||!live.HasCurve||live.HasLocal||live.HasEdge||live.HasNode||live.HasConnection||live.HasSecondary||live.HasContainer)return edges.Single(i=>i.Id==live.Id);
                var parents=actual.Items.Where(i=>i.Id==live.Owner&&i.HasEdge&&!i.HasLane&&i.HasLocal&&i.HasCurve).ToArray();if(parents.Length!=1)return live;var parent=parents[0];
                var originals=Items.Where(i=>i.HasEdge&&!i.HasLane&&i.HasLocal&&ReferenceEquals(i.Prefab,parent.Prefab)&&SameSource(i,parent)).ToArray();
                var survivor=originals.Where(i=>i.Id==parent.Id).ToArray();if(survivor.Length==1)originals=survivor;
                if(originals.Length!=1)return live;var oldParent=originals[0];var parentProof=edges.Single(i=>i.Id==parent.Id);
                if(!CanRestoreLocal(oldParent.Local,parent.Local)||!SameData(oldParent,parentProof,false,false,false))return live;
                var lanes=Items.Where(i=>i.Owner==oldParent.Id&&i.HasLane&&ReferenceEquals(i.Prefab,live.Prefab)&&i.Lane.m_StartNode.GetOwnerIndex()==oldParent.Id.Index&&i.Lane.m_MiddleNode.GetOwnerIndex()==oldParent.Id.Index&&i.Lane.m_EndNode.GetOwnerIndex()==oldParent.Id.Index).ToArray();
                if(lanes.Length!=1)return live;var old=lanes[0];
                if(!ReferenceEquals(old.Prefab,oldParent.Prefab)||!ReferenceEquals(live.Prefab,parent.Prefab)||old.HasLocal||old.HasEdge||old.HasNode||old.HasConnection||old.HasSecondary||old.HasContainer||!old.HasCurve||
                    oldParent.SubLanes.Count(l=>l.m_SubLane==old.Id)!=1||parent.SubLanes.Count(l=>l.m_SubLane==live.Id)!=1||
                    oldParent.SubLanes.Single(l=>l.m_SubLane==old.Id).m_PathMethods!=parent.SubLanes.Single(l=>l.m_SubLane==live.Id).m_PathMethods||
                    live.Lane.m_StartNode.GetOwnerIndex()!=parent.Id.Index||live.Lane.m_MiddleNode.GetOwnerIndex()!=parent.Id.Index||live.Lane.m_EndNode.GetOwnerIndex()!=parent.Id.Index||
                    !math.isfinite(old.Curve.m_Length)||!math.isfinite(live.Curve.m_Length)||math.abs(old.Curve.m_Length-oldParent.Curve.m_Length)>=1e-4f||math.abs(live.Curve.m_Length-parent.Curve.m_Length)>=1e-4f||
                    !SameCurve(old.Curve.m_Bezier,oldParent.Curve.m_Bezier,true,8)||!SameCurve(live.Curve.m_Bezier,parent.Curve.m_Bezier,true,8)||
                    !Near(old.Curve.m_Bezier.a,live.Curve.m_Bezier.a)||!Near(old.Curve.m_Bezier.d,live.Curve.m_Bezier.d))return live;
                var copy=live.DetachedCopy();var curve=copy.Curve.m_Bezier;curve.b.y=old.Curve.m_Bezier.b.y;curve.c.y=old.Curve.m_Bezier.c.y;copy.Curve.m_Bezier=curve;return copy;
            }).ToArray();
            return new AreaCompanionNetwork{Owner=actual.Owner,Items=items,Anchors=actual.Anchors,RootNets=actual.RootNets,RootLanes=actual.RootLanes};
        }
        private bool TryMapCore(AreaCompanionNetwork actual,bool seeds,out Dictionary<Entity,Entity> map,out string refusal,bool compareLocal,bool nativeRounding,bool rebuiltObjects,bool diagnostics=false) {
            refusal=null;
            if(actual==null){map=null;return false;}
            map=new Dictionary<Entity,Entity>{{Owner,actual.Owner}};
            if(Items.Length!=actual.Items.Length||!MapAnchors(actual,map,nativeRounding,rebuiltObjects))return false;
            var used=new HashSet<Entity>();
            var reversed=new HashSet<Entity>();var anchorMap=map;
            bool Reverse(Item a,Item b)=>rebuiltObjects&&nativeRounding&&ReversedConnection(a,b,actual,anchorMap,seeds,compareLocal);
            bool Generated(Item a)=>rebuiltObjects&&nativeRounding&&GeneratedConnection(a)&&a.Owner==Owner&&!RootNets.Contains(a.Id);
            bool Matches(Item a,Item b)=>Generated(a)?ConnectionMatch(a,b,actual,anchorMap,seeds,compareLocal,false)||Reverse(a,b):SameData(a,b,seeds,compareLocal,nativeRounding);
            // A surviving full Entity identity is stronger than a geometric
            // match. Reserve survivors first so coincident peers cannot steal
            // their matches. All typed data and topology are still checked.
            foreach(var expected in Items) {
                var survivors=actual.Items.Where(value=>value.Id==expected.Id).ToArray();
                if(survivors.Length==0)continue;
                if(survivors.Length!=1){if(diagnostics)refusal="typed mapping duplicate survivor expected "+Describe(expected);return false;}
                // Generated spawn connections are keyed by their endpoint pair, not
                // their current allocated entity. Native rebuilding can reuse an ID
                // for a different pair. Match every such row uniquely below instead.
                if(Generated(expected))continue;
                if(!Matches(expected,survivors[0])||!used.Add(expected.Id)||map.ContainsKey(expected.Id)){
                    if(diagnostics)refusal="typed mapping survivor reservation differences="+DataDifferences(expected,survivors[0],seeds,compareLocal,nativeRounding)+" used="+used.Contains(expected.Id)+" mapped="+map.ContainsKey(expected.Id)+" expected "+Describe(expected)+"; live "+Describe(survivors[0])+"; anchors="+DescribeMap(anchorMap);return false;
                }
                map.Add(expected.Id,expected.Id);
            }
            foreach(var expected in Items) {
                if(map.ContainsKey(expected.Id))continue;
                var candidates=actual.Items.Where(value=>!used.Contains(value.Id)&&Matches(expected,value)).ToArray();
                if(candidates.Length!=1){if(diagnostics)refusal="typed mapping candidate count="+candidates.Length+" expected "+Describe(expected)+"; anchors="+DescribeMap(anchorMap)+"; candidates "+string.Join(" | ",actual.Items.Where(i=>ReferenceEquals(i.Prefab,expected.Prefab)).Take(8).Select(Describe));return false;}
                used.Add(candidates[0].Id);map.Add(expected.Id,candidates[0].Id);
            }
            var mapping=map;
            foreach(var expected in Items){var value=actual.Items.Single(i=>i.Id==mapping[expected.Id]);
                if(!SameData(expected,value,seeds,compareLocal,nativeRounding)&&Reverse(expected,value))reversed.Add(expected.Id);}
            if(!SameLinks(RootNets,actual.RootNets,mapping)||!SameLaneLinks(RootLanes,actual.RootLanes,mapping))return false;
            foreach(var expected in Items) {
                var value=actual.Items.Single(i=>i.Id==mapping[expected.Id]);
                if(!mapping.TryGetValue(expected.Owner,out var owner)||owner!=value.Owner)return false;
                if(expected.HasEdge&&(!mapping.TryGetValue(expected.Edge.m_Start,out var start)||start!=value.Edge.m_Start||
                    !mapping.TryGetValue(expected.Edge.m_End,out var end)||end!=value.Edge.m_End))return false;
                if(expected.HasLane&&!Generated(expected)&&!reversed.Contains(expected.Id)&&(!SamePath(expected.Lane.m_StartNode,value.Lane.m_StartNode,mapping)||
                    !SamePath(expected.Lane.m_MiddleNode,value.Lane.m_MiddleNode,mapping)||!SamePath(expected.Lane.m_EndNode,value.Lane.m_EndNode,mapping)))return false;
                if(expected.HasConnection&&(!mapping.TryGetValue(expected.Connection.m_AccessRestriction,out var restriction)?expected.Connection.m_AccessRestriction!=value.Connection.m_AccessRestriction:restriction!=value.Connection.m_AccessRestriction))return false;
                if(expected.HasParking&&((mapping.TryGetValue(expected.Parking.m_AccessRestriction,out var parkingRestriction)?parkingRestriction:expected.Parking.m_AccessRestriction)!=value.Parking.m_AccessRestriction||!SamePath(expected.Parking.m_AdditionalStartNode,value.Parking.m_AdditionalStartNode,mapping)))return false;
                if(expected.HasCar&&(mapping.TryGetValue(expected.Car.m_AccessRestriction,out var carRestriction)?carRestriction:expected.Car.m_AccessRestriction)!=value.Car.m_AccessRestriction)return false;
                if(expected.HasPedestrian&&(mapping.TryGetValue(expected.Pedestrian.m_AccessRestriction,out var pedestrianRestriction)?pedestrianRestriction:expected.Pedestrian.m_AccessRestriction)!=value.Pedestrian.m_AccessRestriction)return false;
                if(!SameLinks(expected.Connections,value.Connections,mapping)||!SameLinks(expected.SubNets,value.SubNets,mapping)||
                    !SameLaneLinks(expected.SubLanes,value.SubLanes,mapping))return false;
            }
            EntityReferenceMap.MarkReversed(map,reversed);
            return true;
        }
        // RoadConnectionSystem.CheckConnection builds endpoint nodes at index 0,
        // middle at index 3 on an endpoint, and may retain either curve direction.
        // Only generated, source-free object-to-object secondary connections qualify.
        private bool ReversedConnection(Item a,Item b,AreaCompanionNetwork actual,Dictionary<Entity,Entity> map,bool seeds,bool compareLocal)=>ConnectionMatch(a,b,actual,map,seeds,compareLocal,true);
        internal static bool GeneratedConnection(Item a)=>a.HasLane&&a.HasCurve&&a.HasConnection&&a.HasSecondary&&!a.HasNode&&!a.HasEdge&&!a.HasLocal&&!a.HasContainer&&!a.HasSeed&&
            (a.Connection.m_Flags&Game.Net.ConnectionLaneFlags.Inside)!=0&&(a.Connection.m_Flags&Game.Net.ConnectionLaneFlags.Outside)==0&&CanonicalConnection(a.Lane);
        private bool ConnectionMatch(Item a,Item b,AreaCompanionNetwork actual,Dictionary<Entity,Entity> map,bool seeds,bool compareLocal,bool reverse){
            if(!GeneratedConnection(a)||!GeneratedConnection(b)||a.Owner!=Owner||b.Owner!=actual.Owner||RootNets.Contains(a.Id)||actual.RootNets.Contains(b.Id))return false;
            var start=Anchors.Where(x=>x.Id.Index==a.Lane.m_StartNode.GetOwnerIndex()).ToArray();
            var end=Anchors.Where(x=>x.Id.Index==a.Lane.m_EndNode.GetOwnerIndex()).ToArray();
            if(start.Length!=1||end.Length!=1||start[0].Id==end[0].Id||
                !SamePath(a.Lane.m_StartNode,reverse?b.Lane.m_EndNode:b.Lane.m_StartNode,map)||!SamePath(a.Lane.m_EndNode,reverse?b.Lane.m_StartNode:b.Lane.m_EndNode,map))return false;
            var normalized=b.DetachedCopy();if(reverse){var c=b.Curve.m_Bezier;
                normalized.Curve.m_Bezier=new Colossal.Mathematics.Bezier4x3{a=c.d,b=c.c,c=c.b,d=c.a};}
            return SameData(a,normalized,seeds,compareLocal,true);
        }
        private static string DescribeMap(Dictionary<Entity,Entity> map)=>string.Join(", ",map.Take(16).Select(p=>p.Key+" -> "+p.Value));
        private static bool CanonicalConnection(Game.Net.Lane lane){
            var start=lane.m_StartNode.GetOwnerIndex();var end=lane.m_EndNode.GetOwnerIndex();var middle=lane.m_MiddleNode.GetOwnerIndex();
            return start!=0&&end!=0&&start!=end&&(middle==start||middle==end)&&
                lane.m_StartNode.Equals(new Game.Pathfind.PathNode(new Entity{Index=start},(ushort)0))&&
                lane.m_EndNode.Equals(new Game.Pathfind.PathNode(new Entity{Index=end},(ushort)0))&&
                lane.m_MiddleNode.Equals(new Game.Pathfind.PathNode(new Entity{Index=middle},(ushort)3));
        }
        private bool MapAnchors(AreaCompanionNetwork actual,Dictionary<Entity,Entity> map,bool rounding,bool rebuiltObjects=false) {
            if(Anchors.Length!=actual.Anchors.Length)return false;
            var used=new HashSet<Entity>();
            foreach(var anchor in Anchors){
                var survivors=actual.Anchors.Where(a=>a.Id==anchor.Id).ToArray();
                if(survivors.Length==0)continue;
                if(survivors.Length!=1)return false;
                // Native object-list rebuilding can reuse a surviving object at a
                // different source row. Only the spawn restoration caller permits
                // remapping that case; unique complete pose/local/owner proof still applies.
                if(!SameAnchor(anchor,survivors[0],rounding)){if(rebuiltObjects)continue;return false;}
                if(map.ContainsKey(anchor.Id)||!used.Add(anchor.Id))return false;
                map.Add(anchor.Id,anchor.Id);
            }
            foreach(var anchor in Anchors.Where(a=>!map.ContainsKey(a.Id))){
                var matches=actual.Anchors.Where(a=>!used.Contains(a.Id)&&SameAnchor(anchor,a,rounding)).ToArray();
                if(matches.Length!=1)return false;used.Add(matches[0].Id);map.Add(anchor.Id,matches[0].Id);
            }
            foreach(var anchor in Anchors){var value=actual.Anchors.Single(a=>a.Id==map[anchor.Id]);
                if(!map.TryGetValue(anchor.Owner,out var owner)||owner!=value.Owner)return false;}
            return true;
        }
        private static bool SameAnchor(Anchor a,Anchor b,bool rounding)=>!ReferenceEquals(a.Prefab,null)&&ReferenceEquals(a.Prefab,b.Prefab)&&a.HasLocal==b.HasLocal&&
            Near(a.Transform.m_Position,b.Transform.m_Position,rounding)&&math.all(math.isfinite(a.Transform.m_Rotation.value))&&math.all(math.isfinite(b.Transform.m_Rotation.value))&&
            math.min(math.lengthsq(a.Transform.m_Rotation.value-b.Transform.m_Rotation.value),math.lengthsq(a.Transform.m_Rotation.value+b.Transform.m_Rotation.value))<1e-8f&&
            (!a.HasLocal||(math.all(math.isfinite(a.Local.m_Position))&&math.all(math.isfinite(a.Local.m_Rotation.value))&&a.Local.Equals(b.Local)));
        private static bool SameLinks(Entity[] expected,Entity[] actual,Dictionary<Entity,Entity> map)=>expected.Length==actual.Length&&
            expected.Distinct().Count()==expected.Length&&actual.Distinct().Count()==actual.Length&&expected.All(map.ContainsKey)&&
            expected.Select(id=>map[id]).OrderBy(id=>id.Index).SequenceEqual(actual.OrderBy(id=>id.Index));
        private static bool SameLaneLinks(Game.Net.SubLane[] expected,Game.Net.SubLane[] actual,Dictionary<Entity,Entity> map)=>
            SameLinks(expected.Select(l=>l.m_SubLane).ToArray(),actual.Select(l=>l.m_SubLane).ToArray(),map)&&
            expected.All(l=>actual.Single(a=>a.m_SubLane==map[l.m_SubLane]).m_PathMethods==l.m_PathMethods);
        private static bool SamePath(Game.Pathfind.PathNode expected,Game.Pathfind.PathNode actual,Dictionary<Entity,Entity> map) {
            int oldIndex=expected.GetOwnerIndex();
            // Empty native path nodes have no owner; retain all their bits.
            if(oldIndex==0)return expected.Equals(actual);
            var keys=map.Keys.Where(id=>id.Index==oldIndex).ToArray();
            if(keys.Length!=1||map[keys[0]].Index!=actual.GetOwnerIndex())return false;
            expected.SetOwner(Entity.Null);actual.SetOwner(Entity.Null);
            return expected.Equals(actual);
        }
        internal static bool SameTypedData(Item a,Item b,bool seeds,bool compareLocal)=>SameData(a,b,seeds,compareLocal,false);
        // Diagnostics only: keep these labels aligned with SameData. No recovery
        // decision uses this report; full typed data and mapped links remain required.
        internal static string DataDifferences(Item a,Item b,bool seeds,bool compareLocal,bool nativeRounding){
            var differences=new List<string>();
            void Check(string name,bool equal){if(!equal)differences.Add(name);}
            Check("prefab",ReferenceEquals(a.Prefab,b.Prefab));Check("container",ReferenceEquals(a.Container,b.Container));
            Check("schema",a.HasNode==b.HasNode&&a.HasEdge==b.HasEdge&&a.HasLane==b.HasLane&&a.HasCurve==b.HasCurve&&a.HasLocal==b.HasLocal&&a.HasContainer==b.HasContainer&&a.HasSeed==b.HasSeed&&a.HasConnection==b.HasConnection&&a.HasSecondary==b.HasSecondary&&a.HasParking==b.HasParking&&a.HasGarage==b.HasGarage&&a.HasCar==b.HasCar&&a.HasPedestrian==b.HasPedestrian&&a.HasSlave==b.HasSlave);
            Check("parking",!a.HasParking||SameParking(a.Parking,b.Parking));Check("garage",!a.HasGarage||a.Garage.Equals(b.Garage));
            Check("car",!a.HasCar||SameCar(a.Car,b.Car));Check("pedestrian",!a.HasPedestrian||SamePedestrian(a.Pedestrian,b.Pedestrian));Check("slave",!a.HasSlave||a.Slave.Equals(b.Slave));
            Check("connection",!a.HasConnection||(a.Connection.m_Flags==b.Connection.m_Flags&&a.Connection.m_RoadTypes==b.Connection.m_RoadTypes&&a.Connection.m_TrackTypes==b.Connection.m_TrackTypes));
            Check("node",!a.HasNode||(Near(a.Node.m_Position,b.Node.m_Position,nativeRounding)&&math.min(math.lengthsq(a.Node.m_Rotation.value-b.Node.m_Rotation.value),math.lengthsq(a.Node.m_Rotation.value+b.Node.m_Rotation.value))<1e-8f));
            Check("curve",!a.HasCurve||SameWorldCurve(a,b,nativeRounding));Check("length",!a.HasCurve||(math.isfinite(a.Curve.m_Length)&&math.isfinite(b.Curve.m_Length)&&math.abs(a.Curve.m_Length-b.Curve.m_Length)<1e-4f));
            Check("local",!a.HasLocal||!compareLocal||SameCurve(a.Local.m_Curve,b.Local.m_Curve));
            Check("containerData",!a.HasContainer||(a.ContainerData.m_Scale.Equals(b.ContainerData.m_Scale)&&a.ContainerData.m_Intensity.Equals(b.ContainerData.m_Intensity)&&a.ContainerData.m_GroupIndex==b.ContainerData.m_GroupIndex));
            Check("seed",!seeds||!a.HasSeed||a.Seed.Equals(b.Seed));
            return differences.Count==0?"none":string.Join(",",differences);
        }
        private static bool SameParking(Game.Net.ParkingLane a,Game.Net.ParkingLane b){a.m_AccessRestriction=b.m_AccessRestriction;a.m_AdditionalStartNode=b.m_AdditionalStartNode;return a.Equals(b);}
        private static bool SameCar(Game.Net.CarLane a,Game.Net.CarLane b){a.m_AccessRestriction=b.m_AccessRestriction;return a.Equals(b);}
        private static bool SamePedestrian(Game.Net.PedestrianLane a,Game.Net.PedestrianLane b){a.m_AccessRestriction=b.m_AccessRestriction;return a.Equals(b);}
        private static bool SameData(Item a,Item b,bool seeds,bool compareLocal,bool nativeRounding)=>ReferenceEquals(a.Prefab,b.Prefab)&&ReferenceEquals(a.Container,b.Container)&&
            a.HasNode==b.HasNode&&a.HasEdge==b.HasEdge&&a.HasLane==b.HasLane&&a.HasCurve==b.HasCurve&&a.HasLocal==b.HasLocal&&
            a.HasContainer==b.HasContainer&&a.HasSeed==b.HasSeed&&a.HasConnection==b.HasConnection&&a.HasSecondary==b.HasSecondary&&a.HasParking==b.HasParking&&(!a.HasParking||SameParking(a.Parking,b.Parking))&&
            a.HasGarage==b.HasGarage&&(!a.HasGarage||a.Garage.Equals(b.Garage))&&a.HasCar==b.HasCar&&(!a.HasCar||SameCar(a.Car,b.Car))&&a.HasPedestrian==b.HasPedestrian&&(!a.HasPedestrian||SamePedestrian(a.Pedestrian,b.Pedestrian))&&a.HasSlave==b.HasSlave&&(!a.HasSlave||a.Slave.Equals(b.Slave))&&
            (!a.HasConnection||(a.Connection.m_Flags==b.Connection.m_Flags&&a.Connection.m_RoadTypes==b.Connection.m_RoadTypes&&a.Connection.m_TrackTypes==b.Connection.m_TrackTypes))&&(!a.HasNode||(Near(a.Node.m_Position,b.Node.m_Position,nativeRounding)&&
                math.min(math.lengthsq(a.Node.m_Rotation.value-b.Node.m_Rotation.value),math.lengthsq(a.Node.m_Rotation.value+b.Node.m_Rotation.value))<1e-8f))&&
            (!a.HasCurve||(SameWorldCurve(a,b,nativeRounding)&&math.isfinite(a.Curve.m_Length)&&math.isfinite(b.Curve.m_Length)&&math.abs(a.Curve.m_Length-b.Curve.m_Length)<1e-4f))&&(!a.HasLocal||!compareLocal||SameCurve(a.Local.m_Curve,b.Local.m_Curve))&&
            (!a.HasContainer||(a.ContainerData.m_Scale.Equals(b.ContainerData.m_Scale)&&a.ContainerData.m_Intensity.Equals(b.ContainerData.m_Intensity)&&a.ContainerData.m_GroupIndex==b.ContainerData.m_GroupIndex))&&
            (!seeds||!a.HasSeed||a.Seed.Equals(b.Seed));
        private static bool SameWorldCurve(Item a,Item b,bool nativeRounding) {
            var before=a.Curve.m_Bezier;var after=b.Curve.m_Bezier;
            if(SameCurve(before,after,nativeRounding,a.HasLane?8:4))return true;
            // Native fence regeneration can round the two interior control heights
            // differently. Only fresh edges with a finite recorded local curve are
            // eligible; endpoints, horizontal geometry and the full graph stay guarded.
            // This compares derived geometry only: it never writes a world curve.
            if(!nativeRounding||!a.HasEdge||a.HasLane||a.Id==b.Id||!a.HasLocal||
                !CanRestoreLocal(a.Local,b.Local))return false;
            return Near(before.a,after.a,true)&&Near(before.d,after.d,true)&&
                InteriorHeight(before.b,after.b)&&InteriorHeight(before.c,after.c);
        }
        private static bool InteriorHeight(float3 a,float3 b)=>
            Near(new float3(a.x,0,a.z),new float3(b.x,0,b.z),true)&&
            Near(new float3(0,a.y,0),new float3(0,b.y,0),true,8);
        private static bool Near(float3 a,float3 b,bool nativeRounding=false,int precisionSteps=4) {
            if(!math.all(math.isfinite(a))||!math.all(math.isfinite(b)))return false;
            if(!nativeRounding)return math.lengthsq(a-b)<1e-8f;
            // Four float precision steps for owner/world transform round trips,
            // bounded to half a millimetre. Source and local-cache checks stay strict.
            var tolerance=math.min(new float3(0.0005f),math.max(new float3(0.0001f),math.max(math.abs(a),math.abs(b))*(precisionSteps*1.192092896e-7f)));
            return math.all(math.abs(a-b)<tolerance);
        }
        private static bool SameCurve(Colossal.Mathematics.Bezier4x3 a,Colossal.Mathematics.Bezier4x3 b,bool nativeRounding=false,int precisionSteps=4)=>Near(a.a,b.a,nativeRounding,precisionSteps)&&Near(a.b,b.b,nativeRounding,precisionSteps)&&Near(a.c,b.c,nativeRounding,precisionSteps)&&Near(a.d,b.d,nativeRounding,precisionSteps);
        internal static bool LocalSnapshotEqual(Game.Tools.LocalCurveCache a,Game.Tools.LocalCurveCache b)=>
            math.all(math.asuint(a.m_Curve.a)==math.asuint(b.m_Curve.a))&&math.all(math.asuint(a.m_Curve.b)==math.asuint(b.m_Curve.b))&&math.all(math.asuint(a.m_Curve.c)==math.asuint(b.m_Curve.c))&&math.all(math.asuint(a.m_Curve.d)==math.asuint(b.m_Curve.d));
        internal static bool CanRestoreLocal(Game.Tools.LocalCurveCache expected,Game.Tools.LocalCurveCache actual)=>Finite(expected.m_Curve)&&(!Finite(actual.m_Curve)||SameCurve(expected.m_Curve,actual.m_Curve));
        internal string ExplainMismatch(AreaCompanionNetwork actual,bool seeds,bool compareLocal=true,bool nativeRounding=false,bool rebuiltObjects=false,Game.Objects.Transform? markerOwner=null) {
            if(markerOwner.HasValue)actual=MarkerHeightWitness(actual,markerOwner.Value,!compareLocal);
            if(actual==null)return "missing graph";
            if(Items.Length!=actual.Items.Length)return "item count expected="+Items.Length+" actual="+actual.Items.Length;
            var anchors=new Dictionary<Entity,Entity>{{Owner,actual.Owner}};if(!MapAnchors(actual,anchors,nativeRounding,rebuiltObjects))return "owned object path anchors differ or are ambiguous; expected="+Anchors.Length+" actual="+actual.Anchors.Length+"; before="+string.Join(" | ",Anchors.Take(8).Select(DescribeAnchor))+"; live="+string.Join(" | ",actual.Anchors.Take(8).Select(DescribeAnchor));
            if(TryMapCore(actual,seeds,out var map,out var refusal,compareLocal,nativeRounding,rebuiltObjects,true))return "none";
            if(refusal!=null)return refusal;
            foreach(var expected in Items)if(!map.ContainsKey(expected.Id))return "typed mapping expected "+Describe(expected)+"; candidates "+string.Join(" | ",actual.Items.Where(a=>ReferenceEquals(a.Prefab,expected.Prefab)&&a.HasNode==expected.HasNode&&a.HasEdge==expected.HasEdge&&a.HasLane==expected.HasLane).Take(4).Select(Describe));
            if(!SameLinks(RootNets,actual.RootNets,map))return "root network membership";
            if(!SameLaneLinks(RootLanes,actual.RootLanes,map))return "root lane membership/path methods";
            foreach(var expected in Items){var value=actual.Items.Single(i=>i.Id==map[expected.Id]);
                if(!map.TryGetValue(expected.Owner,out var owner)||owner!=value.Owner)return "owner expected "+Describe(expected)+" actual "+Describe(value);
                if(expected.HasEdge&&(!map.TryGetValue(expected.Edge.m_Start,out var start)||start!=value.Edge.m_Start||!map.TryGetValue(expected.Edge.m_End,out var end)||end!=value.Edge.m_End))return "edge endpoints expected "+Describe(expected)+" actual "+Describe(value);
                if(expected.HasLane&&(!SamePath(expected.Lane.m_StartNode,value.Lane.m_StartNode,map)||!SamePath(expected.Lane.m_MiddleNode,value.Lane.m_MiddleNode,map)||!SamePath(expected.Lane.m_EndNode,value.Lane.m_EndNode,map)))return "lane path nodes expected "+Describe(expected)+" actual "+Describe(value);
                if(!SameLinks(expected.Connections,value.Connections,map)||!SameLinks(expected.SubNets,value.SubNets,map)||!SameLaneLinks(expected.SubLanes,value.SubLanes,map))return "connections/subnets/sublanes expected "+Describe(expected)+" actual "+Describe(value);
            }
            return "graph differs";
        }
        private static string PrefabName(PrefabBase prefab){if(ReferenceEquals(prefab,null))return "null";try{return prefab.name;}catch{return prefab.GetType().FullName;}}
        private static string DescribeAnchor(Anchor a)=>"id="+a.Id+" owner="+a.Owner+" prefab="+PrefabName(a.Prefab)+" world="+a.Transform.m_Position+" rotation="+a.Transform.m_Rotation.value+" local="+a.HasLocal+":"+a.Local.m_Position+"/"+a.Local.m_Rotation.value+" mesh="+a.Local.m_ParentMesh+" probability="+a.Local.m_Probability+" group="+a.Local.m_GroupIndex;
        internal static string Describe(Item i)=>"id="+i.Id+" owner="+i.Owner+" prefab="+PrefabName(i.Prefab)+
            " container="+i.HasContainer+":"+PrefabName(i.Container)+"/"+i.ContainerData.m_Scale+"/"+i.ContainerData.m_Intensity+"/"+i.ContainerData.m_GroupIndex+
            " node="+i.HasNode+":"+i.Node.m_Position+"/"+i.Node.m_Rotation.value+" edge="+i.HasEdge+":"+i.Edge.m_Start+"/"+i.Edge.m_End+
            " curve="+i.HasCurve+":"+Point(i.Curve.m_Bezier.a)+"/"+Point(i.Curve.m_Bezier.b)+"/"+Point(i.Curve.m_Bezier.c)+"/"+Point(i.Curve.m_Bezier.d)+" length="+i.Curve.m_Length.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+
            " local="+i.HasLocal+":"+i.Local.m_Curve.a+"/"+i.Local.m_Curve.b+"/"+i.Local.m_Curve.c+"/"+i.Local.m_Curve.d+" seed="+i.HasSeed+":"+i.Seed.m_Seed+
            " parking="+i.HasParking+":"+i.Parking.m_Flags+"/"+i.Parking.m_FreeSpace+"/"+i.Parking.m_ParkingFee+"/"+i.Parking.m_ComfortFactor+"/"+i.Parking.m_TaxiAvailability+"/"+i.Parking.m_TaxiFee+" restriction="+i.Parking.m_AccessRestriction+" additional="+Path(i.Parking.m_AdditionalStartNode)+
            " secondary="+i.HasSecondary+" connection="+i.HasConnection+":"+i.Connection.m_Flags+"/"+i.Connection.m_AccessRestriction+"/"+i.Connection.m_RoadTypes+"/"+i.Connection.m_TrackTypes+" paths="+(i.HasLane?Path(i.Lane.m_StartNode)+"/"+Path(i.Lane.m_MiddleNode)+"/"+Path(i.Lane.m_EndNode):"-")+
            " connections="+string.Join(",",i.Connections.Select(e=>e.ToString()))+" subnets="+i.SubNets.Length+" sublanes="+i.SubLanes.Length+
            " typed lane="+i.HasLane+" garage="+i.HasGarage+":"+Fields(i.Garage)+" car="+i.HasCar+":"+Fields(i.Car)+" pedestrian="+i.HasPedestrian+":"+Fields(i.Pedestrian)+" slave="+i.HasSlave+":"+Fields(i.Slave);
        private static string Point(float3 p)=>FormattableString.Invariant($"({p.x:R},{p.y:R},{p.z:R})");
        private static string Fields<T>(T data) where T:struct=>string.Join(",",typeof(T).GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Select(f=>{
            var value=f.GetValue(data);return f.Name+":"+(value is float number?number.ToString("R",System.Globalization.CultureInfo.InvariantCulture):Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture));}));
        private static string Path(Game.Pathfind.PathNode node)=>"owner="+node.GetOwnerIndex()+" bits="+string.Join(",",typeof(Game.Pathfind.PathNode).GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Select(f=>f.Name+":"+f.GetValue(node)));
        private static bool Finite(Colossal.Mathematics.Bezier4x3 curve)=>math.all(math.isfinite(curve.a))&&math.all(math.isfinite(curve.b))&&math.all(math.isfinite(curve.c))&&math.all(math.isfinite(curve.d));
    }
}
