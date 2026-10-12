using System;
using System.Collections.Generic;
using Game.Input;
namespace MW.EditorTools.Settings
{
    internal static class NativeShortcutCodec
    {
        internal static ProxyBinding Restore(ProxyBinding template, string key, int mask)
        {
            var modifiers = new List<ProxyModifier>();
            // Keep the composite's native modifier name/component metadata.
            var prototype = template.modifiers[0];
            string[] paths = {"ctrl","shift","alt","meta"};
            for(int i=0;i<4;i++) if ((mask & (1<<i)) != 0) {
                var modifier=prototype; modifier.m_Path="<Keyboard>/"+paths[i]; modifiers.Add(modifier);
            }
            return template.WithPath("<Keyboard>/"+key.ToLowerInvariant()).WithModifiers(modifiers);
        }
        internal static bool TryRead(ProxyBinding binding, out string key, out int mask)
        {
            key=null; mask=0;
            if (!binding.isKeyboard || string.IsNullOrEmpty(binding.path) || !binding.path.StartsWith("<Keyboard>/",StringComparison.OrdinalIgnoreCase)) return false;
            key=binding.path.Substring(11).ToUpperInvariant();
            if (key.Length!=1 || key[0]<'A' || key[0]>'Z') return false;
            foreach(var modifier in binding.modifiers) {
                var path=(modifier.m_Path ?? "").ToLowerInvariant();
                if (!path.StartsWith("<keyboard>/")) return false;
                var name=path.Substring(11);
                switch(name) {
                    case "ctrl": case "leftctrl": case "rightctrl": mask|=1; break;
                    case "shift": case "leftshift": case "rightshift": mask|=2; break;
                    case "alt": case "leftalt": case "rightalt": mask|=4; break;
                    case "meta": case "leftmeta": case "rightmeta": mask|=8; break;
                    default: return false;
                }
            }
            return true;
        }
    }
}
