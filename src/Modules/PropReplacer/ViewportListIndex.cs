using System.Collections.Generic;
namespace MW.EditorTools.Modules.PropReplacer {
 internal static class ViewportListIndex {
  internal static int Resolve(IList<int> filtered,int length,int source) {
   if(length<0||length>4096||source<0)return -1;
   if(filtered==null)return source<length?source:-1;
   if(filtered.Count!=length)return -1;
   int found=-1;
   for(int i=0;i<filtered.Count;i++)if(filtered[i]==source){if(found>=0)return -1;found=i;}
   return found;
  }
 }
}
