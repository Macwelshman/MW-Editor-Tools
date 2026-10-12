# Browser category shortcut feasibility — 2026-10-12

Scope: read-only inspection requested by user. No mod implementation, deployment or game-file changes.

VERIFIED-STATIC assembly: Game, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null; MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef; SHA256 AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A. Source: installed Cities2_Data/Managed/Game.dll. Evidence in docs/evidence/browser-shortcuts-*.txt.

VERIFIED-STATIC: EditorPrefabTool::.ctor(World), IL_0008 identifies tool as PrefabTool; IL_002a–002f attaches PrefabToolPanelSystem. EditorToolUISystem::SelectTool(string), IL_0020–0025 resolves the existing tool and sets activeTool. EditorTool::OnEnable, IL_001c sets EditorPanelUISystem.activePanel. This is a concrete native browser-opening route, with normal tool activation side effects: EditorPrefabTool::OnEnable IL_0012 calls ActivatePrefabTool with the last selected prefab.

VERIFIED-STATIC: EditorAssetCategorySystem::GeneratePropCategories IL_0007 contains Props; GenerateSurfaceCategories IL_0007 contains Surfaces. EditorAssetCategory exposes public id/path, and EditorAssetCategorySystem.GetCategories/GetHierarchy are public. PrefabToolPanelSystem stores its actual menu in private m_CategoryMenu; access would require a narrowly checked reflection accessor or locating the actual widget through the panel interface. Category indices must be resolved from current menu items, never hard-coded screen positions or translated labels.

VERIFIED-STATIC: HierarchyMenu<T>::OnSetItemSelected IL_0033 calls public SetItemSelected(int,bool) after mapping a visible row to the underlying item index. SetItemSelected IL_0054–005e invokes onSelectionChange. PrefabToolPanelSystem::OnCreate IL_020f–021a binds that callback to UpdatePrefabs. UpdatePrefabs IL_000f reads the selected category and IL_0046 replaces picker contents. SetItemSelected takes the underlying menu index, not the visible row index.

VERIFIED-STATIC: PrefabToolPanelSystem::OnStartRunning IL_001d rebuilds category items and IL_0023 refreshes prefabs. Selection must occur after this initialization, otherwise activation may reset it.

HYPOTHESIS: configurable shortcuts can select the existing PrefabTool, wait until its category menu is ready, locate a current category by id and invoke its native SetItemSelected method. Reuse existing guarded input recording/focus/typing/dialog checks. Execute on the editor update path and refuse missing/ambiguous categories. Decide explicitly whether existing search text should be cleared; otherwise a correct category can still appear empty.

UNSUPPORTED/UNPROVEN: runtime category switching, collapsed-parent visibility/scrolling, opening from a closed browser, active placement-tool effects, search behaviour, shortcut conflicts and lifecycle timing. No claim that vanilla already provides assignable per-category shortcuts.

Next smallest experiment: an explicitly authorised two-action prototype (Props and Surfaces) tested on a disposable editor asset, browser closed/open, search populated/empty and typing/dialog focus. No asset edits or saves needed to validate navigation itself.
