using System;
using Game.Prefabs;
using Game.Tools;
using Game.Objects;
using Unity.Mathematics;
using MW.EditorTools.Modules.PropReplacer;

namespace Game.Prefabs {
    struct PrefabRef {public Unity.Entities.Entity m_Prefab;}
    struct SubLane {public Unity.Entities.Entity m_Prefab;public Colossal.Mathematics.Bezier4x3 m_Curve;public int2 m_NodeIndex,m_ParentMesh;}
    struct SubNet {public Unity.Entities.Entity m_Prefab;public Colossal.Mathematics.Bezier4x3 m_Curve;public int2 m_NodeIndex,m_ParentMesh;}
    class PrefabBase {}
    class ObjectSubObjectInfo {
        public PrefabBase m_Object;
        public float3 m_Position;
        public quaternion m_Rotation;
        public int m_ParentMesh,m_Probability,m_GroupIndex;
    }
    class ObjectSubAreaInfo {public PrefabBase m_AreaPrefab;public float3[] m_NodePositions;public int[] m_ParentMeshes;}
    class ObjectSubNetInfo {public PrefabBase m_NetPrefab;public Colossal.Mathematics.Bezier4x3 m_BezierCurve;public int2 m_ParentMesh,m_NodeIndex;}
    class ObjectSubLaneInfo {public PrefabBase m_LanePrefab;public Colossal.Mathematics.Bezier4x3 m_BezierCurve;public int2 m_ParentMesh,m_NodeIndex;}
    [Flags] enum SubObjectFlags {AnchorTop=1,AnchorCenter=2,OnGround=256,OnAttachedParent=67108864}
    struct SubObject {public Unity.Entities.Entity m_Prefab;public SubObjectFlags m_Flags;public float3 m_Position;public quaternion m_Rotation;public int m_ParentIndex,m_GroupIndex,m_Probability;}
}
namespace Game.Tools { struct LocalTransformCache { public float3 m_Position; public quaternion m_Rotation; public int m_ParentMesh,m_Probability,m_GroupIndex,m_PrefabSubIndex; } struct LocalNodeCache {public float3 m_Position;public int m_ParentMesh;} }
namespace Game.Objects { struct Transform { public float3 m_Position; public quaternion m_Rotation; } }

