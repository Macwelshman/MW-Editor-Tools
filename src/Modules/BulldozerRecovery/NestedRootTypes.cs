using System;
using System.Linq;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class NestedRootTypes {
        internal static T[] Copy<T>(T[] types) where T:class {
            if(types==null||types.Length<1||types.Length>4||types.Any(type=>ReferenceEquals(type,null)))throw new NotSupportedException("Nested recovery requires one to four root prefab identities.");
            for(int i=0;i<types.Length;i++)for(int j=0;j<i;j++)if(ReferenceEquals(types[i],types[j]))throw new NotSupportedException("Duplicate nested root prefab identity.");
            return types.ToArray();
        }
    }
}
