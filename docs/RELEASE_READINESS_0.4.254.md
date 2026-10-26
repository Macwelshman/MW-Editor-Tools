# Release readiness - 0.4.254

User accepted Backquote hide/show on 26 October 2026 and authorised updating the installer and existing private GitHub release. Macwelshman/MW-Editor-Tools privacy and main default branch were verified through GitHub. Publication is limited to that repository.

## Accepted runtime

VERIFIED-RUNTIME: the user confirmed the 0.4.254 Backquote hide/show workflow works. Single-key configuration, default migration and native screen implementation are described in HIDE_UI_0.4.252.md through HIDE_UI_0.4.254.md. Earlier accepted editing behavior and limits remain documented in RELEASE_READINESS_0.4.251.md. This does not establish acceptance of every key, persistence case or third-party shortcut combination.

VERIFIED-STATIC: final packaged DLL, UI JavaScript, CSS and Harmony hashes match the accepted installed runtime. Assembly version is 0.4.254.0. No runtime code was changed during release packaging and no game files were modified.

## Validation

Release and production UI builds passed during feature implementation. All 35 managed suites and 18 UI scripts passed for this release; results/logs are in evidence/release-0.4.254. Exact-build native area allocation/inspector and PreCulling scheduling contract checks passed. These managed/static results do not replace game acceptance.

The versioned Windows installer passed 43 safety checks against its actual 17-file payload in disposable workspace folders. Checks include folder-junction refusal, game-running refusal, backups, corrupt payload, failures and rollback. Embedded EXE payload hashes, all 18 ZIP entries, release checksums and installed-runtime equality verify. Tests did not alter the production mod directory.

The existing NU1900 advisory-feed warning remains; no new dependency or fresh vulnerability clearance is claimed.

## Documentation and assets

The 19-page PDF and Markdown manual include the Backquote default, standalone key recording and configuration. All eight original screenshots remain included. The older Options screenshot is explicitly labelled as predating the hide/show UI controls. All pages were rendered and visually reviewed; the final changed caption was re-rendered and reviewed. The PDF contains eight screenshots and the cover icon.

Assets: MWEditorTools-0.4.254-Setup.exe, MWEditorTools-0.4.254.zip, MW-Editor-Tools-User-Manual-0.4.254.pdf and SHA256SUMS.txt. Target release page: https://github.com/Macwelshman/MW-Editor-Tools/releases/tag/v0.4.254.

No repository visibility change or publication to another platform is authorised. The documented 0.4.251 unsupported editing/recovery operations remain unchanged.
