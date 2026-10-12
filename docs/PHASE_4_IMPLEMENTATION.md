# Phase 4 — shared editor framework

Status: IMPLEMENTED; automated checks pass; in-game acceptance pending.

## Scope

Independent native draggable MW Editor Tools window, asset context, six navigation sections, persistent settings, recent recovery history and Undo/Redo controls accessible on every tab. The Prop Replacer, Components, Colour Variations and Multi-Save tabs explicitly identify their later-phase status; original projects remain separate.

Component confirmation and supported list recovery can be enabled separately. Shortcuts and inspector controls have separate switches. History depth is configurable from 20 to 50; key letters are configurable with Ctrl for Undo and Ctrl+Shift for Redo. Defaults are Ctrl+Z and Ctrl+Shift+Z, as explicitly confirmed by the user.

The window stays open across asset changes; recovery remains scoped to the asset and clears on context changes. History remains session-only. Settings and normalized window coordinates are saved in MWEditorTools-settings.json in the game's persistent data directory. Settings changes that alter recovery/depth clear existing history.

## Architecture and evidence

VERIFIED-STATIC: C# net48 Release build against the installed game references succeeds without warnings/errors; TypeScript checks and production webpack build pass. The UI package contains only the module and CSS; no React/game libraries are bundled. The original UI projects are read solely for native panel/API patterns, not modified.

Shared injected services: EditorContextService supplies existing native inspector, current root prefab, editor/load availability and native dialog status; EditorToolsSettings owns persistence and validation; TransactionHistory provides typed bounded transactions; EditorToolsUISystem binds commands/settings/status to the native Editor UI. ListRecovery continues to own its operation-specific capture, validation and restore. This keeps the shared layer from inventing general entity reconstruction.

Input uses the installed Unity.InputSystem Keyboard API. Exact Ctrl/Shift combinations, editor availability, application focus and native dialogs gate shortcut dispatch. The always-mounted Editor frontend reports focus every 100ms and on focus events, covering inputs, text areas, select controls, editable elements and textbox/combobox roles. A missing/stale heartbeat (>750ms) suppresses shortcuts. Only a fresh key press dispatches; the original recovery checks revalidate before writing.

VERIFIED-AUTOMATED: 18 existing confirmation assertions, 22 snapshot/history assertions, 39 Phase 4 shortcut/settings/history assertions. These test managed logic; they do not run Unity's input, Cohtml focus, native dialogs or deferred ECS.

VERIFIED-RUNTIME (user-reported, earlier phase): Phase 2 confirmation and Phase 3 Object/Area/Net/Effect recovery, multiple steps and save/reopen passed on the installed Phase 3 build. Phase 4 requires a regression check.

UNPROVEN: live window styling/dragging, UI discovery of the new module, focus reports, actual shortcut timing/conflicts, persistence across a real game restart and compatibility with unrelated mods. No Phase 4 runtime pass claimed.

UNSUPPORTED: whole-component recovery, bulldozer undo, cross-asset history, arbitrary editor-edit undo and future module integration. Keyboard Undo/Redo only acts on supported deletion history.

Game.dll evidence remains MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef; SHA256 AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A. Previous phase evidence records exact native methods. Installed game assemblies remain untouched.

## Build

dotnet build MW.EditorTools.csproj -c Release
dotnet run --project tests/Phase4Safety -c Release

In UI: npm ci --ignore-scripts --no-audit --no-fund; npm run build.
Pinned package-lock includes a Node type version compatible with TypeScript 4.8.4. No automatic build deployment.
