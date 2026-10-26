using System;
using System.Reflection;

namespace UnityEngine { internal static class Time { internal static int frameCount = 1; } }
namespace MW.EditorTools
{
    internal static class Mod
    {
        internal static readonly Logger Log = new Logger();
        internal sealed class Logger { internal int Warnings; internal void Warn(string message) { Warnings++; } }
    }
}
namespace MW.EditorTools.Modules.BulldozerRecovery
{
    internal sealed partial class BulldozerTrace
    {
        private const string Id = "MW.CompatibilityTest";
        private bool Busy;
        internal string Status;
        internal static void CheckConflictGate(MethodBase target, Action installForeign, Action<bool, string> check)
        {
            var trace = new BulldozerTrace();
            trace.patchMonitor.Watch(target);
            check(trace.PatchesCompatible(true), "actual recovery gate accepts owned hooks");
            installForeign();
            int warnings = Mod.Log.Warnings;
            check(!trace.PatchesCompatible(true) && trace.Status.Contains("unavailable"), "actual recovery gate refuses later patch and explains conflict");
            check(!trace.Busy, "idle conflict does not invent an in-flight operation");
            trace.Busy = true;
            check(!trace.PatchesCompatible(true) && trace.Busy && trace.Status.Contains("without saving"), "in-flight conflict retains busy interlock and requests reload");
            check(Mod.Log.Warnings == warnings + 1, "conflict reported once without repeated log flood");
        }
    }
}
