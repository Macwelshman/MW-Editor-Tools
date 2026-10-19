# Release notes - 0.4.171

- Adds Undo for the last single replacement or batch replacement. Use Ctrl+Z or the shared Undo button to restore the entire action, including original prefab references, placement and row settings.
- Keeps replacement Undo available when native regeneration clears the inspector or selects the replacement prefab. Unrelated edits and asset changes still invalidate the action safely.
- Corrects viewport source matching for native placement projection and compiled ground placement. Normalizes native marker identities and briefly retries unsettled highlights without changing the chosen target.
- Extends guarded source matching for foliage, surfaces, areas and directly bound networks. Ambiguous or unsupported targets continue to refuse replacement.
- Updates the user manual, including replacement Undo. Both the installer and ZIP contain the PDF and Markdown manual; the PDF is also attached separately.

Replacement Undo has user-confirmed in-game acceptance on 0.4.171. Runtime logs record completed wall and foliage batch restoration. Existing move/deletion Undo implementations are preserved. Automated checks cover the broader mapping changes; fresh native surface/area/network and replacement persistence acceptance are not claimed.

Close Cities: Skylines II, then run **MWEditorTools-0.4.171-Setup.exe**. The unsigned Windows installer requires .NET Framework 4.8, backs up the existing mod folder and verifies installed files. **MWEditorTools-0.4.171.zip**, the PDF manual and **SHA256SUMS.txt** are also supplied. The repository remains private.
