# MW Editor Tools 0.4.256 — Options tabs and Editor Environment

Options → MW Editor Tools now has **Keyboard Shortcuts**, **Tools** and **Environment** tabs. The Environment tab integrates MW Editor Environment's time of day/year, precipitation, cloud coverage and snow settings. Saved values and defaults are retained. Apply changes saves drafts from all tabs; environment presets apply on the next editor load.

The existing editor-only preset, photo-mode return, paused-editor climate refresh and snow cleanup are retained. Disposal stops retained callbacks; a missed-preload fallback avoids restoring editor values into a city. Local installation backs up the standalone installation and moves it out of active Mods to prevent duplicate controllers/menu entries. Original source and saved settings remain intact.

VERIFIED-STATIC: Release/UI builds, all 37 managed suites, 18 UI scripts and native API contract checks pass. Native layout, persistence and environmental appearance still need grouped acceptance. Stable/performance-test compatibility and existing Undo scope are preserved. This local build is not a published release.

See [integration details](ENVIRONMENT_INTEGRATION_0.4.256.md).
