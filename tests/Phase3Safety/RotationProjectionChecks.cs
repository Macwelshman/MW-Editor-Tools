using System;
using Game.Prefabs;
using Game.Objects;
using Game.Tools;
using Unity.Mathematics;
using MW.EditorTools.History;
using MW.EditorTools.Modules.BulldozerRecovery;
internal static class RotationProjectionChecks {
    internal static int Run(){int count=0;void Check(bool ok,string label){if(!ok)throw new Exception("Rotation projection: "+label);count++;}
        void Refuse(Action action,string label){try{action();}catch(NotSupportedException){count++;return;}throw new Exception("Rotation projection: "+label);}
        var owner=new Transform{m_Rotation=quaternion.identity};var local=new LocalTransformCache{m_Position=new float3(16.65672f,-.01623535f,-3.368393f),m_Rotation=new quaternion(0,1,0,0),m_ParentMesh=-1,m_Probability=100,m_PrefabSubIndex=54};
        var tilt=new quaternion(-.008925325f,.9999602f,0,0);var world=new Transform{m_Position=local.m_Position,m_Rotation=tilt};
        var row=new ObjectSubObjectInfo{m_Position=local.m_Position,m_Rotation=tilt,m_ParentMesh=-1,m_Probability=100};
        ObjectSubObjectInfo Copy(ObjectSubObjectInfo r)=>(ObjectSubObjectInfo)PrefabSnapshot.Take(r).Materialize();
        Refuse(()=>BoundDescriptorIndex.Resolve(new[]{row},null,local),"logged tilted source row reproduces strict-cache mismatch");
        Check(BoundDescriptorIndex.NativeRotation(owner,world,local).Equals(tilt),"logged just-over-one-degree tilt uses exact world-derived rotation");
        Check(BoundDescriptorIndex.ResolveNative(new[]{row},null,local,owner,world)==0,"logged rack maps to one exact native source row");
        var restored=BoundDescriptorIndex.ForNativeSource(new[]{row},null,local,owner,world);
        Check(restored.m_Rotation.Equals(tilt)&&restored.m_PrefabSubIndex==0&&local.m_Rotation.Equals(new quaternion(0,1,0,0)),"only detached restoration expectation adopts proved source rotation/index");
        var cached=Copy(row);cached.m_Rotation=local.m_Rotation;
        bool Rows(ObjectSubObjectInfo[] before,ObjectSubObjectInfo[] after)=>BoundDescriptorIndex.NativeRows(before,after,new PrefabBase[]{null},new[]{local},new[]{world},owner);
        Check(Rows(new[]{cached},new[]{row}),"complete row serialization permits exact native quaternion projection");
        Check(Rows(new[]{row},new[]{row}),"unchanged source remains exact");
        var wrong=Copy(row);wrong.m_Rotation.value.x+=.000001f;Check(!Rows(new[]{cached},new[]{wrong}),"tiny unproved quaternion change is not accepted as an angle tolerance");
        wrong=Copy(row);wrong.m_Probability=50;Check(!Rows(new[]{cached},new[]{wrong}),"probability cannot change with rotation serialization");
        wrong=Copy(row);wrong.m_GroupIndex=1;Check(!Rows(new[]{cached},new[]{wrong}),"group cannot change");
        wrong=Copy(row);wrong.m_ParentMesh=0;Check(!Rows(new[]{cached},new[]{wrong}),"attachment cannot change");
        Refuse(()=>BoundDescriptorIndex.ResolveNative(new[]{row,row},null,local,owner,world),"duplicate native rows remain ambiguous");
        Refuse(()=>BoundDescriptorIndex.ResolveNative(new[]{cached,cached,row},null,local,owner,world),"strict cache ambiguity cannot fall through to a projected row");
        var nearWorld=world;nearWorld.m_Rotation=math.mul(local.m_Rotation,quaternion.RotateX(math.radians(.9f)));
        Check(BoundDescriptorIndex.NativeRotation(owner,nearWorld,local).Equals(local.m_Rotation),"below-one-degree branch retains entire cached quaternion");
        var nearRow=Copy(row);nearRow.m_Rotation=nearWorld.m_Rotation;Refuse(()=>BoundDescriptorIndex.ResolveNative(new[]{nearRow},null,local,owner,nearWorld),"below-threshold raw world rotation cannot substitute for the native cached result");
        var rotatedOwner=owner;rotatedOwner.m_Rotation=quaternion.RotateY(.4f);var rotatedWorld=world;rotatedWorld.m_Position=math.mul(rotatedOwner.m_Rotation,local.m_Position);rotatedWorld.m_Rotation=math.mul(rotatedOwner.m_Rotation,tilt);
        var exact=math.mul(math.inverse(rotatedOwner.m_Rotation),rotatedWorld.m_Rotation);var rotatedRow=Copy(row);rotatedRow.m_Rotation=exact;
        Check(BoundDescriptorIndex.NativeRotation(rotatedOwner,rotatedWorld,local).Equals(exact),"nonidentity owner uses exact inverse-owner quaternion multiplication");
        Check(BoundDescriptorIndex.ResolveNative(new[]{rotatedRow},null,local,rotatedOwner,rotatedWorld)==0,"rotated owner still resolves uniquely");
        var invalid=world;invalid.m_Rotation.value.x=float.NaN;Refuse(()=>BoundDescriptorIndex.NativeRotation(owner,invalid,local),"nonfinite world quaternion refused");
        invalid=world;invalid.m_Rotation=default;Refuse(()=>BoundDescriptorIndex.NativeRotation(owner,invalid,local),"zero world quaternion refused");
        var invalidLocal=local;invalidLocal.m_Rotation.value*=2;Refuse(()=>BoundDescriptorIndex.NativeRotation(owner,world,invalidLocal),"nonunit cache quaternion refused");
        Check(world.m_Rotation.Equals(tilt)&&cached.m_Rotation.Equals(local.m_Rotation)&&row.m_Rotation.Equals(tilt),"world/cache/source snapshots stay unchanged");
        var reloadedCache=local;reloadedCache.m_Position.y=0;var reloadedWorld=world;reloadedWorld.m_Position=reloadedCache.m_Position;reloadedWorld.m_Rotation=reloadedCache.m_Rotation;
        var retainedSource=Copy(row);retainedSource.m_Position.y=-.01330566f;
        string diagnostic=null;try{BoundDescriptorIndex.ResolveNative(new[]{retainedSource},null,reloadedCache,owner,reloadedWorld);}catch(NotSupportedException error){diagnostic=error.Message;}
        Check(diagnostic!=null&&diagnostic.Contains("owner=")&&diagnostic.Contains("world=")&&diagnostic.Contains("world-local rotation="),"post-ready source/cache disagreement stays refused and records complete world/owner witnesses");
        Check(retainedSource.m_Position.y==-.01330566f&&retainedSource.m_Rotation.Equals(tilt)&&reloadedCache.m_Position.y==0,"diagnostics do not rewrite discrepant source/cache values");
        return count;
    }
}
