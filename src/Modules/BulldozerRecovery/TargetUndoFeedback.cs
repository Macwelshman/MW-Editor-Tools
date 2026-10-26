using System;
using MW.EditorTools.Modules.Targeting;
using Unity.Entities;

namespace MW.EditorTools.Modules.BulldozerRecovery
{
    internal sealed partial class BulldozerTrace
    {
        // Read-only preflight. Never calls Begin/Invalidate/Prepare/Restore or
        // retains these detached images as an action in the shared Undo slot.
        internal string TargetUndoFeedback(Entity id, bool selected)
        {
            if (!PatchesCompatible()) return Status;
            if (Busy || MovePreviewActive) return "Wait for the current edit or Undo to finish.";
            if (!context.EditorAvailable || context.DialogBusy) return "Close the dialog and return to the editor.";
            var em = world.EntityManager;
            if (!em.Exists(id) || em.HasComponent<Game.Common.Deleted>(id) || em.HasComponent<Game.Common.Created>(id) ||
                em.HasComponent<Game.Common.Updated>(id) || em.HasComponent<Game.Tools.Temp>(id)) return "The element is still updating; select it again when it settles.";
            var kind = TargetKinds.Read(em, id);
            bool bound = em.HasComponent<Game.Common.Owner>(id);
            if (disabled || (kind == TargetKind.Effect && effectDisabled) ||
                ((kind == TargetKind.Network || kind == TargetKind.Lane) && networkDisabled))
                return "Undo for this element stopped after an error. Reload before further recovery.";
            if (((kind == TargetKind.Surface || kind == TargetKind.Space) && areaMoveDisabled) ||
                ((kind == TargetKind.Prop || kind == TargetKind.Foliage || kind == TargetKind.Marker) && moveDisabled))
                return "Move Undo is unavailable in this session; deletion has separate capture checks.";
            if (kind == TargetKind.None) return "This element type is not supported by MW Undo.";
            if (kind == TargetKind.Network || kind == TargetKind.Lane)
                return !bound ? "Standalone network/lane Undo is not supported." : em.HasComponent<Game.Net.Node>(id) ?
                    "Select a network segment; junction-only Undo is not supported." :
                    "Bound deletion candidate. Source and companion checks run on deletion. Network/lane move Undo is not supported.";
            if (!selected) return "Native Undo candidate. Click to check its data and companions; eligibility is rechecked when the action is captured.";
            try
            {
                if (kind == TargetKind.Effect) { CaptureEffect(id); if (bound) CaptureMoveOwner(em.GetComponentData<Game.Common.Owner>(id).m_Owner, true); }
                else if (kind == TargetKind.Surface || kind == TargetKind.Space)
                { CaptureStandaloneArea(id, bound); if (bound) CaptureMoveOwner(em.GetComponentData<Game.Common.Owner>(id).m_Owner); }
                else
                {
                    var group = CaptureMoveGroup(id);
                    if (!MoveGroupSettled(group)) return "The element is still updating; select it again when it settles.";
                    if (bound) CaptureMoveOwner(group[0].Owner);
                }
                return "Data preflight passed. Native move/deletion capture still checks source identity, companions and references before recording Undo.";
            }
            catch (Exception error)
            {
                if (error.Message.IndexOf("Anarchy.", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Undo cannot capture this element while Anarchy's extra protection/transform data is attached.";
                if (error.Message.IndexOf("Unhandled", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Undo cannot capture this element's additional component data.";
                return "Undo preflight could not verify this element: " + error.Message;
            }
        }
    }
}
