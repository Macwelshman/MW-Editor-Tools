using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Game.Prefabs;
using Game.UI.Editor;
using Unity.Collections;
using Unity.Entities;
namespace MW.EditorTools.Modules.ComponentManager.Pack {
 internal sealed partial class PriorityBatch {
  internal static PrefabBase[] Workspace(World world){
   if(!VerifiedGameBuild.Game(typeof(EditorHierarchyUISystem).Module.ModuleVersionId))throw new NotSupportedException("Workspace API build is unsupported.");
   var hierarchy=world.GetExistingSystemManaged<EditorHierarchyUISystem>();var registry=world.GetExistingSystemManaged<PrefabSystem>();
   if(hierarchy==null||!hierarchy.Enabled||registry==null)throw new InvalidOperationException("Open an editor workspace first.");
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;
   typeof(SystemBase).GetMethod("CompleteDependency",flags).Invoke(hierarchy,null);
   var rows=(NativeList<EditorHierarchyUISystem.HierarchyItem>)typeof(EditorHierarchyUISystem).GetField("m_Hierarchy",flags).GetValue(hierarchy);
   if(!rows.IsCreated)throw new InvalidOperationException("Workspace is not ready.");
   var found=new HashSet<PrefabBase>();
   for(int i=0;i<rows.Length;i++){var id=rows[i].id;PrefabBase asset;if((id.type==EditorHierarchyUISystem.ItemType.Object||id.type==EditorHierarchyUISystem.ItemType.Prefab)&&registry.TryGetPrefab(id.prefab,out asset)&&asset!=null)found.Add(asset);}
   MW.EditorTools.Mod.Log.Info("Priority workspace snapshot: "+rows.Length+" hierarchy rows, "+found.Count+" resolved assets.");
   return found.OrderBy(a=>a.name,StringComparer.Ordinal).ToArray();
  }
 }
}
