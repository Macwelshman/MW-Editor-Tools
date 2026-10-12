using System;using System.Collections;
namespace MW.EditorTools.Modules.BulldozerRecovery {internal sealed partial class BulldozerTrace {
        private static bool EmptyPreparationField(string component,string field,object value){
            if(component=="ObjectSubNets"&&field=="m_InvertWhen")return value!=null&&Convert.ToInt64(value)==0;
            bool list=(component=="ObjectSubAreas"&&field=="m_SubAreas")||(component=="ObjectSubNets"&&field=="m_SubNets")||(component=="ObjectSubLanes"&&field=="m_SubLanes")||(component=="EffectSource"&&(field=="m_Effects"||field=="m_AnimationCurves"));
            return list&&(value==null||(value is ICollection collection&&collection.Count==0));
        }
}}

