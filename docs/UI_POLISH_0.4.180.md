# Clipboard, reports and Undo presentation — 0.4.180

21 October 2026. User requested Replacer Clear Clipboard matching Components, clearing paste information on asset switch, and inspector Undo initially closed with text not capitalised.

VERIFIED-STATIC: Replacer Clear Clipboard is a separate button in its action row and disabled when empty. Components tracks report asset identity independently from component/selection revisions, clearing latest compatibility report and result text on an actual prefab switch; same-asset component refresh retains reports. ClearReport preserves Restore data and copied clipboard entries. Replacer also clears status on asset switches, before command/shortcut handling. Native inspector Undo section is created expanded=false. The editor's native section label CSS forces text-transform:uppercase; a text-only span on the exact editorPanel.mwRecovery heading overrides this to none, without altering other headers or focus controls.

VERIFIED-STATIC: Release/webpack pass; 55 component transfer checks pass including report clear preserving Restore. Focused Components/heading/recovery UI checks pass, including exact Undo text-transform override. Prior native checkbox FOCUS_DISABLED fix is retained. Existing NU1900 warning remains.

UNSUPPORTED/UNPROVEN: native initial collapse, heading casing, final layout and report clearing on asset switch require grouped in-game acceptance. No computer use or publication.
