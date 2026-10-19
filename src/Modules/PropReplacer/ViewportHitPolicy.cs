using System;
using System.Collections.Generic;
namespace MW.EditorTools.Modules.PropReplacer {
 internal static class ViewportHitPolicy {
  internal static bool MatchesPath<T>(T hit,T owner,Func<T,bool> live,Func<T,Tuple<bool,T>> parent,Func<T,T,int> links) {
   var seen=new HashSet<T>();var current=hit;
   for(int depth=0;depth<9;depth++){
    if(!live(current)||!seen.Add(current))return false;
    if(EqualityComparer<T>.Default.Equals(current,owner))return true;
    var next=parent(current);if(!next.Item1||links(next.Item2,current)!=1)return false;
    current=next.Item2;
   }
   return false;
  }
 }
}
