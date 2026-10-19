using System;
using System.Collections.Generic;
using MW.EditorTools.History;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class NormalizedAreaFields {
        // Call only after the retained live companions have matched their
        // before-images across the native stability window.
        internal static void Apply(Dictionary<string,PrefabSnapshot> before,Dictionary<string,PrefabSnapshot> after,Action<string,PrefabSnapshot> replace) {
            foreach(var key in new[]{"ObjectSubNets.m_SubNets","EffectSource.m_Effects"})
                if(before.ContainsKey(key)&&after.TryGetValue(key,out var value))replace(key,value);
        }
    }
}
