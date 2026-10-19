using System;
using System.Linq;
using Game.Prefabs;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private bool nestedForestArmed;
        private NestedNode[][] forestBefore;
        internal void ArmNestedForest(){if(!CanArm)return;ArmNestedGroup();if(nestedArmed){nestedForestArmed=true;Status="Armed nested set history test: delete fixture groups individually in Everything mode; keep another bound prop. Undo and Redo step through the deletions.";Mod.Log.Info(Status);}}
        private static bool SameNestedPlacement(NestedNode a,NestedNode b)=>GraphEqual(new GraphValue{Prefab=a.Prefab,HasTransform=true,Position=a.Data.Transform.m_Position,Rotation=a.Data.Transform.m_Rotation},new GraphValue{Prefab=b.Prefab,HasTransform=true,Position=b.Data.Transform.m_Position,Rotation=b.Data.Transform.m_Rotation});
        private NestedNode[][] CaptureForestNow(Record entry){
            if(entry.Area)return CaptureAreaProps(entry.Owner);
            var groups=NestedRoots(entry.RootTypes??new[]{entry.Nested[0].Prefab},entry.Owner).Select(root=>CaptureNestedImage(root,false)).ToArray();
            if(groups.Any(group=>!AllowedBoundTopology(group)||group[0].Owner!=entry.Owner))throw new NotSupportedException("Prop set topology changed.");
            if(groups.SelectMany(group=>group.Select(node=>node.Original)).Distinct().Count()!=groups.Sum(group=>group.Length))throw new NotSupportedException("Nested set shares descendants.");
            return groups;
        }
        private static bool ExactNestedGroup(NestedNode[] expected,NestedNode[] actual)=>actual.Length==expected.Length&&expected.Select((e,i)=>e.Parent==actual[i].Parent&&e.Depth==actual[i].Depth&&ReferenceEquals(e.Prefab,actual[i].Prefab)&&e.HasSurface==actual[i].HasSurface&&SameNestedElevation(e,actual[i])&&e.Local.Equals(actual[i].Local)&&e.Data.Matches(actual[i].Data)).All(value=>value);
        private static bool NestedGroupAtIndex(NestedNode[] expected,NestedNode[] actual,int rootIndex){if(expected.Length!=actual.Length)return false;for(int i=0;i<expected.Length;i++){var e=expected[i];var a=actual[i];var local=e.Local;if(i==0)local.m_PrefabSubIndex=rootIndex;if(e.Parent!=a.Parent||e.Depth!=a.Depth||!ReferenceEquals(e.Prefab,a.Prefab)||e.HasSurface!=a.HasSurface||!SameNestedElevation(e,a)||!local.Equals(a.Local)||!e.Data.Matches(a.Data))return false;}return true;}
        private bool RetainedAfterRemoval(NestedNode[] expected,NestedNode[] actual,int removedIndex){int index=expected[0].Local.m_PrefabSubIndex;var rows=TargetRoot?.GetComponent<ObjectSubObjects>()?.m_SubObjects;int next=BoundDescriptorIndex.Resolve(rows,expected[0].Prefab,expected[0].Local);bool valid=NestedGroupAtIndex(expected,actual,next);Mod.Log.Info("Nested retained index transition before="+index+" expectedAfter="+next+" actualAfter="+actual[0].Local.m_PrefabSubIndex+" allOtherValuesExact="+valid);return valid;}
        private bool ForestSourcesValid(Record entry,NestedNode[][] actual){
            if(entry.ForestFrom==null)return true;
            var nodes=actual.SelectMany(GroupSources).ToArray();
            // Native list insertion may repurpose existing fixture entities at another placement.
            // Every surviving old node must be present in the complete, strictly captured target graph.
            return entry.ForestFrom.SelectMany(GroupSources).All(id=>!world.EntityManager.Exists(id)||nodes.Contains(id));
        }
        private bool ForestStateMatches(Record entry,bool before,bool log){
            if(!NestedReferencesMatch(entry))return false;
            try{
                if(!before&&entry.ForestFrom==null&&GroupSources(entry.Nested).Any(id=>world.EntityManager.Exists(id)))return false;var expected=before?entry.ForestBefore:entry.ForestAfter;
                var actual=MatchForest(expected,CaptureForestNow(entry));
                if(actual==null)return false;
                bool valid=expected.Select((group,i)=>ExactNestedGroup(group,actual[i])).All(value=>value);
                bool identities=RetainedFoliageIdentitiesMatch(entry,actual);
                if(log)Mod.Log.Info("Nested set exact validation groups="+expected.Length+" valid="+valid+" retainedFoliageIdentities="+identities);
                return valid&&ForestSourcesValid(entry,actual)&&identities;
            }catch(NotSupportedException error){if(log)Mod.Log.Info("Nested set validation refused: "+error.Message);return false;}
        }
        private bool RestoreForestValues(Record entry){
            if(nestedSeedsRestored||!NestedReferencesMatch(entry))return false;
            var expected=restoringUndo?entry.ForestBefore:entry.ForestAfter;
            NestedNode[][] actual;
            try{actual=MatchForest(expected,CaptureForestNow(entry));}catch(NotSupportedException){return false;}
            bool log=UnityEngine.Time.frameCount-restoreFrame>=120;
            if(actual!=null&&expected.Where((group,i)=>group.Length!=actual[i].Length).Any())return false;
            if(actual==null){if(log)Mod.Log.Info("Nested set preflight: root count or placement mapping mismatch.");return false;}
            if(!ForestSourcesValid(entry,actual)){if(log)Mod.Log.Info("Nested set preflight: a surviving source node is outside the mapped fixture graph.");return false;}
            var reused=expected.Select((group,i)=>ExactNestedGroup(group,actual[i])).ToArray();
            for(int g=0;g<expected.Length;g++)if(reused[g]&&!ExactNestedGroup(expected[g],actual[g])){if(log)Mod.Log.Info("Nested set preflight: reused group differs from target image group="+g);return false;}
            foreach(var pair in expected.SelectMany((group,g)=>group.Select((node,n)=>new{Expected=node,Actual=actual[g][n]}))){
                var e=pair.Expected;var a=pair.Actual;
                var unchanged=new MainPropData(e.Data.Transform,e.Data.Seed,a.Data.Surface,e.Data.Colours,a.Data.CustomColours);
                if(e.Parent!=a.Parent||e.Depth!=a.Depth||!ReferenceEquals(e.Prefab,a.Prefab)||e.HasSurface!=a.HasSurface||!SameNestedElevation(e,a,false)||!e.Local.Equals(a.Local)||!SameNestedPlacement(e,a)||!e.Data.Matches(unchanged)||((e.Data.Colours==null)!=(a.Data.Colours==null))||(e.Data.Colours?.Length??0)!=(a.Data.Colours?.Length??0)){
                    if(log)Mod.Log.Info("Nested set preflight mismatch expectedOriginal="+e.Original+" actual="+a.Original+" parent="+(e.Parent==a.Parent)+" prefab="+ReferenceEquals(e.Prefab,a.Prefab)+" surfacePresence="+(e.HasSurface==a.HasSurface)+" localCache="+e.Local.Equals(a.Local)+" cacheIndex="+e.Local.m_PrefabSubIndex+"/"+a.Local.m_PrefabSubIndex+" placement="+SameNestedPlacement(e,a)+" unchangedTypedValues="+e.Data.Matches(unchanged)+" coloursPresence="+((e.Data.Colours==null)==(a.Data.Colours==null))+" coloursLength="+(e.Data.Colours?.Length??-1)+"/"+(a.Data.Colours?.Length??-1));
                    return false;
                }
            }
            for(int g=0;g<expected.Length;g++)for(int n=0;n<expected[g].Length;n++){
                if(reused[g])continue;
                var e=expected[g][n];var id=actual[g][n].Original;
                world.EntityManager.SetComponentData(id,e.Data.Seed);world.EntityManager.SetComponentData(id,e.Data.Transform);
                if(e.HasColor)world.EntityManager.SetComponentData(id,e.Color);if(e.HasDecoration)world.EntityManager.SetComponentEnabled<Game.Objects.Decoration>(id,e.DecorationEnabled);
                if(e.HasPlant)world.EntityManager.SetComponentData(id,e.Plant);if(e.HasTree)world.EntityManager.SetComponentData(id,e.Tree);
                var colours=e.Data.Colours;if(colours!=null){var buffer=world.EntityManager.GetBuffer<Game.Rendering.MeshColor>(id);for(int i=0;i<colours.Length;i++)buffer[i]=colours[i];}
            }
            nestedSeedsRestored=true;Mod.Log.Info("Nested set typed values restored changedOrNewGroups="+reused.Count(value=>!value)+" unchangedGroups="+reused.Count(value=>value)+" totalNodes="+expected.Sum(group=>group.Length)+"; already exact groups untouched; changed/new native nodes restored, awaiting exact validation across three frames.");return true;
        }
    }
}
