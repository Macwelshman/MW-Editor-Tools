using System;
using System.Collections;
using System.Reflection;
using Game.Prefabs;

namespace MW.EditorTools.Modules.PropReplacer {
 public sealed partial class PropReplacerSystem {
  private static void VerifyAreaRenderApi() {
   if(!VerifiedGameBuild.AreaCollections(typeof(PrefabBase).Module.ModuleVersionId,
      typeof(Colossal.Collections.NativeHeapBlock).Module.ModuleVersionId,
      typeof(Unity.Collections.NativeQueue<>).Module.ModuleVersionId))throw new NotSupportedException("Unverified area allocation collections API.");
  }
  private static void VerifySurfaceApi(PrefabSystem registry,PrefabBase root) {
   if(!VerifiedGameBuild.Game(typeof(PrefabBase).Module.ModuleVersionId))throw new NotSupportedException("Unverified surface replacement API build.");
   var pending=typeof(PrefabSystem).GetField("m_UpdateMap",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(registry) as IDictionary;
   if(pending==null)throw new NotSupportedException("Surface update readiness API unavailable.");
   if(pending.Contains(root))throw new InvalidOperationException("Wait for the owning prefab refresh to finish before replacing.");
  }
 }
}
