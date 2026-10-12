# Version 0.4.1 — launcher visibility test

Phase 4 only. Candidate correction for the missing MW Tools button reported on 0.4.0.

1. Restart CS2, open the Asset Editor and select a duplicate asset.
2. Find the labelled **MW Tools** button below the existing icons, immediately to the right of Workspace.
3. Click it: the shared window should open. Click it again: the window should close. Reopen it and test its Close button.
4. Check that the existing Multi-Save and Colour Variation launchers still work.
5. If the button appears, continue the full checklist in PHASE_4_VALIDATION.md.
6. If it is still absent, stop window/shortcut testing and report the failure. The new logs distinguish UI bindings, frontend mounting and editor availability. Preserve Player.log and Logs/MWEditorTools.log after closing CS2.

VERIFIED-RUNTIME (user screenshot): 0.4.0 inspector recovery controls were present with a selected asset; MW Tools was absent.

VERIFIED-STATIC: the previous launcher had no explicit dimensions and shared a fixed toolbar row with other mods. Version 0.4.1 supplies explicit dimensions and a separate row. Recovery and shortcut policies are unchanged. UI logging is limited to first mount and availability changes.

HYPOTHESIS: native button sizing or overlap caused the missing launcher. The screenshot does not prove the cause; editor-state publication or module mounting may also be involved. Version 0.4.1 adds diagnostic evidence for these possibilities.

UNPROVEN: in-game visibility and window operation in 0.4.1. Automated builds cannot establish these results.
