using System;
using System.Collections.Generic;
namespace MW.EditorTools.Modules.PropReplacer {
 internal interface IViewportTargetSource<T> { bool TryGetTarget(out T entity, out string reason); }
 internal interface IViewportHoverSource<T> { bool TryGetHoverTarget(out T entity, out string reason); }
 internal static class ViewportHitPolicy {
  internal static bool Normalize<T>(T value,Func<T,bool> live,Func<T,Tuple<bool,T>> redirect,out T result) {
   var seen=new HashSet<T>();
   for(int depth=0;depth<5;depth++){
    if(!live(value)||!seen.Add(value)){result=default(T);return false;}
    var next=redirect(value);if(!next.Item1){result=value;return true;}value=next.Item2;
   }
   result=default(T);return false;
  }
  internal static bool Agrees<T>(T hit,T rayOwner,T highlighted,Func<T,bool> live,Func<T,Tuple<bool,T>> parent,Func<T,T,int> links) {
   if(!live(highlighted)||!MatchesPath(hit,rayOwner,live,parent,links))return false;
   // The current hit must either belong to the highlighted owner or be that
   // exact highlighted child. Sibling/other-asset agreement is insufficient.
   return EqualityComparer<T>.Default.Equals(rayOwner,highlighted)||EqualityComparer<T>.Default.Equals(hit,highlighted);
  }
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
