using System;
using Game.Prefabs;
namespace MW.EditorTools.Modules.PropReplacer {
 internal static class ViewportRowType {
  internal static Type Resolve(bool effectContainer,bool laneContainer,bool area,bool network,bool lane) =>
   effectContainer?typeof(EffectSource.EffectSettings):laneContainer?typeof(ObjectSubLaneInfo):area?typeof(ObjectSubAreaInfo):network?typeof(ObjectSubNetInfo):lane?typeof(ObjectSubLaneInfo):typeof(ObjectSubObjectInfo);
 }
}
