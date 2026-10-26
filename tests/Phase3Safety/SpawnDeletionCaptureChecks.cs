using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Game.Net;
using Game.Pathfind;
using MW.EditorTools.Modules.BulldozerRecovery;
internal static class SpawnDeletionCaptureChecks {
    static Entity Id(int n)=>new Entity{Index=n,Version=1};
    static readonly Game.Prefabs.PrefabBase prefab=(Game.Prefabs.PrefabBase)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Game.Prefabs.MarkerObjectPrefab));
    static readonly Game.Prefabs.PrefabBase lanePrefab=(Game.Prefabs.PrefabBase)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Game.Prefabs.NetLanePrefab));
    static AreaCompanionNetwork Graph(int[] nodes,int[] ids,int reverse=0){
        var owner=Id(1);var anchors=nodes.Select(n=>new AreaCompanionNetwork.Anchor{Id=Id(n),Owner=owner,Prefab=prefab,HasLocal=true,Transform=new Game.Objects.Transform{m_Position=new float3(n*3,511.9453f,0),m_Rotation=quaternion.identity},Local=new Game.Tools.LocalTransformCache{m_Position=new float3(n*3,0,0),m_Rotation=quaternion.identity,m_ParentMesh=-1,m_Probability=100}}).ToArray();
        var items=ids.Select((id,i)=>{bool r=(reverse&(1<<i))!=0;var a=anchors[r?i+1:i];var b=anchors[r?i:i+1];return new AreaCompanionNetwork.Item{Id=Id(id),Owner=owner,Prefab=lanePrefab,HasLane=true,HasCurve=true,HasConnection=true,HasSecondary=true,Connection=new ConnectionLane{m_AccessRestriction=owner,m_Flags=ConnectionLaneFlags.Inside|ConnectionLaneFlags.Pedestrian},Lane=new Lane{m_StartNode=new PathNode(a.Id,(ushort)0),m_MiddleNode=new PathNode(a.Id,(ushort)3),m_EndNode=new PathNode(b.Id,(ushort)0)},Curve=new Curve{m_Bezier=NetUtils.StraightCurve(a.Transform.m_Position,b.Transform.m_Position),m_Length=math.distance(a.Transform.m_Position,b.Transform.m_Position)}};}).ToArray();
        return new AreaCompanionNetwork{Owner=owner,Anchors=anchors,Items=items,RootLanes=items.Select(i=>new SubLane{m_SubLane=i.Id,m_PathMethods=(PathMethod)1}).ToArray()};
    }
    static SpawnAccessImage Image(Entity lane,float position)=>new SpawnAccessImage{HasSpawn=true,HasAccess=true,HasRoute=true,HasTakeoff=true,Spawn=new Game.Objects.SpawnLocation{m_ConnectedLane1=lane,m_CurvePosition1=position,m_ConnectedLane2=lane,m_CurvePosition2=position},Access=new Game.Routes.AccessLane{m_Lane=lane,m_CurvePos=position},Route=new Game.Routes.RouteLane{m_StartLane=lane,m_EndLane=lane,m_StartCurvePos=position,m_EndCurvePos=position},References=new[]{new SpawnAccessImage.Reference{Id=lane,HasLane=true}}};
    internal static int Run(){int count=0;void Check(bool ok,string label){if(!ok)throw new Exception("Spawn deletion capture: "+label);count++;}
        var before=Graph(new[]{20,21,22,23},new[]{30,31,32});
        for(int r=0;r<4;r++)foreach(var ids in new[]{new[]{30,31},new[]{31,32},new[]{40,41},new[]{32,30}}){
            var after=Graph(new[]{20,22,23},ids,r);
            Check(OwnedConnectionPolicy.AfterObjectDeletion(before,after,new[]{Id(21)})==null,"legacy projection rejects an interior native replacement");
            var proof=SpawnConnectionDeletion.TryProject(before,after,new[]{Id(21)},out var reason);
            Check(proof!=null,"two-neighbour contraction with reused/fresh IDs and mixed direction: "+reason);
            Check(proof.Removed.SetEquals(new[]{Id(30),Id(31)})&&proof.RetainedMap[Id(32)]==after.Items[1].Id,"removed connections follow semantic ownership rather than reused IDs");
            Check(proof.Network.TryMap(after,true,out _),"proved after image passes ordinary strict companion validation");
            var original=Image(Id(30),.25f);var native=Image(after.Items[0].Id,.7f);var allowed=new HashSet<Entity>(after.Items.Select(i=>i.Id));
            Check(original.NativeConnectionsMatch(native,proof.Removed,allowed,proof.RetainedMap),"native replacement attachment is validated once in live namespace, including same raw ID");
            var unchanged=Image(Id(32),.25f);var unchangedNative=Image(after.Items[1].Id,(r&2)!=0?.75f:.25f);
            Check(unchanged.NativeConnectionsMatch(unchangedNative,proof.Removed,allowed,proof.RetainedMap),"retained references map and complement orientation without double remapping");
            unchangedNative.Access.m_CurvePos=.5f;Check(!unchanged.NativeConnectionsMatch(unchangedNative,proof.Removed,allowed,proof.RetainedMap),"unchanged connection attachment cannot drift");
            native.Takeoff.m_AccessRestriction=Id(999);Check(!original.NativeConnectionsMatch(native,proof.Removed,allowed,proof.RetainedMap),"replacement cannot change unrelated takeoff restrictions");
        }
        var live=Graph(new[]{20,22,23},new[]{30,31});
        live.Items[0].Curve.m_Bezier.b.y+=.02f;Check(SpawnConnectionDeletion.TryProject(before,live,new[]{Id(21)},out var bad)==null&&bad.Contains("replacement geometry"),"bridge geometry change rejected with precise diagnostic");
        live=Graph(new[]{20,22,23},new[]{30,31});live.Items[1].Curve.m_Bezier.b.x+=.02f;Check(SpawnConnectionDeletion.TryProject(before,live,new[]{Id(21)},out _) ==null,"retained geometry change rejected");
        live=Graph(new[]{20,22,23},new[]{30,31});live.Items[0].Connection.m_Flags|=ConnectionLaneFlags.Disabled;Check(SpawnConnectionDeletion.TryProject(before,live,new[]{Id(21)},out _)==null,"bridge flags preserved");
        live=Graph(new[]{20,22,23},new[]{30,31});live.Items[0].Connection.m_AccessRestriction=Id(999);Check(SpawnConnectionDeletion.TryProject(before,live,new[]{Id(21)},out _)==null,"bridge restriction reference preserved");
        live=Graph(new[]{20,22,23},new[]{30,31});live.RootLanes[0].m_PathMethods=(PathMethod)2;Check(SpawnConnectionDeletion.TryProject(before,live,new[]{Id(21)},out _)==null,"bridge path methods preserved");
        live=Graph(new[]{20,22,23},new[]{30,31});live.Anchors[0].Prefab=lanePrefab;Check(SpawnConnectionDeletion.TryProject(before,live,new[]{Id(21)},out _)==null,"bridge cannot change retained anchor prefab");
        live=Graph(new[]{20,22,23},new[]{30,31});live.Items[0].Id=live.Items[1].Id;live.RootLanes[0].m_SubLane=live.Items[0].Id;Check(SpawnConnectionDeletion.TryProject(before,live,new[]{Id(21)},out _)==null,"duplicate entity identities rejected");
        live=Graph(new[]{20,22,23},new[]{30,31});live.Items[0].Lane.m_EndNode=new PathNode(Id(23),(ushort)0);Check(SpawnConnectionDeletion.TryProject(before,live,new[]{Id(21)},out _)==null,"unrelated neighbour pair rejected");
        var leaf=Graph(new[]{21,22,23},new[]{40,41},3);var leafProof=SpawnConnectionDeletion.TryProject(before,leaf,new[]{Id(20)},out _);Check(leafProof!=null&&leafProof.Removed.SetEquals(new[]{Id(30)}),"end deletion permits only proved retained regenerated connections");
        var afterRestore=Graph(new[]{20,21,22,23},new[]{42,40,41},5);var second=Graph(new[]{20,22,23},new[]{41,42},3);Check(SpawnConnectionDeletion.TryProject(afterRestore,second,new[]{Id(21)},out _)!=null,"interior deletion after a previous regenerated restore is captured independently");
        Check(before.Items.Length==3&&before.Items[0].Id==Id(30)&&before.Items[0].Lane.m_EndNode.GetOwnerIndex()==21&&before.Anchors.Length==4,"complete original Undo graph remains unchanged");
        var img=Image(Id(30),.25f);var badImage=Image(Id(99),.5f);var allowedLinks=new HashSet<Entity>{Id(40)};Check(!img.NativeConnectionsMatch(badImage,new HashSet<Entity>{Id(30)},allowedLinks,new Dictionary<Entity,Entity>()),"replacement reference outside proved after graph rejected");
        badImage=Image(Id(40),float.NaN);Check(!img.NativeConnectionsMatch(badImage,new HashSet<Entity>{Id(30)},allowedLinks,new Dictionary<Entity,Entity>()),"nonfinite native replacement position rejected");
        var isolated=img.DetachedCopy();isolated.References[0].Id=Id(99);isolated.RegistrationTypes=new[]{1};Check(img.References[0].Id==Id(30)&&img.RegistrationTypes.Length==0,"adopted native snapshots cannot mutate original reference witnesses or registration entries");
        var unexpected=Image(Id(40),.5f);unexpected.References=new[]{unexpected.References[0],new SpawnAccessImage.Reference{Id=Id(999)}};Check(!img.NativeConnectionsMatch(unexpected,new HashSet<Entity>{Id(30)},allowedLinks,new Dictionary<Entity,Entity>()),"new unrelated reference witness refused");
        return count;
    }
}
