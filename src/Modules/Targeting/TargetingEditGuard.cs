using System;
using System.Reflection;
using Game.UI.Editor;
using HarmonyLib;
using Unity.Entities;

namespace MW.EditorTools.Modules.Targeting {
 // Independent of deletion/move patches: stop native raycasts before the inspector
 // queues a rebuild. Vanilla edits still run; no widget value or source is changed.
 internal sealed class TargetingEditGuard : IDisposable {
  private const string Id="MW.EditorTools.Targeting.InspectorEdit";
  private static TargetingEditGuard current;
  private readonly EditorTargetingTool tool;
  private readonly Harmony harmony=new Harmony(Id);
  internal TargetingEditGuard(EditorTargetingTool value) {
   tool=value;
   if(!VerifiedGameBuild.Game(typeof(InspectorPanelSystem).Module.ModuleVersionId))throw new NotSupportedException("Unverified inspector refresh API.");
   var method=typeof(InspectorPanelSystem).GetMethod("UpdateParent",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(bool)},null)??throw new MissingMethodException("InspectorPanelSystem.UpdateParent");
   current=this;
   try {harmony.Patch(method,prefix:new HarmonyMethod(typeof(TargetingEditGuard).GetMethod(nameof(BeforeEdit),BindingFlags.Static|BindingFlags.NonPublic)){priority=Priority.First});}
   catch {Dispose();throw;}
  }
  private static bool BeforeEdit() {
   try {current?.tool.PauseForInspectorEdit();return true;}
   catch(Exception error){Mod.Log.Error(error,"Native inspector refresh blocked because targeting cleanup failed. Stop replacing before retrying the edit.");return false;}
  }
  public void Dispose(){harmony.UnpatchAll(Id);if(ReferenceEquals(current,this))current=null;}
 }
}
