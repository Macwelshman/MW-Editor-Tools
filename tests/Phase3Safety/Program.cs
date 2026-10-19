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
        checks+=NativeCollectionChecks.Run();
        checks+=AreaCompanionChecks.Run();
        checks+=NetworkDeletionChecks.Run();
        checks+=StandaloneAreaChecks.Run();
        var nativeEffects=new[]{new Game.Effects.EnabledEffect{m_EffectIndex=4,m_EnabledIndex=100},new Game.Effects.EnabledEffect{m_EffectIndex=2,m_EnabledIndex=200}};
        var enabledImage=MW.EditorTools.Modules.BulldozerRecovery.EnabledEffectImage.Capture(nativeEffects);
        nativeEffects[0].m_EnabledIndex=900;nativeEffects[1].m_EnabledIndex=800;
        Check(enabledImage.SequenceEqual(MW.EditorTools.Modules.BulldozerRecovery.EnabledEffectImage.Capture(nativeEffects)),"native effect allocator indices excluded from semantic snapshot");
        Array.Reverse(nativeEffects);Check(enabledImage.SequenceEqual(MW.EditorTools.Modules.BulldozerRecovery.EnabledEffectImage.Capture(nativeEffects)),"effect buffer order independent");
        nativeEffects[0].m_EffectIndex=7;Check(!enabledImage.SequenceEqual(MW.EditorTools.Modules.BulldozerRecovery.EnabledEffectImage.Capture(nativeEffects)),"enabled effect identity changes detected and source mutations detached");
        Check(MW.EditorTools.Modules.BulldozerRecovery.EnabledEffectImage.Capture(Array.Empty<Game.Effects.EnabledEffect>()).Length==0,"empty enabled effects supported");
        Refused(()=>MW.EditorTools.Modules.BulldozerRecovery.EnabledEffectImage.Capture(new Game.Effects.EnabledEffect[257]),"enabled effect buffer budget enforced");
        Refused(()=>MW.EditorTools.Modules.BulldozerRecovery.EnabledEffectImage.Capture(null),"missing enabled effect image refused");
        var slot=new LastDeletion<object>();var oldDelete=new object();var newDelete=new object();
        Check(slot.UndoCount==0&&slot.RedoCount==0,"last deletion starts empty");
        slot.Record(oldDelete);slot.Record(newDelete);slot.SetDepth(50);
        Check(slot.UndoCount==1&&ReferenceEquals(slot.Latest,newDelete)&&slot.All.Length==1,"new deletion replaces old regardless of legacy depth setting");
        Check(!slot.Undo(_=>false)&&ReferenceEquals(slot.Latest,newDelete),"failed completion leaves last deletion available");
        Check(slot.Undo(v=>ReferenceEquals(v,newDelete))&&slot.UndoCount==0&&slot.RedoCount==0,"successful completion consumes slot without redo");
        Check(!slot.Undo(_=>true)&&!slot.Redo(_=>throw new Exception()),"consumed deletion cannot run again or redo");
        slot.Record(oldDelete);slot.Clear();Check(slot.UndoCount==0,"unsupported deletion or context invalidation can clear slot");
        slot.Record(oldDelete);Check(!slot.Undo(_=>{slot.Record(newDelete);return true;})&&ReferenceEquals(slot.Latest,newDelete),"stale completion cannot consume replacement");
        var cache=new Game.Tools.LocalTransformCache{m_PrefabSubIndex=99,m_Position=new float3(1,2,3),m_Rotation=new quaternion(0,0,0,1),m_Probability=100,m_ParentMesh=-1};
        Check(MW.EditorTools.Modules.BulldozerRecovery.PropComponentSchema.Known.Contains("Game.Common.BatchesUpdated"),"actual prop registry accepts derived batch refresh marker");
        Check(typeof(Game.Common.BatchesUpdated).GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Length==0,"batch marker has no captured state");
        Check(!MW.EditorTools.Modules.BulldozerRecovery.PropComponentSchema.Known.Contains("Game.Common.Deleted")&&!MW.EditorTools.Modules.BulldozerRecovery.PropComponentSchema.Known.Contains("Game.Tools.Temp")&&!MW.EditorTools.Modules.BulldozerRecovery.PropComponentSchema.Known.Contains("Game.Tools.SaveInstance"),"unsafe lifecycle markers remain excluded");
        var descriptor=new ObjectSubObjectInfo{m_Position=cache.m_Position,m_Rotation=cache.m_Rotation,m_Probability=100,m_ParentMesh=-1};
        Check(MW.EditorTools.Modules.BulldozerRecovery.BoundDescriptorIndex.Resolve(new[]{descriptor},null,cache)==0,"unique placement resolves stale native index");
        Check(cache.m_PrefabSubIndex==99,"descriptor resolution never mutates live cache input");
        var aligned=MW.EditorTools.Modules.BulldozerRecovery.BoundDescriptorIndex.ForSource(new[]{descriptor},null,cache);
        var indexOnly=cache;indexOnly.m_PrefabSubIndex=0;
        Check(aligned.Equals(indexOnly),"source alignment changes only detached descriptor index");
        Check(cache.m_PrefabSubIndex==99,"source alignment preserves captured input and native cache");
        var secondRow=new ObjectSubObjectInfo{m_Position=new float3(9,2,3),m_Rotation=cache.m_Rotation,m_Probability=100,m_ParentMesh=-1};
        var beforeIndex=MW.EditorTools.Modules.BulldozerRecovery.BoundDescriptorIndex.ForSource(new[]{secondRow,descriptor},null,cache);
        var afterIndex=MW.EditorTools.Modules.BulldozerRecovery.BoundDescriptorIndex.ForSource(new[]{descriptor,secondRow},null,beforeIndex);
        Check(beforeIndex.m_PrefabSubIndex==1&&afterIndex.m_PrefabSubIndex==0,"native row reorder updates expected restoration index by complete placement");
        var repeated=MW.EditorTools.Modules.BulldozerRecovery.BoundDescriptorIndex.ForSource(new[]{descriptor,secondRow},null,afterIndex);
        Check(repeated.Equals(afterIndex),"subsequent source alignment is stable");
        Refused(()=>MW.EditorTools.Modules.BulldozerRecovery.BoundDescriptorIndex.ForSource(new[]{descriptor,descriptor},null,cache),"alignment refuses ambiguous sibling placements");
        Refused(()=>MW.EditorTools.Modules.BulldozerRecovery.BoundDescriptorIndex.ForSource(new[]{secondRow},null,cache),"alignment refuses changed placement");
        Refused(()=>MW.EditorTools.Modules.BulldozerRecovery.BoundDescriptorIndex.Resolve(new[]{descriptor,descriptor},null,cache),"duplicate placement refuses ambiguous mapping");
        var changedCache=cache;changedCache.m_ParentMesh=7;
        Refused(()=>MW.EditorTools.Modules.BulldozerRecovery.BoundDescriptorIndex.Resolve(new[]{descriptor},null,changedCache),"parent mesh must match");
        changedCache=cache;changedCache.m_Probability=50;
        Refused(()=>MW.EditorTools.Modules.BulldozerRecovery.BoundDescriptorIndex.Resolve(new[]{descriptor},null,changedCache),"probability must match");
        changedCache=cache;changedCache.m_GroupIndex=5;
        Refused(()=>MW.EditorTools.Modules.BulldozerRecovery.BoundDescriptorIndex.Resolve(new[]{descriptor},null,changedCache),"group must match");
        changedCache=cache;changedCache.m_Position.x=6;
        Refused(()=>MW.EditorTools.Modules.BulldozerRecovery.BoundDescriptorIndex.Resolve(new[]{descriptor},null,changedCache),"position must match");
        Refused(()=>MW.EditorTools.Modules.BulldozerRecovery.BoundDescriptorIndex.Resolve(null,null,cache),"missing descriptor list refused");
        string mismatch=null;try{MW.EditorTools.Modules.BulldozerRecovery.BoundDescriptorIndex.Resolve(new[]{descriptor},null,changedCache);}catch(NotSupportedException error){mismatch=error.Message;}
        Check(mismatch!=null&&mismatch.Contains("cache position=")&&mismatch.Contains("row[0] position=")&&mismatch.Contains("probability="),"placement refusal reports cache and source values without relaxing equality");
        checks+=AreaMoveChecks.Run();
        checks+=EffectEditChecks.Run();
        foreach (var element in RecoveryListSchema.Elements)
        {
            var rows = Array.CreateInstance(element, 3);
            for (int i = 0; i < rows.Length; i++) rows.SetValue(Activator.CreateInstance(element), i);
            var original = PrefabSnapshot.Take(rows);
            var cleared = RecoveryListSchema.AfterDeletion(rows, -1, true);
            Check(((Array)cleared.Materialize()).Length == 0, "Clear empties full underlying list: " + element.Name);
            Check(original.Matches(rows), "Clear capture leaves source intact: " + element.Name);
            var removed = RecoveryListSchema.AfterDeletion(rows, 1, false);
            Check(((Array)removed.Materialize()).Length == 2, "Row deletion retains other rows: " + element.Name);
            Check(original.Matches(rows), "Row capture leaves source intact: " + element.Name);
        }
        var laneRows = new[] { new ObjectSubLaneInfo { m_NodeIndex = new int2(1,2) }, new ObjectSubLaneInfo { m_NodeIndex = new int2(3,4) } };
        var laneResult = (ObjectSubLaneInfo[])RecoveryListSchema.AfterDeletion(laneRows, 0, false).Materialize();
        Check(laneResult[0].m_NodeIndex.Equals(new int2(3,4)), "Lane deletion uses underlying index");
        laneResult[0].m_NodeIndex = new int2(99);
        Check(laneRows[1].m_NodeIndex.Equals(new int2(3,4)), "Lane result cannot mutate retained source");
        var laneList = new List<ObjectSubLaneInfo>(laneRows);
        Check(((List<ObjectSubLaneInfo>)RecoveryListSchema.AfterDeletion(laneList, -1, true).Materialize()).Count == 0 && laneList.Count == 2, "List Clear isolated from source");
        Check(((Array)RecoveryListSchema.AfterDeletion(Array.Empty<ObjectSubLaneInfo>(), -1, true).Materialize()).Length == 0, "Empty Clear stays empty");
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
        Check(MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.RemovedIndex(areaRows,null)==-1,"missing area after-image refused");
        Check(MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.RemovedIndex(Array.Empty<ObjectSubAreaInfo>(),Array.Empty<ObjectSubAreaInfo>())==-1,"empty area attempt cannot create Undo");
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

        var rowA=new ObjectSubObjectInfo{m_Position=new float3(1),m_Probability=70,m_ParentMesh=-1};
        var rowB=new ObjectSubObjectInfo{m_Position=new float3(2),m_Probability=100,m_ParentMesh=3};
        var rowC=new ObjectSubObjectInfo{m_Position=new float3(3),m_Probability=100,m_ParentMesh=0};
        Check(MW.EditorTools.Modules.BulldozerRecovery.DescriptorPermutation.Matches(new[]{rowA,rowB,rowC},new[]{rowC,rowA,rowB}),"native descriptor reorder preserves complete values");
        Check(!MW.EditorTools.Modules.BulldozerRecovery.DescriptorPermutation.Matches(new[]{rowA,rowB},new[]{rowA,rowA}),"descriptor replacement refused");
        Check(!MW.EditorTools.Modules.BulldozerRecovery.DescriptorPermutation.Matches(new[]{rowA,rowA},new[]{rowA,rowA}),"ambiguous duplicate rows refused");
        Check(!MW.EditorTools.Modules.BulldozerRecovery.DescriptorPermutation.Matches(new[]{rowA},Array.Empty<ObjectSubObjectInfo>()),"extra removal refused");
        var changedRow=new ObjectSubObjectInfo{m_Position=rowA.m_Position,m_Probability=71,m_ParentMesh=-1};
        Check(!MW.EditorTools.Modules.BulldozerRecovery.DescriptorPermutation.Matches(new[]{rowA},new[]{changedRow}),"probability change cannot masquerade as reorder");
        changedRow.m_Probability=70;changedRow.m_ParentMesh=2;
        Check(!MW.EditorTools.Modules.BulldozerRecovery.DescriptorPermutation.Matches(new[]{rowA},new[]{changedRow}),"parent mesh change cannot masquerade as reorder");
        var thirdPolygon=new ObjectSubAreaInfo{m_NodePositions=new[]{new float3(20),new float3(21),new float3(22)},m_ParentMeshes=new[]{-1,-1,-1}};
        Check(MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.RemovedIndex(new[]{polygon,otherPolygon,thirdPolygon},new[]{thirdPolygon,polygon})==1,"area deletion accepts retained polygon reorder");
        var roundedPolygon=(ObjectSubAreaInfo)PrefabSnapshot.Take(polygon).Materialize();roundedPolygon.m_NodePositions[0].x+=.00001f;
        Check(MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.RemovedIndex(new[]{polygon,otherPolygon},new[]{roundedPolygon})==1,"native retained polygon float rounding accepted within existing geometry tolerance");
        roundedPolygon.m_NodePositions[0].x+=.01f;
        Check(MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.RemovedIndex(new[]{polygon,otherPolygon},new[]{roundedPolygon})==-1,"actual retained polygon move refused");
        var effectNative=new EffectSource.EffectSettings{m_Intensity=.75f,m_AnimationIndex=2,m_PositionOffset=new float3(1,2,3)};
        var effectExact=PrefabSnapshot.Take(effectNative);effectNative.m_Intensity+=.00001f;
        Check(effectExact.MatchesNativeGeometry(effectNative)&&!effectExact.Matches(effectNative),"native comparison tolerates rounding without weakening exact snapshot checks");
        effectNative.m_AnimationIndex=3;Check(!effectExact.MatchesNativeGeometry(effectNative),"native comparison keeps animation indices exact");effectNative.m_AnimationIndex=2;
        effectNative.m_Intensity=.76f;Check(!effectExact.MatchesNativeGeometry(effectNative),"native comparison refuses meaningful intensity change");
        effectNative.m_Intensity=float.NaN;Check(!effectExact.MatchesNativeGeometry(effectNative),"native comparison refuses nonfinite values");
        effectNative.m_Intensity=.75f;effectNative.m_PositionOffset.x+=.01f;Check(!effectExact.MatchesNativeGeometry(effectNative),"native comparison refuses geometry move");
        var netExpected=new ObjectSubNetInfo{m_NodeIndex=new int2(-1,-1),m_ParentMesh=new int2(-1,-1)};
        var netActual=(ObjectSubNetInfo)PrefabSnapshot.Take(netExpected).Materialize();netActual.m_NodeIndex=new int2(0,1);
        Check(MW.EditorTools.Modules.BulldozerRecovery.AreaNetDescriptorPolicy.Matches(netExpected,netActual)&&netExpected.m_NodeIndex.Equals(new int2(-1,-1)),"area-only net index normalization leaves source snapshot untouched");
        netActual.m_ParentMesh=new int2(1,2);Check(!MW.EditorTools.Modules.BulldozerRecovery.AreaNetDescriptorPolicy.Matches(netExpected,netActual),"net parent meshes stay exact");
        Check(!MW.EditorTools.Modules.BulldozerRecovery.AreaNetDescriptorPolicy.Matches(netExpected,null),"missing net descriptor refused");
        netActual=(ObjectSubNetInfo)PrefabSnapshot.Take(netExpected).Materialize();netActual.m_BezierCurve=new Colossal.Mathematics.Bezier4x3{a=new float3(1),b=new float3(2),c=new float3(3),d=new float3(4)};
        netActual.m_Upgrades=Array.Empty<NetPieceRequirements>();
        Check(MW.EditorTools.Modules.BulldozerRecovery.AreaNetDescriptorPolicy.Matches(netExpected,netActual),"serialized geometry is checked through complete graph; empty upgrade representations equivalent");
        Check(netExpected.m_BezierCurve.a.Equals(new float3(0))&&netExpected.m_Upgrades==null,"serialized comparison never modifies source snapshot");
        netActual.m_Upgrades=new[]{(NetPieceRequirements)1};Check(!MW.EditorTools.Modules.BulldozerRecovery.AreaNetDescriptorPolicy.Matches(netExpected,netActual),"upgrade requirement changes refused");netActual.m_Upgrades=null;
        Check(MW.EditorTools.Modules.BulldozerRecovery.AreaNetDescriptorPolicy.MatchesSet(new[]{netExpected,netExpected},new[]{netActual,netActual}),"repeated descriptor metadata matched by counts while native graph retains unique mapping");
        Check(!MW.EditorTools.Modules.BulldozerRecovery.AreaNetDescriptorPolicy.MatchesSet(new[]{netExpected},Array.Empty<ObjectSubNetInfo>()),"network descriptor deletion refused");
        netActual.m_BezierCurve.a=new float3(float.NaN);Check(!MW.EditorTools.Modules.BulldozerRecovery.AreaNetDescriptorPolicy.Matches(netExpected,netActual),"invalid serialized curve refused despite live graph authority");
        var beforeFields=new Dictionary<string,PrefabSnapshot>{{"ObjectSubNets.m_SubNets",PrefabSnapshot.Take(new[]{netExpected})},{"EffectSource.m_Effects",PrefabSnapshot.Take(new List<EffectSource.EffectSettings>())},{"ObjectSubAreas.m_SubAreas",PrefabSnapshot.Take(new[]{polygon})}};
        var normalizedNet=new ObjectSubNetInfo{m_NodeIndex=new int2(2,3)};
        var afterFields=new Dictionary<string,PrefabSnapshot>{{"ObjectSubNets.m_SubNets",PrefabSnapshot.Take(new[]{normalizedNet})},{"EffectSource.m_Effects",PrefabSnapshot.Take(new List<EffectSource.EffectSettings>())},{"ObjectSubAreas.m_SubAreas",PrefabSnapshot.Take(Array.Empty<ObjectSubAreaInfo>())}};
        var originalAreaImage=beforeFields["ObjectSubAreas.m_SubAreas"];var originalNetImage=beforeFields["ObjectSubNets.m_SubNets"];int normalizedFields=0;
        MW.EditorTools.Modules.BulldozerRecovery.NormalizedAreaFields.Apply(beforeFields,afterFields,(key,value)=>{beforeFields[key]=value;normalizedFields++;});
        Check(normalizedFields==2&&ReferenceEquals(beforeFields["ObjectSubNets.m_SubNets"],afterFields["ObjectSubNets.m_SubNets"]),"verified normalized ancillary images used for restore");
        Check(ReferenceEquals(originalAreaImage,beforeFields["ObjectSubAreas.m_SubAreas"])&&originalAreaImage.Matches(new[]{polygon}),"deleted area before-image unchanged");
        Check(originalNetImage.Matches(new[]{netExpected})&&netExpected.m_NodeIndex.Equals(new int2(-1,-1)),"original source and detached image remain unmodified");
        normalizedFields=0;MW.EditorTools.Modules.BulldozerRecovery.NormalizedAreaFields.Apply(beforeFields,new Dictionary<string,PrefabSnapshot>(),(key,value)=>normalizedFields++);
        Check(normalizedFields==0,"absent ancillary component not introduced");
        var areaIdentity=(AreaPrefab)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(SpacePrefab));
        var surfaceIdentity=(AreaPrefab)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(SurfacePrefab));
        var targetRow=new ObjectSubAreaInfo{m_AreaPrefab=areaIdentity,m_NodePositions=polygon.m_NodePositions.ToArray(),m_ParentMeshes=polygon.m_ParentMeshes?.ToArray()};
        var retainedRow=new ObjectSubAreaInfo{m_AreaPrefab=surfaceIdentity,m_NodePositions=polygon.m_NodePositions.ToArray(),m_ParentMeshes=polygon.m_ParentMeshes?.ToArray()};
        var rewrittenRow=new ObjectSubAreaInfo{m_AreaPrefab=surfaceIdentity,m_NodePositions=new[]{new float3(1),new float3(2),new float3(3),new float3(4)},m_ParentMeshes=new[]{-1,-1,-1,-1}};
        Check(MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.ReferencesAfterRemoval(new[]{targetRow,retainedRow},new[]{rewrittenRow},areaIdentity),"retained surface serialization may change only with separate full live-area validation");
        Check(!MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.ReferencesAfterRemoval(new[]{targetRow,retainedRow},new[]{targetRow},areaIdentity),"wrong prefab removal refused");
        Check(!MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.ReferencesAfterRemoval(new[]{targetRow,retainedRow},Array.Empty<ObjectSubAreaInfo>(),areaIdentity),"multiple area removals refused");
        Check(!MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.ReferencesAfterRemoval(new[]{targetRow,retainedRow},new[]{rewrittenRow},null),"missing native target identity refused");
        rewrittenRow.m_NodePositions[0]=new float3(float.NaN);Check(!MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.ReferencesAfterRemoval(new[]{targetRow,retainedRow},new[]{rewrittenRow},areaIdentity),"nonfinite serialized polygon refused");
        rewrittenRow.m_NodePositions[0]=new float3(1);rewrittenRow.m_ParentMeshes=new[]{-1};Check(!MW.EditorTools.Modules.BulldozerRecovery.AreaRemovalPolicy.ReferencesAfterRemoval(new[]{targetRow,retainedRow},new[]{rewrittenRow},areaIdentity),"invalid serialized parent mesh shape refused");
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
