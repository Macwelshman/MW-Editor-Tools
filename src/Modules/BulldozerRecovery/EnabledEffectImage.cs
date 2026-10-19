using System;
using System.Linq;
using Game.Effects;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class EnabledEffectImage {
        // EnabledIndex belongs to native effect allocation/cleanup. Never restore
        // an old allocation index onto a regenerated object.
        internal static int[] Capture(EnabledEffect[] effects) {
            if(effects==null||effects.Length>256)throw new NotSupportedException("Enabled effect buffer exceeds budget or is missing.");
            return effects.Select(e=>e.m_EffectIndex).OrderBy(i=>i).ToArray();
        }
    }
}
