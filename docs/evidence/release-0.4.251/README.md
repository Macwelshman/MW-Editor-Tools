# 0.4.251 distribution preparation

VERIFIED-STATIC: Release/UI builds, all 35 managed projects and all 18 UI scripts pass. See managed-results.json and ui-results.json for exit codes and per-suite logs. Exact Game.dll allocation/inspector and PreCulling scheduling contract checks pass. Existing NU1900 advisory-feed warning remains; some harnesses report existing test-only unused-field warnings.

runtime-hashes.json records byte-for-byte equality between all four rebuilt runtime files and the accepted installed 0.4.251 build. Production installation was not changed.

Final installer: 43 actual-embedded-payload safety checks pass against 17 files, including all eight screenshots, in disposable project fixtures. EXE resources, all 18 ZIP entries, checksum manifest and installed-runtime equality verify. Folder-link fixture creation needed execution outside restricted filesystem handling; the fixture stayed inside outputs/release-0.4.251, with no production write.

26 October 2026: all eight supplied screenshots copied unchanged to docs/manual-*.png and integrated into the screenshot guide. All 19 final PDF pages visually reviewed. The final package was rebuilt after PDF layout corrections and reverified. PDF/image checks and screenshot hashes are recorded in manual-validation.json.

Native acceptance is preserved separately in ../release-review-0.4.251/native-acceptance/README.md. Automated checks are not additional native acceptance.
