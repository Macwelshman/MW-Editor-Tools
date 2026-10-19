using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Game.Prefabs;
using MW.EditorTools.Modules.BulldozerRecovery;
using Unity.Entities;
using Unity.Mathematics;

internal static class NetworkDeletionChecks {
    private static int checks;
    private static Entity Id(int i)=>new Entity{Index=i,Version=1};
    private static void Check(bool v,string label){if(!v)throw new Exception("FAILED network deletion: "+label);checks++;}
    private static AreaCompanionNetwork Graph(bool deleted=false,bool removeOrphans=false) {
        var items=new[]{
            new AreaCompanionNetwork.Item{Id=Id(2),Owner=Id(1),HasNode=true,Node=new Game.Net.Node{m_Position=new float3(0),m_Rotation=quaternion.identity},Connections=deleted?Array.Empty<Entity>():new[]{Id(4)}},
            new AreaCompanionNetwork.Item{Id=Id(3),Owner=Id(1),HasNode=true,Node=new Game.Net.Node{m_Position=new float3(10,0,0),m_Rotation=quaternion.identity},Connections=deleted?new[]{Id(6)}:new[]{Id(4),Id(6)}},
            new AreaCompanionNetwork.Item{Id=Id(4),Owner=Id(1),HasEdge=true,Edge=new Game.Net.Edge{m_Start=Id(2),m_End=Id(3)},HasSeed=true,Seed=new Game.Common.PseudoRandomSeed{m_Seed=7}},
            new AreaCompanionNetwork.Item{Id=Id(5),Owner=Id(1),HasNode=true,Node=new Game.Net.Node{m_Position=new float3(20,0,0),m_Rotation=quaternion.identity},Connections=new[]{Id(6)}},
            new AreaCompanionNetwork.Item{Id=Id(6),Owner=Id(1),HasEdge=true,Edge=new Game.Net.Edge{m_Start=Id(3),m_End=Id(5)},HasSeed=true,Seed=new Game.Common.PseudoRandomSeed{m_Seed=9}},
            new AreaCompanionNetwork.Item{Id=Id(7),Owner=Id(4),HasLane=true,Lane=new Game.Net.Lane{m_StartNode=new Game.Pathfind.PathNode(Id(4),(ushort)2)},HasCurve=true,Curve=new Game.Net.Curve{m_Bezier=new Colossal.Mathematics.Bezier4x3{a=new float3(1),b=new float3(2),c=new float3(3),d=new float3(4)},m_Length=4}}
        };
        items[2].SubLanes=new[]{new Game.Net.SubLane{m_SubLane=Id(7)}};
        if(deleted)items=items.Where(i=>i.Id!=Id(4)&&i.Id!=Id(7)&&(!removeOrphans||i.Id!=Id(2))).ToArray();
        return new AreaCompanionNetwork{Owner=Id(1),Items=items,RootNets=items.Where(i=>i.Owner==Id(1)).Select(i=>i.Id).ToArray()};
    }
    internal static int Run() {
        var before=Graph();var after=Graph(true);
        Check(before.TryMap(before,true,out _),"fixture closed and unambiguous");
        Check(NetworkTargetSelection.Resolve(new[]{Id(2),Id(3)},before)==Id(4),"native endpoint pair resolves one segment");
        Check(NetworkTargetSelection.Resolve(new[]{Id(3),Id(2)},before)==Id(4),"reversed endpoint pair resolves same segment");
        Check(NetworkTargetSelection.Resolve(new[]{Id(4),Id(4)},before)==Id(4),"repeated direct edge point resolves one segment");
        Check(NetworkTargetSelection.Resolve(new[]{Id(2),Id(4),Id(3)},before)==Id(4),"edge plus its endpoints remains one segment");
        Check(NetworkTargetSelection.Resolve(new[]{Id(4),Id(5)},before)==Entity.Null,"unrelated endpoint refused");
        Check(NetworkTargetSelection.Resolve(new[]{Id(2)},before)==Entity.Null,"junction-only selection refused");
        Check(NetworkTargetSelection.Resolve(new[]{Id(2),Id(3),Id(5)},before)==Entity.Null,"multi-segment node path refused");
        Check(NetworkTargetSelection.Resolve(new[]{Id(4),Id(6)},before)==Entity.Null,"two selected edges refused");
        Check(NetworkTargetSelection.Resolve(new[]{Id(2),Id(5)},before)==Entity.Null,"disconnected endpoints refused");
        Check(NetworkTargetSelection.Resolve(new[]{Id(2),Id(99)},before)==Entity.Null,"missing endpoint refused");
        Check(NetworkTargetSelection.Resolve(new[]{Id(7)},before)==Entity.Null,"generated lane refused");
        var parallel=Graph();parallel.Items=parallel.Items.Concat(new[]{new AreaCompanionNetwork.Item{Id=Id(8),Owner=Id(1),HasEdge=true,Edge=new Game.Net.Edge{m_Start=Id(2),m_End=Id(3)}}}).ToArray();parallel.RootNets=parallel.RootNets.Concat(new[]{Id(8)}).ToArray();
        Check(NetworkTargetSelection.Resolve(new[]{Id(2),Id(3)},parallel)==Entity.Null,"ambiguous parallel segments refused");
        Check(NetworkDeletionPolicy.Matches(before,Id(4),after),"one edge plus owned generated lane removed");
        Check(NetworkDeletionPolicy.Matches(before,Id(4),Graph(true,true)),"orphan endpoint may disappear");
        Check(!NetworkDeletionPolicy.Matches(before,Id(4),Graph()),"target still exists");
        Check(!NetworkDeletionPolicy.Matches(before,Id(99),after),"missing target refused");
        Check(!NetworkDeletionPolicy.Matches(before,Id(2),after),"junction-only target refused");
        after=Graph(true);after.Owner=Id(8);Check(!NetworkDeletionPolicy.Matches(before,Id(4),after),"different owning asset refused");
        after=Graph(true);after.Items=after.Items.Where(i=>i.Id!=Id(6)).ToArray();Check(!NetworkDeletionPolicy.Matches(before,Id(4),after),"second edge deletion refused");
        after=Graph(true);after.Items.Single(i=>i.Id==Id(6)).Seed.m_Seed=10;Check(!NetworkDeletionPolicy.Matches(before,Id(4),after),"retained seed change refused");
        after=Graph(true);after.Items.Single(i=>i.Id==Id(6)).Edge.m_End=Id(2);Check(!NetworkDeletionPolicy.Matches(before,Id(4),after),"retained topology change refused");
        after=Graph(true);after.Items.Single(i=>i.Id==Id(3)).Connections=Array.Empty<Entity>();Check(!NetworkDeletionPolicy.Matches(before,Id(4),after),"retained connection loss refused");
        after=Graph(true);after.Items.Single(i=>i.Id==Id(5)).Node.m_Position.x=21;Check(!NetworkDeletionPolicy.Matches(before,Id(4),after),"retained geometry change refused");
        after=Graph(true);after.Items.Single(i=>i.Id==Id(6)).Owner=Id(2);Check(!NetworkDeletionPolicy.Matches(before,Id(4),after),"retained owner change refused");
        after=Graph(true);after.Items=after.Items.Concat(new[]{before.Items.Single(i=>i.Id==Id(7))}).ToArray();Check(!NetworkDeletionPolicy.Matches(before,Id(4),after),"dangling generated lane refused");
        after=Graph(true);after.Items=after.Items.Concat(new[]{after.Items[0]}).ToArray();Check(!NetworkDeletionPolicy.Matches(before,Id(4),after),"duplicate entity refused");
        after=Graph(true);after.Items.Single(i=>i.Id==Id(6)).Id=Id(60);Check(!NetworkDeletionPolicy.Matches(before,Id(4),after),"unrelated new network refused");
        Check(before.Items.Single(i=>i.Id==Id(3)).Connections.Length==2&&before.Items.Single(i=>i.Id==Id(4)).SubLanes.Length==1,"before-image links remain immutable");
        var lane=(NetLanePrefab)RuntimeHelpers.GetUninitializedObject(typeof(NetLanePrefab));
        var other=(NetLanePrefab)RuntimeHelpers.GetUninitializedObject(typeof(NetLanePrefab));
        ObjectSubLaneInfo Row(NetLanePrefab prefab,int mesh=2)=>new ObjectSubLaneInfo{m_LanePrefab=prefab,m_ParentMesh=new int2(mesh),m_BezierCurve=new Colossal.Mathematics.Bezier4x3{a=new float3(1),b=new float3(2),c=new float3(3),d=new float3(4)}};
        var a=Row(lane);var originalIndex=a.m_NodeIndex;var b=Row(lane);b.m_NodeIndex=new int2(10,11);b.m_BezierCurve.b.x=8;
        Check(NetworkDescriptorPolicy.LaneMatches(a,b),"native curve/index representation may normalize with mandatory live graph authority");
        Check(a.m_NodeIndex.Equals(originalIndex)&&a.m_BezierCurve.b.x==2,"normalization never mutates source row");
        Check(!NetworkDescriptorPolicy.LaneMatches(a,Row(other)),"lane prefab identity preserved");
        Check(!NetworkDescriptorPolicy.LaneMatches(a,Row(lane,3)),"lane parent mesh preserved");
        b=Row(lane);b.m_BezierCurve.a.x=float.NaN;Check(!NetworkDescriptorPolicy.LaneMatches(a,b),"nonfinite descriptor refused");
        Check(NetworkDescriptorPolicy.Removed(new[]{Row(lane),Row(other)},new[]{Row(other)},r=>ReferenceEquals(r.m_LanePrefab,lane),NetworkDescriptorPolicy.LaneMatches),"exact one selected lane row removal");
        Check(!NetworkDescriptorPolicy.Removed(new[]{Row(lane),Row(other)},new[]{Row(lane)},r=>ReferenceEquals(r.m_LanePrefab,lane),NetworkDescriptorPolicy.LaneMatches),"wrong lane row removed refused");
        Check(!NetworkDescriptorPolicy.Removed(new[]{Row(lane),Row(other)},Array.Empty<ObjectSubLaneInfo>(),r=>true,NetworkDescriptorPolicy.LaneMatches),"multiple descriptor removal refused");
        Check(NetworkDescriptorPolicy.Set(new[]{Row(lane),Row(lane)},new[]{Row(lane),Row(lane)},NetworkDescriptorPolicy.LaneMatches),"identical metadata counts preserved");
        return checks;
    }
}
