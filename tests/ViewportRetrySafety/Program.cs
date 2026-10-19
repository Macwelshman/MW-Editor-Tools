using System;
namespace Unity.Entities {readonly record struct Entity(int Id);}
namespace Game.Prefabs {class PrefabBase{}}
namespace Game.Tools {class DefaultToolSystem{}class ToolSystem{public object activeTool;}}
namespace UnityEngine {static class Time{public static int frameCount;public static float realtimeSinceStartup;}}
namespace UnityEngine.InputSystem {
 class Key{public bool isPressed,wasPressedThisFrame;}
 class Keyboard{public static Keyboard current;public Key ctrlKey=new(),altKey=new(),leftMetaKey=new(),rightMetaKey=new(),cKey=new(),vKey=new(),shiftKey=new();}
}
namespace MW.EditorTools {static class Mod{public static Logger Log=new();public class Logger{public void Info(string text){}}}}
namespace MW.EditorTools.Modules.PropReplacer {
 public sealed partial class PropReplacerSystem {
  private sealed class Clip{}
  private sealed class Context{public bool EditorAvailable=true;public Game.Prefabs.PrefabBase Asset=new();}
  private sealed class TestWorld{public Game.Tools.ToolSystem Tool=new();public T GetExistingSystemManaged<T>() where T:class=>Tool as T;}
  private sealed class ViewportHitNotReady:InvalidOperationException{internal Unity.Entities.Entity Expected;internal Game.Tools.DefaultToolSystem Tool;internal ViewportHitNotReady(Unity.Entities.Entity id,Game.Tools.DefaultToolSystem tool){Expected=id;Tool=tool;}}
  private readonly TestWorld World=new();private Context context=new();private Clip clipboard=new();private Func<bool> shortcutAllowed=()=>true;private string status;
  private Unity.Entities.Entity? requiredViewportHit;private Unity.Entities.Entity currentHit=new(7);private bool notReady=true,fail;private int attempts,successes,browserCalls;private bool lastBatch;
  private void CopyHover(){browserCalls++;CopyViewport();}private void PasteHover(bool batch){browserCalls++;PasteViewport(batch);}
  private void CopyViewport(){Act();clipboard=new();}private void PasteViewport(bool batch){Act();lastBatch=batch;}
  private void Act(){attempts++;if(requiredViewportHit.HasValue&&requiredViewportHit.Value!=currentHit)throw new InvalidOperationException("Moved cursor");if(fail)throw new InvalidOperationException("Unsafe source");if(notReady)throw new ViewportHitNotReady(currentHit,(Game.Tools.DefaultToolSystem)World.Tool.activeTool);successes++;}
  private static int checks;private static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
  private static PropReplacerSystem Start(bool copy=true,bool batch=false){
   UnityEngine.Time.frameCount=100;UnityEngine.Time.realtimeSinceStartup=0;
   var s=new PropReplacerSystem();s.World.Tool.activeTool=new Game.Tools.DefaultToolSystem();
   var k=UnityEngine.InputSystem.Keyboard.current=new();k.ctrlKey.isPressed=true;k.cKey.wasPressedThisFrame=copy;k.vKey.wasPressedThisFrame=!copy;k.shiftKey.isPressed=batch;
   s.PollClipboardShortcuts();Check(s.pendingViewportClipboard!=null&&s.successes==0,"Transient miss queues once without performing action");k.cKey.wasPressedThisFrame=k.vKey.wasPressedThisFrame=false;
   return s;
  }
  private void Step(){UnityEngine.Time.frameCount++;UnityEngine.Time.realtimeSinceStartup+=.02f;PollClipboardShortcuts();}
  internal static void Run(){
   var s=Start();s.notReady=false;s.Step();Check(s.successes==1&&s.pendingViewportClipboard==null&&s.requiredViewportHit==null,"Pinned copy completes once on stable native hit");Check(s.browserCalls==1,"Retry bypasses later browser samples");s.Step();Check(s.successes==1,"Completed command cannot repeat");
   s=Start(false,true);s.notReady=false;UnityEngine.InputSystem.Keyboard.current.shiftKey.isPressed=false;s.Step();Check(s.successes==1&&s.lastBatch,"Replace-all mode is captured rather than reread on retry");
   s=Start();s.notReady=false;s.currentHit=new(8);s.Step();Check(s.successes==0&&s.pendingViewportClipboard==null,"Cursor target change cancels without applying");
   s=Start();s.context.Asset=new();s.Step();Check(s.successes==0&&s.pendingViewportClipboard==null,"Asset change cancels");
   s=Start();s.clipboard=new();s.Step();Check(s.successes==0&&s.pendingViewportClipboard==null,"Clipboard change cancels");
   s=Start();s.World.Tool.activeTool=new Game.Tools.DefaultToolSystem();s.Step();Check(s.successes==0&&s.pendingViewportClipboard==null,"Tool change cancels");
   s=Start();s.shortcutAllowed=()=>false;s.Step();Check(s.successes==0&&s.pendingViewportClipboard==null,"Typing/dialog/save shortcut gate cancels");
   s=Start();s.context.EditorAvailable=false;s.Step();Check(s.successes==0&&s.pendingViewportClipboard==null,"Editor exit cancels");
   s=Start();s.Step();Check(s.successes==0&&s.pendingViewportClipboard!=null,"Same pinned target can wait another frame");UnityEngine.Time.frameCount=113;s.PollClipboardShortcuts();Check(s.successes==0&&s.pendingViewportClipboard==null,"Frame deadline stops retry");
   s=Start();UnityEngine.Time.realtimeSinceStartup=.36f;s.Step();Check(s.successes==0&&s.pendingViewportClipboard==null,"Real-time deadline stops retry");
   s=Start();s.notReady=false;s.PollClipboardShortcuts();Check(s.attempts==1&&s.successes==0,"Initial frame cannot execute a second time");s.Step();Check(s.successes==1,"Next-frame action remains available");
   s=Start();s.notReady=false;s.fail=true;s.Step();Check(s.successes==0&&s.pendingViewportClipboard==null,"Non-transient source validation failure is not retried");
   s=Start();s.notReady=false;var k=UnityEngine.InputSystem.Keyboard.current;k.vKey.wasPressedThisFrame=true;s.currentHit=new(9);s.PollClipboardShortcuts();k.vKey.wasPressedThisFrame=false;s.Step();Check(s.successes==1&&s.pendingViewportClipboard==null,"New shortcut replaces pending intent without duplicate old action");
   Console.WriteLine("PASS: "+checks+" actual-source bounded viewport retry, context cancellation and single-dispatch checks.");
  }
 }
}
class Program{static void Main()=>MW.EditorTools.Modules.PropReplacer.PropReplacerSystem.Run();}
