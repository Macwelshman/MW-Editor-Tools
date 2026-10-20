# Release notes - 0.4.173

- Fixes a launcher layout update loop that could crash the shared game UI when activating the developer menu with TAB. Launcher measurements are now deferred and coalesced, and repositioning cannot synchronously trigger itself. The user confirmed the game UI returns when closing the developer menu.
- Restores the native prefab/inspector refresh after confirmed component removal, including Effects components copied to imported assets. Retained components are checked, and unrelated effects are not manually deleted. Native fire-marker cleanup still needs separate confirmation.
- Preserves the accepted single/batch replacement Undo and existing move/deletion recovery behaviour.
- Updates the user manual. PDF and Markdown copies are included inside both the installer and ZIP; the PDF is also attached separately.

Close Cities: Skylines II, then run **MWEditorTools-0.4.173-Setup.exe**. The unsigned Windows installer requires .NET Framework 4.8, backs up the existing mod folder and verifies installed files. A manual ZIP, standalone PDF and SHA256SUMS.txt are supplied. The repository remains private.
