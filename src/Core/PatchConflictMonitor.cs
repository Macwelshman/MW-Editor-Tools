using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace MW.EditorTools
{
    // Read Harmony metadata only. Never remove, reorder or whitelist foreign patches.
    // A detected change stays refused until reload: old snapshots cannot be assumed
    // valid merely because a conflicting mod subsequently removes its patch.
    internal sealed class PatchConflictMonitor
    {
        private readonly string owner;
        private readonly HashSet<string> accepted;
        private readonly HashSet<MethodBase> methods = new HashSet<MethodBase>();
        private int checkedFrame = int.MinValue;
        internal string Conflict { get; private set; }

        internal PatchConflictMonitor(string owner, params string[] compatibleOwners)
        {
            this.owner = owner;
            accepted = new HashSet<string>(compatibleOwners) { owner };
        }

        internal void Watch(MethodBase method) { methods.Add(method); checkedFrame = int.MinValue; }
        internal void Forget(MethodBase method) { methods.Remove(method); checkedFrame = int.MinValue; }

        internal bool Check(int frame, bool force = false)
        {
            if (Conflict != null) return false;
            if (!force && checkedFrame == frame) return true;
            checkedFrame = frame;
            try
            {
                foreach (var method in methods)
                {
                    var patches = Harmony.GetPatchInfo(method);
                    bool owned = false;
                    List<string> foreign = null;
                    if (patches != null) foreach (var id in patches.Owners)
                    {
                        owned |= id == owner;
                        if (!accepted.Contains(id))
                        {
                            if (foreign == null) foreign = new List<string>();
                            foreign.Add(id);
                        }
                    }
                    if (foreign == null && owned) continue;
                    foreign?.Sort(StringComparer.Ordinal);
                    Conflict = method.DeclaringType?.FullName + "." + method.Name + ": " +
                        (foreign != null ? "foreign patch owners " + string.Join(", ", foreign) : "MW hook removed");
                    return false;
                }
                return true;
            }
            catch (Exception error)
            {
                Conflict = "Patch ownership could not be verified: " + error.GetType().Name;
                return false;
            }
        }
    }
}
