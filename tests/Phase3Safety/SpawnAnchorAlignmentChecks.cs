using System;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Game.Tools;
namespace MW.EditorTools.Modules.BulldozerRecovery {
 internal sealed partial class BulldozerTrace {
  private class AnchorData {internal Game.Objects.Transform Transform;}
  private class NestedNode {internal Entity Original,Owner;internal Game.Prefabs.PrefabBase Prefab;internal LocalTransformCache Local;internal AnchorData Data;internal int Parent=-1,Depth;}
  internal static int RunSpawnAnchorChecks(){
   int count=0;void Check(bool ok,string label){if(!ok)throw new Exception("Spawn anchor alignment: "+label);count++;}
   void Refuse(Action action,string label){try{action();}catch(NotSupportedException){count++;return;}throw new Exception("Spawn anchor alignment: "+label);}
   Entity Id(int n)=>new Entity{Index=n,Version=1};
   var prefab=(Game.Prefabs.PrefabBase)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Game.Prefabs.ObjectPrefab));
   var pose=new Game.Objects.Transform{m_Position=new float3(2,511.9453f,3),m_Rotation=quaternion.identity};
   var original=new NestedNode{Depth=0,Original=Id(20),Owner=Id(1),Prefab=prefab,Data=new AnchorData{Transform=pose},Local=new LocalTransformCache{m_Position=new float3(2,-0.000030517578125f,3),m_Rotation=quaternion.identity,m_ParentMesh=-1,m_Probability=100,m_PrefabSubIndex=58}};
   var prepared=new NestedNode{Original=original.Original,Owner=original.Owner,Prefab=prefab,Data=original.Data,Local=original.Local};prepared.Local.m_Position.y=0;
   AreaCompanionNetwork Graph()=>new AreaCompanionNetwork{Owner=Id(1),Anchors=new[]{new AreaCompanionNetwork.Anchor{Id=original.Original,Owner=original.Owner,Prefab=prefab,Transform=pose,HasLocal=true,Local=AreaCompanionEffect.DetachedLocal(original.Local)}}};
   var entry=new NetworkEntry{BoundRecord=new object(),Network=Graph(),Props=new[]{new[]{prepared}}};var sourceGraph=entry.Network;
   var trace=new BulldozerTrace();trace.AlignSpawnAnchorExpectations(entry,new[]{new[]{original}});
   Check(!ReferenceEquals(entry.Network,sourceGraph)&&entry.Network.Anchors[0].Local.m_Position.y==0,"spawn anchor uses detached proved source cache expectation");
   Check(sourceGraph.Anchors[0].Local.m_Position.y==original.Local.m_Position.y&&original.Local.m_Position.y<0,"original anchor and prop snapshots stay unchanged");
   Check(entry.Network.Anchors[0].Local.m_PrefabSubIndex==0&&prepared.Local.m_PrefabSubIndex==58,"anchor cache index remains detached without changing prepared prop index");
   var actual=Graph();actual.Anchors[0].Local=AreaCompanionEffect.DetachedLocal(prepared.Local);
   Check(!sourceGraph.TryMap(actual,true,out _)&&entry.Network.TryMap(actual,true,out _),"source-aligned cache fixes anchor comparison while default comparison stays strict");
   var ordinary=new NetworkEntry{Network=Graph(),Props=entry.Props};var ordinaryGraph=ordinary.Network;
   trace.AlignSpawnAnchorExpectations(ordinary,new[]{new[]{original}});Check(ReferenceEquals(ordinary.Network,ordinaryGraph),"ordinary recovery does not adopt spawn anchor expectations");
   var broken=new NetworkEntry{BoundRecord=new object(),Network=Graph(),Props=entry.Props};broken.Network.Anchors[0].Owner=Id(99);
   Refuse(()=>trace.AlignSpawnAnchorExpectations(broken,new[]{new[]{original}}),"changed anchor ownership refused");
   broken=new NetworkEntry{BoundRecord=new object(),Network=Graph(),Props=entry.Props};broken.Network.Anchors[0].Local.m_Probability=50;
   Refuse(()=>trace.AlignSpawnAnchorExpectations(broken,new[]{new[]{original}}),"changed original attachment probability refused");
   broken=new NetworkEntry{BoundRecord=new object(),Network=Graph(),Props=entry.Props};broken.Network.Anchors[0].Transform.m_Position.x++;
   Refuse(()=>trace.AlignSpawnAnchorExpectations(broken,new[]{new[]{original}}),"unproved original world pose refused");
   broken=new NetworkEntry{BoundRecord=new object(),Network=Graph(),Props=entry.Props};
   Refuse(()=>trace.AlignSpawnAnchorExpectations(broken,Array.Empty<NestedNode[]>()),"anchor outside captured forest refused");
   var changed=new NestedNode{Original=prepared.Original,Owner=prepared.Owner,Prefab=prefab,Data=prepared.Data,Local=prepared.Local};changed.Local.m_ParentMesh=0;
   broken=new NetworkEntry{BoundRecord=new object(),Network=Graph(),Props=new[]{new[]{changed}}};
   Refuse(()=>trace.AlignSpawnAnchorExpectations(broken,new[]{new[]{original}}),"prepared attachment change cannot enter aligned anchor expectation");
   var marker=new NetworkEntry{MarkerMove=true,Network=Graph(),Props=entry.Props};trace.AlignSpawnAnchorExpectations(marker,new[]{new[]{original}});
   Check(marker.Network.Anchors[0].Local.m_Position.y==0&&sourceGraph.Anchors[0].Local.m_Position.y<0,"marker move restoration uses aligned detached anchors without changing original deletion snapshot");
   var tiltedPose=pose;tiltedPose.m_Rotation=quaternion.RotateX(.03f);
   var tiltedOriginal=new NestedNode{Original=original.Original,Owner=original.Owner,Prefab=prefab,Local=original.Local,Data=new AnchorData{Transform=tiltedPose}};
   var tiltedPrepared=new NestedNode{Original=original.Original,Owner=original.Owner,Prefab=prefab,Local=original.Local,Data=tiltedOriginal.Data};tiltedPrepared.Local.m_Rotation=tiltedPose.m_Rotation;
   var tiltedGraph=Graph();tiltedGraph.Anchors[0].Transform=tiltedPose;
   var rotationEntry=new NetworkEntry{MarkerMove=true,Transform=new Game.Objects.Transform{m_Rotation=quaternion.identity},Network=tiltedGraph,Props=new[]{new[]{tiltedPrepared}}};
   trace.AlignSpawnAnchorExpectations(rotationEntry,new[]{new[]{tiltedOriginal}});
   Check(rotationEntry.Network.Anchors[0].Local.m_Rotation.Equals(tiltedPose.m_Rotation)&&tiltedGraph.Anchors[0].Local.m_Rotation.Equals(original.Local.m_Rotation),"native source rotation aligns only a detached anchor expectation");
   tiltedPrepared.Local.m_Rotation=quaternion.RotateX(.031f);
   rotationEntry.Network=tiltedGraph;
   Refuse(()=>trace.AlignSpawnAnchorExpectations(rotationEntry,new[]{new[]{tiltedOriginal}}),"arbitrary prepared local rotation remains refused");
   return count;
  }
 }
}
