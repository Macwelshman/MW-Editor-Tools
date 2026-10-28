# MW Editor Tools 0.4.257 — Environment, Options tabs and beta compatibility

Options → MW Editor Tools now has **Keyboard Shortcuts**, **Tools** and **Environment** tabs. The integrated Environment preset provides time of day/year, precipitation, clouds and snow, preserving the standalone mod's saved values and defaults. Presets apply once on editor load and after leaving photo mode, including while editor simulation is stopped.

**Enable environment** appears at the top of the Environment tab. Switching it off disables the preset controls and retains their values. Apply changes saves drafts from all tabs; environment changes take effect on the next editor load. Disabled sessions use the native environment.

The checkbox defaults to enabled for new and existing settings, preserving the earlier behavior. Settings edits do not change the current session's captured preset. Reset defaults enables the preset again.

Adds explicit compatibility for the audited CS2 **performance-test** beta, Steam build **26269843**, alongside the previously verified stable build. Selected native IL, private fields, ECS and rendering dependencies were checked before enabling confirmation, Undo and targeting. Unknown or mixed dependency builds remain guarded. Existing recovery scope and limits are unchanged.

The Windows installer backs up and verifies existing mod files. If the sibling standalone MW Editor Environment mod is installed, it is backed up and archived outside Mods to prevent competing controllers; saved settings remain intact. ZIP users must move that standalone mod outside Mods themselves. The updated illustrated Markdown/PDF manual describes the new tabs, presets, checkbox and installation workflow.

VERIFIED-RUNTIME: 0.4.257 initialized without MW errors in the recorded beta editor session; the enabled preset applied once, refreshed while paused and logged cleanup on exit. This does not establish every beta Undo route, disabled-checkbox persistence or environmental visual case.

VERIFIED-STATIC: Release/UI builds and managed/UI regressions pass. Focused checks include old settings without the new flag, saved disabled state, draft behavior, disabled load/photo return without environment writes, and re-enabling on the next load. Installer checks cover payload hashes, linked paths, game-running refusal, standalone migration and rollback. The existing NU1900 advisory-feed warning remains. Native checkbox layout/persistence and full beta action acceptance remain pending.

Assets: Windows setup EXE, manual-install ZIP, illustrated PDF manual and SHA256SUMS.txt. Published only through the existing private repository.
