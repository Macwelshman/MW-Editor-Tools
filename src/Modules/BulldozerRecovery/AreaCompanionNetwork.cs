using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed class AreaCompanionNetwork {
        internal sealed class Anchor {
            internal Entity Id,Owner;
            internal PrefabBase Prefab;
            internal Game.Objects.Transform Transform;
            internal bool HasLocal;
            internal Game.Tools.LocalTransformCache Local;
        }
        internal Anchor[] Anchors=Array.Empty<Anchor>();
        internal sealed class Item {
            internal Entity Id,Owner;
            internal PrefabBase Prefab,Container;
            internal bool HasNode,HasEdge,HasLane,HasCurve,HasLocal,HasContainer,HasSeed;
            internal Game.Net.Node Node;
            internal Game.Net.Edge Edge;
            internal Game.Net.Lane Lane;
            internal Game.Net.Curve Curve;
            internal Game.Tools.LocalCurveCache Local;
            internal Game.Tools.EditorContainer ContainerData;
            internal Game.Common.PseudoRandomSeed Seed;
            internal Entity[] Connections=Array.Empty<Entity>();
            internal Entity[] SubNets=Array.Empty<Entity>();
            internal Game.Net.SubLane[] SubLanes=Array.Empty<Game.Net.SubLane>();
        }
        internal Entity Owner;
        internal Item[] Items=Array.Empty<Item>();
        internal Entity[] RootNets=Array.Empty<Entity>();
        internal Game.Net.SubLane[] RootLanes=Array.Empty<Game.Net.SubLane>();
        internal static bool SourcesValid(IEnumerable<Entity> sources,IEnumerable<Entity> live,Func<Entity,bool> exists) {
            var ids=new HashSet<Entity>(live);return sources.All(id=>!exists(id)||ids.Contains(id));
        }
        internal bool TryMap(AreaCompanionNetwork actual,bool seeds,out Dictionary<Entity,Entity> map,bool compareLocal=true,bool nativeRounding=false) {
            if(actual==null){map=null;return false;}
            map=new Dictionary<Entity,Entity>{{Owner,actual.Owner}};
            if(Items.Length!=actual.Items.Length||!MapAnchors(actual,map,nativeRounding))return false;
            var used=new HashSet<Entity>();
            // A surviving full Entity identity is stronger than a geometric
            // match. Reserve survivors first so coincident peers cannot steal
            // their matches. All typed data and topology are still checked.
            foreach(var expected in Items) {
                var survivors=actual.Items.Where(value=>value.Id==expected.Id).ToArray();
                if(survivors.Length==0)continue;
                if(survivors.Length!=1||!SameData(expected,survivors[0],seeds,compareLocal,nativeRounding)||!used.Add(expected.Id)||map.ContainsKey(expected.Id))return false;
                map.Add(expected.Id,expected.Id);
            }
            foreach(var expected in Items) {
                if(map.ContainsKey(expected.Id))continue;
                var candidates=actual.Items.Where(value=>!used.Contains(value.Id)&&SameData(expected,value,seeds,compareLocal,nativeRounding)).ToArray();
                if(candidates.Length!=1)return false;
                used.Add(candidates[0].Id);map.Add(expected.Id,candidates[0].Id);
            }
            var mapping=map;
            if(!SameLinks(RootNets,actual.RootNets,mapping)||!SameLaneLinks(RootLanes,actual.RootLanes,mapping))return false;
            foreach(var expected in Items) {
                var value=actual.Items.Single(i=>i.Id==mapping[expected.Id]);
                if(!mapping.TryGetValue(expected.Owner,out var owner)||owner!=value.Owner)return false;
                if(expected.HasEdge&&(!mapping.TryGetValue(expected.Edge.m_Start,out var start)||start!=value.Edge.m_Start||
                    !mapping.TryGetValue(expected.Edge.m_End,out var end)||end!=value.Edge.m_End))return false;
                if(expected.HasLane&&(!SamePath(expected.Lane.m_StartNode,value.Lane.m_StartNode,mapping)||
                    !SamePath(expected.Lane.m_MiddleNode,value.Lane.m_MiddleNode,mapping)||!SamePath(expected.Lane.m_EndNode,value.Lane.m_EndNode,mapping)))return false;
                if(!SameLinks(expected.Connections,value.Connections,mapping)||!SameLinks(expected.SubNets,value.SubNets,mapping)||
                    !SameLaneLinks(expected.SubLanes,value.SubLanes,mapping))return false;
            }
            return true;
        }
        private bool MapAnchors(AreaCompanionNetwork actual,Dictionary<Entity,Entity> map,bool rounding) {
            if(Anchors.Length!=actual.Anchors.Length)return false;
            var used=new HashSet<Entity>();
            foreach(var anchor in Anchors){
                var survivors=actual.Anchors.Where(a=>a.Id==anchor.Id).ToArray();
                if(survivors.Length==0)continue;
                if(survivors.Length!=1||!SameAnchor(anchor,survivors[0],rounding)||map.ContainsKey(anchor.Id)||!used.Add(anchor.Id))return false;
                map.Add(anchor.Id,anchor.Id);
            }
            foreach(var anchor in Anchors.Where(a=>!map.ContainsKey(a.Id))){
                var matches=actual.Anchors.Where(a=>!used.Contains(a.Id)&&SameAnchor(anchor,a,rounding)).ToArray();
                if(matches.Length!=1)return false;used.Add(matches[0].Id);map.Add(anchor.Id,matches[0].Id);
            }
            foreach(var anchor in Anchors){var value=actual.Anchors.Single(a=>a.Id==map[anchor.Id]);
                if(!map.TryGetValue(anchor.Owner,out var owner)||owner!=value.Owner)return false;}
            return true;
        }
        private static bool SameAnchor(Anchor a,Anchor b,bool rounding)=>!ReferenceEquals(a.Prefab,null)&&ReferenceEquals(a.Prefab,b.Prefab)&&a.HasLocal==b.HasLocal&&
            Near(a.Transform.m_Position,b.Transform.m_Position,rounding)&&math.all(math.isfinite(a.Transform.m_Rotation.value))&&math.all(math.isfinite(b.Transform.m_Rotation.value))&&
            math.min(math.lengthsq(a.Transform.m_Rotation.value-b.Transform.m_Rotation.value),math.lengthsq(a.Transform.m_Rotation.value+b.Transform.m_Rotation.value))<1e-8f&&
            (!a.HasLocal||(math.all(math.isfinite(a.Local.m_Position))&&math.all(math.isfinite(a.Local.m_Rotation.value))&&a.Local.Equals(b.Local)));
        private static bool SameLinks(Entity[] expected,Entity[] actual,Dictionary<Entity,Entity> map)=>expected.Length==actual.Length&&
            expected.Distinct().Count()==expected.Length&&actual.Distinct().Count()==actual.Length&&expected.All(map.ContainsKey)&&
            expected.Select(id=>map[id]).OrderBy(id=>id.Index).SequenceEqual(actual.OrderBy(id=>id.Index));
        private static bool SameLaneLinks(Game.Net.SubLane[] expected,Game.Net.SubLane[] actual,Dictionary<Entity,Entity> map)=>
            SameLinks(expected.Select(l=>l.m_SubLane).ToArray(),actual.Select(l=>l.m_SubLane).ToArray(),map)&&
            expected.All(l=>actual.Single(a=>a.m_SubLane==map[l.m_SubLane]).m_PathMethods==l.m_PathMethods);
        private static bool SamePath(Game.Pathfind.PathNode expected,Game.Pathfind.PathNode actual,Dictionary<Entity,Entity> map) {
            int oldIndex=expected.GetOwnerIndex();
            // Empty native path nodes have no owner; retain all their bits.
            if(oldIndex==0)return expected.Equals(actual);
            var keys=map.Keys.Where(id=>id.Index==oldIndex).ToArray();
            if(keys.Length!=1||map[keys[0]].Index!=actual.GetOwnerIndex())return false;
            expected.SetOwner(Entity.Null);actual.SetOwner(Entity.Null);
            return expected.Equals(actual);
        }
        private static bool SameData(Item a,Item b,bool seeds,bool compareLocal,bool nativeRounding)=>ReferenceEquals(a.Prefab,b.Prefab)&&ReferenceEquals(a.Container,b.Container)&&
            a.HasNode==b.HasNode&&a.HasEdge==b.HasEdge&&a.HasLane==b.HasLane&&a.HasCurve==b.HasCurve&&a.HasLocal==b.HasLocal&&
            a.HasContainer==b.HasContainer&&a.HasSeed==b.HasSeed&&(!a.HasNode||(Near(a.Node.m_Position,b.Node.m_Position,nativeRounding)&&
                math.min(math.lengthsq(a.Node.m_Rotation.value-b.Node.m_Rotation.value),math.lengthsq(a.Node.m_Rotation.value+b.Node.m_Rotation.value))<1e-8f))&&
            (!a.HasCurve||(SameCurve(a.Curve.m_Bezier,b.Curve.m_Bezier,nativeRounding,a.HasLane?8:4)&&math.isfinite(a.Curve.m_Length)&&math.isfinite(b.Curve.m_Length)&&math.abs(a.Curve.m_Length-b.Curve.m_Length)<1e-4f))&&(!a.HasLocal||!compareLocal||SameCurve(a.Local.m_Curve,b.Local.m_Curve))&&
            (!a.HasContainer||(a.ContainerData.m_Scale.Equals(b.ContainerData.m_Scale)&&a.ContainerData.m_Intensity.Equals(b.ContainerData.m_Intensity)&&a.ContainerData.m_GroupIndex==b.ContainerData.m_GroupIndex))&&
            (!seeds||!a.HasSeed||a.Seed.Equals(b.Seed));
        private static bool Near(float3 a,float3 b,bool nativeRounding=false,int precisionSteps=4) {
            if(!math.all(math.isfinite(a))||!math.all(math.isfinite(b)))return false;
            if(!nativeRounding)return math.lengthsq(a-b)<1e-8f;
            // Four float precision steps for owner/world transform round trips,
            // bounded to half a millimetre. Source and local-cache checks stay strict.
            var tolerance=math.min(new float3(0.0005f),math.max(new float3(0.0001f),math.max(math.abs(a),math.abs(b))*(precisionSteps*1.192092896e-7f)));
            return math.all(math.abs(a-b)<tolerance);
        }
        private static bool SameCurve(Colossal.Mathematics.Bezier4x3 a,Colossal.Mathematics.Bezier4x3 b,bool nativeRounding=false,int precisionSteps=4)=>Near(a.a,b.a,nativeRounding,precisionSteps)&&Near(a.b,b.b,nativeRounding,precisionSteps)&&Near(a.c,b.c,nativeRounding,precisionSteps)&&Near(a.d,b.d,nativeRounding,precisionSteps);
        internal static bool CanRestoreLocal(Game.Tools.LocalCurveCache expected,Game.Tools.LocalCurveCache actual)=>Finite(expected.m_Curve)&&(!Finite(actual.m_Curve)||SameCurve(expected.m_Curve,actual.m_Curve));
        internal string ExplainMismatch(AreaCompanionNetwork actual,bool seeds,bool compareLocal=true,bool nativeRounding=false) {
            if(actual==null)return "missing graph";
            if(Items.Length!=actual.Items.Length)return "item count expected="+Items.Length+" actual="+actual.Items.Length;
            var anchors=new Dictionary<Entity,Entity>{{Owner,actual.Owner}};if(!MapAnchors(actual,anchors,nativeRounding))return "owned object path anchors differ or are ambiguous; expected="+Anchors.Length+" actual="+actual.Anchors.Length;
            if(TryMap(actual,seeds,out var map,compareLocal,nativeRounding))return "none";
            foreach(var expected in Items)if(!map.ContainsKey(expected.Id))return "typed mapping expected "+Describe(expected)+"; candidates "+string.Join(" | ",actual.Items.Where(a=>ReferenceEquals(a.Prefab,expected.Prefab)&&a.HasNode==expected.HasNode&&a.HasEdge==expected.HasEdge&&a.HasLane==expected.HasLane).Take(4).Select(Describe));
            if(!SameLinks(RootNets,actual.RootNets,map))return "root network membership";
            if(!SameLaneLinks(RootLanes,actual.RootLanes,map))return "root lane membership/path methods";
            foreach(var expected in Items){var value=actual.Items.Single(i=>i.Id==map[expected.Id]);
                if(!map.TryGetValue(expected.Owner,out var owner)||owner!=value.Owner)return "owner expected "+Describe(expected)+" actual "+Describe(value);
                if(expected.HasEdge&&(!map.TryGetValue(expected.Edge.m_Start,out var start)||start!=value.Edge.m_Start||!map.TryGetValue(expected.Edge.m_End,out var end)||end!=value.Edge.m_End))return "edge endpoints expected "+Describe(expected)+" actual "+Describe(value);
                if(expected.HasLane&&(!SamePath(expected.Lane.m_StartNode,value.Lane.m_StartNode,map)||!SamePath(expected.Lane.m_MiddleNode,value.Lane.m_MiddleNode,map)||!SamePath(expected.Lane.m_EndNode,value.Lane.m_EndNode,map)))return "lane path nodes expected "+Describe(expected)+" actual "+Describe(value);
                if(!SameLinks(expected.Connections,value.Connections,map)||!SameLinks(expected.SubNets,value.SubNets,map)||!SameLaneLinks(expected.SubLanes,value.SubLanes,map))return "connections/subnets/sublanes expected "+Describe(expected)+" actual "+Describe(value);
            }
            return "graph differs";
        }
        internal static string Describe(Item i)=>"id="+i.Id+" owner="+i.Owner+" prefab="+(ReferenceEquals(i.Prefab,null)?"null":i.Prefab.name)+
            " container="+i.HasContainer+":"+(ReferenceEquals(i.Container,null)?"null":i.Container.name)+"/"+i.ContainerData.m_Scale+"/"+i.ContainerData.m_Intensity+"/"+i.ContainerData.m_GroupIndex+
            " node="+i.HasNode+":"+i.Node.m_Position+"/"+i.Node.m_Rotation.value+" edge="+i.HasEdge+":"+i.Edge.m_Start+"/"+i.Edge.m_End+
            " curve="+i.HasCurve+":"+i.Curve.m_Bezier.a+"/"+i.Curve.m_Bezier.b+"/"+i.Curve.m_Bezier.c+"/"+i.Curve.m_Bezier.d+" length="+i.Curve.m_Length+
            " local="+i.HasLocal+":"+i.Local.m_Curve.a+"/"+i.Local.m_Curve.b+"/"+i.Local.m_Curve.c+"/"+i.Local.m_Curve.d+" seed="+i.HasSeed+":"+i.Seed.m_Seed+
            " paths="+(i.HasLane?Path(i.Lane.m_StartNode)+"/"+Path(i.Lane.m_MiddleNode)+"/"+Path(i.Lane.m_EndNode):"-")+
            " connections="+string.Join(",",i.Connections.Select(e=>e.ToString()))+" subnets="+i.SubNets.Length+" sublanes="+i.SubLanes.Length;
        private static string Path(Game.Pathfind.PathNode node)=>"owner="+node.GetOwnerIndex()+" bits="+string.Join(",",typeof(Game.Pathfind.PathNode).GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Select(f=>f.Name+":"+f.GetValue(node)));
        private static bool Finite(Colossal.Mathematics.Bezier4x3 curve)=>math.all(math.isfinite(curve.a))&&math.all(math.isfinite(curve.b))&&math.all(math.isfinite(curve.c))&&math.all(math.isfinite(curve.d));
    }
}
