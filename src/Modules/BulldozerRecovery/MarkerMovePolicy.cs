using System;
using Game.Prefabs;
using Game.Tools;
using Unity.Mathematics;
using MW.EditorTools.History;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class MarkerMovePolicy {
        internal static bool Supports(string name,bool bound,int nodes,bool marker,bool spawn,bool takeoff,bool access,bool route,bool lanes)=>bound&&nodes==1&&marker&&!lanes&&
            ((name=="Restricted Spawn Location"&&spawn&&!takeoff&&!access&&!route)||(name=="Pedestrian Access Location"&&!spawn&&takeoff&&access&&route));
        internal static bool SourceRows(ObjectSubObjectInfo[] before,ObjectSubObjectInfo[] after,int oldIndex,int newIndex,PrefabBase[] prefabs,LocalTransformCache[] locals,Game.Objects.Transform[] worlds,Game.Objects.Transform owner){
            if(before==null||after==null||before.Length!=after.Length||oldIndex<0||newIndex<0||oldIndex>=before.Length||newIndex>=after.Length||before[oldIndex]==null||after[newIndex]==null)return false;
            var expected=(ObjectSubObjectInfo[])PrefabSnapshot.Take(before).Materialize();var original=expected[oldIndex];var moved=after[newIndex];
            original.m_Position=moved.m_Position;original.m_Rotation=moved.m_Rotation;original.m_ParentMesh=moved.m_ParentMesh;
            if(!PrefabSnapshot.Take(original).Matches(moved))return false;
            return BoundDescriptorIndex.NativeRows(expected,after,prefabs,locals,worlds,owner);
        }
    }
}
