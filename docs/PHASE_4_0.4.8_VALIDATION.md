# Version 0.4.8 — draft text editing and configurable modifiers

VERIFIED-RUNTIME (user, 0.4.7): invalid entries, Reset window position and persistent window pass. Text input only works when replacing selected characters; Backspace and ordinary typing fail. Runtime modifier selection requested.

VERIFIED-STATIC: Options.Apply previously validated and ReadCurrent replaced draft text on every native per-keystroke apply. 0.4.8 Apply/ApplyAndSave preserve drafts; Apply changes button validates and commits text, switches and modifier choices to existing persistent settings. Invalid entries retain previous active values. Drafts do not change active shortcuts or history. Reset remains independent. Native typing behavior requires runtime confirmation.

Each shortcut has independent Ctrl, Shift, Alt and Windows switches. Exact modifier mask required; defaults remain Ctrl+Z and Ctrl+Shift+Z. Identical key+mask combinations cause no action. Settings JSON stores masks and migrates old files missing masks to defaults while preserving other values. History displays active combinations. Editor availability, focus heartbeat, typing, dialog and fresh-key-press gates retained.

VERIFIED-AUTOMATED: 311 Phase 4 assertions (all 16x16 modifier matches, duplicate suppression, typing, migration, previous settings/history checks); 11 Options editing assertions exercise actual Options source with draft deletion/typing and explicit commit (native persistence mocked); 11 constructor-bypass lifecycle checks and shared Settings rendering pass. VERIFIED-STATIC: C# Release zero warnings/errors; pinned UI build/type checks pass. No game assemblies modified; no later phase implementation.

Test list (duplicate asset):
1. Restart CS2; confirm all mod icons and MW window still work.
2. Options → MW Editor Tools: history limit Ctrl+A, Backspace, type 37 one digit at a time. Repeat by deleting individual digits; draft must stay editable. Click Apply changes; expect 37.
3. Delete shortcut letter with Backspace, type x; use Delete and normal typing too. Click Apply changes; expect X. Try invalid text; previous active value should remain after Apply changes.
4. Set Undo Alt+X and Redo Ctrl+Alt+R using the modifier switches. Click Apply changes. Check active combinations shown in History and test supported Undo/Redo.
5. Press with a missing or extra modifier: no action. While typing in any field (including these Options fields), recovery history must not move. Holding the key must not repeat.
6. Configure identical letter/modifiers for both: neither action should run. Choose distinct combinations again.
7. Edit draft values but do not click Apply changes: active shortcuts/history should remain unchanged. Click Apply changes and verify a changed history limit clears history, while unchanged limit does not.
8. Close/reopen Options and restart CS2 after committing: values and modifiers must persist. Reset window and saved opening position must still work.
9. Restore defaults with Ctrl Undo, Ctrl+Shift Redo, Z/Z, limit 20 and shortcuts enabled; click Apply changes. Run remaining PHASE_4_VALIDATION.md regressions.

UNPROVEN: native text editing and modifier runtime acceptance until user confirmation.
