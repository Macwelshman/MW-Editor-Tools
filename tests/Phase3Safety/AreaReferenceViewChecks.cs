using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Game.Prefabs;
using Game.Tools;
namespace MW.EditorTools.Modules.BulldozerRecovery {
 internal sealed partial class BulldozerTrace {
  private class AreaImage {
   internal Entity Original;internal AreaPrefab Prefab;internal Game.Areas.Node[] Nodes;internal LocalNodeCache[] Local;internal int Metadata;
   internal AreaImage WithLocal(LocalNodeCache[] value){var copy=(AreaImage)MemberwiseClone();copy.Local=value;return copy;}
  }
  private class BoundCompanions {internal AreaImage[] Areas;}
  private class Payload {internal ObjectSubAreaInfo[] Rows;internal bool Valid=true;internal bool Matches(PrefabBase asset)=>Valid;}
  private class NetworkEntry {internal object BoundRecord;internal bool MarkerMove;internal AreaCompanionNetwork Network;internal NestedNode[][] Props;internal bool AreaMove=true;internal BoundCompanions CompanionMap;internal Payload Before;internal PrefabBase Asset;internal Game.Objects.Transform Transform;}
  private bool networkRestoring=true,ownerValid=true;private NetworkEntry networkPending;private AreaImage[] liveAreas;
  private BoundCompanions CaptureBoundCompanions(Entity owner,bool lanes,bool invalid)=>new BoundCompanions{Areas=liveAreas};
  private bool NetworkOwnerValid(NetworkEntry entry)=>ownerValid;
  private static T[] NetworkRows<T>(Payload image,string key)=>image.Rows as T[];
  private static bool AreaEqual(AreaImage a,AreaImage b)=>ReferenceEquals(a.Prefab,b.Prefab)&&a.Metadata==b.Metadata&&AreaGeometry.Matches(a.Nodes,a.Local,b.Nodes,b.Local);
  private bool MapAreaLanes(BoundCompanions expected,BoundCompanions actual,Dictionary<Entity,Entity> map,bool seeds,bool native,bool strictLocal)=>true;
  internal static int RunAreaReferenceChecks(){
   int checks=0;void Check(bool ok,string label){if(!ok)throw new Exception("Area reference view: "+label);checks++;}
   void Refuse(Action action,string label){try{action();}catch(NotSupportedException){checks++;return;}throw new Exception("Area reference view: "+label);}
   var prefab=(AreaPrefab)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(AreaPrefab));
   BulldozerTrace Case(){
    var nodes=new[]{new Game.Areas.Node{m_Position=new float3(0,0,0)},new Game.Areas.Node{m_Position=new float3(4,0,0)},new Game.Areas.Node{m_Position=new float3(0,0,4)}};
    var cached=nodes.Select(n=>new LocalNodeCache{m_Position=n.m_Position+new float3(0,0.02f,0),m_ParentMesh=-1}).ToArray();
    var area=new AreaImage{Original=new Entity{Index=1,Version=1},Prefab=prefab,Nodes=nodes,Local=cached};
    var before=new BoundCompanions{Areas=new[]{area}};
    var actual=area.WithLocal(nodes.Select(n=>new LocalNodeCache{m_Position=n.m_Position,m_ParentMesh=-1}).ToArray());
    actual.Original=new Entity{Index=2,Version=1};
    return new BulldozerTrace{liveAreas=new[]{actual},networkPending=new NetworkEntry{Asset=prefab,CompanionMap=before,Before=new Payload{Rows=new[]{new ObjectSubAreaInfo{m_AreaPrefab=prefab,m_NodePositions=nodes.Select(n=>n.m_Position).ToArray(),m_ParentMeshes=new[]{-1,-1,-1}}}},Transform=new Game.Objects.Transform{m_Rotation=quaternion.identity}}};
   }
   var trace=Case();var expected=trace.networkPending.CompanionMap;
   Check(!AreaEqual(expected.Areas[0],trace.liveAreas[0]),"raw native regenerated cache differs");
   var view=trace.CaptureAreaReferenceView(expected,Entity.Null,true);
   Check(AreaEqual(expected.Areas[0],view.Areas[0])&&view.Areas[0].Original==trace.liveAreas[0].Original,"actual-source proof view preserves native identity with verified original cache expectation");
   Check(trace.liveAreas[0].Local[0].m_Position.y==0&&expected.Areas[0].Local[0].m_Position.y==0.02f,"proof view writes neither live cache nor captured before image");
   Check(trace.AreaReferenceMap(expected,Entity.Null,new Dictionary<Entity,Entity>(),true,false),"reference mapping can precede guarded cache restoration");
   trace=Case();trace.networkRestoring=false;view=trace.CaptureAreaReferenceView(trace.networkPending.CompanionMap,Entity.Null,true);
   Check(!AreaEqual(trace.networkPending.CompanionMap.Areas[0],view.Areas[0]),"ordinary capture cannot use restoration proof view");
   trace=Case();trace.networkPending.AreaMove=false;view=trace.CaptureAreaReferenceView(trace.networkPending.CompanionMap,Entity.Null,true);
   Check(!AreaEqual(trace.networkPending.CompanionMap.Areas[0],view.Areas[0]),"prop/effect restoration cannot use area move cache exception");
   trace=Case();trace.networkPending.Before.Valid=false;Refuse(()=>trace.CaptureAreaReferenceView(trace.networkPending.CompanionMap,Entity.Null,true),"changed source refuses read normalization");
   trace=Case();trace.ownerValid=false;Refuse(()=>trace.CaptureAreaReferenceView(trace.networkPending.CompanionMap,Entity.Null,true),"changed owner refuses read normalization");
   trace=Case();trace.liveAreas[0].Metadata=1;Refuse(()=>trace.CaptureAreaReferenceView(trace.networkPending.CompanionMap,Entity.Null,true),"area metadata change refused");
   trace=Case();trace.liveAreas[0].Local[0].m_ParentMesh=0;Refuse(()=>trace.CaptureAreaReferenceView(trace.networkPending.CompanionMap,Entity.Null,true),"area attachment change refused");
   trace=Case();trace.liveAreas[0].Local[0].m_Position.y=0.003f;Refuse(()=>trace.CaptureAreaReferenceView(trace.networkPending.CompanionMap,Entity.Null,true),"unproved finite regenerated cache refused");
   trace=Case();trace.networkPending.Before.Rows[0].m_NodePositions[0].y=1;Refuse(()=>trace.CaptureAreaReferenceView(trace.networkPending.CompanionMap,Entity.Null,true),"target source must match captured geometry calculation");
   trace=Case();trace.liveAreas=new[]{trace.liveAreas[0],trace.liveAreas[0]};Refuse(()=>trace.CaptureAreaReferenceView(trace.networkPending.CompanionMap,Entity.Null,true),"extra area count refused");
   trace=Case();trace.liveAreas[0].Local[0].m_Position.y=float.NaN;Refuse(()=>trace.CaptureAreaReferenceView(trace.networkPending.CompanionMap,Entity.Null,true),"nonfinite regenerated node cache refused");
   return checks;
  }
 }
}
