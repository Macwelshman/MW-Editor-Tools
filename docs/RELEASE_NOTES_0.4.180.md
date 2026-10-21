# Release notes - 0.4.180

- Select components directly using native checkboxes beside their Object Info headings. Tooltips explain selection; component activation remains separate. Embedded checkboxes use the native focus-disabled key to avoid registering a Toggle beneath a leaf Button.
- Copy, Copy All, Select All and Deselect All are grouped in Components, with visible selection instructions and no duplicate component list.
- Paste checks the selected mode and current destination automatically. Add missing, Replace present and Replace and add retain ownership, dependency, settings, activation, rollback and Restore safeguards. Compatibility/skipped-item reports remain visible and clear when selecting another asset.
- Replacer and Components both have a separate Clear Clipboard button, disabled when empty. Replacer result information also clears on asset changes.
- The inspector Undo section starts collapsed and its heading uses normal casing. Existing shared-header Undo and Ctrl+Z behavior are retained.
- Includes an updated PDF/Markdown user manual in the Windows installer and ZIP; the PDF is also available separately.

Validation: Release/interface builds and focused UI/managed checks pass, including 55 component transfer checks and embedded focus-key regression coverage. Final native acceptance of the newest focus/layout/report refinements is still pending; this release does not claim those checks replace in-game validation. Prior 0.4.173 launcher/developer-menu and accepted replacement Undo fixes are retained. Whole-component/building Undo remains unsupported.

A reported hierarchy prefab-index exception while placing SR ResidentialLow buildings remains unresolved. The log contains duplicate prefab-ID warnings, and both subscribed/local ImportedData copies are present. A collision is a hypothesis, not a confirmed cause or a fix in this release.

Close Cities: Skylines II, then run **MWEditorTools-0.4.180-Setup.exe**. The unsigned Windows installer needs .NET Framework 4.8, backs up the existing mod folder and verifies installed files. Manual ZIP, standalone PDF and SHA256SUMS.txt are provided. The repository remains private.
