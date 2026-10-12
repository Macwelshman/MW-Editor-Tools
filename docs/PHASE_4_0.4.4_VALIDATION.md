# Version 0.4.4 — settings contract correction

VERIFIED-RUNTIME (0.4.3 user report): persistent icon and tooltip work; clicking does not show the window. Logs/MWEditorTools.log frontend diagnostic at 17:50:28 reports available=true, settings=false, launcher=true with visible 56x56 bounds. The Tools component explicitly returns null without state.settings. This proves the missing settings contract blocks the window; whether native panel rendering has any further issue remains UNPROVEN.

VERIFIED-STATIC: settings are now encoded explicitly using normalized scalar fields and invariant numeric formatting, appended to the UI state JSON. The unsupported nested runtime serialization path is removed. Settings persistence writes the same explicit schema and retains existing load support. Window open/close commands are logged. Recovery and shortcut policies unchanged; persistent icon retained.

VERIFIED-AUTOMATED: 43 Phase 4 assertions, including JSON parsing with French decimal culture, complete field coverage, unsafe key normalization and boolean values. C# Release zero warnings/errors and pinned UI build/type checks pass. Earlier 40 Phase 2/3 assertions cover unchanged recovery logic.

Test list:
1. Restart CS2; enter Asset Editor without selecting an asset. Persistent icon and tooltip should remain.
2. Click the icon: shared window should open with No asset selected and disabled Undo/Redo. Close and reopen using both icon and Close button.
3. Select a duplicate asset; check label updates. Change tabs and drag the window.
4. In Settings, change a switch/key/history limit; restore defaults. Restart CS2 and check settings/position persistence.
5. Continue PHASE_4_VALIDATION.md, especially recovery/typing/dialog checks.
6. Report the exact failed step if window still does not open. Diagnostics should now report settings=true; logs record the open/close command.

Runtime acceptance of 0.4.4 remains UNPROVEN.
