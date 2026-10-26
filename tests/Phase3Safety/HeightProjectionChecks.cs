using System;
using Game.Prefabs;
using Game.Tools;
using Game.Objects;
using Unity.Mathematics;
using MW.EditorTools.Modules.BulldozerRecovery;
using MW.EditorTools.History;
internal static class HeightProjectionChecks {
 internal static int Run(){
  int checks=0;void Check(bool value,string name){if(!value)throw new Exception("Height projection: "+name);checks++;}
  void Refuse(Action action,string name){try{action();}catch(NotSupportedException){checks++;return;}throw new Exception("Height projection: "+name);}
  var owner=new Transform{m_Position=new float3(0,511.9566f,0),m_Rotation=quaternion.identity};
  var world=new Transform{m_Position=new float3(16.69779f,511.9453f,6.854797f),m_Rotation=quaternion.identity};
  var cache=new LocalTransformCache{m_Position=new float3(16.69779f,0,6.854801f),m_Rotation=quaternion.identity,m_ParentMesh=-1,m_Probability=100,m_PrefabSubIndex=50};
  var row=new ObjectSubObjectInfo{m_Position=cache.m_Position,m_Rotation=cache.m_Rotation,m_ParentMesh=-1,m_Probability=100};
  ObjectSubObjectInfo Clone(ObjectSubObjectInfo r)=>(ObjectSubObjectInfo)PrefabSnapshot.Take(r).Materialize();
  var projected=BoundDescriptorIndex.NativePosition(owner,world,cache);
  Check(projected.y==-0.01129150390625f&&projected.z==world.m_Position.z,"exact logged height and native unsnapped vector");
  var saved=Clone(row);saved.m_Position=projected;
  Refuse(()=>BoundDescriptorIndex.Resolve(new[]{saved},null,cache),"default descriptor comparison stays strict");
  Check(BoundDescriptorIndex.ResolveNative(new[]{saved},null,cache,owner,world)==0,"world-derived exact row resolves without height tolerance");
  Check(cache.m_Position.y==0&&cache.m_PrefabSubIndex==50&&row.m_Position.y==0,"projection does not mutate live cache or source row");
  var restored=BoundDescriptorIndex.ForNativeSource(new[]{saved},null,cache,owner,world);var wanted=cache;wanted.m_Position=projected;wanted.m_PrefabSubIndex=0;
  Check(restored.Equals(wanted)&&cache.m_Position.y==0,"only detached restoration cache position and index align to source");
  bool Rows(ObjectSubObjectInfo[] a,ObjectSubObjectInfo[] b)=>BoundDescriptorIndex.NativeRows(a,b,new PrefabBase[]{null},new[]{cache},new[]{world},owner);
  Check(Rows(new[]{row},new[]{saved}),"native row serialization accepted under complete world witness");
  Check(Rows(new[]{row},new[]{row}),"unchanged exact source rows preserved");
  Check(row.m_Position.y==0&&saved.m_Position.Equals(projected),"row validation is detached and read only");
  var wrong=Clone(saved);wrong.m_Position.y+=0.00001f;Check(!Rows(new[]{row},new[]{wrong}),"even tiny unproved serialized height difference refused");
  wrong=Clone(saved);wrong.m_Position.x+=0.00001f;Check(!Rows(new[]{row},new[]{wrong}),"unproved horizontal serialization refused");
  wrong=Clone(saved);wrong.m_Probability=50;Check(!Rows(new[]{row},new[]{wrong}),"probability preserved");
  wrong=Clone(saved);wrong.m_GroupIndex=1;Check(!Rows(new[]{row},new[]{wrong}),"group preserved");
  wrong=Clone(saved);wrong.m_ParentMesh=1;Check(!Rows(new[]{row},new[]{wrong}),"parent mesh preserved");
  wrong=Clone(saved);wrong.m_Rotation=quaternion.RotateY(1);Check(!Rows(new[]{row},new[]{wrong}),"rotation preserved");
  Check(!Rows(new[]{row},Array.Empty<ObjectSubObjectInfo>()),"missing retained row refused");
  Refuse(()=>BoundDescriptorIndex.ResolveNative(new[]{saved,saved},null,cache,owner,world),"ambiguous projected rows refused");
  Refuse(()=>BoundDescriptorIndex.ResolveNative(new[]{row,row,saved},null,cache,owner,world),"ambiguous strict rows cannot fall through to projection");
  var badWorld=world;badWorld.m_Position.x+=0.001f;
  Refuse(()=>BoundDescriptorIndex.ResolveNative(new[]{saved},null,cache,owner,badWorld),"horizontal cache versus world discrepancy refused");
  badWorld=world;badWorld.m_Position.y=float.NaN;Refuse(()=>BoundDescriptorIndex.NativePosition(owner,badWorld,cache),"nonfinite world witness refused");
  var near=world;near.m_Position.y=owner.m_Position.y-0.009f;
  Check(BoundDescriptorIndex.NativePosition(owner,near,cache).Equals(cache.m_Position),"native below one centimetre branch retains whole cached vector");
  var thresholdOwner=new Transform{m_Rotation=quaternion.identity};var thresholdWorld=new Transform{m_Position=new float3(0,0.01f,0),m_Rotation=quaternion.identity};var zero=new LocalTransformCache{m_Rotation=quaternion.identity};
  Check(BoundDescriptorIndex.NativePosition(thresholdOwner,thresholdWorld,zero).Equals(thresholdWorld.m_Position),"native threshold equality uses world projection");
  var rotatedOwner=new Transform{m_Position=new float3(20,30,40),m_Rotation=quaternion.RotateY(0.7f)};
  var rotatedWorld=new Transform{m_Position=rotatedOwner.m_Position+math.mul(rotatedOwner.m_Rotation,new float3(2,0.02f,3)),m_Rotation=quaternion.identity};var rotatedCache=cache;rotatedCache.m_Position=new float3(2,0,3);
  var expected=math.mul(math.inverse(rotatedOwner.m_Rotation),rotatedWorld.m_Position-rotatedOwner.m_Position);
  Check(BoundDescriptorIndex.NativePosition(rotatedOwner,rotatedWorld,rotatedCache).Equals(expected),"rotated owner uses exact native inverse transform order");
  var flatOwner=new Transform{m_Position=new float3(0,511.9453f,0),m_Rotation=quaternion.identity};
  var flatWorld=new Transform{m_Position=new float3(28.89187f,511.9453f,-5.936932f),m_Rotation=cache.m_Rotation};
  var tiny=cache;tiny.m_Position=new float3(28.89187f,-0.000030517578125f,-5.936932f);var flatRow=Clone(row);flatRow.m_Position=tiny.m_Position;flatRow.m_Position.y=0;
  Refuse(()=>BoundDescriptorIndex.Resolve(new[]{flatRow},null,tiny),"default comparison remains strict for tiny zero-height serialization");
  Check(BoundDescriptorIndex.ResolveNative(new[]{flatRow},null,tiny,flatOwner,flatWorld)==0,"logged fence companion tiny cache height maps to exact ground-zero source");
  var tinyRow=Clone(flatRow);tinyRow.m_Position=tiny.m_Position;
  Check(BoundDescriptorIndex.NativeRows(new[]{tinyRow},new[]{flatRow},new PrefabBase[]{null},new[]{tiny},new[]{flatWorld},flatOwner),"complete row proof admits only witnessed zero-height normalization");
  Check(tiny.m_Position.y==-0.000030517578125f&&tinyRow.m_Position.y==tiny.m_Position.y,"ground association preserves original captured cache and source");
  var badFlat=Clone(flatRow);badFlat.m_Position.y=0.00001f;Refuse(()=>BoundDescriptorIndex.ResolveNative(new[]{badFlat},null,tiny,flatOwner,flatWorld),"small arbitrary nonzero source height still refused");
  var shifted=flatWorld;shifted.m_Position.y+=0.001f;Refuse(()=>BoundDescriptorIndex.ResolveNative(new[]{flatRow},null,tiny,flatOwner,shifted),"ground exception requires near-zero world witness");
  var attachedTiny=tiny;attachedTiny.m_ParentMesh=0;var attachedRow=Clone(flatRow);attachedRow.m_ParentMesh=0;Refuse(()=>BoundDescriptorIndex.ResolveNative(new[]{attachedRow},null,attachedTiny,flatOwner,flatWorld),"ground exception cannot detach a mesh attachment");
  Refuse(()=>BoundDescriptorIndex.ResolveNative(new[]{flatRow,flatRow},null,tiny,flatOwner,flatWorld),"ground-zero source association remains unique");
  var material=tiny;material.m_Position.y=0.001f;Refuse(()=>BoundDescriptorIndex.ResolveNative(new[]{flatRow},null,material,flatOwner,flatWorld),"material cache height discrepancy still refused");
  return checks;
 }
}
