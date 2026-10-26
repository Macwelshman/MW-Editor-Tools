using System;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Game.Net;
using Game.Pathfind;
using Unity.Entities;
using Unity.Mathematics;
using MW.EditorTools.History;
using MW.EditorTools.Modules.BulldozerRecovery;
internal static class MarkerMoveChecks {
    static Entity Id(int n)=>new Entity{Index=n,Version=1};
    static readonly PrefabBase prefab=(PrefabBase)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(MarkerObjectPrefab));
    static readonly PrefabBase lane=(PrefabBase)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(NetLanePrefab));
    static AreaCompanionNetwork Chain(int[] nodes,float[] positions,int[] ids,int reverse=0){
        var owner=Id(1);var anchors=nodes.Select((n,i)=>new AreaCompanionNetwork.Anchor{Id=Id(n),Owner=owner,Prefab=prefab,HasLocal=true,Transform=new Game.Objects.Transform{m_Position=new float3(positions[i],0,0),m_Rotation=quaternion.identity},Local=new LocalTransformCache{m_Position=new float3(positions[i],0,0),m_Rotation=quaternion.identity,m_ParentMesh=-1,m_Probability=100}}).ToArray();
        return new AreaCompanionNetwork{Owner=owner,Anchors=anchors,Items=ids.Select((id,i)=>{var a=anchors[(reverse&(1<<i))==0?i:i+1];var b=anchors[(reverse&(1<<i))==0?i+1:i];return new AreaCompanionNetwork.Item{Id=Id(id),Owner=owner,Prefab=lane,HasCurve=true,HasLane=true,HasConnection=true,HasSecondary=true,Connection=new Game.Net.ConnectionLane{m_AccessRestriction=owner,m_Flags=ConnectionLaneFlags.Inside|ConnectionLaneFlags.Pedestrian},Lane=new Lane{m_StartNode=new PathNode(a.Id,(ushort)0),m_MiddleNode=new PathNode(a.Id,(ushort)3),m_EndNode=new PathNode(b.Id,(ushort)0)},Curve=new Curve{m_Bezier=NetUtils.StraightCurve(a.Transform.m_Position,b.Transform.m_Position),m_Length=math.distance(a.Transform.m_Position,b.Transform.m_Position)}};}).ToArray(),RootLanes=ids.Select(n=>new Game.Net.SubLane{m_SubLane=Id(n),m_PathMethods=(PathMethod)1}).ToArray()};
    }
    internal static int Run(){int count=0;void Check(bool ok,string label){if(!ok)throw new Exception("Marker move: "+label);count++;}
        Check(MarkerMovePolicy.Supports("Restricted Spawn Location",true,1,true,true,false,false,false,false),"direct bound Restricted Spawn marker supported");
        Check(MarkerMovePolicy.Supports("Pedestrian Access Location",true,1,true,false,true,true,true,false),"direct bound Pedestrian Access marker supported");
        Check(!MarkerMovePolicy.Supports("Other Marker",true,1,true,true,false,false,false,false),"other marker categories stay excluded");
        Check(!MarkerMovePolicy.Supports("Restricted Spawn Location",false,1,true,true,false,false,false,false),"unbound marker move remains excluded");
        Check(!MarkerMovePolicy.Supports("Restricted Spawn Location",true,2,true,true,false,false,false,false),"nested marker group remains excluded");
        Check(!MarkerMovePolicy.Supports("Pedestrian Access Location",true,1,true,false,true,true,true,true),"marker-owned editable lanes remain excluded");
        var before=Chain(new[]{20,21,22,23},new[]{0f,3f,6f,9f},new[]{30,31,32});
        for(int direction=0;direction<8;direction++){
            var after=Chain(new[]{20,21,22,23},new[]{0f,4f,6f,9f},new[]{31,40,30},direction);
            var proof=MarkerMoveNetwork.Validate(before,after,Id(21),out var reason);
            Check(proof!=null,"interior marker translation with reused IDs and reversed connections: "+reason);
            Check(proof.ChangedLanes.SetEquals(new[]{Id(30),Id(31)})&&proof.References[Id(32)]==Id(30),"only incident connections may change; retained lane maps semantically");
        }
        var reordered=Chain(new[]{20,22,23,21},new[]{0f,6f,9f,12f},new[]{40,30,31},3);
        Check(MarkerMoveNetwork.Validate(before,reordered,Id(21),out _)!=null,"marker crossing neighbour order preserves virtual-deletion common graph");
        var end=Chain(new[]{20,21,22,23},new[]{-2f,3f,6f,9f},new[]{40,41,42},4);
        Check(MarkerMoveNetwork.Validate(before,end,Id(20),out _)!=null,"endpoint marker move supported");
        var bad=Chain(new[]{20,21,22,23},new[]{0f,4f,7f,9f},new[]{40,41,42});
        Check(MarkerMoveNetwork.Validate(before,bad,Id(21),out _)==null,"another marker cannot move with target");
        bad=Chain(new[]{20,21,22,23},new[]{0f,4f,6f,9f},new[]{40,41,42});bad.Items[2].Connection.m_Flags|=ConnectionLaneFlags.Disabled;
        Check(MarkerMoveNetwork.Validate(before,bad,Id(21),out _)==null,"unrelated connection flags cannot change");
        bad=Chain(new[]{20,21,22,23},new[]{0f,4f,6f,9f},new[]{40,41,42});bad.Items[0].Curve.m_Bezier.b.y=.1f;
        Check(MarkerMoveNetwork.Validate(before,bad,Id(21),out _)==null,"changed incident curve must remain exact native straight geometry");
        bad=Chain(new[]{20,21,22,23},new[]{0f,4f,6f,9f},new[]{40,41,42});bad.RootLanes[2].m_PathMethods=(PathMethod)2;
        Check(MarkerMoveNetwork.Validate(before,bad,Id(21),out _)==null,"path methods preserved");
        var pedestrian=MarkerMoveNetwork.Validate(before,before,Id(99),out _);
        Check(pedestrian!=null&&pedestrian.ChangedLanes.Count==0,"Pedestrian Access movement without owned path anchor keeps full network exact");
        Check(MarkerMoveNetwork.Validate(before,reordered,Id(99),out _)==null,"unanchored access marker cannot authorize network geometry changes");
        var ownerPose=new Game.Objects.Transform{m_Position=new float3(46.6719f,511.9453f,56.15701f),m_Rotation=quaternion.identity};
        AreaCompanionNetwork WithFence(bool moved){
            var g=Chain(new[]{20,21,22,23},moved?new[]{0f,4f,6f,9f}:new[]{0f,3f,6f,9f},new[]{30,31,32});
            var local=new Colossal.Mathematics.Bezier4x3{a=new float3(11.00137f,0,8.000008f),b=new float3(11.00137f,.000793457f,0),c=new float3(11.00137f,-.000213623f,-8),d=new float3(11.00137f,0,-16)};
            float3 Project(float3 p)=>math.mul(ownerPose.m_Rotation,p)+ownerPose.m_Position;
            var curve=new Colossal.Mathematics.Bezier4x3{a=Project(local.a),b=Project(local.b),c=Project(local.c),d=Project(local.d)};
            if(moved){curve.b.y=511.9455f;curve.c.y=511.9452f;}
            var fence=new AreaCompanionNetwork.Item{Id=Id(102),Owner=g.Owner,Prefab=lane,HasEdge=true,HasCurve=true,HasLocal=true,HasSeed=true,Seed=new Game.Common.PseudoRandomSeed{m_Seed=9364},Edge=new Game.Net.Edge{m_Start=Id(100),m_End=Id(101)},Curve=new Curve{m_Bezier=curve,m_Length=24.00001f},Local=new LocalCurveCache{m_Curve=local}};
            var nodes=new[]{new AreaCompanionNetwork.Item{Id=Id(100),Owner=g.Owner,Prefab=lane,HasNode=true,Node=new Node{m_Position=curve.a,m_Rotation=quaternion.identity},Connections=new[]{fence.Id}},new AreaCompanionNetwork.Item{Id=Id(101),Owner=g.Owner,Prefab=lane,HasNode=true,Node=new Node{m_Position=curve.d,m_Rotation=quaternion.identity},Connections=new[]{fence.Id}}};
            g.Items=g.Items.Concat(nodes).Concat(new[]{fence}).ToArray();g.RootNets=nodes.Select(n=>n.Id).Concat(new[]{fence.Id}).ToArray();return g;
        }
        var fenceBefore=WithFence(false);var fenceAfter=WithFence(true);
        Check(MarkerMoveNetwork.Validate(fenceBefore,fenceAfter,Id(21),out _)==null,"logged first-attempt fence drift reproduces old capture refusal");
        Check(MarkerMoveNetwork.Validate(fenceBefore,fenceAfter,Id(21),out var heightReason,ownerPose)!=null,"identical local curve proves logged marker capture height normalization: "+heightReason);
        Check(!fenceBefore.TryMap(fenceAfter,true,out _,true,true,true),"ordinary mapping still refuses the observed fence drift");
        var fenceRestored=WithFence(true);fenceRestored.Anchors=fenceBefore.Anchors;fenceRestored.Items[0]=fenceBefore.Items[0];fenceRestored.Items[1]=fenceBefore.Items[1];
        Check(fenceBefore.TryMap(fenceRestored,true,out _,true,true,true,ownerPose),"marker restoration full graph accepts same local witness");
        Check(fenceRestored.TryMap(fenceBefore,true,out _,true,true,true,ownerPose),"comparison also supports restoring the original height representation");
        fenceRestored.Items.Last().Id=Id(103);fenceRestored.RootNets[fenceRestored.RootNets.Length-1]=Id(103);
        foreach(var n in fenceRestored.Items.Where(i=>i.HasNode))n.Connections=new[]{Id(103)};
        Check(fenceBefore.TryMap(fenceRestored,true,out var freshFenceMap,true,true,true,ownerPose)&&freshFenceMap[Id(102)]==Id(103),"fresh restored fence retains full endpoint and reference mapping");
        fenceRestored.Items.Last().Local.m_Curve.b.y=float.NaN;
        Check(!fenceBefore.TryMap(fenceRestored,true,out _,true,true,true,ownerPose),"height allowance cannot substitute an invalid local-cache witness");
        foreach(var mutation in new Action<AreaCompanionNetwork.Item>[] {
            i=>i.Local.m_Curve.b.y+=.00001f,i=>i.Local.m_Curve.b.y=float.NaN,i=>i.Curve.m_Bezier.b.y+=.02f,
            i=>i.Curve.m_Bezier.b.x+=.001f,i=>i.Curve.m_Bezier.a.y+=.001f,i=>i.Curve.m_Length+=.01f,
            i=>i.Seed.m_Seed++,i=>i.Edge.m_End=Id(100),i=>i.HasSecondary=true,i=>i.Owner=Id(999),i=>i.Connections=new[]{Id(999)}
        }){
            var altered=WithFence(true);mutation(altered.Items.Last());
            Check(MarkerMoveNetwork.Validate(fenceBefore,altered,Id(21),out _,ownerPose)==null,"height witness cannot hide cache, geometry, metadata, ownership or reference mutation");
        }
        var wrongOwner=ownerPose;wrongOwner.m_Position.y+=.01f;
        Check(MarkerMoveNetwork.Validate(fenceBefore,fenceAfter,Id(21),out _,wrongOwner)==null,"height witness requires the guarded owner pose");
        Check(fenceBefore.Items.Last().Curve.m_Bezier.b.y==511.9461f&&fenceAfter.Items.Last().Curve.m_Bezier.b.y==511.9455f,"height comparison leaves both original snapshots untouched");
        AreaCompanionNetwork WithFenceLane(bool moved){
            var graph=WithFence(moved);var parent=graph.Items.Last();var child=new AreaCompanionNetwork.Item{Id=Id(104),Owner=parent.Id,Prefab=parent.Prefab,HasLane=true,HasCurve=true,HasSeed=true,Seed=new Game.Common.PseudoRandomSeed{m_Seed=33808},
                Lane=new Lane{m_StartNode=new PathNode(parent.Id,(ushort)0),m_MiddleNode=new PathNode(parent.Id,(ushort)1),m_EndNode=new PathNode(parent.Id,(ushort)2)},Curve=parent.Curve};
            child.Curve.m_Length=24;child.Curve.m_Bezier.b.y=moved?511.9456f:511.946f;child.Curve.m_Bezier.c.y=moved?511.9453f:511.9451f;
            parent.SubLanes=new[]{new Game.Net.SubLane{m_SubLane=child.Id,m_PathMethods=(PathMethod)1}};graph.Items=graph.Items.Concat(new[]{child}).ToArray();return graph;
        }
        var childBefore=WithFenceLane(false);var childAfter=WithFenceLane(true);
        Check(MarkerMoveNetwork.Validate(childBefore,childAfter,Id(21),out var childReason,ownerPose)!=null,"logged first-attempt child lane drift is proved against preserved parent fence: "+childReason);
        var edgeOnlyAfter=WithFence(true);edgeOnlyAfter.Items.Last().SubLanes=childAfter.Items.Single(i=>i.HasEdge).SubLanes;edgeOnlyAfter.Items=edgeOnlyAfter.Items.Concat(new[]{childBefore.Items.Last().DetachedCopy()}).ToArray();edgeOnlyAfter.Items.Last().Curve.m_Bezier.b.y=511.9464f;
        Check(MarkerMoveNetwork.Validate(childBefore,edgeOnlyAfter,Id(21),out _,ownerPose)==null,"lane must follow the raw live parent rather than only the detached normalized parent");
        var restoredChild=WithFenceLane(true);restoredChild.Anchors=childBefore.Anchors;restoredChild.Items[0]=childBefore.Items[0];restoredChild.Items[1]=childBefore.Items[1];
        Check(childBefore.TryMap(restoredChild,true,out _,true,true,true,ownerPose),"child-lane proof also applies to marker restoration final graph");
        var freshParent=restoredChild.Items.Single(i=>i.HasEdge);var freshChild=restoredChild.Items.Last();freshParent.Id=Id(105);freshParent.SubLanes[0].m_SubLane=Id(106);freshChild.Id=Id(106);freshChild.Owner=Id(105);
        freshChild.Lane=new Lane{m_StartNode=new PathNode(Id(105),(ushort)0),m_MiddleNode=new PathNode(Id(105),(ushort)1),m_EndNode=new PathNode(Id(105),(ushort)2)};
        restoredChild.RootNets=restoredChild.RootNets.Select(id=>id==Id(102)?Id(105):id).ToArray();foreach(var n in restoredChild.Items.Where(i=>i.HasNode))n.Connections=new[]{Id(105)};
        Check(childBefore.TryMap(restoredChild,true,out var freshChildMap,true,true,true,ownerPose)&&freshChildMap[Id(102)]==Id(105)&&freshChildMap[Id(104)]==Id(106),"fresh restored parent/child references map together under the same geometry proof");
        var savedLocal=freshParent.Local;
        freshParent.Local.m_Curve.a.x=float.NaN;
        Check(childBefore.TryMap(restoredChild,false,out _,false,true,false,ownerPose),"fence restoration preflight uses parent source witness with a pending nonfinite native cache");
        Check(!childBefore.TryMap(restoredChild,true,out _,true,true,false,ownerPose),"strict final fence verification still rejects an unrepaired nonfinite cache");
        var invalidPose=ownerPose;invalidPose.m_Position.y+=.02f;
        Check(!childBefore.TryMap(restoredChild,false,out _,false,true,false,invalidPose),"nonfinite-cache witness cannot use a different owner pose");
        freshParent.Local=savedLocal;
        Check(childBefore.TryMap(restoredChild,true,out _,true,true,false,ownerPose),"direct fence restoration with fresh parent and child IDs verifies exact data after cache repair");
        freshParent.Seed.m_Seed++;freshChild.Seed.m_Seed++;
        Check(childBefore.TryMap(restoredChild,false,out _,false,true,true,ownerPose),"seed repair preflight retains parent-derived geometry proof");
        Check(!childBefore.TryMap(restoredChild,true,out _,true,true,true,ownerPose),"final child/parent seed verification cannot be bypassed");
        foreach(var mutation in new Action<AreaCompanionNetwork>[] {
            g=>g.Items.Last().Curve.m_Bezier.b.y+=.001f,g=>g.Items.Last().Curve.m_Bezier.b.x+=.001f,g=>g.Items.Last().Curve.m_Bezier.a.y+=.001f,
            g=>g.Items.Last().Curve.m_Length+=.01f,g=>g.Items.Last().Seed.m_Seed++,g=>g.Items.Last().HasLocal=true,g=>g.Items.Last().HasSecondary=true,
            g=>g.Items.Last().Lane.m_MiddleNode=new PathNode(Id(102),(ushort)3),g=>g.Items.Last().Owner=Id(999),
            g=>g.Items.Single(i=>i.HasEdge).Local.m_Curve.b.y+=.00001f,g=>g.Items.Single(i=>i.HasEdge).SubLanes[0].m_PathMethods=(PathMethod)2,
            g=>g.Items.Single(i=>i.HasEdge).SubLanes=Array.Empty<Game.Net.SubLane>(),g=>g.Items.Last().Connections=new[]{Id(999)}
        }){var altered=WithFenceLane(true);mutation(altered);Check(MarkerMoveNetwork.Validate(childBefore,altered,Id(21),out _,ownerPose)==null,"parent witness preserves child geometry/schema/seed/path/reference/ownership and parent local/membership data");}
        Check(childBefore.Items.Last().Curve.m_Bezier.b.y==511.946f&&childAfter.Items.Last().Curve.m_Bezier.b.y==511.9456f,"child-lane normalization never changes original snapshots");
        var largeBefore=WithFence(false);var largeAfter=WithFence(true);
        foreach(var graph in new[]{largeBefore,largeAfter}){var edge=graph.Items.Last();edge.Local.m_Curve.b.y=-.003326416f;edge.Local.m_Curve.c.y=-.008117676f;edge.Curve.m_Length=24.04335f;}
        largeBefore.Items.Last().Curve.m_Bezier.b.y=511.942f;largeBefore.Items.Last().Curve.m_Bezier.c.y=511.9372f;
        Check(MarkerMoveNetwork.Validate(largeBefore,largeAfter,Id(21),out _,ownerPose)!=null,"logged 3.5mm/8mm drift serializes to unchanged native curve cache");
        largeAfter.Items.Last().Curve.m_Bezier.c.y=511.9473f;
        Check(MarkerMoveNetwork.Validate(largeBefore,largeAfter,Id(21),out _,ownerPose)==null,"native one-centimetre cache boundary remains strict");
        AreaCompanionNetwork Layout(bool moved,int reverse=0){
            var g=Chain(new[]{20,21,22,23},new[]{0f,3f,4f,-5f},new[]{30,31,32});
            var positions=new[]{moved?new float3(8,0,1):new float3(0),new float3(3,0,0),new float3(0,0,4),new float3(-5,0,0)};
            for(int n=0;n<4;n++){g.Anchors[n].Transform.m_Position=positions[n];g.Anchors[n].Local.m_Position=positions[n];}
            var pairs=moved?new[]{(20,21),(20,22),(21,23),(21,22),(22,23)}:new[]{(20,21),(20,22),(20,23),(21,22),(22,23)};var template=g.Items[0];
            g.Items=pairs.Select((pair,n)=>{var a=g.Anchors.Single(p=>p.Id.Index==((reverse&(1<<n))==0?pair.Item1:pair.Item2));var b=g.Anchors.Single(p=>p.Id.Index==((reverse&(1<<n))==0?pair.Item2:pair.Item1));var item=template.DetachedCopy();item.Id=Id(30+n);item.Lane=new Lane{m_StartNode=new PathNode(a.Id,(ushort)0),m_MiddleNode=new PathNode(a.Id,(ushort)3),m_EndNode=new PathNode(b.Id,(ushort)0)};item.Curve=new Curve{m_Bezier=NetUtils.StraightCurve(a.Transform.m_Position,b.Transform.m_Position),m_Length=math.distance(a.Transform.m_Position,b.Transform.m_Position)};return item;}).ToArray();
            g.RootLanes=g.Items.Select(i=>new Game.Net.SubLane{m_SubLane=i.Id,m_PathMethods=(PathMethod)1}).ToArray();return g;
        }
        var branchedBefore=Layout(false);var branchedAfter=Layout(true);
        Check(MarkerMoveNetwork.Validate(branchedBefore,branchedAfter,Id(20),out var chainReason)==null&&chainReason.Contains("incident=3"),"three-link native layout reproduces old chain restriction with precise diagnostics");
        for(int reverse=0;reverse<32;reverse++){
            branchedAfter=Layout(true,reverse);
            var proof=MarkerMoveNetwork.Validate(branchedBefore,branchedAfter,Id(20),out var layoutReason,null,branchedBefore.Anchors,branchedAfter.Anchors);
            Check(proof!=null,"complete directional layout handles branch rewiring and reversed/reused links: "+layoutReason);
            Check(proof.ChangedLanes.SetEquals(new[]{Id(30),Id(31),Id(32)})&&proof.References[Id(33)]==Id(33),"only proved changed physical links may reconnect references");
        }
        bool LayoutAccepted(AreaCompanionNetwork graph)=>MarkerMoveNetwork.Validate(branchedBefore,graph,Id(20),out _,null,branchedBefore.Anchors,graph.Anchors)!=null;
        branchedAfter=Layout(true);branchedAfter.Items[0].Connection.m_Flags|=ConnectionLaneFlags.Disabled;Check(!LayoutAccepted(branchedAfter),"branched native layout preserves every connection setting");
        branchedAfter=Layout(true);branchedAfter.Items=branchedAfter.Items.Skip(1).ToArray();branchedAfter.RootLanes=branchedAfter.RootLanes.Skip(1).ToArray();Check(!LayoutAccepted(branchedAfter),"missing required directional pair refused");
        branchedAfter=Layout(true);branchedAfter.Anchors[1].Transform.m_Position.x+=1;Check(!LayoutAccepted(branchedAfter),"other peer placement remains immutable");
        branchedAfter=Layout(true);branchedAfter.Items[0].Curve.m_Bezier.b.y=.1f;Check(!LayoutAccepted(branchedAfter),"branched link must retain native straight geometry");
        branchedAfter=Layout(true);branchedAfter.RootLanes[0].m_PathMethods=(PathMethod)2;Check(!LayoutAccepted(branchedAfter),"branched layout preserves native root methods");
        var tiedPeers=new[]{branchedBefore.Anchors[0].DetachedCopy(),branchedBefore.Anchors[1].DetachedCopy(),branchedBefore.Anchors[2].DetachedCopy()};tiedPeers[0].Transform.m_Position=new float3(0);tiedPeers[1].Transform.m_Position=new float3(3,0,1);tiedPeers[2].Transform.m_Position=new float3(3,0,-1);
        Check(MarkerConnectionLayout.Pairs(tiedPeers,out var tieReason)==null&&tieReason.Contains("equidistant"),"unknown native tie ordering cannot authorize a different layout");
        var isolatedBefore=Chain(new[]{20},new[]{0f},Array.Empty<int>());var isolatedAfter=Chain(new[]{20},new[]{4f},Array.Empty<int>());
        Check(MarkerMoveNetwork.Validate(isolatedBefore,isolatedAfter,Id(20),out _,null,isolatedBefore.Anchors,isolatedAfter.Anchors)!=null,"complete one-marker group needs no invented connections");
        var extra=Layout(true);var duplicate=extra.Items[0].DetachedCopy();duplicate.Id=Id(99);extra.Items=extra.Items.Concat(new[]{duplicate}).ToArray();extra.RootLanes=extra.RootLanes.Concat(new[]{new Game.Net.SubLane{m_SubLane=Id(99),m_PathMethods=(PathMethod)1}}).ToArray();Check(!LayoutAccepted(extra),"duplicate physical pair is never accepted");
        extra=Layout(true);extra.Items[0].Lane.m_EndNode=new PathNode(Id(999),(ushort)0);Check(!LayoutAccepted(extra),"connection outside complete owned group refused");
        var accessBefore=new SpawnAccessImage{HasTakeoff=true,HasAccess=true,HasRoute=true,Takeoff=new Game.Routes.TakeoffLocation{m_AccessRestriction=Id(1)},Access=new Game.Routes.AccessLane{m_Lane=Id(31),m_CurvePos=.2f},Route=new Game.Routes.RouteLane{m_StartLane=Id(31),m_EndLane=Id(31),m_StartCurvePos=.2f,m_EndCurvePos=.2f},References=new[]{new SpawnAccessImage.Reference{Id=Id(31),HasLane=true},new SpawnAccessImage.Reference{Id=Id(1)}}};
        var accessAfter=new SpawnAccessImage{HasTakeoff=true,HasAccess=true,HasRoute=true,Takeoff=accessBefore.Takeoff,Access=new Game.Routes.AccessLane{m_Lane=Id(32),m_CurvePos=.7f},Route=new Game.Routes.RouteLane{m_StartLane=Id(32),m_EndLane=Id(32),m_StartCurvePos=.7f,m_EndCurvePos=.7f},References=new[]{new SpawnAccessImage.Reference{Id=Id(32),HasLane=true},new SpawnAccessImage.Reference{Id=Id(1)}}};
        var allowed=new System.Collections.Generic.HashSet<Entity>(before.Items.Select(item=>item.Id));var reconnect=new System.Collections.Generic.HashSet<Entity>{Id(31)};
        Check(accessBefore.NativeConnectionsMatch(accessAfter,reconnect,allowed,pedestrian.References),"Pedestrian Access native movement can reconnect within complete unchanged owner graph");
        accessAfter.Takeoff.m_AccessRestriction=Id(999);Check(!accessBefore.NativeConnectionsMatch(accessAfter,reconnect,allowed,pedestrian.References),"Pedestrian Access move preserves access restriction reference");accessAfter.Takeoff=accessBefore.Takeoff;
        Check(!accessBefore.NativeConnectionsMatch(accessAfter,reconnect,new System.Collections.Generic.HashSet<Entity>(),pedestrian.References),"Pedestrian Access move cannot adopt unknown external lane");
        var unconnected=new SpawnAccessImage{HasTakeoff=true,HasAccess=true,HasRoute=true,Takeoff=accessBefore.Takeoff,Access=new Game.Routes.AccessLane{m_CurvePos=-1},Route=new Game.Routes.RouteLane{m_StartCurvePos=-1,m_EndCurvePos=-1},References=new[]{new SpawnAccessImage.Reference{Id=Id(1)}}};
        Check(unconnected.NativeConnectionsMatch(accessAfter,new System.Collections.Generic.HashSet<Entity>{Entity.Null},allowed,pedestrian.References),"moved access marker may acquire its first connection only in proved owner graph");
        Check(unconnected.NativeConnectionsMatch(unconnected,new System.Collections.Generic.HashSet<Entity>{Entity.Null},allowed,pedestrian.References),"unchanged unconnected sentinel values remain exact");
        Check(!unconnected.NativeConnectionsMatch(accessAfter,new System.Collections.Generic.HashSet<Entity>(),allowed,pedestrian.References),"stationary unconnected marker cannot gain a connection implicitly");
        var badIncident=Chain(new[]{20,21,22,23},new[]{-2f,3f,6f,9f},new[]{40,41,42});badIncident.Items[0].Connection.m_Flags|=ConnectionLaneFlags.Disabled;
        Check(MarkerMoveNetwork.Validate(before,badIncident,Id(20),out _)==null,"endpoint marker cannot change even an incident connection's settings");
        ObjectSubObjectInfo Row(float x)=>new ObjectSubObjectInfo{m_Object=null,m_Position=new float3(x,0,0),m_Rotation=quaternion.identity,m_ParentMesh=-1,m_Probability=100};
        var oldRows=new[]{Row(0),Row(3)};var newRows=new[]{Row(0),Row(4)};
        bool Sources(ObjectSubObjectInfo[] rows)=>MarkerMovePolicy.SourceRows(oldRows,rows,1,1,new PrefabBase[]{null,null},new[]{new LocalTransformCache{m_Position=new float3(0),m_Rotation=quaternion.identity,m_ParentMesh=-1,m_Probability=100},new LocalTransformCache{m_Position=new float3(4,0,0),m_Rotation=quaternion.identity,m_ParentMesh=-1,m_Probability=100}},new[]{new Game.Objects.Transform{m_Position=new float3(0),m_Rotation=quaternion.identity},new Game.Objects.Transform{m_Position=new float3(4,0,0),m_Rotation=quaternion.identity}},new Game.Objects.Transform{m_Rotation=quaternion.identity});
        Check(Sources(newRows),"only target source pose changes");
        var changed=(ObjectSubObjectInfo[])PrefabSnapshot.Take(newRows).Materialize();changed[1].m_Probability=50;Check(!Sources(changed),"moved marker probability cannot change");
        changed=(ObjectSubObjectInfo[])PrefabSnapshot.Take(newRows).Materialize();changed[0].m_Position.x=1;Check(!Sources(changed),"another saved source row cannot move");
        Check(oldRows[1].m_Position.x==3&&before.Anchors[1].Transform.m_Position.x==3&&before.Items[0].Lane.m_EndNode.GetOwnerIndex()==21,"original source, anchor and path snapshots untouched");
        return count;
    }
}



