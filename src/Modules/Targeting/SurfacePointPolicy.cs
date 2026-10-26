using System;
using Unity.Mathematics;

namespace MW.EditorTools.Modules.Targeting {
 // Ground-area native results retain the terrain hit position. Verify that
 // point against the actual candidate boundary; never substitute another area.
 internal static class SurfacePointPolicy {
  internal static bool Contains(float3 point,int count,Func<int,float3> node) {
   if(count<3||count>4096||!math.all(math.isfinite(point)))return false;
   bool inside=false,onEdge=false;double area=0;
   for(int i=0,j=count-1;i<count;j=i++) {
    var a=node(j);var b=node(i);if(!math.all(math.isfinite(a))||!math.all(math.isfinite(b)))return false;
    double dx=(double)b.x-a.x,dz=(double)b.z-a.z,px=(double)point.x-a.x,pz=(double)point.z-a.z;
    double length=dx*dx+dz*dz;
    if(length>0){double t=Math.Max(0,Math.Min(1,(px*dx+pz*dz)/length));double ex=px-t*dx,ez=pz-t*dz;if(ex*ex+ez*ez<=.000001)onEdge=true;}
    if((a.z>point.z)!=(b.z>point.z)&&point.x<a.x+dx*((double)point.z-a.z)/dz)inside=!inside;
    area+=(double)a.x*b.z-(double)b.x*a.z;
   }
   return Math.Abs(area)>1e-8&&(inside||onEdge);
  }
 }
}
