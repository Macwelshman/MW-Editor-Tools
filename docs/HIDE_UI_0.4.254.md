# Backquote hide/show UI default - 0.4.254

User requested Backquote (`) as the default on 2026-10-26. Fresh settings, native binding metadata, invalid-value fallback, legacy settings and Reset Defaults now use BACKQUOTE with zero modifiers. Existing Ctrl+H defaults migrate on load; other custom bindings and the enable switch are retained. The native hide/show implementation is unchanged.

VERIFIED-STATIC: installed Game.dll Game.Input.BindingKeyboard includes Backquote. The hide binding keeps its own action identity and seeds modifier metadata from the existing Ctrl+Z template, so the new modifier-free native default still supports recording later modifier combinations without indexing an empty modifier list. The Options harness models an empty default modifier list.

Native acceptance pending: restart and check Backquote hides/restores, Escape restores, custom bindings persist, and Reset Defaults displays Backquote. See HIDE_UI_0.4.252.md and HIDE_UI_0.4.253.md for native API evidence and existing input safeguards.

VERIFIED-STATIC: Release/UI builds, OptionsSafety (61 assertions), Phase4Safety (355 assertions) and whitespace checks passed. Existing NU1900 vulnerability-feed warning remains. INSTALLED: 0.4.254 with Cities2 absent; nine previous files backed up and hash-verified under outputs/rollback/MWEditorTools-before-0.4.254-20261026-184822/MWEditorTools. All three installed hashes and assembly version match staging. See evidence/hide-ui-0.4.254/installed-hashes.csv. Native acceptance remains pending.

VERIFIED-RUNTIME 2026-10-26: user confirmed the Backquote shortcut works and authorised updating the installer and existing private GitHub release.
