using System;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;
using Unity.Mathematics;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed class EffectChild {
        internal Entity Id,Owner;
        internal PrefabBase Prefab;
        internal Game.Objects.Transform Transform;
        internal bool HasLocal;
        internal LocalTransformCache Local;
        // Only while observing an accepted native edit: keep native pose
        // normalization, without accepting replacement, reparenting or settings edits.
        internal static bool NativeSettlement(EffectChild[] before,EffectChild[] after) {
            if(before==null||after==null||before.Select(x=>x.Id).Distinct().Count()!=before.Length)return false;
            var normalized=after.Select(y=>{
                var x=before.SingleOrDefault(b=>b.Id==y.Id);
                if(x==null)return y;
                var local=y.Local;local.m_Position=x.Local.m_Position;local.m_Rotation=x.Local.m_Rotation;
                return new EffectChild{Id=y.Id,Owner=y.Owner,Prefab=y.Prefab,Transform=x.Transform,HasLocal=y.HasLocal,Local=local};
            }).ToArray();
            return after.All(y=>y.Finite)&&Matches(before,normalized);
        }
        private bool Finite=>math.all(math.isfinite(Transform.m_Position))&&math.all(math.isfinite(Transform.m_Rotation.value))&&
            (!HasLocal||(math.all(math.isfinite(Local.m_Position))&&math.all(math.isfinite(Local.m_Rotation.value))));
        internal static bool Matches(EffectChild[] before,EffectChild[] after)=>before!=null&&after!=null&&before.Length==after.Length&&
            before.Select(x=>x.Id).Distinct().Count()==before.Length&&after.Select(x=>x.Id).Distinct().Count()==after.Length&&
            before.All(x=>x.Finite&&!ReferenceEquals(x.Prefab,null)&&after.Count(y=>y.Finite&&x.Id==y.Id&&x.Owner==y.Owner&&ReferenceEquals(x.Prefab,y.Prefab)&&x.Transform.Equals(y.Transform)&&x.HasLocal==y.HasLocal&&(!x.HasLocal||x.Local.Equals(y.Local)))==1);
    }
}
