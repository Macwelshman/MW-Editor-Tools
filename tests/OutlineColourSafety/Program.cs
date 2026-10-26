using System;
using System.Reflection;
using Unity.Entities;
using UnityEngine;
using Game.Rendering;
using MW.EditorTools.Modules.Targeting;
class Program {
 static int checks;static void Check(bool v,string text){if(!v)throw new Exception(text);checks++;}
 static Entity Id(int i,int version=0)=>new(){Index=i,Version=version};
 static bool Same(Color a,Color b)=>a.r==b.r&&a.g==b.g&&a.b==b.b&&a.a==b.a;
 static Color Blue=>new(.2f,.3f,.8f,1);static Color Gold=>new Color(1,.8f,.15f,1).linear;static Color Cyan=>new Color(.1f,.9f,1,1).linear;
 static void Main(){
 var world=new World{Manager=new()};var manager=world.Manager;var store=manager.Store;var em=world.EntityManager;var target=new EditorTargetingTool{Hover=Id(1),Selected=Id(2)};target.Owned.UnionWith(new[]{Id(1),Id(2)});
 var system=new OutlineColourSystem{World=world};bool refused=false;try{system.Initialize(target);}catch(NotSupportedException){refused=true;}Check(refused,"unverified API build refuses prototype");typeof(OutlineColourSystem).GetField("targeting",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(system,target);
 Add(world,Id(1),0,0);Add(world,Id(2),0,1);Add(world,Id(3),0,2);int writes=em.Writes;
 system.Tick();Check(Same(store.Colours[(0,7,0)],Gold)&&Same(store.Colours[(0,7,1)],Cyan),"simultaneous gold hover and cyan selection");Check(Same(store.Colours[(0,7,2)],Blue),"unrelated object sharing material/group untouched");Check(em.Writes==writes,"renderer never writes ECS or asset data");Check(manager.Readers==1&&manager.Writers==1&&Unity.Jobs.JobHandle.Completions>0,"dependencies completed and writer access registered");
 system.Tick();Check(Same(store.Colours[(0,7,0)],Gold)&&Same(store.Colours[(0,7,1)],Cyan),"stable repeated frames retain colours");
 target.Hover=Id(2);system.Tick();Check(Same(store.Colours[(0,7,0)],Blue)&&Same(store.Colours[(0,7,1)],Cyan),"retired hover restored and selected wins over hover");
 var foreign=new Color(.9f,.2f,.4f,1);store.Colours[(0,7,1)]=foreign;target.Hover=target.Selected=Entity.Null;system.Tick();Check(Same(store.Colours[(0,7,1)],foreign),"cleanup preserves later foreign/native colour change");
 store.Colours[(0,7,1)]=Blue;target.Hover=Id(1);target.Selected=Id(2);system.Tick();store.Identities[(0,0)]=new InstanceData{m_Entity=Id(1,1)};target.Hover=Entity.Null;system.Tick();Check(Same(store.Colours[(0,7,0)],Gold),"recycled render slot is never restored through stale entity identity");store.Identities[(0,0)]=new InstanceData{m_Entity=Id(1)};store.Colours[(0,7,0)]=Blue;
 target.Hover=Id(1);target.Owned.Remove(Id(1));system.Tick();Check(Same(store.Colours[(0,7,0)],Blue),"pre-existing foreign highlight is never recoloured");target.Owned.Add(Id(1));
 em.Put(Id(1),new Game.Tools.Warning());store.Colours[(0,7,0)]=foreign;system.Tick();Check(Same(store.Colours[(0,7,0)],foreign),"native warning colour wins");em.Data[Id(1)].Remove(typeof(Game.Tools.Warning));store.Colours[(0,7,0)]=Blue;
 Add(world,Id(4),1,0);em.Put(Id(4),new Game.Net.Edge());store.Colours[(1,13,0)]=Blue;target.Hover=Id(4);target.Owned.Add(Id(4));system.Tick();Check(Same(store.Colours[(1,13,0)],Gold)&&Same(store.Colours[(1,7,0)],Blue),"network uses its own outline property only");
 Add(world,Id(5),2,0);em.Put(Id(5),new Game.Net.Lane());store.Colours[(2,5,0)]=Blue;target.Selected=Id(5);target.Owned.Add(Id(5));system.Tick();Check(Same(store.Colours[(2,5,0)],Cyan),"lane selection uses lane outline property");
 target.Ready=false;system.Tick();Check(Same(store.Colours[(1,13,0)],Blue)&&Same(store.Colours[(2,5,0)],Blue),"dialog/context/tool pause restores all owned colours");target.Ready=true;
 var batches=em.GetBuffer<MeshBatch>(Id(1),true);batches.Add(batches[0]);target.Hover=Id(1);target.Selected=Entity.Null;system.Tick();system.Release();Check(Same(store.Colours[(0,7,0)],Blue),"duplicate batch records do not corrupt original-colour restoration");system.Release();Check(!system.Enabled,"release idempotently disables optional renderer");
 typeof(OutlineColourSystem).GetField("targeting",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(system,target);system.Enabled=true;store.FailAt=store.Writes+1;system.Tick();Check(!system.Enabled&&MW.EditorTools.Mod.Log.Errors>0&&target.Status.Contains("unavailable"),"writer failure disables only optional colour renderer");Check(Same(store.Colours[(0,7,0)],Blue),"failed write cleanup preserves original colour");
 Check(em.Writes==writes+5,"only explicit test setup changed ECS values");
 target.Hover=target.Selected=Entity.Null;system.Enabled=true;int completed=Unity.Jobs.JobHandle.Completions;system.Tick();Check(Unity.Jobs.JobHandle.Completions==completed,"inactive clean renderer adds no job fence or render-buffer work");
 Add(world,Id(6),3,0);target.Owned.Add(Id(6));var multi=em.GetBuffer<MeshBatch>(Id(6),true);multi.Add(new MeshBatch{m_GroupIndex=3,m_InstanceIndex=1,m_MeshIndex=1});store.Identities[(3,1)]=new InstanceData{m_Entity=Id(6),m_MeshIndex=1};store.Colours[(3,7,1)]=Blue;target.Selected=Id(6);target.Hover=Entity.Null;system.Tick();Check(Same(store.Colours[(3,7,0)],Cyan)&&Same(store.Colours[(3,7,1)],Cyan),"all exact submeshes receive selected colour");system.Release();Check(Same(store.Colours[(3,7,0)],Blue)&&Same(store.Colours[(3,7,1)],Blue),"all submeshes restore on release");
 Console.WriteLine($"PASS: {checks} actual-source per-instance outline, identity, cleanup, dependency, priority and failure-isolation assertions. Native renderer acceptance pending.");
 }
 static void Add(World w,Entity id,int group,int instance){w.EntityManager.Put(id,new Buffer<MeshBatch>{new(){m_GroupIndex=group,m_InstanceIndex=instance}});w.Manager.Store.Groups[group]=new(){m_Mesh=Id(100+group)};w.Manager.Store.Identities[(group,instance)]=new(){m_Entity=id};w.Manager.Store.Colours[(group,7,instance)]=Blue;}
}