class Program {
    static int checks;
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
    static void Refuse(Action action,string label){bool refused=false;try{action();}catch(InvalidOperationException){refused=true;}Check(refused,label);}
    static ObjectSubObjectInfo Row(PrefabBase p,float3 pos,quaternion rotation,int mesh=-1,int probability=100,int group=0)=>new(){m_Object=p,m_Position=pos,m_Rotation=rotation,m_ParentMesh=mesh,m_Probability=probability,m_GroupIndex=group};
    static void Main(){
        var wall=new PrefabBase();var other=new PrefabBase();
        var local=new LocalTransformCache{m_Position=new float3(7.500013f,0,-5.800007f),m_Rotation=quaternion.RotateY(math.PI),m_ParentMesh=-1,m_Probability=100,m_GroupIndex=0,m_PrefabSubIndex=36};
        var owner=new Transform{m_Position=new float3(100,512,200),m_Rotation=quaternion.identity};
        var pose=new Transform{m_Position=owner.m_Position+new float3(7.500013f,.04623413f,-5.800007f),m_Rotation=local.m_Rotation};
        // Source coordinates are written by the native world-to-local projection.
        var projected=pose.m_Position-owner.m_Position;
        var recordedPosition=new float3(7.500013f,.04623413f,-5.800007f);
        var rows=new[]{Row(wall,new float3(7.500014f,.04623413f,-9.500004f),local.m_Rotation),Row(wall,recordedPosition,local.m_Rotation)};
        Check(ViewportPropSource.Resolve(rows,wall,local,pose,owner)==1,"Reported 4.6 cm cache/source height mismatch resolves exact live row");
        Check(local.m_PrefabSubIndex==36&&local.m_Position.y==0&&rows[1].m_Position.Equals(recordedPosition),"Resolution never changes cache or source settings");
        local.m_PrefabSubIndex=0;
        Check(ViewportPropSource.Resolve(rows,wall,local,pose,owner)==1,"Stale cache index is not trusted");
        var wrongCacheRow=Row(wall,local.m_Position,local.m_Rotation);
        Check(ViewportPropSource.Resolve(new[]{wrongCacheRow,rows[1]},wall,local,pose,owner)==1,"Cache-only decoy cannot steal the live projected row");
        Refuse(()=>ViewportPropSource.Resolve(new[]{wrongCacheRow},wall,local,pose,owner),"Cache-only row refused when live pose disagrees");
        Refuse(()=>ViewportPropSource.Resolve(new[]{rows[1],rows[1]},wall,local,pose,owner),"Duplicate projected rows remain ambiguous even with native index");
        Refuse(()=>ViewportPropSource.Resolve(rows,other,local,pose,owner),"Different prefab identity refused");
        foreach(var field in new[]{0,1,2}){
            var changed=Row(wall,projected,local.m_Rotation,field==0?0:-1,field==1?50:100,field==2?1:0);
            Refuse(()=>ViewportPropSource.Resolve(new[]{changed},wall,local,pose,owner),"Mesh/probability/group mismatch refused "+field);
        }
        Refuse(()=>ViewportPropSource.Resolve(new[]{Row(wall,projected,quaternion.identity)},wall,local,pose,owner),"Different rotation refused");
        Refuse(()=>ViewportPropSource.Resolve(new[]{Row(wall,projected+new float3(.02f,0,0),local.m_Rotation)},wall,local,pose,owner),"Horizontal mismatch refused");
        var small=local;small.m_Position=projected+new float3(0,.005f,0);
        Check(ViewportPropSource.Resolve(new[]{Row(wall,small.m_Position,local.m_Rotation)},wall,small,pose,owner)==0,"Native sub-centimetre cache snapping preserved");
        Refuse(()=>ViewportPropSource.Resolve(new[]{Row(wall,projected,local.m_Rotation)},wall,small,pose,owner),"Cannot bypass native cache snapping with a raw pose row");
        var exact=local;exact.m_Position=projected;
        Check(ViewportPropSource.Resolve(rows,wall,exact,pose,owner)==1,"Existing exact-cache path preserved");
        var rotatedOwner=new Transform{m_Position=new float3(5,17,-10),m_Rotation=quaternion.RotateY(.8f)};
        var rotatedPose=new Transform{m_Position=rotatedOwner.m_Position+math.mul(rotatedOwner.m_Rotation,projected),m_Rotation=math.mul(rotatedOwner.m_Rotation,local.m_Rotation)};
        Check(ViewportPropSource.Resolve(rows,wall,local,rotatedPose,rotatedOwner)==1,"Rotated and translated owner projects uniquely");
        var rotationCache=local;rotationCache.m_Rotation=math.mul(local.m_Rotation,quaternion.RotateY(.005f));
        Check(ViewportPropSource.Resolve(new[]{Row(wall,projected,rotationCache.m_Rotation)},wall,rotationCache,pose,owner)==0,"Native small-angle cache snapping preserved");
        var farRotation=local;farRotation.m_Rotation=math.mul(local.m_Rotation,quaternion.RotateY(.1f));
        Check(ViewportPropSource.Resolve(rows,wall,farRotation,pose,owner)==1,"Larger cached rotation drift uses live projection");
        var bad=pose;bad.m_Position.x=float.NaN;
        Refuse(()=>ViewportPropSource.Resolve(rows,wall,local,bad,owner),"Nonfinite live transform refused");
        var badOwner=owner;badOwner.m_Rotation=default;
        Refuse(()=>ViewportPropSource.Resolve(rows,wall,local,pose,badOwner),"Zero owner rotation refused");
        var badLocal=local;badLocal.m_Position.y=float.PositiveInfinity;
        Refuse(()=>ViewportPropSource.Resolve(rows,wall,badLocal,pose,owner),"Nonfinite cache refused");
        badLocal=local;badLocal.m_Rotation=default;
        Refuse(()=>ViewportPropSource.Resolve(rows,wall,badLocal,pose,owner),"Zero cache rotation refused");
        Refuse(()=>ViewportPropSource.Resolve(null,wall,local,pose,owner),"Missing source list refused");
        var diagnostic=ViewportPropSource.Describe(rows,wall,local,pose,owner);
        Check(diagnostic.Contains("livePosition=")&&diagnostic.Contains("ownerPosition=")&&diagnostic.Contains("projectedPosition="),"Diagnostics retain live, owner and projected values");
        Check(diagnostic.Contains("cacheProbability=100")&&diagnostic.Contains("positionDelta=")&&diagnostic.Contains("oppositeRotationDeltaSq="),"Diagnostics distinguish placement, settings and quaternion sign");
        var repeated=new ObjectSubObjectInfo[12];for(int i=0;i<12;i++)repeated[i]=Row(wall,recordedPosition+new float3(100+i,0,0),local.m_Rotation);repeated[11]=rows[1];
        diagnostic=ViewportPropSource.Describe(repeated,wall,local,pose,owner);
        Check(diagnostic.Contains("row[11]"),"Nearest repeated wall is included regardless of source order");
        Check(diagnostic.Split("; row[").Length==9,"Diagnostic candidate list is bounded to eight rows");
        Check(ViewportPropSource.Describe(rows,wall,local,pose,badOwner).Contains("projectionError="),"Invalid placement still produces a diagnostic without masking original refusal");
        Geometry();
        Ground();
        PropReplacerSystem.AdapterChecks();
        Console.WriteLine("PASS: "+checks+" viewport prop/area/network projection checks; actual installed mathematics libraries, managed game data doubles.");
    }
    static void Ground(){
        var wall=new PrefabBase();var rows=new ObjectSubObjectInfo[44];
        var rotation=new quaternion(-2.672338e-7f,-.7071067f,2.098793e-7f,.7071068f);
        rows[43]=Row(wall,new float3(5.5f,-.02758789f,-11.49998f),rotation);
        var cache=new LocalTransformCache{m_Position=new float3(5.5f,0,-11.49998f),m_Rotation=rotation,m_ParentMesh=-1,m_Probability=100,m_GroupIndex=0,m_PrefabSubIndex=43};
        var owner=new Transform{m_Position=new float3(2.361972f,511.9453f,21.84178f),m_Rotation=quaternion.identity};
        var pose=new Transform{m_Position=new float3(7.861973f,511.9453f,10.3418f),m_Rotation=rotation};
        Refuse(()=>ViewportPropSource.Resolve(rows,wall,cache,pose,owner),"Recorded 0.4.167 wall refusal reproduced without native proof");
        var native=new SubObject{m_Prefab=17,m_Flags=SubObjectFlags.OnGround,m_Position=rows[43].m_Position,m_Rotation=rotation,m_ParentIndex=-1,m_GroupIndex=0,m_Probability=100};
        var proof=new bool[44];proof[43]=ViewportPropSource.ProvesGround(rows[43],native,true);
        Check(proof[43],"Matching compiled OnGround entry proves height normalization contract");
        Check(ViewportPropSource.GroundSources(rows,wall,new[]{native},17)[43],"Compiled buffer length/order need not equal authored object rows");
        Check(!ViewportPropSource.GroundSources(rows,wall,new[]{native,native},17)[43],"Duplicate compiled entries cannot prove source identity");
        var nonGround=native;nonGround.m_Flags=0;
        Check(!ViewportPropSource.GroundSources(rows,wall,new[]{native,nonGround},17)[43],"Compiled ambiguity including non-ground entry is refused");
        Check(!ViewportPropSource.GroundSources(rows,wall,new[]{native},18)[43],"Different compiled prefab entity cannot prove grounding");
        Check(ViewportPropSource.Resolve(rows,wall,cache,pose,owner,proof)==43,"Recorded wall maps uniquely with compiled ground proof and native index");
        Check(rows[43].m_Position.y==-.02758789f&&cache.m_Position.y==0,"Ground mapping preserves authored height and cache");
        rows[1]=Row(wall,cache.m_Position,rotation);
        Refuse(()=>ViewportPropSource.Resolve(rows,wall,cache,pose,owner,proof),"Stacked same-horizontal rows are refused even when one equals raw cache height");rows[1]=null;
        rows[0]=Row(wall,new float3(2,-.01f,3),rotation);proof[0]=true;var stale=cache;stale.m_PrefabSubIndex=0;
        Refuse(()=>ViewportPropSource.Resolve(rows,wall,stale,pose,owner,proof),"Ground fallback cannot trust a stale native index");
        Check(!ViewportPropSource.ProvesGround(rows[43],native,false),"Compiled prefab identity must agree");
        var changed=native;changed.m_Position.y+=.001f;
        Check(!ViewportPropSource.ProvesGround(rows[43],changed,true),"Compiled descriptor must match authored source exactly");
        changed=native;changed.m_Probability=50;
        Check(!ViewportPropSource.ProvesGround(rows[43],changed,true),"Compiled source settings must match exactly");
        changed=native;changed.m_Flags=0;
        Check(!ViewportPropSource.ProvesGround(rows[43],changed,true),"Ordinary placements cannot ignore height");
        changed=native;changed.m_Flags|=SubObjectFlags.AnchorTop;
        Check(!ViewportPropSource.ProvesGround(rows[43],changed,true),"Top-anchored ground placement needs its separate native height contract");
        changed=native;changed.m_Flags|=SubObjectFlags.OnAttachedParent;
        Check(!ViewportPropSource.ProvesGround(rows[43],changed,true),"Attached-parent grounding cannot use terrain contract");
        changed=native;changed.m_ParentIndex=0;
        Check(!ViewportPropSource.ProvesGround(rows[43],changed,true),"Compiled parent attachment cannot be ignored");
        var tilted=owner;tilted.m_Rotation=quaternion.RotateX(.3f);
        Refuse(()=>ViewportPropSource.Resolve(rows,wall,cache,pose,tilted,proof),"Tilted owner cannot use local horizontal ground mapping");
        var elevated=cache;elevated.m_Position.y=.05f;
        Refuse(()=>ViewportPropSource.Resolve(rows,wall,elevated,pose,owner,proof),"Nonzero cached height cannot use zero-height ground normalization");
        proof[43]=false;
        Refuse(()=>ViewportPropSource.Resolve(rows,wall,cache,pose,owner,proof),"Failed compiled source proof retains strict matching");
    }
    static void Geometry(){
        var area=new PrefabBase();var other=new PrefabBase();
        var owner=new Transform{m_Position=new float3(10,20,30),m_Rotation=quaternion.RotateY(.6f)};
        var saved=new[]{new float3(0,.04623413f,0),new float3(4,.04623413f,0),new float3(4,.04623413f,4),new float3(0,.04623413f,4)};
        var world=Array.ConvertAll(saved,p=>owner.m_Position+math.mul(owner.m_Rotation,p));
        var cache=Array.ConvertAll(saved,p=>new LocalNodeCache{m_Position=new float3(p.x,0,p.z),m_ParentMesh=-1});
        var row=new ObjectSubAreaInfo{m_AreaPrefab=area,m_NodePositions=saved};
        var projected=ViewportGeometrySource.Area(world,cache,owner);
        Check(ViewportGeometrySource.AreaMatches(row,area,projected,cache),"Uneven-ground area/surface resolves live projected polygon");
        Check(cache[0].m_Position.y==0&&row.m_NodePositions[0].y==saved[0].y,"Area projection never mutates cache/source arrays");
        Check(!ViewportGeometrySource.AreaMatches(row,other,projected,cache),"Area reference identity required");
        var reversed=(float3[])projected.Clone();Array.Reverse(reversed);
        Check(!ViewportGeometrySource.AreaMatches(row,area,reversed,cache),"Node order cannot be reversed to force a match");
        row.m_ParentMeshes=new[]{-1,2,-1,-1};
        Check(!ViewportGeometrySource.AreaMatches(row,area,projected,cache),"Area parent mesh mismatch refused");
        cache[1].m_ParentMesh=2;
        Check(ViewportGeometrySource.AreaMatches(row,area,projected,cache),"Explicit bound parent meshes preserved");
        row.m_ParentMeshes=new[]{-1};
        Check(!ViewportGeometrySource.AreaMatches(row,area,projected,cache),"Incomplete source mesh array refused");
        row.m_ParentMeshes=null;cache[1].m_ParentMesh=-1;
        row.m_NodePositions=new[]{saved[0],saved[1],saved[2]};
        Check(!ViewportGeometrySource.AreaMatches(row,area,projected,cache),"Changed polygon node count refused");
        row.m_NodePositions=saved;
        Refuse(()=>ViewportGeometrySource.Area(new[]{world[0],world[1]},new LocalNodeCache[2],owner),"Incomplete polygon refused");
        Refuse(()=>ViewportGeometrySource.Area(world,new LocalNodeCache[3],owner),"Mismatched live/cache counts refused");
        var badWorld=(float3[])world.Clone();badWorld[0].x=float.NaN;
        Refuse(()=>ViewportGeometrySource.Area(badWorld,cache,owner),"Nonfinite live polygon refused");
        var badCache=(LocalNodeCache[])cache.Clone();badCache[0].m_Position.y=float.PositiveInfinity;
        Refuse(()=>ViewportGeometrySource.Area(world,badCache,owner),"Nonfinite polygon cache refused");
        var origin=new Transform{m_Rotation=quaternion.identity};
        Check(ViewportGeometrySource.Project(new float3(0,.005f,0),origin,float3.zero).Equals(float3.zero),"Sub-centimetre node snaps to cache");
        Check(ViewportGeometrySource.Project(new float3(0,.01f,0),origin,float3.zero).y==.01f,"One-centimetre boundary retains live position");
        var net=new PrefabBase();var lane=new PrefabBase();
        var curve=new Colossal.Mathematics.Bezier4x3{a=saved[0],b=saved[1],c=saved[2],d=saved[3]};
        var live=new Colossal.Mathematics.Bezier4x3{a=world[0],b=world[1],c=world[2],d=world[3]};
        var cached=new Colossal.Mathematics.Bezier4x3{a=cache[0].m_Position,b=cache[1].m_Position,c=cache[2].m_Position,d=cache[3].m_Position};
        var netRow=new ObjectSubNetInfo{m_NetPrefab=net,m_BezierCurve=curve,m_ParentMesh=new int2(-1,2),m_NodeIndex=new int2(3,8)};
        var laneRow=new ObjectSubLaneInfo{m_LanePrefab=lane,m_BezierCurve=curve,m_ParentMesh=netRow.m_ParentMesh,m_NodeIndex=netRow.m_NodeIndex};
        var result=ViewportGeometrySource.Curve(live,cached,owner);
        Check(ViewportGeometrySource.NetworkMatches(netRow,net,result,new int2(-1,2),false),"Four-point network curve projects across cache height drift");
        Check(ViewportGeometrySource.NetworkMatches(laneRow,lane,result,new int2(-1,2),false),"Lane container uses lane prefab identity");
        Check(!ViewportGeometrySource.NetworkMatches(laneRow,net,result,new int2(-1,2),false),"Container prefab cannot be replaced by marker prefab identity");
        Check(!ViewportGeometrySource.NetworkMatches(netRow,net,result,new int2(-1,-1),false),"Endpoint parent mesh mismatches refused");
        Check(!ViewportGeometrySource.NetworkMatches(netRow,net,result,new int2(-1,2),true),"Distinct-endpoint segment cannot match point topology");
        netRow.m_NodeIndex=new int2(-1,8);
        Check(!ViewportGeometrySource.NetworkMatches(netRow,net,result,new int2(-1,2),false),"Unresolved source topology refused");
        netRow.m_NodeIndex=new int2(3,8);
        var shifted=result;shifted.c.y+=.02f;
        Check(!ViewportGeometrySource.NetworkMatches(netRow,net,shifted,new int2(-1,2),false),"A mismatched middle control point cannot be ignored");
        var raw=ViewportGeometrySource.Curve(live,null,owner);
        Check(ViewportGeometrySource.NetworkMatches(netRow,net,raw,new int2(-1,2),false),"Native curve without cache uses live geometry");
        var small=curve;small.a.y+=.005f;small.b.y+=.005f;
        var snapped=ViewportGeometrySource.Curve(small,curve,origin);
        Check(snapped.a.Equals(curve.a)&&snapped.b.Equals(curve.b)&&snapped.c.Equals(curve.c),"Curve points snap independently using native threshold");
        var invalid=live;invalid.b.z=float.NaN;
        Refuse(()=>ViewportGeometrySource.Curve(invalid,cached,owner),"Nonfinite live curve refused");
        invalid=cached;invalid.c.x=float.PositiveInfinity;
        Refuse(()=>ViewportGeometrySource.Curve(live,invalid,owner),"Nonfinite curve cache refused");
        var point=ViewportGeometrySource.Project(world[0],owner,cache[0].m_Position);
        var pointCurve=new Colossal.Mathematics.Bezier4x3{a=point,b=point,c=point,d=point};
        laneRow.m_BezierCurve=pointCurve;laneRow.m_NodeIndex=new int2(5);laneRow.m_ParentMesh=new int2(-1);
        Check(ViewportGeometrySource.NetworkMatches(laneRow,lane,pointCurve,new int2(-1),true),"Native point lane container retains degenerate curve topology");
        Check(!ViewportGeometrySource.NetworkMatches(laneRow,lane,pointCurve,new int2(-1),false),"Point lane cannot match ordinary segment topology");
        Check(netRow.m_NodeIndex.Equals(new int2(3,8))&&netRow.m_ParentMesh.Equals(new int2(-1,2))&&netRow.m_BezierCurve.a.Equals(curve.a),"Source curve/topology/meshes stay unchanged");
    }
}
