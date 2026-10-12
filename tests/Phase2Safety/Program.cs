using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MW.EditorTools.Modules.DeletionProtection;

internal static class Program
{
    private static int checks;
    private static bool allowOnce;
    private static int mutations;
    private static bool nested;
    private static bool fail;

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAILED: " + name);
        checks++;
    }

    private static void Main()
    {
        var gate = new ConfirmationGate();
        Check(MW.EditorTools.DeletionPatchPolicy.IsKnownOwner(MW.EditorTools.DeletionPatchPolicy.ConfirmationOwner), "confirmation owner allowed");
        Check(MW.EditorTools.DeletionPatchPolicy.IsKnownOwner(MW.EditorTools.DeletionPatchPolicy.RecoveryOwner), "own recovery invalidation prefix compatible");
        Check(!MW.EditorTools.DeletionPatchPolicy.IsKnownOwner("ThirdParty.Deletion"), "unknown deletion patch refused");
        var first = gate.Begin(0);
        Check(first != 0, "request token issued");
        Check(gate.Begin(1) == 0, "duplicate request refused");
        Check(!gate.Consume(first + 1, true, 2) && gate.Busy, "foreign reply cannot cancel current request");
        Check(!gate.Consume(first, false, 3) && !gate.Busy, "cancel never authorises deletion");
        Check(!gate.Consume(first, true, 4), "late confirm after cancel refused");
        var second = gate.Begin(10);
        gate.Invalidate();
        var third = gate.Begin(11);
        Check(!gate.Consume(second, true, 12) && gate.Busy, "stale reply cannot affect newer request");
        Check(gate.Consume(third, true, 13), "current confirmation accepted");
        Check(!gate.Consume(third, true, 14), "confirmation consumed once");
        var expiry = gate.Begin(20);
        Check(!gate.Expired(79.99) && gate.Expired(80), "deadline boundary");
        Check(!gate.Consume(expiry, true, 80), "expired confirmation refused");
        var closed = gate.Begin(100);
        Check(!gate.Consume(closed, false, 101), "dismissal refused");

        // Managed Harmony smoke test only; not the Unity game handler.
        var harmony = new Harmony("MW.EditorTools.SafetyHarness");
        var method = typeof(Program).GetMethod(nameof(Delete), BindingFlags.NonPublic | BindingFlags.Static);
        harmony.Patch(method, prefix: new HarmonyMethod(typeof(Program).GetMethod(nameof(Prefix), BindingFlags.NonPublic | BindingFlags.Static)));
        try
        {
            Delete();
            Check(mutations == 0, "prefix cancellation skips original");
            allowOnce = true;
            nested = true;
            Delete();
            Check(mutations == 1 && !allowOnce, "nested deletion cannot reuse consumed bypass");
            fail = true;
            allowOnce = true;
            try { Delete(); } catch (InvalidOperationException) { }
            finally { allowOnce = false; }
            fail = false;
            Delete();
            Check(mutations == 1 && !allowOnce, "exception does not retain bypass");
        }
        finally { harmony.UnpatchAll("MW.EditorTools.SafetyHarness"); }
        Delete();
        Check(mutations == 2, "unpatch restores original");
        Console.WriteLine($"PASS: {checks} safety assertions. Unity/native dialog tests remain pending.");
    }

    private static bool Prefix()
    {
        if (!allowOnce) return false;
        allowOnce = false;
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Delete()
    {
        if (fail) throw new InvalidOperationException("Injected failure");
        mutations++;
        if (nested) { nested = false; Delete(); }
    }
}
