using System;
using System.Linq;
using Game.Net;
using Game.Pathfind;
using Game.Objects;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;
using MW.EditorTools.Modules.BulldozerRecovery;
internal static class BikeRackMoveChecks {
    static Entity Id(int n)=>new Entity{Index=n,Version=1};
    static readonly PrefabBase prefab=(PrefabBase)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(StaticObjectPrefab));
    static Transform Pose(float angle=0)=>new Transform{m_Position=new float3(4,511.9453f,8),m_Rotation=quaternion.RotateY(angle)};
    static AreaCompanionNetwork Root(Transform pose){var owner=Id(1);var target=Id(20);var lane=new AreaCompanionNetwork.Item{Id=Id(30),Owner=owner,Prefab=prefab,HasLane=true,HasCurve=true,HasConnection=true,HasSecondary=true,
        HasGarage=true,Garage=new GarageLane{m_ComfortFactor=4,m_ParkingFee=2,m_VehicleCapacity=6},Lane=new Lane{m_StartNode=new PathNode(target,(ushort)0),m_MiddleNode=new PathNode(target,(ushort)3),m_EndNode=new PathNode(target,(ushort)0)},Connection=new Game.Net.ConnectionLane{m_AccessRestriction=owner,m_Flags=ConnectionLaneFlags.Inside,m_RoadTypes=RoadTypes.Bicycle},Curve=new Curve{m_Bezier=NetUtils.StraightCurve(pose.m_Position,pose.m_Position)}};
        return new AreaCompanionNetwork{Owner=owner,Items=new[]{lane},RootLanes=new[]{new Game.Net.SubLane{m_SubLane=lane.Id,m_PathMethods=(PathMethod)96}},Anchors=new[]{new AreaCompanionNetwork.Anchor{Id=target,Owner=owner,Prefab=prefab,Transform=pose,HasLocal=true,Local=new Game.Tools.LocalTransformCache{m_Position=pose.m_Position,m_Rotation=pose.m_Rotation,m_ParentMesh=-1,m_Probability=100}}}};
    }
    static AreaCompanionNetwork Owned(Transform pose){float3 P(float x)=>math.mul(pose.m_Rotation,new float3(x,0,0))+pose.m_Position;var target=Id(20);return new AreaCompanionNetwork{Owner=target,RootLanes=new[]{new Game.Net.SubLane{m_SubLane=Id(40),m_PathMethods=(PathMethod)96}},Items=new[]{new AreaCompanionNetwork.Item{Id=Id(40),Owner=target,Prefab=prefab,HasLane=true,HasCurve=true,HasParking=true,
        HasCar=true,Car=new Game.Net.CarLane{m_AccessRestriction=Id(1),m_SpeedLimit=5},HasPedestrian=true,Pedestrian=new Game.Net.PedestrianLane{m_AccessRestriction=Id(1)},HasSlave=true,Slave=new SlaveLane{m_Group=3},Lane=new Lane{m_StartNode=new PathNode(target,(ushort)0),m_MiddleNode=new PathNode(target,(ushort)1),m_EndNode=new PathNode(target,(ushort)2)},Parking=new Game.Net.ParkingLane{m_AccessRestriction=Id(1),m_AdditionalStartNode=new PathNode(target,(ushort)3),m_FreeSpace=4,m_ComfortFactor=30,m_ParkingFee=2,m_Flags=ParkingLaneFlags.AllowEnter|ParkingLaneFlags.AllowExit},Curve=new Curve{m_Bezier=NetUtils.StraightCurve(P(-2),P(2)),m_Length=4}}}};}
    internal static int Run(){int count=0;void Check(bool ok,string label){if(!ok)throw new Exception("Bike rack move: "+label);count++;}
        Check(BikeRackMovePolicy.Supports("BikeRack01",true,1,true,false,false,false),"single bound bike rack profile");
        Check(!BikeRackMovePolicy.Supports("BikeRack01",false,1,true,false,false,false),"standalone remains excluded");
        Check(!BikeRackMovePolicy.Supports("OtherRack",true,1,true,false,false,false),"other prefabs remain excluded");
        Check(!BikeRackMovePolicy.Supports("BikeRack01",true,2,true,false,false,false),"nested groups remain excluded");
        Check(BikeRackMovePolicy.Supports("BikeRack01",true,1,false,false,false,false,true),"observed lane-only rack is eligible without spawn data");
        Check(!BikeRackMovePolicy.Supports("BikeRack01",true,1,false,false,false,false,false),"missing spawn and lanes cannot authorize companion capture");
        Check(!BikeRackMovePolicy.Supports("BikeRack02",true,1,false,false,false,false,true),"other lane-only racks remain outside this change");
        Check(!BikeRackMovePolicy.Supports("BikeRack01",true,1,false,true,false,false,true),"unexpected takeoff companion refused");
        Check(BikeRackMoveEligibility.Dispatch(false,true),"lane-only rack dispatches to companion validation");
        Check(!BikeRackMoveEligibility.Dispatch(false,false)&&BikeRackMoveEligibility.Dispatch(true,false),"ordinary and marker dispatch preserved");
        var before=Pose();var owner=new Transform{m_Rotation=quaternion.identity};var original=Root(before);var originalOwned=Owned(before);
        for(int rotation=0;rotation<8;rotation++){
            var after=Pose(rotation*.31f);after.m_Position+=new float3(3,0,-7);
            var proof=BikeRackMovePolicy.Validate(original,Root(after),originalOwned,Owned(after),Id(20),before,after,owner,out var reason);
            Check(proof!=null,"translated/rotated self connection and owned parking geometry: "+reason);
            Check(proof.ChangedLanes.SetEquals(new[]{Id(30),Id(40)})&&proof.References[Id(40)]==Id(40),"complete proved companion reference set");
            var emptyRoot=new AreaCompanionNetwork{Owner=Id(1)};
            var laneOnly=BikeRackMovePolicy.Validate(emptyRoot,emptyRoot,originalOwned,Owned(after),Id(20),before,after,owner,out var laneReason);
            Check(laneOnly!=null&&laneOnly.ChangedLanes.SetEquals(new[]{Id(40)})&&!laneOnly.References.ContainsKey(Id(30)),"lane-only rack movement proves its owned graph without inventing a spawn/self connection: "+laneReason);
        }
        var moved=Pose(.5f);moved.m_Position.x+=3;
        foreach(var mutate in new Action<AreaCompanionNetwork>[] {
            g=>g.Items[0].Parking.m_FreeSpace++,g=>g.Items[0].Parking.m_ParkingFee++,g=>g.Items[0].Parking.m_ComfortFactor++,g=>g.Items[0].Parking.m_TaxiFee++,g=>g.Items[0].Parking.m_TaxiAvailability++,
            g=>g.Items[0].Parking.m_Flags|=ParkingLaneFlags.ParkingDisabled,g=>g.Items[0].Parking.m_AccessRestriction=Id(999),g=>g.Items[0].Parking.m_AdditionalStartNode=new PathNode(Id(999),(ushort)3),
            g=>g.Items[0].Curve.m_Bezier.b.y+=.01f,g=>g.Items[0].Lane.m_MiddleNode=new PathNode(Id(20),(ushort)4),g=>g.RootLanes[0].m_PathMethods=(PathMethod)1,g=>g.Items[0].Owner=Id(999),
            g=>g.Items[0].Car.m_SpeedLimit++,g=>g.Items[0].Car.m_AccessRestriction=Id(999),g=>g.Items[0].Pedestrian.m_AccessRestriction=Id(999),g=>g.Items[0].Slave.m_Group++,g=>g.Items[0].HasSlave=false
        }){var bad=Owned(moved);mutate(bad);Check(BikeRackMovePolicy.Validate(original,Root(moved),originalOwned,bad,Id(20),before,moved,owner,out _)==null,"owned parking settings, geometry, ownership and references preserved");}
        var rootBad=Root(moved);rootBad.Items[0].Connection.m_Flags|=ConnectionLaneFlags.Disabled;
        Check(BikeRackMovePolicy.Validate(original,rootBad,originalOwned,Owned(moved),Id(20),before,moved,owner,out _)==null,"self connection flags cannot change");
        foreach(var mutation in new Action<AreaCompanionNetwork.Item>[] {i=>i.Garage.m_VehicleCount++,i=>i.Garage.m_VehicleCapacity++,i=>i.Garage.m_ComfortFactor++,i=>i.Garage.m_ParkingFee++,i=>i.HasGarage=false}){
            rootBad=Root(moved);mutation(rootBad.Items[0]);Check(BikeRackMovePolicy.Validate(original,rootBad,originalOwned,Owned(moved),Id(20),before,moved,owner,out _)==null,"native garage data and schema preserved");
        }
        rootBad=Root(moved);rootBad.Items[0].Lane.m_EndNode=new PathNode(Id(99),(ushort)0);
        Check(BikeRackMovePolicy.Validate(original,rootBad,originalOwned,Owned(moved),Id(20),before,moved,owner,out _)==null,"unproved external root connection excluded");
        rootBad=Root(moved);rootBad.Items[0].Curve.m_Bezier.b.y+=.01f;
        Check(BikeRackMovePolicy.Validate(original,rootBad,originalOwned,Owned(moved),Id(20),before,moved,owner,out _)==null,"self curve must move rigidly");
        Check(BikeRackMovePolicy.Validate(original,Root(moved),originalOwned,null,Id(20),before,moved,owner,out _)==null,"owned lane schema preserved");
        var regenerated=Owned(moved);regenerated.Items[0].Id=Id(41);regenerated.RootLanes[0].m_SubLane=Id(41);
        Check(BikeRackMovePolicy.Validate(original,Root(moved),originalOwned,regenerated,Id(20),before,moved,owner,out var mapReason)?.References[Id(40)]==Id(41),"unique regenerated parking lane maps semantically: "+mapReason);
        Check(BikeRackMovePolicy.Validate(original,Root(moved),null,null,Id(20),before,moved,owner,out _)!=null,"rack self connection supported without invented owned lanes");
        var restoredRoot=Root(before);restoredRoot.Anchors[0].Id=Id(21);restoredRoot.Items[0].Id=Id(31);restoredRoot.RootLanes[0].m_SubLane=Id(31);restoredRoot.Items[0].Lane=new Lane{m_StartNode=new PathNode(Id(21),(ushort)0),m_MiddleNode=new PathNode(Id(21),(ushort)3),m_EndNode=new PathNode(Id(21),(ushort)0)};
        Check(original.TryMap(restoredRoot,true,out var restoreMap,true,true,true)&&restoreMap[Id(20)]==Id(21)&&restoreMap[Id(30)]==Id(31),"native restoration maps a regenerated rack and root self connection");
        var restoredOwned=Owned(before);restoredOwned.Owner=Id(21);restoredOwned.Items[0].Owner=Id(21);restoredOwned.Items[0].Id=Id(41);restoredOwned.RootLanes[0].m_SubLane=Id(41);restoredOwned.Items[0].Lane=new Lane{m_StartNode=new PathNode(Id(21),(ushort)0),m_MiddleNode=new PathNode(Id(21),(ushort)1),m_EndNode=new PathNode(Id(21),(ushort)2)};restoredOwned.Items[0].Parking.m_AdditionalStartNode=new PathNode(Id(21),(ushort)3);
        Check(originalOwned.TryMap(restoredOwned,true,out var ownedRestoreMap)&&ownedRestoreMap[Id(20)]==Id(21)&&ownedRestoreMap[Id(40)]==Id(41),"native restoration remaps owned parking path references together");
        restoredOwned.Items[0].Parking.m_AdditionalStartNode=new PathNode(Id(20),(ushort)3);Check(!originalOwned.TryMap(restoredOwned,true,out _),"stale original parking path reference refused after restoration");
        Check(originalOwned.Items[0].Curve.m_Bezier.a.Equals(Owned(before).Items[0].Curve.m_Bezier.a)&&original.Anchors[0].Transform.Equals(before),"original snapshots unchanged");
        // Rounded 0.4.218 log values alone do not reproduce the live rejection.
        // Keep that distinction explicit instead of widening a geometry tolerance.
        AreaCompanionNetwork LoggedFence(bool after){var lane=new AreaCompanionNetwork.Item{Id=Id(100),Owner=Id(1),Prefab=prefab,HasLane=true,HasCurve=true,HasSeed=true,Seed=new Game.Common.PseudoRandomSeed{m_Seed=13273},
            Lane=new Lane{m_StartNode=new PathNode(Id(1),(ushort)0),m_MiddleNode=new PathNode(Id(1),(ushort)1),m_EndNode=new PathNode(Id(1),(ushort)2)},
            Curve=new Curve{m_Length=24,m_Bezier=new Colossal.Mathematics.Bezier4x3{a=new float3(19.84826f,511.9453f,35.94519f),b=new float3(19.84826f,after?511.9456f:511.946f,27.94519f),c=new float3(19.84826f,after?511.9453f:511.9451f,19.94519f),d=new float3(19.84826f,511.9453f,11.94519f)}}};
            return new AreaCompanionNetwork{Owner=Id(1),Items=new[]{lane},RootLanes=new[]{new Game.Net.SubLane{m_SubLane=lane.Id,m_PathMethods=(PathMethod)1}}};}
        var loggedBefore=LoggedFence(false);var loggedAfter=LoggedFence(true);
        Check(AreaCompanionNetwork.DataDifferences(loggedBefore.Items[0],loggedAfter.Items[0],true,true,true)=="none","printed fence geometry is already within existing native rounding rules");
        Check(BikeRackMovePolicy.Validate(loggedBefore,loggedAfter,originalOwned,Owned(moved),Id(20),before,moved,owner,out _)!=null,"printed first-failure geometry alone is not the complete live mismatch");
        var typedBefore=originalOwned.Items[0];
        foreach(var example in new (string Name,Action<AreaCompanionNetwork.Item> Mutate)[]{("car",i=>i.Car.m_SpeedLimit++),("pedestrian",i=>i.Pedestrian.m_Flags++),("slave",i=>i.Slave.m_Group++),("parking",i=>i.Parking.m_FreeSpace++),("seed",i=>{i.HasSeed=true;})}){
            var changed=typedBefore.DetachedCopy();example.Mutate(changed);var report=AreaCompanionNetwork.DataDifferences(typedBefore,changed,true,true,true);
            Check(report.Contains(example.Name=="seed"?"schema":example.Name),"mismatch diagnostics identify omitted typed field: "+example.Name);
        }
        var description=AreaCompanionNetwork.Describe(typedBefore);
        Check(description.Contains("car=True:")&&description.Contains("slave=True:")&&description.Contains("m_Group:3"),"mismatch diagnostics include complete previously omitted typed structs");
        return count;
    }
}


