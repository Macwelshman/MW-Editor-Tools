# Version 0.4.7 — constructor-bypass lifecycle correction

VERIFIED-RUNTIME: user confirmed all mods returned after 0.4.5 rollback. 0.4.6 is rejected.

VERIFIED-STATIC: installed Game.dll MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef; recorded unchanged SHA256 AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A. Game.Modding.ModManager/ModInfo.Load IL_013a calls FormatterServices.GetUninitializedObject, bypassing Mod constructors/instance field initializers. 0.4.6 MW.EditorTools.dll MVID 16f9ccfe-e0f9-485a-9ee7-cfdab23172c4 initializes optionsLocales only in Mod..ctor IL_0001–0006; runtime OnDispose fails at offset 0x35 while accessing that uninitialized list (IL_0036 field load/IL_003b enumeration). Original registration's list Add also depends on it. Game.Modding.ModManager.InitializeMods IL_008e invokes Dispose in its failure handler before InitializeUIModules IL_00e3, explaining why the cleanup exception aborts remaining initialization. Exact evidence: phase4-lifecycle-il.txt and phase4-mod-load-il.txt; failure logs preserved under evidence/0.4.6-startup-failure.

0.4.7 initializes locale lifecycle state explicitly in OnLoad. Options registration is isolated: failure logs the original error, releases partially registered resources and leaves editor tools active. Cleanup detaches each resource reference first, catches individual release errors and continues; empty/repeated disposal is safe. Original errors are logged before cleanup. Recovery policies unchanged. Requested native Options/text fields/reset and saved-position opening behavior from 0.4.6 retained for runtime validation.

VERIFIED-AUTOMATED: 11 new lifecycle assertions compile and execute actual Mod source with mocked game services and RuntimeHelpers.GetUninitializedObject. They cover constructor bypass, partial registration failure, throwing unregister/locale removal, continued service cleanup and repeat disposal. 51 Phase 4 assertions pass; shared Settings React rendering passes. VERIFIED-STATIC: Release zero warnings/errors, pinned UI build/type checks pass. Tests do not run Unity/Cohtml/native Options registration.

Test in this order:
1. Restart CS2. Confirm all existing MW mod icons return and function. Stop and report if any disappear.
2. Open Options → MW Editor Tools. Check text fields, explanations, shortcut switch and Reset window position.
3. Enter history limit 37 and shortcut letters x/r. Confirm values normalize to 37/X/R and persist on reopening.
4. Try invalid text (19, 51, abc for limit; F12 or Ctrl+Z for letter). Previous values should remain.
5. In a duplicate asset, check Ctrl+X / Ctrl+Shift+R; typing in Options must not move recovery history. Changed limit clears history; unchanged limit does not.
6. Move, close/reopen the window repeatedly. Check it appears directly at saved position. Reset from Options with the window open and closed; it should return to default and persist after restart.
7. Shared Settings tab and original mod windows should remain stable. Restart once more to check initialization and persistence; restore 20, Z/Z and shortcuts enabled.
8. Continue PHASE_4_VALIDATION.md if all new checks pass.

UNPROVEN: native runtime acceptance of 0.4.7 and visual jump correction until user feedback. No Phase 5/module migration.
