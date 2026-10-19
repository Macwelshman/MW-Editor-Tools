using System;
using MW.EditorTools.History;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class DescriptorPermutation {
        // Native SaveInstance can reorder retained rows. Match the complete value,
        // never just prefab identity, and refuse ambiguous duplicates.
        internal static bool Matches<T>(T[] expected,T[] actual,Func<T,T,bool> equal=null) {
            if(expected==null||actual==null||expected.Length!=actual.Length)return false;
            var used=new bool[actual.Length];
            foreach(var value in expected) {
                var snapshot=PrefabSnapshot.Take(value);int found=-1;
                for(int i=0;i<actual.Length;i++)if(!used[i]&&(equal==null?snapshot.Matches(actual[i]):equal(value,actual[i]))) {
                    if(found!=-1)return false;found=i;
                }
                if(found<0)return false;used[found]=true;
            }
            return true;
        }
    }
}
