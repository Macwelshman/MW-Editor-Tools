# Windows installer and release — 0.4.163

User explicitly authorised a mod release with an installer on 19 October 2026. Distribution is a release in the existing private Macwelshman/MW-Editor-Tools GitHub repository; no external mod-platform publication or repository visibility change.

## Payload and installation

MWEditorTools-0.4.163-Setup.exe is a self-contained Windows Forms installer built with the Windows .NET Framework compiler. It requires .NET Framework 4.8, is unsigned, and embeds the four runtime files plus user manual, notices, release notes and installation instructions. No game assemblies or download-on-install dependency are included. The mod runtime files remain identical to installed/accepted 0.4.163.

Default destination is the current user's LocalLow Cities Skylines II Mods/MWEditorTools folder. A folder chooser supports another user Mods location. Destination must end in Mods/MWEditorTools. Game installation folders and junctions/symlinks are refused. No elevation is requested by the installer. A per-user-data-folder exclusive file lock prevents concurrent installs.

The entire previous mod folder is copied recursively and SHA256-verified before updating. Backups are outside Mods, under the user data folder's MWEditorTools-backups directory, so the game cannot discover a second mod DLL there. Unrelated installed files are preserved. Settings outside the mod folder are not modified.

Embedded hashes are checked before writes, and every installed file is checked immediately and in a final pass. Failure rolls back touched files when the game remains closed. If Cities2 starts, further writes and rollback stop; the UI tells the user to close the game and restore the displayed backup. The installer never terminates the game. It has no automatic uninstaller and does not reverse saved asset changes.

## Verification

VERIFIED-STATIC: warning-as-error compilation succeeds. 34 checks exercise the actual embedded release payload in temporary workspace folders: new install/hash verification, upgrade with complete nested backup, unrelated-file preservation, backup outside Mods, running/late-start game refusal, no rollback writes while the game runs, corrupted payload refusal, game/wrong-folder refusal, actual junction refusal and unchanged linked target, partial/final-verification failures with rollback, read-only failure, and concurrent installer exclusion. Production mod directory was not modified by these tests.

The form was rendered offscreen without opening a visible application. Title, destination label/ellipsis, guidance, result area and action layout were inspected; full destination is available by tooltip. This is not a test of interactive Windows folder selection, SmartScreen or every DPI/theme combination.

Final EXE embedded resources and manual ZIP entries are compared with their manifest and build payload. Release SHA256SUMS.txt covers the EXE and ZIP. The four runtime hashes are compared with the installed 0.4.163 files. Remote release downloads are verified after upload, before publishing the GitHub release. Per-run evidence is in docs/evidence/installer-0.4.163.

Previously completed mod readiness: 23 managed suites, seven UI scripts, Release and production UI builds passed. User confirmed UI appearance/Options and earlier feature/Restore/persistence acceptances. No new exhaustive native game run is claimed for installer packaging.

## Rebuild

After building the mod and UI, run Installer/BuildInstaller.ps1. Run Installer/TestInstaller.ps1 for engine checks (the junction fixture may require broader filesystem permission on restricted hosts). Build outputs and fixtures remain under outputs/release-0.4.163 and are excluded from Git. Installer engine/UI/test/build sources are under Installer.
