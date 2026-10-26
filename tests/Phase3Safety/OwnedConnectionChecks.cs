using System;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Game.Net;
using Game.Pathfind;
using MW.EditorTools.Modules.BulldozerRecovery;
internal static class OwnedConnectionChecks {
 static Entity Id(int n)=>new Entity{Index=n,Version=1};
 static readonly Game.Prefabs.PrefabBase prefab=(Game.Prefabs.PrefabBase)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Game.Prefabs.ObjectPrefab));
 static AreaCompanionNetwork Graph(bool deleted=false){
  AreaCompanionNetwork.Anchor Anchor(int n)=>new AreaCompanionNetwork.Anchor{Id=Id(n),Owner=Id(1),Prefab=prefab,Transform=new Game.Objects.Transform{m_Position=new float3(n,0,0),m_Rotation=quaternion.identity}};
  AreaCompanionNetwork.Item Lane(int n,int start,int end)=>new AreaCompanionNetwork.Item{Id=Id(n),Owner=Id(1),HasLane=true,HasCurve=true,HasSeed=true,Seed=new Game.Common.PseudoRandomSeed{m_Seed=7},Lane=new Lane{m_StartNode=new PathNode(Id(start),(ushort)2),m_MiddleNode=new PathNode(Id(start),(ushort)3),m_EndNode=new PathNode(Id(end),(ushort)2)},Curve=new Curve{m_Bezier=new Colossal.Mathematics.Bezier4x3{a=new float3(n,0,0),b=new float3(n,0,1),c=new float3(n,0,2),d=new float3(n,0,3)},m_Length=3}};
  return new AreaCompanionNetwork{Owner=Id(1),Anchors=(deleted?new[]{21,22}:new[]{20,21,22}).Select(Anchor).ToArray(),RootLanes=(deleted?new[]{31}:new[]{30,31}).Select(n=>new SubLane{m_SubLane=Id(n)}).ToArray(),Items=(deleted?new[]{Lane(31,21,22)}:new[]{Lane(30,20,21),Lane(31,21,22)})};
 }
 internal static int Run(){int checks=0;void Check(bool ok,string label){if(!ok)throw new Exception("Owned connection: "+label);checks++;}
  var before=Graph();var after=Graph(true);
  Check(before.TryMap(before,true,out _),"complete source graph is closed");
  Check(!before.TryMap(after,false,out _),"strict unchanged graph refuses deleted connection");
  var projected=OwnedConnectionPolicy.AfterObjectDeletion(before,after,new[]{Id(20)});
  Check(projected!=null&&projected.TryMap(after,true,out _),"only target-owned generated connection and unused anchor disappear");
  Check(before.Items.Length==2&&before.Anchors.Length==3&&before.RootLanes.Length==2,"projection never mutates full restoration image");
  Check(OwnedConnectionPolicy.AfterObjectDeletion(before,after,new[]{Id(21)})==null,"surviving lane still using deleted anchor refuses");
  Check(OwnedConnectionPolicy.AfterObjectDeletion(before,after,new[]{Id(99)})==null,"unrelated deleted object cannot authorize lane loss");
  var changed=Graph(true);changed.Items[0].Curve.m_Bezier.b.x++;
  Check(OwnedConnectionPolicy.AfterObjectDeletion(before,changed,new[]{Id(20)})==null,"retained curve change refused");
  changed=Graph(true);changed.Items[0].Lane.m_EndNode=new PathNode(Id(21),(ushort)2);
  Check(OwnedConnectionPolicy.AfterObjectDeletion(before,changed,new[]{Id(20)})==null,"retained connection endpoint rewrite refused");
  changed=Graph(true);changed.Anchors[0].Owner=Id(9);
  Check(OwnedConnectionPolicy.AfterObjectDeletion(before,changed,new[]{Id(20)})==null,"retained anchor ownership change refused");
  changed=Graph(true);changed.Items[0].Id=new Entity{Index=31,Version=2};changed.RootLanes[0].m_SubLane=changed.Items[0].Id;
  Check(OwnedConnectionPolicy.AfterObjectDeletion(before,changed,new[]{Id(20)})==null,"retained connection identity replacement refused");
  changed=Graph(true);changed.Items[0].Seed.m_Seed=99;
  Check(OwnedConnectionPolicy.AfterObjectDeletion(before,changed,new[]{Id(20)})!=null&&!projected.TryMap(changed,true,out _),"seed repair preflight preserves strict final seed requirement");
  var source=Graph();source.Items[0].HasEdge=true;
  Check(OwnedConnectionPolicy.AfterObjectDeletion(source,after,new[]{Id(20)})==null,"editable edge cannot disappear as generated connection");
  source=Graph();source.RootNets=new[]{Id(30)};
  Check(OwnedConnectionPolicy.AfterObjectDeletion(source,after,new[]{Id(20)})==null,"editable root network cannot disappear");
  source=Graph();source.Items[0].HasLocal=true;
  Check(OwnedConnectionPolicy.AfterObjectDeletion(source,after,new[]{Id(20)})==null,"cached source connection removal remains unsupported");
  source=Graph();source.Items[0].HasContainer=true;
  Check(OwnedConnectionPolicy.AfterObjectDeletion(source,after,new[]{Id(20)})==null,"editor container cannot disappear");
  changed=Graph(true);changed.Items=Array.Empty<AreaCompanionNetwork.Item>();changed.RootLanes=Array.Empty<SubLane>();changed.Anchors=Array.Empty<AreaCompanionNetwork.Anchor>();
  Check(OwnedConnectionPolicy.AfterObjectDeletion(before,changed,new[]{Id(20)})==null,"unrelated retained lane deletion refused");
  // The edited area's source and polygon shape are proved by the caller. This
  // graph accepts only the same generated lane identities and path/seed metadata.
  AreaCompanionNetwork Area(){var lane=Graph(true).Items[0];lane.Owner=Id(50);lane.Lane=new Lane{m_StartNode=new PathNode(Id(50),(ushort)1),m_EndNode=new PathNode(Id(50),(ushort)2)};return new AreaCompanionNetwork{Owner=Id(50),Items=new[]{lane},RootLanes=new[]{new SubLane{m_SubLane=lane.Id}}};}
  var original=Area();var moved=Area();moved.Items[0].Curve.m_Bezier.b.y+=4;moved.Items[0].Curve.m_Length+=1;
  Check(!original.TryMap(moved,true,out _)&&OwnedConnectionPolicy.MapEditedArea(original,moved,out var map)&&map[Id(31)]==Id(31),"proved edited area allows derived geometry change on exact generated lane identity");
  Check(original.Items[0].Curve.m_Length==3,"edited-area mapping does not mutate original geometry");
  moved=Area();moved.Items[0].Lane.m_EndNode=new PathNode(Id(50),(ushort)8);
  Check(!OwnedConnectionPolicy.MapEditedArea(original,moved,out _),"area edit preserves path node index bits");
  moved=Area();moved.Items[0].Owner=Id(99);Check(!OwnedConnectionPolicy.MapEditedArea(original,moved,out _),"area edit cannot reparent lanes");
  moved=Area();moved.Items[0].Seed.m_Seed++;Check(!OwnedConnectionPolicy.MapEditedArea(original,moved,out _),"area edit preserves seed");
  moved=Area();moved.Items[0].Curve.m_Bezier.b.y=float.NaN;Check(!OwnedConnectionPolicy.MapEditedArea(original,moved,out _),"area edit refuses nonfinite generated geometry");
  moved=Area();moved.Items[0].Id=Id(99);moved.RootLanes[0].m_SubLane=Id(99);moved.Items[0].Curve.m_Bezier.b.y++;Check(OwnedConnectionPolicy.MapEditedArea(original,moved,out var regenerated)&&regenerated[Id(31)]==Id(99),"area edit remaps regenerated identity only through unique indexed owner path proof");
  moved=Area();moved.Items[0].HasEdge=true;Check(!OwnedConnectionPolicy.MapEditedArea(original,moved,out _),"area edit cannot turn generated lane into editable edge");
  moved=Area();moved.Items[0].HasLocal=true;Check(!OwnedConnectionPolicy.MapEditedArea(original,moved,out _),"area edit preserves local cache schema");
  moved=Area();moved.RootLanes[0].m_PathMethods=(Game.Pathfind.PathMethod)1;Check(!OwnedConnectionPolicy.MapEditedArea(original,moved,out _),"area edit preserves path methods");
  moved=Area();var duplicate=moved.Items[0].DetachedCopy();duplicate.Id=Id(99);moved.Items=new[]{moved.Items[0],duplicate};moved.RootLanes=new[]{new SubLane{m_SubLane=Id(31)},new SubLane{m_SubLane=Id(99)}};
  var ambiguous=Area();var other=ambiguous.Items[0].DetachedCopy();other.Id=Id(98);ambiguous.Items=new[]{ambiguous.Items[0],other};ambiguous.RootLanes=new[]{new SubLane{m_SubLane=Id(31)},new SubLane{m_SubLane=Id(98)}};moved.Items[0].Curve.m_Bezier.b.y++;moved.Items[1].Curve.m_Bezier.b.y+=2;
  Check(!OwnedConnectionPolicy.MapEditedArea(ambiguous,moved,out _),"duplicate indexed area paths cannot authorize geometry remapping");
  return checks;
 }
}
