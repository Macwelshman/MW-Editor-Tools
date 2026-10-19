namespace UnityEngine {
 public sealed class SerializeField:System.Attribute{}
 public enum WrapMode {Default}
 public struct Keyframe { public float time,value,inTangent,outTangent,inWeight,outWeight;public int weightedMode; }
 public class AnimationCurve {public Keyframe[] keys;public WrapMode preWrapMode,postWrapMode;public AnimationCurve(params Keyframe[] keys){this.keys=keys;}}
}
namespace Game.Prefabs {
 public class SettingsDto {public string Label;public int[] Values;public PrefabBase Reference;}
 [ComponentMenu(typeof(PrefabBase))] public class RichSettings:ComponentBase {public float Number;public SettingsDto[] Items;public System.Collections.Generic.List<SettingsDto> List;public UnityEngine.AnimationCurve Curve;}
 [ComponentMenu(typeof(PrefabBase))] public class UnsupportedSettings:ComponentBase {public System.Action Callback;}
 [ComponentMenu(typeof(PrefabBase))] public class PrivateSettings:ComponentBase {[UnityEngine.SerializeField] private int setting=4;}
 [ComponentMenu(typeof(PrefabBase))] public class CyclicSettings:ComponentBase {public SettingsDtoLoop Loop;}
 public class SettingsDtoLoop {public SettingsDtoLoop Next;}
}
