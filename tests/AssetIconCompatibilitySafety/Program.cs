using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Game.Prefabs;
using HarmonyLib;
using MW.EditorTools.Modules.Compatibility;

internal static class Program
{
    private static int checks;
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
    private sealed class Callback
    {
        internal PrefabBase Prefab = new PrefabBase();
        internal bool AutoSet = true;
        internal int Refreshed, Completed;
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void Run()
        {
            if (AutoSet)
            {
                if (Prefab.TryGet<UIObject>(out var ui)) Prefab.Remove<UIObject>();
                ui = Prefab.AddComponent<UIObject>();
                ui.m_Priority = ui == null ? 1 : ui.m_Priority;
                ui.m_Group = ui == null ? null : ui.m_Group;
                ui.m_Icon = "new-icon";
                Refreshed++;
            }
            Completed++;
        }
    }
    private sealed class DerivedUI : UIObject { }
    private static int foreignCalls;
    private static void ForeignPrefix() { foreignCalls++; }
    private static void Main()
    {
        var transpiler = typeof(AssetIconCreatorCompatibility).GetMethod("PreserveUiObject", BindingFlags.Static | BindingFlags.NonPublic);
        var method = typeof(Callback).GetMethod(nameof(Callback.Run), BindingFlags.Instance | BindingFlags.NonPublic);
        var baseline = new Callback(); var original = baseline.Prefab.AddComponent<UIObject>();
        original.m_Priority = 321; original.m_Group = new object(); baseline.Run();
        Check(!ReferenceEquals(original, baseline.Prefab.Component) && baseline.Prefab.Component.m_Priority != 321,
            "unadapted fixture reproduces component replacement and priority loss");
        const string owner = "MW.EditorTools.AssetIconCreatorCompatibility";
        var harmony = new Harmony(owner);
        harmony.Patch(method, transpiler: new HarmonyMethod(transpiler));
        try
        {
            var callback = new Callback(); var ui = callback.Prefab.AddComponent<UIObject>(); var group = new object();
            ui.m_Priority = -42; ui.m_Group = group; ui.m_Icon = "old"; ui.Debug = true; ui.Active = false;
            callback.Run();
            Check(ReferenceEquals(ui, callback.Prefab.Component), "existing UI Object identity retained");
            Check(ReferenceEquals(ui.m_Group, group), "exact Group reference retained");
            Check(ui.m_Priority == -42, "negative Priority retained");
            Check(ui.m_Icon == "new-icon", "thumbnail updated");
            Check(ui.Debug && !ui.Active, "unrelated UI Object settings retained");
            Check(callback.Prefab.Removes == 0 && callback.Prefab.Adds == 1, "existing component never removed or duplicated");
            Check(callback.Refreshed == 1 && callback.Completed == 1, "native callback refresh/completion execute once");
            callback.Run();
            Check(ReferenceEquals(ui, callback.Prefab.Component) && ui.m_Priority == -42 && callback.Refreshed == 2,
                "repeated thumbnails preserve identity and Priority");
            var missing = new Callback(); missing.Run();
            Check(missing.Prefab.Component != null && missing.Prefab.Adds == 1 && missing.Prefab.Component.m_Icon == "new-icon",
                "missing UI Object created once with thumbnail");
            var other = new Callback(); var otherUi = other.Prefab.AddComponent<UIObject>(); otherUi.m_Priority = int.MaxValue;
            other.Run(); Check(otherUi.m_Priority == int.MaxValue && ui.m_Priority == -42, "different assets retain independent Priorities");
            var disabled = new Callback { AutoSet = false }; disabled.Run();
            Check(disabled.Prefab.Component == null && disabled.Refreshed == 0 && disabled.Completed == 1, "disabled auto assignment stays unchanged");
            var derived = new Callback(); var derivedUi = new DerivedUI { m_Priority = 17, m_Group = group };
            derived.Prefab.Component = derivedUi; derived.Run();
            Check(ReferenceEquals(derived.Prefab.Component, derivedUi) && derived.Prefab.Adds == 0 && derivedUi.m_Priority == 17,
                "same TryGet semantics retain a derived UI component without adding a duplicate");
            foreach (var value in new[] { int.MinValue, 0, 1, 100, int.MaxValue })
            {
                ui.m_Priority = value; callback.Run(); Check(ui.m_Priority == value && ReferenceEquals(ui.m_Group, group), "Priority boundary retained: " + value);
            }

            var remove = typeof(PrefabBase).GetMethod(nameof(PrefabBase.Remove)).MakeGenericMethod(typeof(UIObject));
            var add = typeof(PrefabBase).GetMethod(nameof(PrefabBase.AddComponent)).MakeGenericMethod(typeof(UIObject));
            var generator = new DynamicMethod("labels", typeof(void), Type.EmptyTypes).GetILGenerator(); var label = generator.DefineLabel();
            var first = new CodeInstruction(OpCodes.Callvirt, remove); first.labels.Add(label);
            first.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
            var input = new[] { first, new CodeInstruction(OpCodes.Callvirt, add) };
            var output = ((IEnumerable<CodeInstruction>)transpiler.Invoke(null, new object[] { input })).ToArray();
            Check(output[0].opcode == OpCodes.Pop && output[0].operand == null, "Remove becomes balanced receiver pop");
            Check(output[0].labels.SequenceEqual(first.labels) && output[0].blocks.Count == 1, "branch labels and exception boundaries retained");
            Check(input[0].opcode == OpCodes.Callvirt && Equals(input[0].operand, remove), "input instructions not mutated");
            Check(output[1].opcode == OpCodes.Call && ((MethodInfo)output[1].operand).Name == "ExistingOrNew", "only typed UI allocation routed through reuse helper");
            foreach (var invalid in new[] { Array.Empty<CodeInstruction>(), input.Take(1).ToArray(), input.Reverse().ToArray(), input.Concat(input).ToArray() })
            {
                var unchanged = ((IEnumerable<CodeInstruction>)transpiler.Invoke(null, new object[] { invalid })).ToArray();
                Check(unchanged.Length == invalid.Length && unchanged.Zip(invalid, (a, b) => a.opcode == b.opcode && Equals(a.operand, b.operand)).All(v => v),
                    "unexpected later IL shape left untouched without breaking another mod's patch installation");
                var validate = typeof(AssetIconCreatorCompatibility).GetMethod("ReplacementSites", BindingFlags.Static | BindingFlags.NonPublic);
                Check(!(bool)validate.Invoke(null, new object[] { invalid, 0, 0 }), "unexpected original IL refused by attachment preflight");
            }
            var foreign = new Harmony("Other.ThumbnailTest");
            try
            {
                foreign.Patch(method, prefix: new HarmonyMethod(typeof(Program).GetMethod(nameof(ForeignPrefix), BindingFlags.Static | BindingFlags.NonPublic)));
                var adapter = new AssetIconCreatorCompatibility();
                typeof(AssetIconCreatorCompatibility).GetField("target", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(adapter, method);
                typeof(AssetIconCreatorCompatibility).GetField("harmony", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(adapter, harmony);
                ((MW.EditorTools.PatchConflictMonitor)typeof(AssetIconCreatorCompatibility).GetField("monitor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(adapter)).Watch(method);
                typeof(AssetIconCreatorCompatibility).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(adapter, null);
                var owners = Harmony.GetPatchInfo(method).Owners.ToArray();
                Check(!owners.Contains(owner) && owners.Contains("Other.ThumbnailTest"), "late conflict retires only the optional adapter and retains the foreign patch");
                var reverted = new Callback(); var priorUi = reverted.Prefab.AddComponent<UIObject>(); reverted.Run();
                Check(foreignCalls == 1 && !ReferenceEquals(priorUi, reverted.Prefab.Component), "foreign callback remains functional after adapter cleanup");
                adapter.Release(); Check(Harmony.GetPatchInfo(method).Owners.Contains("Other.ThumbnailTest"), "repeat disposal cannot unpatch another owner");
            }
            finally { foreign.UnpatchAll("Other.ThumbnailTest"); }
        }
        finally { harmony.UnpatchAll(owner); }
        var after = new Callback(); var before = after.Prefab.AddComponent<UIObject>(); after.Run();
        Check(!ReferenceEquals(before, after.Prefab.Component), "adapter unpatch restores original callback");
        var absent = new AssetIconCreatorCompatibility(); absent.Initialize(); absent.Release(); absent.Release();
        Check(true, "absent dependency and repeated cleanup are harmless");
        Console.WriteLine("PASS: " + checks + " actual-adapter/Harmony thumbnail preservation assertions. Native screenshot/save/Undo acceptance remains pending.");
    }
}

namespace Game.Prefabs
{
    public class UIObject { public int m_Priority; public object m_Group; public string m_Icon; public bool Debug, Active = true; }
    public class PrefabBase
    {
        public UIObject Component; public int Adds, Removes;
        public bool TryGet<T>(out T component) where T : class { component = Component as T; return component != null; }
        public void Remove<T>() { Removes++; Component = null; }
        public T AddComponent<T>() where T : UIObject, new() { Adds++; var component = new T(); Component = component; return component; }
    }
}
namespace UnityEngine { public class MonoBehaviour { } public static class Time { public static int frameCount = 1; public static float unscaledTime = 1; } }
namespace MW.EditorTools { internal static class Mod { internal static Logger Log = new Logger(); internal class Logger { internal void Info(string text) { } internal void Warn(string text) { } } } }
