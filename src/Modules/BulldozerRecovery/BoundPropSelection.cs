using System;
using System.Linq;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class BoundPropSelection {
        internal static T[] Select<T>(T[] direct,Func<T,bool> hasChildren) where T:class {
            if(direct==null)return Array.Empty<T>();
            var nested=direct.Where(hasChildren).ToArray();
            // Preserve nested-first adoption on mixed assets; simple-only assets use all roots.
            return nested.Length>0?nested:direct.ToArray();
        }
    }
}
