# MW Editor Tools

Tools for the Cities: Skylines II Asset Editor. Current development version: **0.4.88**.

The mod combines a shared editor window, supported deletion Undo/Redo, component-removal and list-clear confirmation, Colour Variations, and Multi-Save. Browser category shortcuts are configured exclusively in **Options → MW Editor Tools → Browser category shortcuts** for Buildings, Props, Foliage, Areas, Effects and Surfaces.

Browser shortcuts are disabled by default. Enable them, record combinations and click **Apply changes**. Defaults use Ctrl+Alt+B/P/F/A/E/S. Shortcuts pause while typing, recording or using native dialogs. Browser search text is retained.

## Development status

This is an experimental development project. Recovery supports bounded, validated operations; unsupported operations must not be treated as recoverable. Recovery history is session-only. Use disposable duplicate assets for testing and follow the relevant validation checklist.

Version 0.4.88 corrects browser category lookup to use full root paths. Its build and managed regression checks pass; native Props shortcut acceptance remains pending. The earlier area/surface bulldozer recovery acceptance also remains outstanding.

See [project status](PROJECT_STATUS.md), [working rules](AGENTS.md), [test plan](docs/TEST_PLAN.md), and [browser shortcut validation](docs/BROWSER_SHORTCUTS_0.4.88_VALIDATION.md).

## Build

Requirements: .NET SDK with .NET Framework 4.8 targeting support, Node.js/npm, and an installed copy of Cities: Skylines II. Game assemblies are referenced locally and are not included in this repository.

```powershell
 dotnet build MW.EditorTools.csproj -c Release
 cd UI
 npm ci --ignore-scripts --no-audit --no-fund
 npm run build
```

The default game path is `C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II`. Override it with `-p:GamePath="your installation path"` when building the mod. Some inspection-based tests also reference the default installation path.

Run relevant managed regression checks, for example:

```powershell
 dotnet run --project tests/OptionsSafety
 dotnet run --project tests/Phase4Safety
 dotnet run --project tests/BrowserShortcutSafety
```

Managed tests do not establish Unity/native UI or ECS runtime correctness. Building does not deploy the mod. Generated packages, rollback copies, runtime logs, dependencies and build outputs remain local.
