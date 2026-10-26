using System;
using System.Linq;
using System.Globalization;
using System.Text;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;

namespace MW.EditorTools.Modules.PropReplacer {
 public sealed partial class PropReplacerSystem {
  private Entity lastNetworkRowDiagnostic;
  private static readonly bool detailedNetworkDiagnostics=string.Equals(Environment.GetEnvironmentVariable("MW_EDITOR_TOOLS_DIAGNOSTICS"),"1",StringComparison.Ordinal);
  private static string NetworkPointText(Unity.Mathematics.float3 p)=>"("+p.x.ToString("R",CultureInfo.InvariantCulture)+","+p.y.ToString("R",CultureInfo.InvariantCulture)+","+p.z.ToString("R",CultureInfo.InvariantCulture)+")";
  private static string NetworkCurveText(Colossal.Mathematics.Bezier4x3 c)=>"[a="+NetworkPointText(c.a)+" b="+NetworkPointText(c.b)+" c="+NetworkPointText(c.c)+" d="+NetworkPointText(c.d)+"]";
  private void TraceNetworkRowFailure(Row[] rows,Entity id,Entity owner) {
   if(!detailedNetworkDiagnostics||id==lastNetworkRowDiagnostic)return;
   lastNetworkRowDiagnostic=id;
   try {
    var em=World.EntityManager;var text=new StringBuilder("Network list mapping detail: selected=").Append(id).Append(" owner=").Append(owner);
    if(em.HasComponent<Game.Objects.Transform>(owner)){var pose=em.GetComponentData<Game.Objects.Transform>(owner);text.Append(" ownerPosition=").Append(pose.m_Position).Append(" ownerRotation=").Append(pose.m_Rotation);}
    if(em.HasComponent<Game.Net.Curve>(id))text.Append(" worldCurve=").Append(NetworkCurveText(em.GetComponentData<Game.Net.Curve>(id).m_Bezier));
    if(em.HasComponent<LocalCurveCache>(id))text.Append(" cachedCurve=").Append(NetworkCurveText(em.GetComponentData<LocalCurveCache>(id).m_Curve));else text.Append(" NO_LOCAL_CURVE");
    if(em.HasComponent<Game.Net.Curve>(id)&&em.HasComponent<Game.Objects.Transform>(owner)){Colossal.Mathematics.Bezier4x3? local=em.HasComponent<LocalCurveCache>(id)?(Colossal.Mathematics.Bezier4x3?)em.GetComponentData<LocalCurveCache>(id).m_Curve:null;try{text.Append(" projectedCurve=").Append(NetworkCurveText(ViewportGeometrySource.Curve(em.GetComponentData<Game.Net.Curve>(id).m_Bezier,local,em.GetComponentData<Game.Objects.Transform>(owner))));}catch(Exception e){text.Append(" projectionError=").Append(e.Message);}}
    if(em.HasComponent<LocalTransformCache>(id))text.Append(" cachedPoint=").Append(em.GetComponentData<LocalTransformCache>(id).m_Position);
    if(em.HasComponent<Game.Net.Edge>(id)){var edge=em.GetComponentData<Game.Net.Edge>(id);text.Append(" start=").Append(edge.m_Start).Append(" end=").Append(edge.m_End).Append(" meshes=").Append(ViewportParentMesh(edge.m_Start)).Append(",").Append(ViewportParentMesh(edge.m_End));}
    var root=em.GetComponentData<PrefabRef>(owner).m_Prefab;
    text.Append(" root=").Append(root).Append(" rootUpdated=").Append(em.HasComponent<Game.Common.Updated>(root)).Append(" rootCreated=").Append(em.HasComponent<Game.Common.Created>(root));
    text.Append(" childPrefab=").Append(em.GetComponentData<PrefabRef>(id).m_Prefab);
    if(em.HasComponent<Game.Tools.EditorContainer>(id))text.Append(" containerPrefab=").Append(em.GetComponentData<Game.Tools.EditorContainer>(id).m_Prefab);
    text.Append(" compiledNets=").Append(em.HasBuffer<Game.Prefabs.SubNet>(root)?em.GetBuffer<Game.Prefabs.SubNet>(root,true).Length:-1);
    text.Append(" compiledLanes=").Append(em.HasBuffer<Game.Prefabs.SubLane>(root)?em.GetBuffer<Game.Prefabs.SubLane>(root,true).Length:-1);
    foreach(var row in rows.Take(12)) {
     text.Append(" | row=").Append(row.Index);
     if(row.Value is ObjectSubNetInfo net){text.Append(" net=").Append(net.m_NetPrefab?.name).Append(" sourceCurve=").Append(NetworkCurveText(net.m_BezierCurve)).Append(" sourceNodes=").Append(net.m_NodeIndex).Append(" sourceMeshes=").Append(net.m_ParentMesh);if(em.HasBuffer<Game.Prefabs.SubNet>(root)){var b=em.GetBuffer<Game.Prefabs.SubNet>(root,true);if(row.Index>=0&&row.Index<b.Length){var v=b[row.Index];text.Append(" compiledPrefab=").Append(v.m_Prefab).Append(" compiledCurve=").Append(NetworkCurveText(v.m_Curve)).Append(" compiledNodes=").Append(v.m_NodeIndex).Append(" compiledMeshes=").Append(v.m_ParentMesh);}}}
     else if(row.Value is ObjectSubLaneInfo lane){text.Append(" lane=").Append(lane.m_LanePrefab?.name).Append(" sourceCurve=").Append(NetworkCurveText(lane.m_BezierCurve)).Append(" sourceNodes=").Append(lane.m_NodeIndex).Append(" sourceMeshes=").Append(lane.m_ParentMesh);if(em.HasBuffer<Game.Prefabs.SubLane>(root)){var b=em.GetBuffer<Game.Prefabs.SubLane>(root,true);if(row.Index>=0&&row.Index<b.Length){var v=b[row.Index];text.Append(" compiledPrefab=").Append(v.m_Prefab).Append(" compiledCurve=").Append(NetworkCurveText(v.m_Curve)).Append(" compiledNodes=").Append(v.m_NodeIndex).Append(" compiledMeshes=").Append(v.m_ParentMesh);}}}
    }
    MW.EditorTools.Mod.Log.Info(text.ToString());
   }catch(Exception error){MW.EditorTools.Mod.Log.Info("Network row diagnostic unavailable: "+error.Message);}
  }
 }
}
