using System;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;
using UnityEngine.InputSystem;

namespace MW.EditorTools.Modules.PropReplacer {
    public sealed partial class PropReplacerSystem {
        private sealed class ViewportClipboardRequest {
            internal bool Copy,Batch;
            internal Entity Expected;
            internal DefaultToolSystem Tool;
            internal PrefabBase Asset;
            internal Clip Clipboard;
            internal int Frame,DeadlineFrame;
            internal float DeadlineTime;
        }
        private ViewportClipboardRequest pendingViewportClipboard;
        private void PollClipboardShortcuts() {
            var keyboard=Keyboard.current;
            if(keyboard!=null&&keyboard.ctrlKey.isPressed&&!keyboard.altKey.isPressed&&!keyboard.leftMetaKey.isPressed&&!keyboard.rightMetaKey.isPressed&&shortcutAllowed?.Invoke()==true) {
                bool copy=keyboard.cKey.wasPressedThisFrame&&!keyboard.shiftKey.isPressed;
                bool paste=keyboard.vKey.wasPressedThisFrame;
                if(copy||paste) {
                    pendingViewportClipboard=null;
                    try{if(copy)CopyHover();else PasteHover(keyboard.shiftKey.isPressed);}
                    catch(ViewportHitNotReady error) {
                        pendingViewportClipboard=new ViewportClipboardRequest{Copy=copy,Batch=!copy&&keyboard.shiftKey.isPressed,Expected=error.Expected,Tool=error.Tool,Asset=context?.Asset,Clipboard=clipboard,
                            Frame=UnityEngine.Time.frameCount,DeadlineFrame=UnityEngine.Time.frameCount+12,DeadlineTime=UnityEngine.Time.realtimeSinceStartup+.35f};
                        status=error.Message;
                    }
                    catch(Exception error){status=error.Message;MW.EditorTools.Mod.Log.Info("Prop Replacer clipboard shortcut refused: "+error.Message);}
                    return;
                }
            }
            CompleteViewportClipboard();
        }
        private void CompleteViewportClipboard() {
            var request=pendingViewportClipboard;if(request==null)return;
            if(context?.EditorAvailable!=true||shortcutAllowed?.Invoke()!=true||!ReferenceEquals(request.Asset,context.Asset)||!ReferenceEquals(request.Clipboard,clipboard)||
                !ReferenceEquals(World.GetExistingSystemManaged<ToolSystem>()?.activeTool,request.Tool)) {
                pendingViewportClipboard=null;status="Pending clipboard action cancelled because the editor context changed.";return;
            }
            if(UnityEngine.Time.frameCount>request.DeadlineFrame||UnityEngine.Time.realtimeSinceStartup>request.DeadlineTime) {
                pendingViewportClipboard=null;status="The viewport highlight did not settle. Keep the cursor over the intended element and use the shortcut again.";
                MW.EditorTools.Mod.Log.Info("Prop Replacer viewport request expired: target="+request.Expected);return;
            }
            if(UnityEngine.Time.frameCount<=request.Frame)return;
            requiredViewportHit=request.Expected;
            try {
                // Retry only the pinned viewport operation, never a later browser
                // hover sample or a different element under a moved cursor.
                if(request.Copy)CopyViewport();else PasteViewport(request.Batch);
                pendingViewportClipboard=null;
                MW.EditorTools.Mod.Log.Info("Prop Replacer viewport request completed: target="+request.Expected+" copy="+request.Copy+" batch="+request.Batch);
            } catch(ViewportHitNotReady error) {
                if(error.Expected!=request.Expected||!ReferenceEquals(error.Tool,request.Tool)){pendingViewportClipboard=null;status="The viewport target changed; pending clipboard action cancelled.";}
            } catch(Exception error) {
                pendingViewportClipboard=null;status=error.Message;MW.EditorTools.Mod.Log.Info("Prop Replacer pending clipboard action refused: "+error.Message);
            } finally {requiredViewportHit=null;}
        }
    }
}
