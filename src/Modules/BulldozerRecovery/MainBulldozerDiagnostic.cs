using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private Entity[] mainTargets;
        private int mainStarted,mainAttempts,mainCheckpoint;
        private bool mainInApply,mainAccepted;
        internal Func<bool> AutomaticCaptureAllowed;
        private void EnableAutomaticMain(BulldozeToolSystem tool) {
            AutomaticStandaloneCapture.TryEnable(AutomaticCaptureAllowed?.Invoke()==true,
                mainArmed || mainDisabled || Busy || NestedTestRunning || context.DialogBusy,
                tool.actualMode==BulldozeToolSystem.Mode.MainElements || tool.actualMode==BulldozeToolSystem.Mode.Everything,
                ()=> {
                    var points=(NativeList<ControlPoint>)controlPoints.GetValue(tool);
                    if(!points.IsCreated)return false;
                    var targets=new HashSet<Entity>();
                    for(int i=0;i<points.Length;i++) {
                        if(points[i].m_OriginalEntity!=Entity.Null)targets.Add(points[i].m_OriginalEntity);
                        if(targets.Count>1)return false;
                    }
                    return targets.Count==1 && CaptureMainProp(targets.Single()).DetachedValid;
                }, ArmMain);
        }
        // Observe native deletion and record typed images; recovery always uses fresh entity IDs.
        private void MainBegin(BulldozeToolSystem tool) {
            if(mainBusy){FailMain("Another deletion interrupted Main recovery. Discard the disposable editor test state.");return;}
            if(mainArmed&&mainImage!=null){ClearMain();Status="Overlapping Main action; history cleared.";}
            if(tool.actualMode!=BulldozeToolSystem.Mode.MainElements && !(tool.actualMode==BulldozeToolSystem.Mode.Everything&&mainArmed)){ClearMain();return;}
            if(mainAttempts>=8&&!mainArmed)return;
            mainAttempts++;mainInApply=true;mainAccepted=false;mainStarted=Time.frameCount;mainCheckpoint=0;
            var points=(NativeList<ControlPoint>)controlPoints.GetValue(tool);
            var targets=new HashSet<Entity>();
            for(int i=0;i<points.Length&&targets.Count<8;i++)if(points[i].m_OriginalEntity!=Entity.Null)targets.Add(points[i].m_OriginalEntity);
            mainTargets=targets.ToArray();
            mainImage=null;
            Mod.Log.Info("Main bulldozer diagnostic #"+mainAttempts+" targets="+mainTargets.Length+"; recovery enabled="+MainHistoryEnabled);
            foreach(var entity in mainTargets)LogMainEntity("before",entity,true);
            if(mainTargets.Length==1)try{mainImage=CaptureMainProp(mainTargets[0]);}catch(Exception error){ClearMain();Mod.Log.Info("Main prop capture refused: "+error.Message);}else ClearMain();
        }
        private void LogMainEntity(string stage,Entity entity,bool details) {
            var em=world.EntityManager;bool exists=em.Exists(entity);
            string description="Main diagnostic "+stage+" entity="+entity+" exists="+exists;
            if(exists){
                description+=" deleted="+em.HasComponent<Game.Common.Deleted>(entity)+" temp="+em.HasComponent<Temp>(entity)+" owner="+(em.HasComponent<Game.Common.Owner>(entity)?em.GetComponentData<Game.Common.Owner>(entity).m_Owner.ToString():"none");
                if(em.HasComponent<PrefabRef>(entity)){var prefab=world.GetExistingSystemManaged<PrefabSystem>().GetPrefab<PrefabBase>(em.GetComponentData<PrefabRef>(entity));description+=" prefab="+(prefab==null?"null":prefab.name);}
                if(em.HasComponent<Game.Objects.Transform>(entity)){var t=em.GetComponentData<Game.Objects.Transform>(entity);description+=" position="+t.m_Position+" rotation="+t.m_Rotation.value;}
                if(em.HasBuffer<Game.Objects.SubObject>(entity))description+=" children="+em.GetBuffer<Game.Objects.SubObject>(entity,true).Length;
                if(details)using(var types=em.GetComponentTypes(entity,Allocator.Temp))description+=" components="+string.Join(",",types.Select(t=>(t.GetManagedType()?.FullName??("unresolved-type-index="+t.TypeIndex))+ (t.IsBuffer?" [Buffer]":"")));
            }
            Mod.Log.Info(description);
        }
        private void MainEnd() {
            if(!mainInApply)return;mainInApply=false;
            Mod.Log.Info("Main diagnostic native apply accepted="+mainAccepted+"; frame="+Time.frameCount);
            if(!mainAccepted){mainTargets=null;mainImage=null;return;}
            foreach(var entity in mainTargets)LogMainEntity("after Apply",entity,false);
        }
        private void MainTick() {
            if(mainTargets==null||mainInApply)return;
            if(!context.EditorAvailable){mainTargets=null;return;}
            int age=Time.frameCount-mainStarted;
            if(mainImage!=null&&!world.EntityManager.Exists(mainImage.Original)){
                Mod.Log.Info("Main standalone detached candidate after confirmed deletion: valid="+mainImage.DetachedValid+"; no live restoration performed.");
                if(mainArmed&&mainAccepted&&mainImage.DetachedValid&&!mainDisabled){mainHistory.Record(new MainEntry{Image=mainImage,Live=Entity.Null});Status="Recorded standalone Main prop deletion. Undo entries: "+MainUndoCount+".";Mod.Log.Info(Status);}
                mainImage=null;
            }
            int[] checkpoints={1,5,30,120};
            if(mainCheckpoint>=checkpoints.Length)return;
            if(age<checkpoints[mainCheckpoint])return;
            foreach(var entity in mainTargets)LogMainEntity("age="+age,entity,false);
            mainCheckpoint++;
            if(mainCheckpoint==checkpoints.Length){mainTargets=null;if(mainImage!=null){ClearMain();Status="Main deletion was not confirmed; history cleared.";}}
        }
    }
}


