using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using MW.EditorTools.History;
using Unity.Mathematics;

internal static class Program
{
    private static int checks;
    private static void Check(bool value, string name)
    { if (!value) throw new Exception("FAILED: " + name); checks++; }
    private static void Refused(Action action, string name)
    { try { action(); } catch (NotSupportedException) { checks++; return; } throw new Exception("FAILED: " + name); }

    private static void Main()
    {
        Check(MW.EditorTools.Modules.BulldozerRecovery.NestedPayloadPolicy.Accept(0,0,0,0,0), "unattached nested object tree accepted");
        for(int attachment=0;attachment<5;attachment++) {
            var counts=new int[5];counts[attachment]=1;
            Check(!MW.EditorTools.Modules.BulldozerRecovery.NestedPayloadPolicy.Accept(counts[0],counts[1],counts[2],counts[3],counts[4]), "nonempty nested attachment refused: "+attachment);
            counts[attachment]=-1;
            Check(!MW.EditorTools.Modules.BulldozerRecovery.NestedPayloadPolicy.Accept(counts[0],counts[1],counts[2],counts[3],counts[4]), "invalid nested attachment count refused: "+attachment);
        }
        var polygon = new ObjectSubAreaInfo { m_NodePositions=new[]{new float3(0),new float3(3,0,0),new float3(0,0,3)},m_ParentMeshes=new[]{-1,-1,-1} };
        var otherPolygon = new ObjectSubAreaInfo { m_NodePositions=new[]{new float3(10),new float3(13,0,0),new float3(10,0,3)},m_ParentMeshes=new[]{-1,-1,-1} };
        var areaRows = new[]{polygon,otherPolygon};
        Check(MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.RemovedIndex(areaRows,new[]{otherPolygon})==0,"first polygon removal identified");
        Check(MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.RemovedIndex(areaRows,new[]{polygon})==1,"last polygon removal identified");
        Check(MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.RemovedIndex(new[]{polygon},Array.Empty<ObjectSubAreaInfo>())==0,"final polygon removal identified");
        Check(MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.RemovedIndex(new[]{polygon,polygon},new[]{polygon})==-1,"ambiguous duplicate polygon removal refused");
        Check(MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.RemovedIndex(areaRows,Array.Empty<ObjectSubAreaInfo>())==-1,"bulk deletion cannot masquerade as one area");
        Check(MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.RemovedIndex(areaRows,areaRows)==-1,"no-op area deletion refused");
        Check(MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.RemovedIndex(null,areaRows)==-1,"missing area source refused");
        var changedPolygon=(ObjectSubAreaInfo)PrefabSnapshot.Take(polygon).Materialize();changedPolygon.m_ParentMeshes[0]=99;
        Check(MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.RemovedIndex(areaRows,new[]{changedPolygon})==-1,"unrelated parent index change refused");
        Check(MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.AncillaryOnly(0,0,0,0),"initial area-only recovery scope accepted");
        for(int kind=0;kind<4;kind++){var extras=new int[4];extras[kind]=1;Check(!MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.AncillaryOnly(extras[0],extras[1],extras[2],extras[3]),"mixed attachment scope refused "+kind);}
        var nodes=new[]{new Game.Areas.Node{m_Position=new float3(0),m_Elevation=1},new Game.Areas.Node{m_Position=new float3(3,0,0)},new Game.Areas.Node{m_Position=new float3(0,0,3)}};
        var local=nodes.Select(n=>new Game.Tools.LocalNodeCache{m_Position=n.m_Position,m_ParentMesh=-1}).ToArray();
        Check(MW.EditorTools.Modules.BulldozerRecovery.AreaGeometry.Matches(nodes,local,nodes.ToArray(),local.ToArray()),"native area geometry roundtrip");
        var different=nodes.ToArray();different[0].m_Position.x=2;
        Check(!MW.EditorTools.Modules.BulldozerRecovery.AreaGeometry.Matches(nodes,local,different,local),"polygon vertex change detected");
        different=nodes.ToArray();different[0].m_Elevation=3;
        Check(!MW.EditorTools.Modules.BulldozerRecovery.AreaGeometry.Matches(nodes,local,different,local),"area elevation change detected");
        different=nodes.Reverse().ToArray();
        Check(!MW.EditorTools.Modules.BulldozerRecovery.AreaGeometry.Matches(nodes,local,different,local),"polygon winding order preserved");
        var differentLocal=local.ToArray();differentLocal[0].m_ParentMesh=5;
        Check(!MW.EditorTools.Modules.BulldozerRecovery.AreaGeometry.Matches(nodes,local,nodes,differentLocal),"native parent mesh index change detected");
        differentLocal=local.ToArray();differentLocal[0].m_Position.y=1;
        Check(!MW.EditorTools.Modules.BulldozerRecovery.AreaGeometry.Matches(nodes,local,nodes,differentLocal),"native local coordinates checked");
        different=nodes.ToArray();different[0].m_Elevation=float.NaN;
        Check(!MW.EditorTools.Modules.BulldozerRecovery.AreaGeometry.Matches(nodes,local,different,local),"nonfinite area geometry refused");
        Check(!MW.EditorTools.Modules.BulldozerRecovery.AreaGeometry.Matches(nodes,local,nodes,Array.Empty<Game.Tools.LocalNodeCache>()),"missing local geometry refused");
        var lanes = new[] { new ObjectSubLaneInfo {
            m_NodeIndex = new int2(2, 8), m_ParentMesh = new int2(3, 6),
            m_BezierCurve = new Colossal.Mathematics.Bezier4x3 {
                a = new float3(1, 2, 3), b = new float3(4, 5, 6),
                c = new float3(7, 8, 9), d = new float3(10, 11, 12) } } };
        var laneBefore = PrefabSnapshot.Take(lanes);
        Check(laneBefore.Matches(lanes), "lane snapshot initially matches");
        lanes[0].m_NodeIndex = new int2(99);
        Check(!laneBefore.Matches(lanes), "lane node index mutation detected");
        var restoredLane = (ObjectSubLaneInfo[])laneBefore.Materialize();
        Check(restoredLane[0].m_NodeIndex.Equals(new int2(2, 8)) && restoredLane[0].m_ParentMesh.Equals(new int2(3, 6)), "lane node and parent indices restored");
        Check(restoredLane[0].m_BezierCurve.a.Equals(new float3(1, 2, 3)) && restoredLane[0].m_BezierCurve.d.Equals(new float3(10, 11, 12)), "lane curve restored");
        Check(!ReferenceEquals(restoredLane, lanes) && !ReferenceEquals(restoredLane[0], lanes[0]), "lane descriptor deeply copied");
        restoredLane[0].m_ParentMesh = new int2(77);
        Check(!laneBefore.Matches(restoredLane) && laneBefore.Matches(laneBefore.Materialize()), "lane history isolated from restored values");
        restoredLane = (ObjectSubLaneInfo[])laneBefore.Materialize();
        restoredLane[0].m_BezierCurve.b = new float3(88);
        Check(!laneBefore.Matches(restoredLane), "lane curve mutation detected");
        Check(PrefabSnapshot.Take(Array.Empty<ObjectSubLaneInfo>()).Matches(Array.Empty<ObjectSubLaneInfo>()), "empty lanes supported");
        Check(!PrefabSnapshot.Take(null).Matches(Array.Empty<ObjectSubLaneInfo>()), "null and empty lanes distinct");
        Check(laneBefore.ReferencesValid, "null lane prefab reference permitted");
        var objects = new[] { new ObjectSubObjectInfo { m_Position = new float3(1, 2, 3),
            m_Rotation = new quaternion(0, 0, 0, 1), m_Probability = 37, m_ParentMesh = 4, m_GroupIndex = 9 } };
        var objectBefore = PrefabSnapshot.Take(objects);
        objects[0].m_Probability = 99;
        Check(!objectBefore.Matches(objects), "detect live mutation");
        var objectRestored = (ObjectSubObjectInfo[])objectBefore.Materialize();
        Check(objectRestored[0].m_Probability == 37 && objectRestored[0].m_GroupIndex == 9 && objectRestored[0].m_ParentMesh == 4,
            "exact probability/group/parent restoration");
        Check(objectRestored[0].m_Position.Equals(new float3(1, 2, 3)) && objectRestored[0].m_Rotation.Equals(new quaternion(0, 0, 0, 1)), "transform restoration");
        objectRestored[0].m_Probability = 2;
        Check(((ObjectSubObjectInfo[])objectBefore.Materialize())[0].m_Probability == 37, "fresh materialization cannot mutate history");

        var area = new[] { new ObjectSubAreaInfo { m_NodePositions = new[] { new float3(1, 2, 3), new float3(4, 5, 6) }, m_ParentMeshes = new[] { 3, 7 } } };
        var areaBefore = PrefabSnapshot.Take(area);
        area[0].m_NodePositions[0] = new float3(20);
        area[0].m_ParentMeshes[1] = 100;
        var restoredArea = (ObjectSubAreaInfo[])areaBefore.Materialize();
        Check(restoredArea[0].m_NodePositions[0].Equals(new float3(1, 2, 3)) && restoredArea[0].m_ParentMeshes[1] == 7, "deep geometry and parent copies");

        var net = new[] { new ObjectSubNetInfo { m_NodeIndex = new int2(2, 8), m_ParentMesh = new int2(3, 6),
            m_Upgrades = new[] { (NetPieceRequirements)0 } } };
        var netBefore = PrefabSnapshot.Take(net);
        net[0].m_NodeIndex = new int2(0);
        Check(((ObjectSubNetInfo[])netBefore.Materialize())[0].m_NodeIndex.Equals(new int2(2, 8)), "network node indices preserved");
        Check(netBefore.Matches(netBefore.Materialize()), "network curve/value payload round trip");

        var effects = new List<EffectSource.EffectSettings> { new EffectSource.EffectSettings {
            m_Intensity = .75f, m_AnimationIndex = 3, m_ParentMesh = 5, m_Scale = new float3(1, 2, 3) } };
        var effectBefore = PrefabSnapshot.Take(effects);
        effects.Clear();
        var restoredEffects = (List<EffectSource.EffectSettings>)effectBefore.Materialize();
        Check(restoredEffects.Count == 1 && restoredEffects[0].m_Intensity == .75f && restoredEffects[0].m_AnimationIndex == 3,
            "last effect row and settings restored");
        Check(effectBefore.Matches(restoredEffects), "effect payload round trip");
        Check(PrefabSnapshot.Take(new List<EffectSource.AnimationProperties> { new EffectSource.AnimationProperties { m_Duration = 4 } }).ReferencesValid,
            "animation properties included");
        Check(PrefabSnapshot.Take(null).Matches(null), "null preserved");
        Check(PrefabSnapshot.Take(new ObjectSubObjectInfo[0]).Matches(new ObjectSubObjectInfo[0]), "empty distinct from null");
        Refused(() => PrefabSnapshot.Take(new object()), "unknown graph refused");
        Refused(() => PrefabSnapshot.Take(new Unity.Entities.Entity()), "raw entity reference refused");
        Refused(() => PrefabSnapshot.Take(new int[100001]), "snapshot budget enforced");

        var history = new TransactionHistory<int>(20);
        for (int i = 0; i < 25; i++) history.Record(i);
        Check(history.UndoCount == 20, "depth limited to 20");
        Check(!history.Undo(x => false) && history.UndoCount == 20 && history.RedoCount == 0, "failure preserves cursor");
        Check(history.Undo(x => x == 24) && history.NextRedo == 24, "latest operation undone first");
        Check(history.Undo(x => x == 23) && history.RedoCount == 2, "multiple undo steps");
        Check(history.Redo(x => x == 23) && history.Latest == 23, "redo order");
        history.Record(100);
        Check(history.RedoCount == 0 && history.Latest == 100, "new branch clears redo");
        history.Clear();
        Check(history.UndoCount == 0 && history.RedoCount == 0, "asset invalidation clears both stacks");
        var transform=new Game.Objects.Transform{m_Position=new float3(1,2,3),m_Rotation=new quaternion(0,0,0,1)};
        var seed=new Game.Common.PseudoRandomSeed{m_Seed=123};var surface=new Game.Objects.Surface{m_Wetness=4,m_SnowAmount=5};
        var colourArray=new Game.Rendering.MeshColor[2];var customArray=new Game.Rendering.CustomMeshColor[1];
        var prop=new MW.EditorTools.Modules.BulldozerRecovery.MainPropData(transform,seed,surface,colourArray,customArray);
        Check(prop.Matches(prop.Clone()),"actual ECS value types roundtrip independently of prefab snapshot allowlist");
        Check(!ReferenceEquals(prop.Colours,colourArray)&&!ReferenceEquals(prop.CustomColours,customArray)&&!ReferenceEquals(prop.Colours,prop.Colours),"colour buffers never expose source or internal mutable arrays");
        seed.m_Seed=124;Check(!prop.Matches(new MW.EditorTools.Modules.BulldozerRecovery.MainPropData(transform,seed,surface,colourArray,customArray)),"seed changes detected");seed.m_Seed=123;
        surface.m_Wetness=9;Check(!prop.Matches(new MW.EditorTools.Modules.BulldozerRecovery.MainPropData(transform,seed,surface,colourArray,customArray)),"surface changes detected");surface.m_Wetness=4;
        transform.m_Position.x=7;Check(!prop.Matches(new MW.EditorTools.Modules.BulldozerRecovery.MainPropData(transform,seed,surface,colourArray,customArray)),"transform changes detected");
        var missing=new MW.EditorTools.Modules.BulldozerRecovery.MainPropData(transform,seed,surface,null,null);
        Check(!missing.Matches(new MW.EditorTools.Modules.BulldozerRecovery.MainPropData(transform,seed,surface,new Game.Rendering.MeshColor[0],null)),"missing versus empty buffers preserved");
        Refused(()=>new MW.EditorTools.Modules.BulldozerRecovery.MainPropData(transform,seed,surface,new Game.Rendering.MeshColor[257],null),"standalone colour snapshot budget enforced");
        Console.WriteLine($"PASS: {checks} Phase 3 snapshot/history assertions. No Unity native or ECS regeneration test performed.");
    }
}
