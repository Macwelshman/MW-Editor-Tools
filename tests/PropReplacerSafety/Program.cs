using System;
using MW.EditorTools.Modules.PropReplacer;
class Program {
 static void Main(){
  var indices=new[]{0,1,8};var names=new[]{"Fence","Fence","Bush"};
  Check(HoverRowPolicy.Resolve(new[]{"Fence","Element 1Fence"},indices,names)==1,"Duplicate prefab resolves exact underlying row");
  Check(HoverRowPolicy.Resolve(new[]{"Fence"},indices,names)==-1,"Repeated prefab without index refused");
  Check(HoverRowPolicy.Resolve(new[]{"Fence","Element 0FenceElement 1Fence"},indices,names)==-1,"Container containing several row indices refused");
  Check(HoverRowPolicy.Resolve(new[]{"Bush"},indices,names)==2,"Unique prefab resolves");
  Check(HoverRowPolicy.Resolve(new[]{"Fence","Element 9Fence"},indices,names)==-1,"Stale row number refused");
  Check(HoverRowPolicy.Resolve(new[]{"Element 1","Fence"},indices,names)==1,"Number label and exact prefab label resolve");
  Check(HoverRowPolicy.Resolve(new[]{"Missing"},indices,names)==-1,"Unknown label refused");
  Func<int,Tuple<bool,int>> parent=i=>Tuple.Create(i>1,i-1);
  Check(ViewportHitPolicy.MatchesPath(1,1,i=>true,parent,(p,c)=>1),"Same hit and owner retained");
  Check(ViewportHitPolicy.MatchesPath(3,1,i=>true,parent,(p,c)=>1),"Nested actual hit resolves through exact owner links");
  Check(!ViewportHitPolicy.MatchesPath(3,1,i=>i!=2,parent,(p,c)=>1),"Missing intermediate owner refused");
  Check(!ViewportHitPolicy.MatchesPath(3,1,i=>true,parent,(p,c)=>2),"Duplicate child links refused");
  Check(!ViewportHitPolicy.MatchesPath(3,1,i=>true,parent,(p,c)=>0),"Unlinked child refused");
  Check(!ViewportHitPolicy.MatchesPath(3,1,i=>true,i=>Tuple.Create(true,3),(p,c)=>1),"Cyclic owner path refused");
  Check(!ViewportHitPolicy.MatchesPath(20,1,i=>true,parent,(p,c)=>1),"Over-budget owner path refused");
  Console.WriteLine("PASS: 14 actual-source hover target and ownership checks");
 }
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
}
