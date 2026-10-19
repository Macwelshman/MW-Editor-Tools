using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;

namespace MW.EditorTools.Modules.ComponentManager.Transfer
{
    internal sealed class TransferSession
    {
        internal Action BeforeWrite;
        public TransferPlan Preview { get; private set; }
        private TransferTarget undoTarget;
        private List<PlannedChange> undoChanges;
        private ComponentBase[] undoIdentities;
        private string undoFingerprint;
        public string Summary { get { return Preview == null ? "Choose a mode and preview the destination." : string.Join("\n", Preview.Reports); } }
        public bool CanApply { get { return Preview != null && Preview.Changes.Count > 0; } }
        public bool CanRestore { get { return undoTarget != null && !undoChanges.Any(c => c.Before == null && c.Adapter == null); } }
        public void InvalidatePreview() { Preview = null; }
        public void Clear() { Preview = null; undoTarget = null; undoChanges = null; undoIdentities = null; undoFingerprint = null; }
        public string Plan(TransferTarget target, TransferClipboard clipboard, TransferMode mode)
        { Preview = null; Preview = new TransferPlan(target, clipboard, mode); return "Preview for " + target.Prefab.name + ": " + Preview.Changes.Count + " change(s)."; }

        public string Apply(TransferClipboard clipboard, TransferTarget current)
        {
            var plan = Preview;
            if (plan == null || !ReferenceEquals(plan.Target.World, current.World) || !ReferenceEquals(plan.Target.Prefab, current.Prefab) || !ReferenceEquals(plan.Target.Parent, current.Parent))
                throw new InvalidOperationException("Destination changed. Preview again.");
            plan.CheckCurrent(clipboard.Revision);
            if (plan.Changes.Count == 0) throw new InvalidOperationException("Preview contains no transferable changes.");
            BeforeWrite?.Invoke();
            var applied = new List<PlannedChange>(); bool dirty = plan.Target.Prefab.isDirty;
            try
            {
                foreach (var change in plan.Changes)
                {
                    if (change.Existing == null)
                    {
                        if (plan.Target.Exact(change.Type) != null) throw new InvalidOperationException("Component appeared since preview.");
                        var added = plan.Target.Prefab.AddComponent(change.Type);
                        if (added == null) throw new InvalidOperationException("Native component creation failed.");
                        var addition = new PlannedChange { Type = change.Type, Adapter = change.Adapter, Existing = added, After = change.After };
                        applied.Add(addition); added.active = false;
                        if (!ReferenceEquals(added.prefab, plan.Target.Prefab) || !ReferenceEquals(plan.Target.Exact(change.Type), added)) throw new InvalidOperationException("Native creation ownership/identity failed.");
                        if (addition.Adapter != null && addition.After != null)
                        {
                            addition.Adapter.Apply(addition.After, added);added.active=addition.After.Active;
                            var actual = addition.Adapter.Capture(added, plan.Target.Prefab);
                            if (actual.Fingerprint!=addition.After.Fingerprint) throw new InvalidOperationException("Added component values do not match preview.");
                        }
                    }
                    else
                    {
                        applied.Add(change); change.Adapter.Apply(change.After, change.Existing);
                        if (change.Adapter.Capture(change.Existing, plan.Target.Prefab).Fingerprint != change.After.Fingerprint)
                            throw new InvalidOperationException("Written settings did not match preview.");
                    }
                }
                plan.Target.Prefab.isDirty = true;
                plan.Target.Refresh();
            }
            catch (Exception error)
            {
                var failures = Rollback(plan.Target, applied); plan.Target.Prefab.isDirty = dirty;
                try { plan.Target.Refresh(); } catch (Exception refresh) { failures.Add(refresh.Message); }
                Preview = null;
                throw new InvalidOperationException("Apply failed. " + (failures.Count == 0 ? "Prior settings/components restored; refresh requeued. Dirty state may remain." : "Rollback/refresh incomplete: " + string.Join("; ", failures)), error);
            }
            undoTarget = plan.Target; undoChanges = applied; undoIdentities = undoTarget.Identities(); undoFingerprint = undoTarget.Fingerprint(); Preview = null;
            return "Applied " + applied.Count + " component change(s) to " + undoTarget.Prefab.name + ". Refresh queued. New components retain copied activation; existing component activation is preserved."
                + (CanRestore ? " Restore available." : " Automatic restore unavailable for unadapted additions; remove them through Object Info if needed.");
        }
        private static List<string> Rollback(TransferTarget target, IEnumerable<PlannedChange> changes)
        {
            var errors = new List<string>();
            foreach (var change in changes.Reverse())
            {
                try
                {
                    if (!ReferenceEquals(target.Exact(change.Type), change.Existing)) throw new InvalidOperationException("Component identity changed: " + change.Type.Name);
                    if (change.Before == null)
                    {
                        target.Prefab.Remove(change.Type);
                        if (target.Exact(change.Type) != null) throw new InvalidOperationException("Native removal failed: " + change.Type.Name);
                    }
                    else change.Adapter.Apply(change.Before, change.Existing);
                }
                catch (Exception error) { errors.Add(error.Message); }
            }
            return errors;
        }
        public string Restore(TransferTarget current)
        {
            if (undoTarget == null || !ReferenceEquals(current.World, undoTarget.World) || !ReferenceEquals(current.Prefab, undoTarget.Prefab) || !ReferenceEquals(current.Parent, undoTarget.Parent))
                throw new InvalidOperationException("Select the latest destination to restore.");
            undoTarget.Validate(); var now = undoTarget.Identities();
            if (now.Length != undoIdentities.Length || now.Where((c, i) => !ReferenceEquals(c, undoIdentities[i])).Any() || undoTarget.Fingerprint() != undoFingerprint)
                throw new InvalidOperationException("Destination changed since apply. Restore refused to protect later edits.");
            // Unknown additions have no field adapter with which to detect later
            // configuration changes; do not remove them under a false undo guarantee.
            if (undoChanges.Any(c => c.Before == null && c.Adapter == null))
                throw new InvalidOperationException("Restore of unadapted additions cannot verify later settings. Remove those additions through Object Info; typed additions can be restored automatically.");
            BeforeWrite?.Invoke();
            var errors = Rollback(undoTarget, undoChanges);
            try { undoTarget.Refresh(); } catch (Exception error) { errors.Add(error.Message); }
            Preview = null;
            if (errors.Count != 0) throw new InvalidOperationException("Restore incomplete: " + string.Join("; ", errors));
            undoTarget = null; undoChanges = null;
            return "Prior settings restored and tracked additions removed. Refresh queued; save restored values if needed.";
        }
    }
}

