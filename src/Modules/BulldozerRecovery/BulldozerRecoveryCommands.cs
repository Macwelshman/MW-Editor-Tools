using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private sealed class Record {
            internal Game.Objects.Transform AreaOwnerTransform; internal bool Area; internal AreaImage[] BeforeAreas,AfterAreas;
            internal PrefabBase[] RootTypes;
            internal PrefabBase Asset;internal Entity Owner;internal bool Last; internal NestedNode[] Nested,NestedLive; internal NestedNode[][] ForestBefore,ForestAfter,ForestFrom; internal Dictionary<PrefabBase,Payload> NestedReferences;
            internal Payload Before,After;internal GraphValue[] BeforeGraph,AfterGraph;
        }
        private sealed class GraphValue {
            internal PrefabBase Prefab,Container;
            internal bool HasTransform;
            internal float3 Position;
            internal quaternion Rotation;
        }
        private readonly MW.EditorTools.History.TransactionHistory<Record> records=new MW.EditorTools.History.TransactionHistory<Record>(20);
        private Record record=>records.Latest??records.NextRedo;
        private Record restoring;
        private GraphValue[] beforeGraph,afterGraph;
        private PrefabBase armedAsset;
        private bool armed,restoringUndo,disabled,sourceMatched,restoreSourceMatched;
        private int restoreFrame,finalizedFrame;
        private bool nativeFinalized;
        internal bool HasRecord=>record!=null||HasMainRecord;
        internal int UndoCount=>records.UndoCount+MainUndoCount;
        internal int RedoCount=>records.RedoCount+MainRedoCount;
        private static string Describe(Record entry)=>entry==null?null:entry.Area?"Area/surface deletion on "+entry.Asset.name:entry.Nested?.Length>0?
            (entry.Nested.Length==1?"Prop: ":"Prop group: ")+entry.Nested[0].Prefab.name+" on "+entry.Asset.name:
            "Prop deletion on "+entry.Asset.name;
        internal string UndoTarget=>HasMainRecord?mainHistory.Latest?.Image.Prefab.name:Describe(records.Latest);
        internal string RedoTarget=>HasMainRecord?mainHistory.NextRedo?.Image.Prefab.name:Describe(records.NextRedo);
        internal string[] Recent=>HasMainRecord?mainHistory.Recent.Reverse().Select(entry=>"Standalone prop: "+entry.Image.Prefab.name).ToArray():records.Recent.Reverse().Select(Describe).ToArray();
        internal void SetDepth(int depth){records.SetDepth(depth);mainHistory.SetDepth(depth);}
        internal string AssetName=>mainRecord?.Prefab?.name??record?.Asset?.name ?? armedAsset?.name;
        internal bool Busy=>preparingAsset!=null||nestedFixturePending||mainBusy||(mainArmed&&mainImage!=null)||restoring!=null||session.Asset!=null;
        internal bool CanArm=>!disabled && !Busy && context.EditorAvailable && !context.DialogBusy && context.Asset!=null && !context.Asset.isReadOnly;
        internal string StartTestBlockReason=>disabled?"Recovery stopped after an error. Restart CS2 without saving.":Busy?"Wait for the current operation to finish.":!context.EditorAvailable?"Open the asset editor first.":context.DialogBusy?"Close the open dialog or finish shortcut recording first.":context.Asset==null?"Select the outer test asset in Workspace so its inspector opens.":context.Asset.isReadOnly?"The selected asset is read-only. Select a disposable writable duplicate.":null;
        internal void ReportTestBlocked(string reason){Status="Cannot start test: "+reason;Mod.Log.Info(Status);}
        internal void Arm() {
            if(!CanArm){Status="Select a writable disposable duplicate asset first.";return;}
            ClearMain();
            ClearRecord();lastArmed=false;nestedArmed=false;nestedForestArmed=false;forestBefore=null;armed=true;armedAsset=context.Asset;
            Status="Armed for Sub-element deletion history on "+armedAsset.name+". Delete props individually; keep another prop in the asset.";
            Mod.Log.Info(Status);
        }
        internal void ClearRecord() {records.Clear();}
        private static int RecordWeight(Record entry)=>entry.Before.Weight+entry.After.Weight+AreaWeight(entry.BeforeAreas)+AreaWeight(entry.AfterAreas)+(entry.NestedReferences?.Values.Sum(image=>image.Weight)??0)+ForestWeight(entry.ForestBefore)+ForestWeight(entry.ForestAfter); private static int AreaWeight(AreaImage[] areas)=>areas?.Sum(a=>64+32*a.Nodes.Length+32*a.Local.Length)??0; private static int ForestWeight(NestedNode[][] forest)=>forest?.Sum(group=>group.Sum(node=>64+16*((node.Data.Colours?.Length??0)+(node.Data.CustomColours?.Length??0))))??0; private Record Candidate(bool undo)=>undo?records.Latest:records.NextRedo;
        internal void Invalidate() {
            ClearMain();
            ClearRecord();armed=false;lastArmed=false;nestedArmed=false;nestedForestArmed=false;forestBefore=null;nestedBefore=null;nestedReferences=null;armedAsset=null;
            if(restoring!=null){FailRecovery("Recovery interrupted by another edit or asset change. Restart CS2 without saving the disposable test.");return;}
            Reset("Sub-element test history cleared.");
        }
        private bool OwnerMatches(Record entry) {
            if(!context.EditorAvailable||entry.Asset==null||entry.Asset.isReadOnly||!world.EntityManager.Exists(entry.Owner)||world.EntityManager.HasComponent<Game.Common.Deleted>(entry.Owner)||!world.EntityManager.HasComponent<PrefabRef>(entry.Owner))return false;
            if(entry.Area&&(!world.EntityManager.HasComponent<Game.Objects.Transform>(entry.Owner)||!world.EntityManager.GetComponentData<Game.Objects.Transform>(entry.Owner).Equals(entry.AreaOwnerTransform)))return false;
            return ReferenceEquals(world.GetExistingSystemManaged<PrefabSystem>()?.GetPrefab<PrefabBase>(world.EntityManager.GetComponentData<PrefabRef>(entry.Owner)),entry.Asset);
        }
        private bool CanRestore(bool undo) {
            try {
                var candidate=Candidate(undo);
                if(disabled||Busy||candidate==null||!context.EditorAvailable||context.DialogBusy||!OwnerMatches(candidate))return false;
                if(Root!=null&&!ReferenceEquals(Root,candidate.Asset))return false;
                var prefabSystem=world.GetExistingSystemManaged<PrefabSystem>();
                return !((IDictionary)updateMap.GetValue(prefabSystem)).Contains(candidate.Asset) &&
                    (undo?candidate.After:candidate.Before).Matches(candidate.Asset) && (candidate.Area?AreasMatch(undo?candidate.AfterAreas:candidate.BeforeAreas,candidate.Owner):GraphMatches(undo?candidate.AfterGraph:candidate.BeforeGraph,candidate.Owner)) && NestedStateMatches(candidate,!undo,false);
            }catch{return false;}
        }
        internal bool CanUndo=>HasMainRecord?CanMainUndo:CanRestore(true);
        internal bool CanRedo=>HasMainRecord?CanMainRedo:CanRestore(false);
        internal void Undo(){if(HasMainRecord)MainUndo();else Guard(()=>Restore(true));}
        internal void Redo(){if(HasMainRecord)MainRedo();else Guard(()=>Restore(false));}
        private void Restore(bool undo) {
            if(!CanRestore(undo))return;
            var entry=Candidate(undo);var expected=undo?entry.After:entry.Before;var target=undo?entry.Before:entry.After;
            if(!(entry.Area?AreaTargetShape(target,entry.Asset):entry.Last?LastTargetShape(target,entry.Asset):target.Shape(entry.Asset))||!target.Fields.All(v=>v.Data.ReferencesValid))return;
            // Prepare both images before any live mutation; no old Entity IDs are recreated.
            var prepared=target.Prepare();var rollback=expected.Prepare();if(entry.ForestBefore!=null)entry.ForestFrom=CaptureForestNow(entry);
            nestedSeedsRestored=false;nestedValidatedFrames=0;restoring=entry;restoringUndo=undo;restoreFrame=Time.frameCount;
            observedOwner=entry.Owner;dispatched=restoreSourceMatched=nativeFinalized=false;
            try {
                if(entry.Area)AlignArea(target,entry.Asset);else if(entry.Last)AlignLast(target,entry.Asset);
                target.Apply(prepared);
                if(!target.Matches(entry.Asset))throw new InvalidOperationException("Prepared prefab payload did not match after apply.");
                world.GetExistingSystemManaged<PrefabSystem>().UpdatePrefab(entry.Asset,Entity.Null);
                entry.Asset.asset?.MarkDirty();
                Status="Waiting for experimental "+(undo?"Undo":"Redo")+" regeneration…";
                Mod.Log.Info(Status+" asset="+entry.Asset.name);
            }catch(Exception error) {
                try {if(entry.Area)AlignArea(expected,entry.Asset);else if(entry.Last)AlignLast(expected,entry.Asset);expected.Apply(rollback);world.GetExistingSystemManaged<PrefabSystem>().UpdatePrefab(entry.Asset,Entity.Null);}catch(Exception rollbackError){Mod.Log.Error(rollbackError,"Prefab rollback failed; restart CS2 without saving the disposable test.");}
                Mod.Log.Error(error,"Experimental Sub-element recovery failed; prefab rollback attempted. Deferred scene rollback is unproven.");
                FailRecovery("Recovery failed. Restart CS2 without saving the disposable test.");
            }
        }
        private void FailRecovery(string message) {
            disabled=true;restoring=null;ClearRecord();armed=false;lastArmed=false;nestedArmed=false;nestedForestArmed=false;forestBefore=null;nestedBefore=null;nestedReferences=null;armedAsset=null;Reset(message);
            Mod.Log.Error(new InvalidOperationException(message),"Experimental bulldozer recovery disabled; restart CS2 without saving the disposable test.");
        }
        private static void BeforeReplacement(ReplacePrefabSystem __instance,Entity __1,Entity __2,out int __state) {
            int stage=0;Guard(()=> {
                var a=active;if(a==null||!ReferenceEquals(__instance.World,a.world))return;
                a.ObservePreparationReplacement(__1,__2);
                var asset=a.restoring?.Asset ?? a.session.Asset;if(asset==null)return;
                if(a.world.GetExistingSystemManaged<PrefabSystem>().GetEntity(asset)!=__1)return;
                if(a.restoring!=null && __2==Entity.Null)stage=2;
                else if(a.session.Synchronized && __2==a.observedOwner)stage=1;
            });__state=stage;
        }
        private static void AfterReplacement(int __state)=>Guard(()=> {
            if(active==null)return;
            if(__state==1)active.sourceMatched=true;
            if(__state==2){active.restoreSourceMatched=true;active.dispatched=true;Mod.Log.Info("Experimental restoration dispatch matched; watching synchronous native finalization.");}
        });
        private GraphValue[] CaptureGraph(Entity owner) {
            if(!world.EntityManager.Exists(owner)||!world.EntityManager.HasBuffer<Game.Objects.SubObject>(owner))throw new NotSupportedException("No stable owning object buffer.");
            var buffer=world.EntityManager.GetBuffer<Game.Objects.SubObject>(owner,true);
            if(buffer.Length>2000)throw new NotSupportedException("Object graph exceeds test budget.");
            var result=new List<GraphValue>();var seen=new HashSet<Entity>();
            var prefabs=world.GetExistingSystemManaged<PrefabSystem>();
            for(int i=0;i<buffer.Length;i++) {
                var child=buffer[i].m_SubObject;
                if(!world.EntityManager.Exists(child)||world.EntityManager.HasComponent<Game.Common.Deleted>(child))continue;
                if(!seen.Add(child)||!world.EntityManager.HasComponent<Game.Common.Owner>(child)||world.EntityManager.GetComponentData<Game.Common.Owner>(child).m_Owner!=owner||!world.EntityManager.HasComponent<PrefabRef>(child))throw new NotSupportedException("Ambiguous or duplicate child ownership.");
                var value=new GraphValue{Prefab=prefabs.GetPrefab<PrefabBase>(world.EntityManager.GetComponentData<PrefabRef>(child)),HasTransform=world.EntityManager.HasComponent<Game.Objects.Transform>(child)};
                if(world.EntityManager.HasComponent<Game.Tools.EditorContainer>(child))value.Container=prefabs.GetPrefab<PrefabBase>(world.EntityManager.GetComponentData<Game.Tools.EditorContainer>(child).m_Prefab);
                if(value.HasTransform){var transform=world.EntityManager.GetComponentData<Game.Objects.Transform>(child);value.Position=transform.m_Position;value.Rotation=transform.m_Rotation;}
                result.Add(value);
            }
            return result.ToArray();
        }
        private bool GraphMatches(GraphValue[] expected,Entity owner) {
            if(expected==null)return false;
            var actual=CaptureGraph(owner);if(actual.Length!=expected.Length)return false;
            var used=new bool[actual.Length];
            foreach(var value in expected) {
                int found=-1;
                for(int i=0;i<actual.Length;i++)if(!used[i]&&GraphEqual(value,actual[i])){found=i;break;}
                if(found<0)return false;used[found]=true;
            }
            return true;
        }
        private static string DescribeGraph(GraphValue v)=>"prefab="+(v.Prefab==null?"null":v.Prefab.name)+"#"+(v.Prefab==null?0:v.Prefab.GetInstanceID())+" container="+(v.Container==null?"null":v.Container.name)+"#"+(v.Container==null?0:v.Container.GetInstanceID())+" transform="+v.HasTransform+" pos="+v.Position+" rot="+v.Rotation.value;
        private void LogGraphDifference(GraphValue[] expected,Entity owner) {
            var actual=CaptureGraph(owner);
            var used=new bool[actual.Length];int missing=0;
            if(expected!=null)foreach(var value in expected){
                int found=-1;for(int i=0;i<actual.Length;i++)if(!used[i]&&GraphEqual(value,actual[i])){found=i;break;}
                if(found>=0)used[found]=true;else if(missing++<8)Mod.Log.Info("Recovery UNMATCHED expected "+DescribeGraph(value));
            }
            int extra=0;for(int i=0;i<actual.Length;i++)if(!used[i]&&extra++<8)Mod.Log.Info("Recovery UNMATCHED actual "+DescribeGraph(actual[i]));
            Mod.Log.Info("Recovery graph expected="+(expected?.Length??-1)+" actual="+actual.Length+"; diagnostic limited to 32 children per image.");
            if(expected!=null)for(int i=0;i<Math.Min(32,expected.Length);i++)Mod.Log.Info("Recovery expected child["+i+"] "+DescribeGraph(expected[i]));
            for(int i=0;i<Math.Min(32,actual.Length);i++)Mod.Log.Info("Recovery actual child["+i+"] "+DescribeGraph(actual[i]));
        }
        private static bool GraphEqual(GraphValue a,GraphValue b)=>a.Prefab!=null && ReferenceEquals(a.Prefab,b.Prefab) && ReferenceEquals(a.Container,b.Container) && a.HasTransform==b.HasTransform &&
            (!a.HasTransform||(math.lengthsq(a.Position-b.Position)<1e-8f && math.min(math.lengthsq(a.Rotation.value-b.Rotation.value),math.lengthsq(a.Rotation.value+b.Rotation.value))<1e-8f));
    }
}




