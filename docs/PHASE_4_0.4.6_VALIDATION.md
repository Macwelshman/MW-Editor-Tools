# Version 0.4.6 — native Options and saved-position opening

VERIFIED-RUNTIME (user): all 0.4.5 tests pass, including Settings stability. User then requested moving history limit, shortcut settings and explanatory text to CS2 Options with text entry, adding Reset window position, and eliminating a slight window opening jump.

VERIFIED-STATIC: native Options APIs were read from installed Game.dll, MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef, SHA256 AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A (rechecked unchanged). Game.Modding.ModSetting.RegisterInOptionsUI IL_0008 calls Setting.RegisterInOptionsUI; SettingsUITextInputAttribute exists; Setting.Apply and ApplyAndSave are virtual; localization source registration/removal and text/button attributes were verified. Narrow API evidence is phase4-options-api.txt. No game files modified.

0.4.6 registers MW Editor Tools in native Options. History limit, Undo letter and Redo letter use SettingsUITextInput; enable-shortcuts switch, instructions and Reset window position are in the same page. Existing JSON settings persist and initialize the options. Inputs accept integer 20–50 and letters A–Z (trimmed/uppercased); invalid values retain the previous setting. Changed history depth clears recovery history, unchanged depth does not. Options unregister and locale sources are removed on disposal.

The shared Settings tab retains confirmation, recovery and inspector switches. Moved controls and accompanying text are removed. Reset increments a frontend panel revision; obsolete drag callbacks cannot overwrite its position. The native window is hidden for its two initial positioning frames; unsupported display:contents removed. Opening jump cause is a HYPOTHESIS; visual fix remains UNPROVEN.

VERIFIED-STATIC: C# Release zero warnings/errors; pinned production UI build/type checks pass. VERIFIED-AUTOMATED: 51 Phase 4 assertions including text validation and history invalidation. Shared Settings rendering confirms moved controls are absent. Earlier Phase 2/3 policies unchanged.

Test on a duplicate asset:
1. Restart CS2. Open Options → MW Editor Tools. Check labels, instructions, shortcut switch and Reset window position.
2. Enter history limit 37, Undo letter x and Redo letter r. Apply/leave the field as the native menu requires. Check values become 37/X/R.
3. Try invalid entries (19, 51, abc for the limit; F12 or Ctrl+Z for a letter). Previous valid values should remain; editor UI must stay responsive.
4. Return to the editor. Shortcuts should be Ctrl+X and Ctrl+Shift+R. Typing in the Options text fields must not move deletion history. Toggle shortcuts off/on and verify.
5. Record deletion history; change the limit. History should clear. Applying unchanged limit should not clear it.
6. Move the MW window, close/reopen repeatedly. It should appear at the saved position without a rightward starting flash.
7. Reset window position from Options with the MW window open, then with it closed. It should return to default and retain that position on reopening/restarting.
8. Check the shared Settings tab has only its remaining switches/status and remains stable. Existing mod icons remain usable.
9. Restart and check Options values persist. Restore defaults (20, Z/Z, shortcuts enabled) and continue the Phase 4 regression checklist.

Native Options typing/apply, reset remount and opening appearance remain UNPROVEN until user acceptance. No later-phase implementation.
