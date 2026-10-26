using System;
using System.Linq;
using System.Collections.Generic;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;
using MW.EditorTools.Modules.BulldozerRecovery;

internal static class ConnectionRegenerationChecks {
    static Entity Id(int n)=>new Entity{Index=n,Version=1};
    static readonly PrefabBase marker=(PrefabBase)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(MarkerObjectPrefab));
    static readonly PrefabBase lane=(PrefabBase)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(NetLanePrefab));
    // Observed .208 marker positions: a tilted endpoint is intentional, not rounding.
    static readonly float3[] points={new float3(10.73693f,511.9453f,31.45362f),new float3(7.658996f,511.9453f,31.5554f),new float3(4.705703f,511.9453f,31.56176f),new float3(1.526099f,511.9388f,31.28932f)};
    static AreaCompanionNetwork Graph(int[] anchors,int[] ids,int reverse=0){
        var owner=Id(52957);var g=new AreaCompanionNetwork{Owner=owner};
        g.Anchors=anchors.Select((n,i)=>new AreaCompanionNetwork.Anchor{Id=Id(n),Owner=owner,Prefab=marker,Transform=new Game.Objects.Transform{m_Position=points[i],m_Rotation=quaternion.identity},HasLocal=true,Local=new Game.Tools.LocalTransformCache{m_Position=points[i],m_Rotation=quaternion.identity,m_ParentMesh=-1,m_Probability=100}}).ToArray();
        g.Items=Enumerable.Range(0,3).Select(i=>{
            bool r=(reverse&(1<<i))!=0;var a=r?points[i+1]:points[i];var d=r?points[i]:points[i+1];var start=Id(anchors[r?i+1:i]);var end=Id(anchors[r?i:i+1]);
            return new AreaCompanionNetwork.Item{Id=Id(ids[i]),Owner=owner,Prefab=lane,HasLane=true,HasCurve=true,HasConnection=true,HasSecondary=true,
                Connection=new Game.Net.ConnectionLane{m_Flags=ConnectionLaneFlags.Inside|ConnectionLaneFlags.Pedestrian,m_AccessRestriction=owner},
                Lane=new Lane{m_StartNode=new PathNode(start,(ushort)0),m_MiddleNode=new PathNode(start,(ushort)3),m_EndNode=new PathNode(end,(ushort)0)},
                Curve=new Curve{m_Bezier=new Colossal.Mathematics.Bezier4x3{a=a,b=math.lerp(a,d,1f/3f),c=math.lerp(a,d,2f/3f),d=d},m_Length=math.distance(a,d)}};
        }).ToArray();
        g.RootLanes=g.Items.Select(i=>new Game.Net.SubLane{m_SubLane=i.Id,m_PathMethods=(PathMethod)1}).ToArray();return g;
    }
    internal static int Run(){
        int count=0;void Check(bool ok,string label){if(!ok)throw new Exception("Connection regeneration: "+label);count++;}
        var before=Graph(new[]{53035,53038,53037,53036},new[]{53086,53085,53084});
        var permutations=new[]{new[]{53085,52946,52947},new[]{52946,53085,52947},new[]{52947,52946,53085},new[]{53084,53086,53085},new[]{53085,53084,53086},new[]{53086,53085,53084}};
        if(Environment.GetCommandLineArgs().Contains("--compare-208")){
            var legacy=System.Reflection.Assembly.LoadFrom(System.IO.Path.GetFullPath("outputs/test-0.4.208/MW.EditorTools.dll"));
            var graphType=legacy.GetType(typeof(AreaCompanionNetwork).FullName,true);
            object Copy(object value,Type target){
                if(value==null||target.IsInstanceOfType(value))return value;
                if(target.IsArray){var array=(Array)value;var copy=Array.CreateInstance(target.GetElementType(),array.Length);for(int n=0;n<array.Length;n++)copy.SetValue(Copy(array.GetValue(n),target.GetElementType()),n);return copy;}
                var result=Activator.CreateInstance(target,true);foreach(var field in target.GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic)){
                    var original=value.GetType().GetField(field.Name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);if(original!=null)field.SetValue(result,Copy(original.GetValue(value),field.FieldType));}
                return result;
            }
            var legacyBefore=Copy(before,graphType);var legacyAfter=Copy(Graph(new[]{53038,53036,53037,52945},permutations[0],1),graphType);
            var args=new object[]{legacyAfter,true,null,true,true,true};var passed=(bool)graphType.GetMethod("TryMap",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(legacyBefore,args);
            if(passed)throw new Exception("Legacy regression was not reproduced");Console.WriteLine("VERIFIED-STATIC regression: installed 0.4.208 mapper refuses the repurposed connection graph accepted by the corrected mapper.");
        }
        foreach(var ids in permutations)for(int direction=0;direction<8;direction++){
            var after=Graph(new[]{53038,53036,53037,52945},ids,direction);
            Check(before.TryMap(after,true,out var map,true,true,true),"complete native connection graph maps repurposed IDs and direction "+direction);
            Check(Enumerable.Range(0,3).All(i=>map[before.Items[i].Id]==after.Items[i].Id&&EntityReferenceMap.IsReversed(map,before.Items[i].Id)==((direction&(1<<i))!=0)),"every endpoint pair maps uniquely with correct direction witness");
            Check(map[Id(53035)]==Id(53038)&&map[Id(53038)]==Id(53036)&&map[Id(53036)]==Id(52945),"full pose proof maps shuffled surviving object anchors");
        }
        var live=Graph(new[]{53038,53036,53037,52945},permutations[0],1);
        Check(!before.TryMap(live,true,out _,true,true),"ordinary Undo cannot reassign generated connection identities");
        live.Items[0].Connection.m_Flags|=ConnectionLaneFlags.Disabled;Check(!before.TryMap(live,true,out _,true,true,true),"repurposed lane mapping preserves flags");
        live=Graph(new[]{53038,53036,53037,52945},permutations[0],1);live.Items[0].Curve.m_Bezier.b.y+=.01f;Check(!before.TryMap(live,true,out _,true,true,true),"repurposed lane mapping preserves complete geometry");
        live=Graph(new[]{53038,53036,53037,52945},permutations[0],1);live.Items[0].Lane.m_EndNode=new PathNode(Id(52945),(ushort)0);Check(!before.TryMap(live,true,out _,true,true,true),"repurposed lane mapping rejects wrong pair despite identical curve");
        Check(before.ExplainMismatch(live,true,true,true,true).Contains("candidate count=0")&&before.ExplainMismatch(live,true,true,true,true).Contains("anchors="),"diagnostics identify exact candidate refusal and full anchor correspondence");
        live=Graph(new[]{53038,53036,53037,52945},permutations[0],1);live.RootLanes[0].m_PathMethods=(PathMethod)2;Check(!before.TryMap(live,true,out _,true,true,true),"repurposed lane mapping preserves membership methods");
        live=Graph(new[]{53038,53036,53037,52945},permutations[0],1);live.Items[0].Connection.m_AccessRestriction=Id(999);Check(!before.TryMap(live,true,out _,true,true,true),"repurposed lane mapping preserves restriction references");
        // A second delete/restore can rebuild every lane again with a new ID order.
        var restored=Graph(new[]{53038,53036,53037,52945},permutations[0],1);
        var restoredAgain=Graph(new[]{54038,54036,54037,53945},new[]{52947,53085,52946},6);
        Check(restored.TryMap(restoredAgain,true,out var again,true,true,true)&&again[restored.Items[0].Id]==restoredAgain.Items[0].Id,"subsequent full rebuild is mapped afresh without stale lane identity assumptions");
        Check(before.Items[0].Id==Id(53086)&&before.Items[0].Lane.m_StartNode.GetOwnerIndex()==53035&&before.Items[0].Curve.m_Bezier.a.Equals(points[0]),"original graph and references unchanged");
        return count;
    }
}
