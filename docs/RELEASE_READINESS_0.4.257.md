# Release readiness — 0.4.257

On 28 October 2026 the user explicitly authorized updating the installer/manual and publishing 0.4.257 through the existing private Macwelshman/MW-Editor-Tools repository. GitHub CLI verified `isPrivate: true`, default branch `main`, and the existing release history. The connector returned 404 for this private repository; the authenticated local CLI is used for release operations. No visibility change or other platform publication is authorized.

## Packaged runtime and scope

VERIFIED-STATIC: the DLL, UI JavaScript/CSS and Harmony in the release package match the installed 0.4.257 runtime by SHA256. Packaging did not change runtime source or reinstall the mod. The source includes audited stable/performance-test assembly profiles, integrated Editor Environment, the three native Options tabs and the default-on Enable environment checkbox. Existing single-action Undo and unsupported-operation limits are retained.

VERIFIED-RUNTIME: recorded 0.4.257 beta session logs show successful module/Options initialization, enabled environment application, paused climate refresh and cleanup on exit. No MW errors/unavailable-hook messages were logged. This does not certify every native beta Undo action, checkbox persistence, photo-mode/environment appearance or third-party combination. Those limits remain in the release notes and feature documents.

## Validation

All 37 managed suites and 18 UI regression scripts pass. Native API checks pass for the audited beta editor/ECS/rendering contracts, Environment APIs/paired snow buffers, area allocator/inspector signatures and PreCulling registration. Release/UI builds from implementation are retained in the feature evidence; packaging reuses those exact installed binaries. NU1900 remains the existing unavailable NuGet advisory-feed warning; there is no fresh advisory clearance or new dependency.

The versioned Windows installer passes 56 checks against its actual 17-file embedded payload, including embedded hashes, linked target/standalone paths, fresh/update installs, game-running refusal, backups, corruption/write failures, standalone Environment archival, preserved saved settings, and rollback of both mods. Tests use disposable workspace fixtures and do not modify the production installation. The source has exactly one mod entry point and no standalone Environment DLL dependency.

The installer recognizes only the named sibling standalone Environment folder containing MWEditorEnvironment.dll, verifies its backup and moves it outside Mods after the new payload is verified. The completion UI reports both archive paths. Original source projects and saved settings remain intact. ZIP instructions describe the equivalent manual step.

EXE resource hashes, all 18 ZIP entries, the external PDF and release SHA256SUMS manifest are checked. Release runtime hashes are compared with the installed mod. Publication is staged as a draft until all uploaded assets are downloaded again and their hashes match locally.

## Manual and assets

The 20-page PDF and Markdown manual describe Keyboard Shortcuts/Tools/Environment, the enable checkbox, retained preset settings, next-load behavior, beta scope and standalone migration/rollback. All 20 final pages were rendered with Poppler and visually reviewed. All eight original screenshots and the cover icon remain included; the historical Options screenshot is clearly labelled as predating the new layout. Text/image counts and required topics were checked.

Release assets: MWEditorTools-0.4.257-Setup.exe, MWEditorTools-0.4.257.zip, MW-Editor-Tools-User-Manual-0.4.257.pdf and SHA256SUMS.txt. Test/build/package evidence is in [evidence/release-0.4.257](evidence/release-0.4.257). Target release: https://github.com/Macwelshman/MW-Editor-Tools/releases/tag/v0.4.257.
