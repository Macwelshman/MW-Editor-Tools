# Standalone hide UI keys - 0.4.253

User requested single-key shortcuts on 2026-10-26. The hide UI recorder now accepts named keyboard keys without modifiers, including Home/End, Insert/Delete, Page Up/Down, F1-F12, number-row keys, arrows, Space/Tab/Enter/Backspace and punctuation. Standalone letters and modifier combinations remain supported. Existing custom bindings and Ctrl+H default are preserved. Other MW shortcut recorders retain their letter-only scope.

VERIFIED-STATIC: key normalization uses an explicit allowlist; modifier-only and unknown paths are rejected. Saved named keys retain zero modifiers, and polling parses Unity InputSystem.Key case-insensitively. Native screen toggle, typing/dialog guards, restoration and conflicts from 0.4.252 are unchanged; see HIDE_UI_0.4.252.md for native API/hash evidence.

UNSUPPORTED/UNPROVEN: native acceptance pending. In Options, click Hide / show UI shortcut, press Home or F8 without modifiers, then Apply changes. Check hide/show, Escape restoration, typing guard and persistence after restarting. Native/third-party shortcut conflicts remain user-configured.

VERIFIED-STATIC: Release and production UI builds passed; OptionsSafety 61 and Phase4Safety 353 assertions passed. Supported names verified against the installed Unity.InputSystem.Key enum. Whitespace checks pass. Existing NU1900 vulnerability-feed warning remains.

INSTALLED: 0.4.253 after verifying Cities2 was absent. Nine previous files backed up and hash-verified at outputs/rollback/MWEditorTools-before-0.4.253-20261026-183440/MWEditorTools. Three installed payload hashes and assembly version match staging; evidence/hide-ui-0.4.253/installed-hashes.csv. Native acceptance pending.
