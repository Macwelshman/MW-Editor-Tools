using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Game.Prefabs;
using HarmonyLib;
using UnityEngine;

namespace MW.EditorTools.Modules.Compatibility
{
    // Optional adapter: no dependency on, or disk changes to, Asset Icon Creator.
    internal sealed class AssetIconCreatorCompatibility : MonoBehaviour
    {
        private const string Id = "MW.EditorTools.AssetIconCreatorCompatibility";
        private const string SupportedMvid = "2c652cbe-d9b5-46ec-8850-c7a39cf7d8a6";
        private const string CallbackType = "AssetIconCreator.ScreenshotUtility+<>c__DisplayClass33_0";
        private Harmony harmony;
        private MethodInfo target;
        private float nextScan;
        private bool stopped;
        private readonly PatchConflictMonitor monitor = new PatchConflictMonitor(Id);

        internal void Initialize()
        {
            stopped = false;
            harmony = new Harmony(Id);
            Scan();
        }

        private void Update()
        {
            if (stopped) return;
            try
            {
                if (target != null)
                {
                    if (!monitor.Check(Time.frameCount))
                    {
                        Mod.Log.Warn("Asset Icon Creator compatibility stopped: " + monitor.Conflict +
                            ". Automatic UI Object setup may reset Group/Priority; disable that option. Other MW tools remain active.");
                        Release();
                    }
                    return;
                }
                if (Time.unscaledTime < nextScan) return;
                Scan();
            }
            catch (Exception error)
            {
                Mod.Log.Warn("Asset Icon Creator compatibility unavailable: " + error.Message +
                    ". Other MW tools remain active; automatic UI Object setup is unprotected.");
                Release();
            }
        }

        private void Scan()
        {
            nextScan = Time.unscaledTime + 5;
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "AssetIconCreator");
            if (assembly == null) return; // It may load after MW; absence is normal.
            if (assembly.GetName().Version != new Version(1, 4, 1, 0) ||
                assembly.ManifestModule.ModuleVersionId.ToString() != SupportedMvid ||
                !VerifiedGameBuild.Game(typeof(PrefabBase).Module.ModuleVersionId) ||
                typeof(Harmony).Assembly.GetName().Version != new Version(2, 4, 2, 0))
                throw new NotSupportedException("Unverified Asset Icon Creator/game/Harmony build");

            target = assembly.GetType(CallbackType, true).GetMethod("<ProcessScreenshot>b__0",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (target == null || target.ReturnType != typeof(void) || target.GetParameters().Length != 0 ||
                target.MetadataToken != 0x060000a1)
                throw new NotSupportedException("Unverified thumbnail callback signature");
            if (Harmony.GetPatchInfo(target)?.Owners.Any() == true)
                throw new NotSupportedException("Another mod patches the thumbnail callback");
            if (!ReplacementSites(PatchProcessor.GetOriginalInstructions(target), out _, out _))
                throw new NotSupportedException("Thumbnail UI Object replacement IL changed");
            try
            {
                harmony.Patch(target, transpiler: new HarmonyMethod(typeof(AssetIconCreatorCompatibility)
                    .GetMethod(nameof(PreserveUiObject), BindingFlags.Static | BindingFlags.NonPublic)));
                monitor.Watch(target);
                Mod.Log.Info("Asset Icon Creator 1.4.1 compatibility installed: thumbnail updates retain the existing UI Object, Group and Priority.");
            }
            catch { Release(); throw; }
        }

        // Reuse the original instance: snapshot identity, settings and references
        // stay intact. Asset Icon Creator still assigns its icon and refreshes once.
        private static UIObject ExistingOrNew(PrefabBase prefab) =>
            prefab.TryGet<UIObject>(out var existing) ? existing : prefab.AddComponent<UIObject>();

        private static bool UiCall(CodeInstruction instruction, string name)
        {
            var method = instruction.operand as MethodInfo;
            return (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
                method != null && method.DeclaringType == typeof(PrefabBase) && method.Name == name &&
                method.IsGenericMethod && method.GetGenericArguments().SequenceEqual(new[] { typeof(UIObject) }) &&
                method.GetParameters().Length == 0;
        }

        private static IEnumerable<CodeInstruction> PreserveUiObject(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.Select(i => new CodeInstruction(i)).ToList();
            // Another transpiler can run before ours on a later recompile. Do not
            // throw into its installation or rewrite an unfamiliar instruction set.
            // The lifetime owner monitor then retires this optional adapter.
            if (!ReplacementSites(code, out var remove, out var add)) return code;
            // Remove<T>() consumes its prefab receiver and returns void. Pop has
            // exactly that stack effect, retaining labels and exception boundaries.
            code[remove].opcode = OpCodes.Pop;
            code[remove].operand = null;
            code[add].opcode = OpCodes.Call;
            code[add].operand = typeof(AssetIconCreatorCompatibility)
                .GetMethod(nameof(ExistingOrNew), BindingFlags.Static | BindingFlags.NonPublic);
            return code;
        }

        private static bool ReplacementSites(IEnumerable<CodeInstruction> instructions, out int remove, out int add)
        {
            var sites = instructions.Select((i, n) => new { i, n }).ToArray();
            var removes = sites.Where(p => UiCall(p.i, "Remove")).ToArray();
            var adds = sites.Where(p => UiCall(p.i, "AddComponent")).ToArray();
            remove = removes.Length == 1 ? removes[0].n : -1;
            add = adds.Length == 1 ? adds[0].n : -1;
            return remove >= 0 && add > remove;
        }

        internal void Release()
        {
            stopped = true;
            try { if (target != null) harmony?.Unpatch(target, HarmonyPatchType.All, Id); }
            finally { target = null; }
        }

        private void OnDestroy() { Release(); }
    }
}
