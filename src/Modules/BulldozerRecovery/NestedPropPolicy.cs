using System;
using System.Linq;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class NestedPropPolicy {
        internal static bool Accept(int[] parents,int[] depths,object[] prefabs,bool[] deterministicLinks){
            if(parents==null||depths==null||prefabs==null||deterministicLinks==null)return false;
            int count=parents.Length;
            if(count<2||count>8||depths.Length!=count||prefabs.Length!=count||deterministicLinks.Length!=count)return false;
            for(int i=0;i<count;i++){
                if(prefabs[i]==null||!deterministicLinks[i]||Enumerable.Range(0,i).Any(j=>ReferenceEquals(prefabs[j],prefabs[i])))return false;
                if(i==0){if(parents[i]!=-1||depths[i]!=0)return false;}
                else if(parents[i]<0||parents[i]>=i||depths[i]!=depths[parents[i]]+1||depths[i]>4)return false;
            }
            return true;
        }
    }
}
