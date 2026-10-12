using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private NestedNode[][] nestedForest;
        private Entity nestedForestTarget;
        private Dictionary<PrefabBase,Payload> nestedForestReferences;
        private void CaptureNestedForest(Entity target,PrefabBase[] rootTypes=null){
            nestedForest=null;nestedForestReferences=null;
            if(nestedFixture==null)return;
            var roots=NestedRoots(rootTypes??AdoptedNestedTypes);
            if(!roots.Contains(target))return;
            var image=roots.Select(root=>CaptureNestedImage(root,false)).ToArray();
            if(image.Length<1)return;
            if(image.Any(group=>!AllowedBoundTopology(group)||group[0].Owner!=image[0][0].Owner))throw new NotSupportedException("Prop groups require bounded topology and one direct owner.");
            foreach(var group in image)OrdinaryGroupSupported(group);
            var ids=image.SelectMany(group=>group.Select(node=>node.Original)).ToArray();
            if(ids.Distinct().Count()!=ids.Length)throw new NotSupportedException("Forest shares descendants.");
            var references=image.SelectMany(group=>group.Select(node=>node.Prefab)).Distinct().ToDictionary(prefab=>prefab,Capture);
            if(references.Values.Sum(value=>value.Weight)>250000||references.Any(pair=>!NestedReferenceMatches(pair.Key,pair.Value)))throw new NotSupportedException("Forest referenced payload invalid or over budget.");
            nestedForest=image;nestedForestTarget=target;nestedForestReferences=references;
            Mod.Log.Info("Nested forest captured groups="+image.Length+" nodes="+ids.Length+" target="+target+"; typed images detached; recovery requires explicit arming.");
        }
        private void ValidateNestedForest(){
            if(nestedForest==null)return;
            bool references=nestedForestReferences.All(pair=>NestedReferenceMatches(pair.Key,pair.Value));
            foreach(var group in nestedForest){
                bool deleted=group[0].Original==nestedForestTarget;
                bool gone=group.All(node=>!world.EntityManager.Exists(node.Original));
                bool detached=group.All(node=>node.Prefab!=null&&node.Data.Matches(node.Data.Clone()));
                bool retained=false;
                if(!deleted)try{
                    var actual=CaptureNestedImage(group[0].Original,false);
                    retained=actual.Length==group.Length&&group.Select((node,i)=>actual[i].Original==node.Original&&actual[i].Owner==node.Owner&&actual[i].Parent==node.Parent&&actual[i].Depth==node.Depth&&ReferenceEquals(actual[i].Prefab,node.Prefab)&&actual[i].HasSurface==node.HasSurface&&SameNestedElevation(actual[i],node)&&actual[i].Local.Equals(node.Local)&&node.Data.Matches(actual[i].Data)).All(value=>value);
                }catch(NotSupportedException){retained=false;}
                Mod.Log.Info("Nested forest checkpoint root="+group[0].Original+" deletedTarget="+deleted+" allGone="+gone+" detachedValid="+detached+" retainedExact="+retained+" referencesUnchanged="+references+" accepted="+(detached&&references&&(deleted?gone:retained)));
            }
            nestedForest=null;nestedForestReferences=null;
        }
    }
}
