using System;using System.Linq;using System.Collections.Generic;using Unity.Entities;using Unity.Mathematics;
namespace MW.EditorTools {static class Mod {internal static Logger Log=new();internal class Logger {internal void Info(string s){}}}}
namespace MW.EditorTools.Modules.BulldozerRecovery {
internal sealed partial class BulldozerTrace {
internal class NestedNode {internal Entity Original;internal int Parent,Depth,Local;internal object Prefab;internal bool HasColor,HasDecoration,DecorationEnabled;internal Game.Objects.Color Color;internal bool HasSurface,HasPlant,HasTree;internal Game.Objects.Plant Plant;internal Game.Objects.Tree Tree;internal MainPropData Data;}
internal class MainPropImage {internal Entity Original;internal NestedNode[] Group;internal bool DetachedValid=true;}
internal class FakeWorld {internal FakeManager EntityManager=new();}
internal class FakeManager {
internal NestedNode[] Nodes;internal HashSet<Entity> Existing=new();internal int Writes;
internal bool Exists(Entity id)=>Existing.Contains(id);
internal void SetComponentData<T>(Entity id,T value){Writes++;var n=Nodes.Single(n=>n.Original==id);object v=value;if(v is Game.Common.PseudoRandomSeed seed)n.Data=new(n.Data.Transform,seed,n.Data.Surface,null,null);else if(v is Game.Objects.Transform transform)n.Data=new(transform,n.Data.Seed,n.Data.Surface,null,null);else if(v is Game.Objects.Color color)n.Color=color;else if(v is Game.Objects.Plant plant)n.Plant=plant;else if(v is Game.Objects.Tree tree)n.Tree=tree;else throw new Exception("Unexpected write");}
internal void SetComponentEnabled<T>(Entity id,bool enabled){Writes++;Nodes.Single(n=>n.Original==id).DecorationEnabled=enabled;}
internal T[] GetBuffer<T>(Entity id)=>throw new Exception("These cases use absent colour buffers");
}
FakeWorld world=new();MainPropImage mainRecord;Entity mainLive;
NestedNode[] CaptureNestedImage(Entity id,bool log,bool standalone)=>world.EntityManager.Nodes;
static IEnumerable<Entity> GroupSources(NestedNode[] group)=>group.Select(n=>n.Original);
static bool SameNestedElevation(NestedNode a,NestedNode b,bool values=true)=>a.HasColor==b.HasColor&&a.HasDecoration==b.HasDecoration&&(!values||(a.Color.Equals(b.Color)&&a.DecorationEnabled==b.DecorationEnabled))&&a.HasPlant==b.HasPlant&&a.HasTree==b.HasTree&&(!values||(a.Plant.Equals(b.Plant)&&a.Tree.Equals(b.Tree)));
static bool SameNestedPlacement(NestedNode a,NestedNode b)=>a.Data.Transform.Equals(b.Data.Transform)&&ReferenceEquals(a.Prefab,b.Prefab);
static bool ExactNestedGroup(NestedNode[] a,NestedNode[] b)=>a.Length==b.Length&&a.Select((e,i)=>e.Parent==b[i].Parent&&e.Depth==b[i].Depth&&e.Local==b[i].Local&&ReferenceEquals(e.Prefab,b[i].Prefab)&&e.HasSurface==b[i].HasSurface&&SameNestedElevation(e,b[i])&&e.Data.Matches(b[i].Data)).All(v=>v);
static Entity Id(int i)=>new Entity{Index=i,Version=1};
static NestedNode Node(int id,int parent,object prefab,int seed=11)=>new(){Original=Id(id),Parent=parent,Depth=parent<0?0:1,Local=id%10,Prefab=prefab,HasSurface=true,Data=new(new Game.Objects.Transform{m_Position=new float3(id%10),m_Rotation=quaternion.identity},new Game.Common.PseudoRandomSeed{m_Seed=(ushort)seed},default,null,null)};
static BulldozerTrace Ready(){var a=new object();var b=new object();var t=new BulldozerTrace{mainLive=Id(11)};t.mainRecord=new(){Original=Id(1),Group=new[]{Node(1,-1,a),Node(2,0,b)}};t.world.EntityManager.Nodes=new[]{Node(11,-1,a,22),Node(12,0,b,33)};return t;}
static int checks;static void Check(bool v,string label){if(!v)throw new Exception(label);checks++;}
public static void Main(){
var t=Ready();Check(t.MainSourcesGone(t.mainRecord),"all source ids gone");t.world.EntityManager.Existing.Add(Id(2));Check(!t.MainSourcesGone(t.mainRecord),"orphan original child prevents readiness");t.world.EntityManager.Existing.Clear();
Check(!t.RestoreMainNestedValues()&&t.world.EntityManager.Writes==4,"complete matching tree restores typed values then waits");Check(!t.RestoreMainNestedValues()&&!t.RestoreMainNestedValues()&&t.RestoreMainNestedValues(),"three exact frames required");Check(t.world.EntityManager.Writes==4,"validation never repeats writes");Check(t.MainNestedMatches(t.mainRecord,new(){Group=t.world.EntityManager.Nodes}),"full tree equality required");
foreach(var failure in new[]{"missing","parent","depth","prefab","placement","cache","surface","source","reference","plantPresence"}){t=Ready();var nodes=t.world.EntityManager.Nodes;switch(failure){case "missing":t.world.EntityManager.Nodes=new[]{nodes[0]};break;case "parent":nodes[1].Parent=-1;break;case "depth":nodes[1].Depth=2;break;case "prefab":nodes[1].Prefab=new object();break;case "placement":nodes[1].Data=new(new Game.Objects.Transform(),nodes[1].Data.Seed,default,null,null);break;case "cache":nodes[1].Local=99;break;case "surface":nodes[1].HasSurface=false;break;case "source":nodes[1].Original=Id(2);break;case "reference":t.mainRecord.DetachedValid=false;break;case "plantPresence":nodes[1].HasPlant=true;break;}Check(!t.RestoreMainNestedValues()&&t.world.EntityManager.Writes==0,"preflight refuses "+failure+" before any write");}
t=Ready();t.mainRecord.Group[1].HasPlant=t.world.EntityManager.Nodes[1].HasPlant=true;t.mainRecord.Group[1].Plant=new(){m_Pollution=.75f};t.mainRecord.Group[1].HasTree=t.world.EntityManager.Nodes[1].HasTree=true;t.mainRecord.Group[1].Tree=new(){m_Growth=42};Check(!t.RestoreMainNestedValues()&&t.world.EntityManager.Nodes[1].Plant.m_Pollution==.75f&&t.world.EntityManager.Nodes[1].Tree.m_Growth==42,"vegetation state restored after full preflight");t.world.EntityManager.Nodes[1].Plant=new(){m_Pollution=.2f};Check(!t.RestoreMainNestedValues(),"vegetation change prevents final acceptance");
t=Ready();t.RestoreMainNestedValues();Check(!t.RestoreMainNestedValues(),"first exact frame waits");t.world.EntityManager.Nodes[1].Local=99;Check(!t.RestoreMainNestedValues(),"mismatch resets stability");t.world.EntityManager.Nodes[1].Local=2;Check(!t.RestoreMainNestedValues()&&!t.RestoreMainNestedValues()&&t.RestoreMainNestedValues(),"three consecutive exact frames after mismatch");
t=Ready();t.mainRecord.Group[0].HasColor=t.world.EntityManager.Nodes[0].HasColor=true;t.mainRecord.Group[0].Color=new(){m_Index=2,m_Value=42,m_SubColor=true};t.mainRecord.Group[0].HasDecoration=t.world.EntityManager.Nodes[0].HasDecoration=true;t.world.EntityManager.Nodes[0].DecorationEnabled=true;Check(!t.RestoreMainNestedValues()&&t.world.EntityManager.Nodes[0].Color.m_Value==42&&!t.world.EntityManager.Nodes[0].DecorationEnabled,"tree color and Decoration enable state restored after complete preflight");Check(!t.RestoreMainNestedValues()&&!t.RestoreMainNestedValues()&&t.RestoreMainNestedValues(),"tree metadata participates in exact completion");
t=Ready();t.mainRecord.Group=null;Check(t.RestoreMainNestedValues()&&t.world.EntityManager.Writes==0,"simple prop path unchanged");Check(!t.MainNestedMatches(t.mainRecord,new(){Group=t.world.EntityManager.Nodes}),"unexpected children rejected");
Console.WriteLine("PASS: "+checks+" actual-source standalone nested recovery assertions using managed ECS doubles; native regeneration unproven.");
}
}}


