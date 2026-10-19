using Unity.Mathematics;
using Game.Objects;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class PropMovePolicy {
        internal enum CommitAction { Ignore,Wait,Clear }
        internal static CommitAction Commit(bool accepted,bool apply,bool move,bool captured)=>!accepted||!apply?CommitAction.Ignore:move&&captured?CommitAction.Wait:CommitAction.Clear;
        internal static bool Changed(Transform before,Transform after)=>!SamePose(before,after);
        internal static bool SamePose(Transform a,Transform b)=>Finite(a)&&Finite(b)&&math.lengthsq(a.m_Position-b.m_Position)<1e-8f&&
            math.min(math.lengthsq(a.m_Rotation.value-b.m_Rotation.value),math.lengthsq(a.m_Rotation.value+b.m_Rotation.value))<1e-8f;
        internal static bool Finite(Transform value)=>math.all(math.isfinite(value.m_Position))&&math.all(math.isfinite(value.m_Rotation.value))&&math.abs(math.lengthsq(value.m_Rotation.value)-1)<0.001f;
        internal static bool ChildPose(Transform beforeRoot,Transform afterRoot,Transform before,Transform after) {
            if(!Finite(beforeRoot)||!Finite(afterRoot)||!Finite(before)||!Finite(after))return false;
            var rotation=math.mul(afterRoot.m_Rotation,math.inverse(beforeRoot.m_Rotation));
            var position=afterRoot.m_Position+math.mul(rotation,before.m_Position-beforeRoot.m_Position);
            var expected=math.mul(rotation,before.m_Rotation);
            return math.all(math.abs(position-after.m_Position)<0.0005f)&&
                math.min(math.lengthsq(expected.value-after.m_Rotation.value),math.lengthsq(expected.value+after.m_Rotation.value))<1e-8f;
        }
    }
}
