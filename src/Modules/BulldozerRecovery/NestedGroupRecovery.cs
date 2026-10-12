using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private bool nestedArmed;
        private NestedNode[] nestedBefore;
        private Dictionary<PrefabBase,Payload> nestedReferences;
        private PrefabBase[] capturedRootTypes;
        private bool nestedSeedsRestored;
        private int nestedValidatedFrames;
        private bool RestoreNestedSeeds(Record entry){
            if(entry.ForestBefore!=null)return RestoreForestValues(entry);if(entry.Nested==null||nestedSeedsRestored||!NestedReferencesMatch(entry))return false;
            var roots=NestedRoots(entry.Nested[0].Prefab);
            if(roots.Length!=1)return false;
            NestedNode[] actual;
            try{actual=CaptureNestedImage(roots[0],false);}catch(NotSupportedException){return false;}
            var expected=entry.Nested;
            if(actual.Length!=expected.Length||actual[0].Owner!=entry.Owner)return false;
            // Preflight topology and native placement before restoring typed values on fresh nodes.
            for(int i=0;i<expected.Length;i++){
                var a=actual[i];var e=expected[i];
                var preflight=new MainPropData(e.Data.Transform,e.Data.Seed,a.Data.Surface,e.Data.Colours,a.Data.CustomColours);
                var expectedPlacement=new GraphValue{Prefab=e.Prefab,HasTransform=true,Position=e.Data.Transform.m_Position,Rotation=e.Data.Transform.m_Rotation};
                var actualPlacement=new GraphValue{Prefab=a.Prefab,HasTransform=true,Position=a.Data.Transform.m_Position,Rotation=a.Data.Transform.m_Rotation};
                if(a.Parent!=e.Parent||a.Depth!=e.Depth||!ReferenceEquals(a.Prefab,e.Prefab)||a.HasSurface!=e.HasSurface||!SameNestedElevation(a,e)||!a.Local.Equals(e.Local)||!e.Data.Matches(preflight)||!GraphEqual(expectedPlacement,actualPlacement)||((a.Data.Colours==null)!=(e.Data.Colours==null))||(a.Data.Colours?.Length??0)!=(e.Data.Colours?.Length??0)||expected.Any(node=>node.Original==a.Original))return false;
            }
            for(int i=0;i<expected.Length;i++){
                world.EntityManager.SetComponentData(actual[i].Original,expected[i].Data.Seed);
                world.EntityManager.SetComponentData(actual[i].Original,expected[i].Data.Transform);
                var colours=expected[i].Data.Colours;
                if(colours!=null){var buffer=world.EntityManager.GetBuffer<Game.Rendering.MeshColor>(actual[i].Original);for(int c=0;c<colours.Length;c++)buffer[c]=colours[c];}
                Mod.Log.Info("Nested fresh node typed values restored index="+i+" entity="+actual[i].Original+" nativeSeed="+actual[i].Data.Seed.m_Seed+" capturedSeed="+expected[i].Data.Seed.m_Seed+" exact transform/mesh colours restored; unchanged ownership/cache/surface/custom colours preflighted.");
            }
            nestedSeedsRestored=true;
            // Revalidate on the following frame; no history cursor movement here.
            return true;
        }
        internal void ArmNestedGroup(){
            if(!CanArm||nestedFixture==null){Status="Create and place the temporary nested fixture first.";return;}
            Arm();nestedArmed=true;
            Status="Armed nested group test. Everything-delete one supported prop group; keep another prop in the asset.";
            Mod.Log.Info(Status);
        }
        private Entity[] NestedRoots(PrefabBase prefab)=>NestedRoots(new[]{prefab});
        private Entity[] NestedRoots(PrefabBase[] rootTypes){
            rootTypes=NestedRootTypes.Copy(rootTypes);
            if(rootTypes.Any(prefab=>prefab==null))throw new NotSupportedException("Nested root prefab no longer exists.");
            var em=world.EntityManager;var prefabs=world.GetExistingSystemManaged<PrefabSystem>();
            var ids=new HashSet<Entity>(rootTypes.Select(prefab=>prefabs.GetEntity(prefab)));
            if(ids.Contains(Entity.Null)||ids.Count!=rootTypes.Length)throw new NotSupportedException("Nested root prefab entities are unavailable or ambiguous.");
            var result=new List<Entity>();
            using(var query=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<PrefabRef>(),ComponentType.ReadOnly<Game.Objects.Transform>()},None=new[]{ComponentType.ReadOnly<Temp>()}})){
                if(query.CalculateEntityCount()>100000)throw new NotSupportedException("Nested root query exceeds budget.");
                using(var entities=query.ToEntityArray(Allocator.Temp))foreach(var entity in entities)
                    if(ids.Contains(em.GetComponentData<PrefabRef>(entity).m_Prefab)){result.Add(entity);if(result.Count>4)throw new NotSupportedException("Nested root count exceeds four-group budget across all types.");}
            }
            return result.ToArray();
        }
        private void BeginNestedGroup(BulldozeToolSystem tool){
            nestedBefore=null;nestedReferences=null;capturedRootTypes=null;
            if(tool.actualMode!=BulldozeToolSystem.Mode.Everything)throw new NotSupportedException("Nested group test requires Everything mode.");
            var declared=TargetRoot?.GetComponent<ObjectSubObjects>()?.m_SubObjects;
            if(declared==null||declared.Any(value=>value?.m_Object is ObjectPrefab&&(value.m_Object.GetComponent<ObjectSubObjects>()?.m_SubObjects?.Length??0)>0&&!AdoptedNestedTypes.Any(prefab=>ReferenceEquals(prefab,value.m_Object))))throw new NotSupportedException("Nested prop types changed after enabling Undo. Clear history and enable again before testing this asset.");
            var points=(NativeList<ControlPoint>)controlPoints.GetValue(tool);
            var targets=new HashSet<Entity>();
            foreach(var point in points)if(point.m_OriginalEntity!=Entity.Null)targets.Add(point.m_OriginalEntity);
            if(targets.Count!=1)throw new NotSupportedException("Nested group requires one native target.");
            var root=targets.Single();var image=CaptureNestedImage(root);
            bool simpleTarget=image.Length==1&&nestedForestArmed;
            if(simpleTarget)OrdinaryGroupSupported(image);
            var rootTypes=simpleTarget?AdoptedNestedTypes.Concat(new[]{image[0].Prefab}).Distinct().ToArray():AdoptedNestedTypes;
            forestBefore=null;if(nestedForestArmed){CaptureNestedForest(root,rootTypes);if(nestedForest==null)throw new NotSupportedException("No placed prop group available.");forestBefore=nestedForest;if(MatchForest(forestBefore,forestBefore)==null)throw new NotSupportedException("Prop group positions are ambiguous.");}
            // Only the explicitly adopted root identity and bounded topology can enter recovery.
            if(!AllowedBoundTopology(image)||image[0].Owner!=observedOwner||(!simpleTarget&&!AdoptedNestedTypes.Any(prefab=>ReferenceEquals(image[0].Prefab,prefab)))||(!nestedForestArmed&&NestedRoots(AdoptedNestedTypes).Single()!=root))
                throw new NotSupportedException("The nested prop topology or owner does not match the supported group.");
            var references=(forestBefore??new[]{image}).SelectMany(group=>group.Select(node=>node.Prefab)).Distinct().ToDictionary(prefab=>prefab,Capture);foreach(var pair in references)Mod.Log.Info("Nested external reference captured: "+pair.Key.name+" readOnly="+pair.Key.isReadOnly+" matches="+NestedReferenceMatches(pair.Key,pair.Value));
            if(references.Values.Sum(value=>value.Weight)>250000)throw new NotSupportedException("Nested reference payload exceeds budget.");
            nestedBefore=image;nestedReferences=references;capturedRootTypes=NestedRootTypes.Copy(rootTypes);
        }
        // Referenced prefabs are never written; read-only originals are valid external identities.
        private static bool NestedReferenceMatches(PrefabBase asset,Payload image)=>asset!=null&&asset.components.Count==image.Components.Length&&asset.components.Zip(image.Components,(a,b)=>a!=null&&ReferenceEquals(a,b)).All(value=>value)&&image.Components.Select((component,i)=>component.active==image.Active[i]).All(value=>value)&&image.Fields.All(field=>field.Data.ReferencesValid&&field.Data.Matches(field.Field.GetValue(field.Owner)));
        private bool NestedReferencesMatch(Record entry)=>entry.Nested==null||entry.NestedReferences.All(pair=>NestedReferenceMatches(pair.Key,pair.Value));
        private bool NestedRemovalMatches(Payload before,Payload after){
            if(nestedBefore==null||nestedReferences==null){Mod.Log.Info("Nested removal refused: missing capture.");return false;}
            foreach(var pair in nestedReferences)if(!NestedReferenceMatches(pair.Key,pair.Value)){Mod.Log.Info("Nested removal refused: referenced payload changed "+pair.Key.name);return false;}
            const string key="ObjectSubObjects.m_SubObjects";
            var original=(ObjectSubObjectInfo[])before.Values[key].Materialize();
            int descriptor=nestedBefore[0].Local.m_PrefabSubIndex;int matches=forestBefore!=null?(descriptor>=0&&descriptor<original.Length&&ReferenceEquals(original[descriptor].m_Object,nestedBefore[0].Prefab)&&original[descriptor].m_Position.Equals(nestedBefore[0].Local.m_Position)&&original[descriptor].m_Rotation.Equals(nestedBefore[0].Local.m_Rotation)?1:0):original.Count(value=>ReferenceEquals(value.m_Object,nestedBefore[0].Prefab));
            if(matches!=1){Mod.Log.Info("Nested removal refused: root descriptor occurrences="+matches);return false;}
            var retained=forestBefore!=null?original.Where((value,index)=>index!=descriptor).ToArray():original.Where(value=>!ReferenceEquals(value.m_Object,nestedBefore[0].Prefab)).ToArray();
            var actual=after.Values.ContainsKey(key)?(ObjectSubObjectInfo[])after.Values[key].Materialize():Array.Empty<ObjectSubObjectInfo>();
            bool entries=MW.EditorTools.History.PrefabSnapshot.Take(retained).Matches(actual);
            var changed=before.Values.Where(pair=>pair.Key!=key&&(!after.Values.ContainsKey(pair.Key)||!pair.Value.Matches(after.Values[pair.Key].Materialize()))).Select(pair=>pair.Key).ToArray();
            if(!entries||changed.Length!=0){
                Mod.Log.Info("Nested removal refused: retained entries match="+entries+" expected="+retained.Length+" actual="+actual.Length+" other changed fields="+string.Join(",",changed));
                int reported=0;
                for(int i=0;i<Math.Min(retained.Length,actual.Length)&&reported<8;i++){
                    if(MW.EditorTools.History.PrefabSnapshot.Take(retained[i]).Matches(actual[i]))continue;
                    reported++;
                    foreach(var field in typeof(ObjectSubObjectInfo).GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance)){
                        var expectedValue=field.GetValue(retained[i]);var actualValue=field.GetValue(actual[i]);
                        if(!MW.EditorTools.History.PrefabSnapshot.Take(expectedValue).Matches(actualValue))Mod.Log.Info("Nested retained entry mismatch index="+i+" field="+field.Name+" expected="+expectedValue+" actual="+actualValue);
                    }
                }
            }
            return entries&&changed.Length==0;
        }
        private bool NestedStateMatches(Record entry,bool present,bool log){
            if(entry.ForestBefore!=null)return ForestStateMatches(entry,present,log);if(entry.Nested==null)return true;
            if(!NestedReferencesMatch(entry))return false;
            var roots=NestedRoots(entry.Nested[0].Prefab);
            if(!present)return roots.Length==0&&(entry.NestedLive??entry.Nested).All(node=>!world.EntityManager.Exists(node.Original));
            if(roots.Length!=1||world.EntityManager.HasComponent<Game.Common.Deleted>(roots[0]))return false;
            NestedNode[] actual;
            try{actual=CaptureNestedImage(roots[0],false);}catch(NotSupportedException error){if(log)Mod.Log.Info("Nested validation capture refused: "+error.Message);return false;}
            var expected=entry.Nested;
            if(actual.Length!=expected.Length||actual[0].Owner!=entry.Owner)return false;
            bool allMatch=true;
            for(int i=0;i<expected.Length;i++){
                var a=actual[i];var e=expected[i];
                bool data=e.Data.Matches(a.Data),local=e.Local.Equals(a.Local);
                if(log){var withoutSeed=new MainPropData(a.Data.Transform,e.Data.Seed,a.Data.Surface,a.Data.Colours,a.Data.CustomColours);Mod.Log.Info("Nested complete comparison node="+i+" excludingSeed="+e.Data.Matches(withoutSeed)+" seed="+e.Data.Seed.Equals(a.Data.Seed)+" surfaceValue="+e.Data.Surface.Equals(a.Data.Surface)+" meshColours="+((e.Data.Colours==null&&a.Data.Colours==null)||(e.Data.Colours!=null&&a.Data.Colours!=null&&e.Data.Colours.SequenceEqual(a.Data.Colours)))+" customColours="+((e.Data.CustomColours==null&&a.Data.CustomColours==null)||(e.Data.CustomColours!=null&&a.Data.CustomColours!=null&&e.Data.CustomColours.SequenceEqual(a.Data.CustomColours))));}
                if(a.Parent!=e.Parent||a.Depth!=e.Depth||!ReferenceEquals(a.Prefab,e.Prefab)||a.HasSurface!=e.HasSurface||!SameNestedElevation(a,e)||!data||!local){
                    if(log)Mod.Log.Info("Nested validation mismatch node="+i+" parent="+(a.Parent==e.Parent)+" prefab="+ReferenceEquals(a.Prefab,e.Prefab)+" surface="+(a.HasSurface==e.HasSurface)+" typedData="+data+" localCache="+local+" seedExpected="+e.Data.Seed.m_Seed+" seedActual="+a.Data.Seed.m_Seed+" transform="+e.Data.Transform.Equals(a.Data.Transform)+" colours="+e.Data.Colours?.Length+"/"+a.Data.Colours?.Length);
                    allMatch=false;
                }
            }
            if(!allMatch)return false;
            entry.NestedLive=actual;
            return true;
        }
    }
}
