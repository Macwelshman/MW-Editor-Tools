using System;
using MW.EditorTools.Modules.PropReplacer;
class Program {
 static int checks;
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
  Check(ViewportHitPolicy.Agrees(3,1,1,i=>i>0,parent,(p,c)=>1),"Exact highlighted owner agrees with its linked hit");
  Check(ViewportHitPolicy.Agrees(3,1,3,i=>i>0,parent,(p,c)=>1),"Exact highlighted child agrees with current ray owner via unique path");
  Check(!ViewportHitPolicy.Agrees(3,1,2,i=>i>0,parent,(p,c)=>1),"An intermediate ancestor is not silently substituted for highlighted target");
  Check(!ViewportHitPolicy.Agrees(3,1,4,i=>i>0,parent,(p,c)=>1),"Different highlighted entity refuses");
  Check(!ViewportHitPolicy.Agrees(3,1,3,i=>i>0,parent,(p,c)=>2),"Matching highlighted child still needs exact unique owner links");
  Check(!ViewportHitPolicy.Agrees(3,1,0,i=>i>0,parent,(p,c)=>1),"Missing highlight cannot authorize a current hit");
  int normalized;
  Check(ViewportHitPolicy.Normalize(10,i=>i>0,i=>i==10?Tuple.Create(true,3):Tuple.Create(false,0),out normalized)&&normalized==3,"Icon target normalizes to exact real entity");
  Check(ViewportHitPolicy.Normalize(11,i=>i>0,i=>i==11?Tuple.Create(true,10):i==10?Tuple.Create(true,3):Tuple.Create(false,0),out normalized)&&normalized==3,"Temp-original and icon target chain normalizes");
  Check(!ViewportHitPolicy.Normalize(10,i=>i>0,i=>Tuple.Create(true,10),out normalized),"Redirect cycles refuse");
  Check(!ViewportHitPolicy.Normalize(10,i=>i>0,i=>Tuple.Create(true,0),out normalized),"Missing marker target refuses");
  Check(!ViewportHitPolicy.Normalize(10,i=>i>0,i=>Tuple.Create(true,i+1),out normalized),"Over-deep redirect refuses");
  Check(!ViewportHitPolicy.Normalize(10,i=>false,i=>Tuple.Create(false,0),out normalized),"Deleted marker refuses");
  Console.WriteLine("PASS: "+checks+" actual-source hover target, normalized identity and ownership checks");
 }
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
}
