# Version 0.4.3 — persistent editor launcher

User requested the icon remain available without selecting an asset, for future tools that do not depend on selection. Phase 4 framework change only; no future tools implemented.

VERIFIED-STATIC: EditorContextService.EditorAvailable requires a live world, Editor mode and loading finished. Shared UI availability, open/close and settings use this condition. Existing Available still requires the enabled inspector and continues to guard recovery and shortcuts. Undo/Redo buttons are disabled without that context. Asset label reads No asset selected when appropriate.

C# Release build zero warnings/errors; production UI build/type checks pass. VERIFIED-AUTOMATED: 39 Phase 4 assertions passed. Earlier 40 Phase 2/3 assertions cover unchanged recovery logic. UNPROVEN: persistent launcher and window operation in game.

Test list:
1. Restart CS2 and enter the Asset Editor without selecting an asset. Icon should appear below the existing icons (white lines and cyan sliders).
2. Hover for MW Editor Tools tooltip; open/close/reopen the window. No asset selected should be shown and Undo/Redo disabled.
3. Open Settings and change a switch; restore its original value. Settings should work without an asset.
4. Select a duplicate asset, then deselect it. Icon and open window should remain available, and label/history should update safely.
5. Test a supported list deletion and Undo/Redo with the asset selected; continue PHASE_4_VALIDATION.md.
6. If icon/window still fails, report the step and preserve logs. The earlier missing-launcher cause remains unproven.
