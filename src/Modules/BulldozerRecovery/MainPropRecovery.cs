using System;
using System.Linq;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private sealed class MainEntry {internal MainPropImage Image;internal Entity Live;}
        private readonly MW.EditorTools.History.LastDeletion<MainEntry> mainHistory=new MW.EditorTools.History.LastDeletion<MainEntry>();
        private MainEntry mainPending;
        private MainPropImage mainRecord=>mainPending?.Image??(mainHistory.Latest??mainHistory.NextRedo)?.Image;
        private Entity mainLive;
        private bool mainArmed,mainBusy,mainUndoOperation,mainDisabled;
        private int mainOperationFrame;
        internal bool HasMainRecord=>mainRecord!=null;
        internal int MainUndoCount=>mainHistory.UndoCount;
        internal int MainRedoCount=>mainHistory.RedoCount;
        internal bool MainHistoryEnabled=>mainArmed&&!mainDisabled;
        internal bool CanArmMain=>!mainDisabled&&!Busy&&context.EditorAvailable&&!context.DialogBusy;
        internal void ArmMain(){if(MainHistoryEnabled)return;if(!CanArmMain)return;Invalidate();mainArmed=true;Status="Standalone prop Undo enabled (experimental). Main- or Everything-delete a supported standalone prop, then use Undo or Redo. Use a disposable editor session; do not save.";Mod.Log.Info(Status);}
        private void ClearMain(){
            if(mainBusy){FailMain("Main recovery interrupted. Discard the disposable editor test state.");return;}
            mainHistory.Clear();mainPending=null;mainImage=null;mainLive=Entity.Null;mainArmed=false;
        }
        private void FailMain(string message){mainDisabled=true;mainBusy=false;mainArmed=false;mainHistory.Clear();mainPending=null;mainImage=null;mainLive=Entity.Null;Status=message;Mod.Log.Error(new InvalidOperationException(message),"Experimental Main recovery disabled; discard the disposable editor test state.");}
        internal bool CanMainUndo=>MainReady(true);
        internal bool CanMainRedo=>MainReady(false);
        private bool MainReady(bool undo){
            try {
                var entry=undo?mainHistory.Latest:mainHistory.NextRedo;
                if(mainDisabled||Busy||entry==null||!context.EditorAvailable||context.DialogBusy||!entry.Image.DetachedValid)return false;
                if(undo)return entry.Live==Entity.Null&&MainSourcesGone(entry.Image);
                return MainLiveMatches(entry,entry.Live);
            }catch{return false;}
        }
        private bool MainLiveMatches(MainEntry entry,Entity live){
            if(live==Entity.Null||!world.EntityManager.Exists(live)||world.EntityManager.HasComponent<Game.Common.Deleted>(live))return false;
            var value=CaptureMainProp(live,false);
            return ReferenceEquals(value.Prefab,entry.Image.Prefab)&&entry.Image.Data.Matches(value.Data)&&MainNestedMatches(entry.Image,value);
        }
        internal void MainUndo()=>Guard(()=>StartMain(true));
        internal void MainRedo()=>Guard(()=>StartMain(false));
        private void StartMain(bool undo){
            if(!MainReady(undo))return;
            mainPending=undo?mainHistory.Latest:mainHistory.NextRedo;
            mainLive=mainPending.Live;
            mainBusy=true;mainUndoOperation=undo;mainOperationFrame=Time.frameCount;mainNestedValuesRestored=false;mainNestedStableFrames=0;
            try {
                var em=world.EntityManager;
                if(undo){
                    var prefabs=world.GetExistingSystemManaged<PrefabSystem>();var prefab=prefabs.GetEntity(mainRecord.Prefab);
                    if(!em.Exists(prefab)||!em.HasComponent<ObjectData>(prefab))throw new NotSupportedException("Native prefab archetype unavailable.");
                    var archetype=em.GetComponentData<ObjectData>(prefab).m_Archetype;
                    using(var types=archetype.GetComponentTypes(Allocator.Temp)){
                        var names=types.Select(type=>type.GetManagedType()?.FullName??"unknown").ToArray();
                        Mod.Log.Info("Main native creation archetype: "+string.Join(",",names));
                        var unknown=names.Where(name=>!MainKnownTypes.Contains(name)&&name!="Game.Effects.EnabledEffect"&&name!="Game.Objects.Plant"&&name!="Game.Objects.Tree"&&name!="Game.Simulation.UpdateFrame"&&name!="Game.Common.Created"&&name!="Game.Common.Updated"&&name!="Game.Common.Applied").ToArray();
                        if(unknown.Length!=0)throw new NotSupportedException("Unknown archetype components: "+string.Join(",",unknown));
                    }
                    using(var created=new NativeArray<Entity>(1,Allocator.Temp)){em.CreateEntity(archetype,created);mainLive=created[0];}
                    if(mainRecord.Group!=null&&!em.HasBuffer<Game.Objects.SubObject>(mainLive))em.AddBuffer<Game.Objects.SubObject>(mainLive);
                    if(mainRecord.Group!=null){var root=mainRecord.Group[0];if(root.HasColor)PutMain(mainLive,root.Color);if(root.HasDecoration){PutMain(mainLive,new Game.Objects.Decoration());em.SetComponentEnabled<Game.Objects.Decoration>(mainLive,root.DecorationEnabled);}if(root.HasPlant)PutMain(mainLive,root.Plant);if(root.HasTree)PutMain(mainLive,root.Tree);}
                    var data=mainRecord.Data.Clone();
                    PutMain(mainLive,new PrefabRef{m_Prefab=prefab});PutMain(mainLive,data.Transform);PutMain(mainLive,data.Seed);PutMain(mainLive,data.Surface);
                    if(data.Colours!=null){var buffer=em.HasBuffer<Game.Rendering.MeshColor>(mainLive)?em.GetBuffer<Game.Rendering.MeshColor>(mainLive):em.AddBuffer<Game.Rendering.MeshColor>(mainLive);buffer.Clear();foreach(var value in data.Colours)buffer.Add(value);}
                    if(data.CustomColours!=null){var buffer=em.HasBuffer<Game.Rendering.CustomMeshColor>(mainLive)?em.GetBuffer<Game.Rendering.CustomMeshColor>(mainLive):em.AddBuffer<Game.Rendering.CustomMeshColor>(mainLive);buffer.Clear();foreach(var value in data.CustomColours)buffer.Add(value);}
                    PutMain(mainLive,new Game.Common.Applied());PutMain(mainLive,new Game.Common.Created());PutMain(mainLive,new Game.Common.Updated());
                    Mod.Log.Info("Experimental Main Undo created fresh entity="+mainLive+" original="+mainRecord.Original+" prefab="+mainRecord.Prefab.name+"; awaiting native initialization.");
                }else{em.AddComponent<Game.Common.Deleted>(mainLive);Mod.Log.Info("Experimental Main Redo requested deletion of recreated entity="+mainLive);}
                Status="Waiting for experimental Main "+(undo?"Undo initialization":"Redo deletion")+"…";
            }catch(Exception error){
                if(undo&&mainLive!=Entity.Null&&world.EntityManager.Exists(mainLive)){try{world.EntityManager.DestroyEntity(mainLive);}catch(Exception cleanup){Mod.Log.Warn("Main synchronous creation cleanup failed: "+cleanup.Message);}}
                Mod.Log.Warn("Main recovery request failed: "+error.Message);FailMain("Main recovery failed. Discard the disposable editor test state.");
            }
        }
        private void PutMain<T>(Entity entity,T value) where T:unmanaged,IComponentData {if(world.EntityManager.HasComponent<T>(entity))world.EntityManager.SetComponentData(entity,value);else world.EntityManager.AddComponentData(entity,value);}
        private void TickMainRecovery(){
            if(!context.EditorAvailable){ClearMain();return;}
            if(!mainBusy)return;
            int age=Time.frameCount-mainOperationFrame;
            if(age<2)return;
            bool complete=false;
            if(mainUndoOperation){try{complete=RestoreMainNestedValues()&&MainLiveMatches(mainPending,mainLive);}catch(Exception error){if(age>120)Mod.Log.Info("Main initialization mismatch: "+error.Message);}}
            else complete=!world.EntityManager.Exists(mainLive);
            if(complete){
                bool moved=mainUndoOperation?mainHistory.Undo(entry=>ReferenceEquals(entry,mainPending)):mainHistory.Redo(entry=>ReferenceEquals(entry,mainPending));
                if(!moved){FailMain("Main history changed before native completion. Discard the disposable editor test state.");return;}
                mainPending.Live=mainUndoOperation?mainLive:Entity.Null;
                mainPending=null;mainBusy=false;mainLive=Entity.Null;
                Status=mainUndoOperation?"Undid standalone Main prop deletion (experimental)":"Redid standalone Main prop deletion (experimental)";
                Mod.Log.Info(Status+"; captured properties verified or recreated entity deletion confirmed; Undo="+MainUndoCount+" Redo="+MainRedoCount);
            }
            else if(age>120)FailMain("Main recovery did not validate within its native update window. Discard the disposable editor test state.");
        }
    }
}
