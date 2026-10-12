# 0.4.87 — Options browser category shortcuts

User-authorised scope: Options-only configurable native browser navigation for Buildings, Props, Foliage, Areas and Effects; Surfaces retained from the initial request. No added shared-window controls.

VERIFIED-STATIC: Release build zero warnings/errors; production UI build succeeds. Eight relevant managed suites pass 536 assertions: Options35, Phase4 settings/input330, browser navigation14, lifecycle20, component protection18, snapshots/history71, Multi-Save17, bulldozer lifecycle/history31. These use native stubs where necessary and do not prove live Unity/Cohtml behavior.

Browser group has independent enable switch (off by default), six native shortcut recorders and explicit Apply changes. Defaults Ctrl+Alt+B/P/F/A/E/S. Bindings support letters A–Z with exact modifiers. Invalid/empty recordings retain the previous combination. Draft values do not activate until Apply changes. Existing settings files migrate to dormant browser defaults. Duplicate combinations among enabled MW shortcuts suppress all conflicting MW actions. Native binding widgets also supply normal conflict reporting; collisions with other mods or vanilla input remain runtime acceptance work.

Navigation is deferred: key frame queues only; subsequent editor update selects existing PrefabTool and reopens its native panel if needed; waits for native category initialization before selecting a unique current category ID through HierarchyMenu.SetItemSelected. No hard-coded visible row indices. Category expands after selection. Existing search text is retained and may filter the results. Missing/ambiguous categories refuse navigation; API exceptions stop browser dispatch for the session. Focus loss, typing, stale frontend heartbeat, dialogs/recording, loading/editor exit, Multi-Save or recovery busy states cancel dispatch. Palette overlay is included in the existing frontend typing guard. Recovery dispatch remains on its existing deferred path.

VERIFIED-STATIC installed Game.dll identity: Game Version0.0.0.0 MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef SHA256 AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A. See BROWSER_SHORTCUTS_FEASIBILITY.md and evidence/browser-shortcuts-*.txt. Additional category IDs: GenerateBuildingCategories IL_0007 Buildings; GenerateFoliageCategories IL_0007 Foliage; GenerateAreaCategories IL_0007 Areas; GenerateEffectCategories IL_0009 Effects. HierarchyMenu<T>/<get_items>d__22.MoveNext IL_0022–0027 enumerates underlying m_Items without viewport filtering.

UNSUPPORTED/UNPROVEN: native runtime recording, persistence across a game restart, category selection and visibility/scrolling, focus and conflict dialogs, and native placement-tool effects. Opening this browser follows native Add Object behavior and can reactivate the last selected placement prefab. No asset edits or saves are required for acceptance.

Acceptance:
1. Options / MW Editor Tools / Browser category shortcuts: enable, optionally record new combinations, Apply changes.
2. In the Asset Editor, test all six combinations with browser closed and open; category highlight and results must agree. Collapsed category must expand.
3. Existing search text remains; clear it manually if it filters the selected category.
4. Confirm no navigation while typing in browser search, recording a shortcut, handling dialogs or while outside the editor.
5. Give two MW actions identical combinations; neither should run. Restore distinct combinations. Disable browser shortcuts and verify Undo/Redo still works.
6. Close/reopen Options and restart CS2; committed values persist. No disposable edits need saving.

Existing 0.4.86 area/surface recovery acceptance remains outstanding; this update does not establish additional recovery support.

