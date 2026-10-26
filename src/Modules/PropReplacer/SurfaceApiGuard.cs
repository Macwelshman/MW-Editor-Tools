using System;
using System.Collections;
using System.Reflection;
using Game.Prefabs;

namespace MW.EditorTools.Modules.PropReplacer {
 public sealed partial class PropReplacerSystem {
  private static void VerifyAreaRenderApi() {
   if(typeof(Colossal.Collections.NativeHeapBlock).Module.ModuleVersionId.ToString()!="0e5688e6-8747-4130-b712-7b3c25780e8b" ||
      typeof(Unity.Collections.NativeQueue<>).Module.ModuleVersionId.ToString()!="f4ddbc30-cb47-4061-9107-4a184ca0481e")throw new NotSupportedException("Unverified area allocation collections API.");
  }
  private static void VerifySurfaceApi(PrefabSystem registry,PrefabBase root) {
   if(typeof(PrefabBase).Module.ModuleVersionId.ToString()!="e15551e9-3dea-4855-8dde-1da3cea7d7ef")throw new NotSupportedException("Unverified surface replacement API build.");
   var pending=typeof(PrefabSystem).GetField("m_UpdateMap",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(registry) as IDictionary;
   if(pending==null)throw new NotSupportedException("Surface update readiness API unavailable.");
   if(pending.Contains(root))throw new InvalidOperationException("Wait for the owning prefab refresh to finish before replacing.");
  }
 }
}
