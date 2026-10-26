# Release readiness - 0.4.251

Release preparation requested 25 October 2026 for the existing private Macwelshman/MW-Editor-Tools repository. Repository privacy and main branch verified through GitHub. No visibility change or other-platform publishing is authorised.

## Accepted runtime

VERIFIED-RUNTIME: user accepted 0.4.251 selection, row highlighting and single/batch replacement Undo. The preserved session records seven-element batch replacement/Undo and single replacement/Undo, with original prefabs, placement and settings restored. See [native evidence](evidence/release-review-0.4.251/native-acceptance/README.md). Prior fence and bike-rack results remain scoped to their documented cases.

VERIFIED-STATIC: all four rebuilt runtime files exactly match the installed accepted build by SHA256. Assembly version is 0.4.251.0. No runtime source changes were made for packaging; no game files or production mod installation were changed. Hash evidence: evidence/release-0.4.251/runtime-hashes.json.

## Checks

VERIFIED-STATIC: Release and webpack builds, all 35 managed suites and all 18 UI scripts passed. Exact-game area allocation/inspector and PreCulling scheduling contracts passed. Full matrix results are recorded in evidence/release-0.4.251/managed-results.json and ui-results.json.

The installer was versioned to 0.4.251. Its final 43 safety checks pass against all 17 embedded payload files in disposable project folders, including eight screenshot files, folder-link refusal, game-running refusal, verified backup, corrupt payload, write failures and rollback. The fixed payload allowlist was extended only with the eight manual image names. All 18 ZIP entries (payload plus checksum file), embedded resources, release checksums and installed-runtime equality verify.

The Release build emitted the existing NU1900 warning because dependency advisory retrieval was unavailable. This is not a new dependency or a fresh vulnerability clearance. Managed/native doubles do not replace game acceptance.

## Manual and publication status

README, Markdown manual, release notes and installer version references have been updated. The manual corrects current task views, field choices, Recover, priority ordering/pagination, localisation additions, hover versus clicked selection, row reveal and third-party Undo limits. The PDF builder now embeds local screenshot references and captions.

VERIFIED-STATIC, 26 October 2026: all eight user-supplied in-game screenshots are included unchanged in the repository and offline installer/ZIP manuals. The 19-page PDF embeds every screenshot with an accurate caption. All rendered pages were visually reviewed; forced section breaks were removed to eliminate near-empty continuation pages. Screenshot/text references and PDF image/page counts were checked. The priority screenshot shows a successful scan before selection/preview, and the field screenshot includes Is Debug Object; captions describe those actual states rather than implying completed operations.

## Remaining limits

UNSUPPORTED/UNPROVEN: network moves; standalone network/junction recovery; whole-building/component Undo; whole-polygon creation/deletion through node tools; merged edits; arbitrary standalone replacement; universal component copying; and guaranteed third-party-tool recovery. Precision/group transforms, duplication and filtered deletion remain proposals.

Surface/area replacement requires compatible layouts and exact unique source/owner matches. The latest general replacement test does not establish a fresh dedicated surface visual/material/rebuild/save-reopen pass or a fresh localisation/priority persistence pass. Arbitrary mod combinations and native performance remain unproven. These gaps are documented rather than advertised as accepted.

Release assets: MWEditorTools-0.4.251-Setup.exe, MWEditorTools-0.4.251.zip, MW-Editor-Tools-User-Manual-0.4.251.pdf and SHA256SUMS.txt. Release page: https://github.com/Macwelshman/MW-Editor-Tools/releases/tag/v0.4.251.
