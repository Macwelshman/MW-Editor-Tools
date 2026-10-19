# Release notes — 0.4.163 candidate

This candidate brings together the accepted Undo and Phase 5 workflows with the compact shared UI. It is prepared for private review, not a public release.

- One-action Undo for supported prop/foliage/nested-group, effect, surface/area and bound lane/network operations, including supported built-in moves and area/surface node edits. Same-added-node movements combine with the addition.
- Browser-list and viewport element copy, replacement and batch replacement, including tested nested props/effects and retained-source foliage protection.
- Cross-asset component settings transfer, selected/all copy, three paste modes, dependency-aware planning and guarded Restore.
- Integrated Colours and Multi-Save in the shared window.
- Four compact tabs with introductory lines, header Undo, on-demand guidance and consistent Save button styling.
- Concise Options, with browser defaults Alt+B/D/F/A/E/S for Buildings/Props/Foliage/Areas/Effects/Surfaces. Former defaults migrate; other custom bindings remain.

User-reported native acceptance covers the implemented Undo stage and persistence, element replacement/batch and persistence, and all three component modes with settings/Restore/persistence. The user also confirmed that UI appearance and Options were already checked. A fresh combined native run and individual Alt key dispatch were not separately reported in this readiness pass. No new native tests were performed by automation during release readiness.

Limits: no Redo/multi-history, network moves, standalone network/junction recovery, whole-building/component Undo, whole-polygon node-tool recovery or third-party move-tool guarantee. Unsupported component settings and ambiguous source/ownership matches refuse or skip with reasons. A previously intermittent viewport source-row/type mismatch remains unexplained; it did not recur in the accepted effect session.

Install only while Cities2 is fully closed. Preserve the current MWEditorTools mod folder, then copy the four runtime files into that existing folder: MW.EditorTools.dll, MW.EditorTools.mjs, MW.EditorTools.css, 0Harmony.dll. Do not copy anything into the game installation. Keep THIRD_PARTY_NOTICES.md with redistributed packages. Use the package SHA256 manifest to verify files. Restore the previous folder with the game closed if rollback is needed.

Release and UI builds pass. A NU1900 warning indicates online package vulnerability metadata was unavailable; this pass is not a dependency vulnerability audit. See RELEASE_READINESS_0.4.163.md for the regression evidence and outstanding native gate.

