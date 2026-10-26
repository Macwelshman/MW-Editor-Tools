using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MW.EditorTools;

internal static class CompatibilityChecks
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Target() { }
    private static void Hook() { }
    private static void CompatibleHook() { }
    private static void ForeignHook() { }

    internal static void Run(Action<bool, string> check)
    {
        const string own = "MW.CompatibilityTest", compatible = "MW.CompatibleTest", foreign = "Other.EditorTest";
        var target = typeof(CompatibilityChecks).GetMethod(nameof(Target), BindingFlags.Static | BindingFlags.NonPublic);
        var hook = new HarmonyMethod(typeof(CompatibilityChecks).GetMethod(nameof(Hook), BindingFlags.Static | BindingFlags.NonPublic));
        var compatibleHook = new HarmonyMethod(typeof(CompatibilityChecks).GetMethod(nameof(CompatibleHook), BindingFlags.Static | BindingFlags.NonPublic));
        var foreignHook = new HarmonyMethod(typeof(CompatibilityChecks).GetMethod(nameof(ForeignHook), BindingFlags.Static | BindingFlags.NonPublic));
        var first = new Harmony(own); var second = new Harmony(compatible); var other = new Harmony(foreign);
        try
        {
            first.Patch(target, prefix: hook);
            var monitor = new PatchConflictMonitor(own, compatible);
            monitor.Watch(target);
            check(monitor.Check(1), "owned hook accepted");
            second.Patch(target, postfix: compatibleHook);
            check(monitor.Check(2), "explicit compatible owner accepted");
            other.Patch(target, transpiler: new HarmonyMethod(typeof(CompatibilityChecks).GetMethod(nameof(Transpile), BindingFlags.Static | BindingFlags.NonPublic)));
            check(!monitor.Check(2, true), "forced action check detects later patch within same frame");
            check(monitor.Conflict.Contains(foreign) && monitor.Conflict.Contains(nameof(Target)), "conflict identifies owner and target");
            other.UnpatchAll(foreign);
            check(!monitor.Check(3), "conflict stays refused after foreign patch removed");
            var fresh = new PatchConflictMonitor(own, compatible); fresh.Watch(target);
            check(fresh.Check(3), "new lifetime accepts clean hooks");
            first.UnpatchAll(own);
            check(!fresh.Check(4) && fresh.Conflict.Contains("removed"), "removed MW hook detected");
            var rolledBack = new PatchConflictMonitor(own); rolledBack.Watch(target); rolledBack.Forget(target);
            check(rolledBack.Check(5), "rolled back optional hook excluded from monitoring");
            first.Patch(target, prefix: hook);
            second.UnpatchAll(compatible);
            foreach (var kind in new[] { HarmonyPatchType.Prefix, HarmonyPatchType.Postfix, HarmonyPatchType.Finalizer })
            {
                var later = new PatchConflictMonitor(own); later.Watch(target);
                check(later.Check(10), "clean before later " + kind);
                if (kind == HarmonyPatchType.Prefix) other.Patch(target, prefix: foreignHook);
                else if (kind == HarmonyPatchType.Postfix) other.Patch(target, postfix: foreignHook);
                else other.Patch(target, finalizer: foreignHook);
                check(!later.Check(11), "later " + kind + " detected by next frame");
                other.UnpatchAll(foreign);
            }
            MW.EditorTools.Modules.BulldozerRecovery.BulldozerTrace.CheckConflictGate(target,
                () => other.Patch(target, postfix: foreignHook), check);
        }
        finally { first.UnpatchAll(own); second.UnpatchAll(compatible); other.UnpatchAll(foreign); }
    }

    private static System.Collections.Generic.IEnumerable<CodeInstruction> Transpile(System.Collections.Generic.IEnumerable<CodeInstruction> instructions) => instructions;
}
