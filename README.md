# MW Editor Tools

Tools for the Cities: Skylines II Asset Editor. Current release: **0.4.164**. The GitHub repository and release are private.

The shared window contains **Replacer**, **Components**, **Colours** and **Multi-Save**, with a compact **Undo** button in its header. Most guidance is available through tooltips or Details. The coloured diamond icon stays visible and toggles the window open or closed; it starts beside the sidebar and moves right around occupied icon positions.

**[Read the user manual](docs/USER_MANUAL.md)** for installation, step-by-step workflows, shortcuts and troubleshooting. A [PDF reference copy](output/pdf/MW-Editor-Tools-User-Manual-0.4.164.pdf) is also available.

**[Download the Windows installer or manual ZIP](https://github.com/Macwelshman/MW-Editor-Tools/releases/tag/v0.4.164)**. Close Cities: Skylines II before installation. The installer backs up the existing mod folder and verifies installed files.

## Features

- **Undo:** one shared last-action slot, normally Ctrl+Z. Supported paths include standalone/bound/nested props and foliage deletion/moves; bound/unbound supported surface/area deletion and node moves; single-node addition/deletion, combining addition with subsequent moves of that same node; direct native effect deletion/moves; and supported directly bound lane/network deletion. Cancelled/no-op moves preserve the previous slot. Asset changes and unrelated writes can invalidate it. There is no Redo or multiple-action history.
- **Replacer:** hover an element in Object Info or the viewport and press Ctrl+C. Ctrl+V replaces the destination; Ctrl+Shift+V replaces all matching elements in its owning asset list. Destination placement and row settings are preserved. Exact source-row/ownership matching is required; unsupported or ambiguous targets refuse. This replaces attached source-list elements, not arbitrary standalone placements.
- **Components:** copy selected components or all supported components, including settings, between assets. Paste modes are Add missing, Replace present, and Replace and add. Preview reports changes and skips. Existing activation is preserved; additions retain copied activation. Restore is available until later destination changes invalidate it. Opaque/native or unsupported component settings are explicitly skipped.
- **Colours:** edit channel palettes, generate colour combinations, replace/append variation groups, copy/paste colour settings and undo the last colour edit. Uncommitted drafts and selection changes are guarded.
- **Multi-Save:** save unlocked workspace assets, including hidden entries, with progress, Stop and detailed results. Repeated instances save once; locked assets are excluded.
- **Deletion confirmation:** confirmation before component removal or clearing lists, configured in Options.

## Shortcuts and Options

Browser shortcuts are disabled by default. Enable them in **Options → MW Editor Tools → Browser shortcuts**, edit bindings if desired, then Apply changes.

| Shortcut | Browser category |
|---|---|
| Alt+B | Buildings |
| Alt+D | Props |
| Alt+F | Foliage |
| Alt+A | Areas |
| Alt+E | Effects |
| Alt+S | Surfaces |

Former default Ctrl+Alt combinations migrate to these defaults. Other customised bindings are retained. Shortcuts pause while typing, recording, using dialogs, saving or recovering. Browser search text is retained. Undo's default is Ctrl+Z; Replacer uses Ctrl+C/Ctrl+V/Ctrl+Shift+V.

## Supported scope and limits

Undo does not cover network moves, standalone network/junction recovery, whole-polygon creation/deletion through node tools, multi-polygon/merged edits, whole-building/component recovery or third-party tools. Component Restore is separate from the shared Undo action. Undo history is session-only; persistence acceptance concerns the resulting saved asset, not preserving an Undo slot across reloads.

The requested Undo and Phase 5 workflows have user-reported native acceptance, including persistence. The newer compact UI/Options have automated checks and user-reported appearance/Options acceptance on 0.4.163. A fresh combined native run and individual Alt key dispatch were not separately reported in this readiness pass. An earlier intermittent viewport type/source-row mismatch remains unexplained and was not reproduced in the accepted effect test; diagnostic refusal remains in place.

Use disposable asset copies for validation. Review [release readiness](docs/RELEASE_READINESS_0.4.164.md), [release notes](docs/RELEASE_NOTES_0.4.164.md), [component transfer scope](docs/PHASE5_0.4.160_COMPONENT_SETTINGS.md) and [current project status](PROJECT_STATUS.md).

## Build and checks

Requirements: .NET SDK with .NET Framework 4.8 targeting support, Node.js/npm, and an installed copy of Cities: Skylines II. Game assemblies are referenced locally and are never included in the repository/package.

```powershell
dotnet build MW.EditorTools.csproj -c Release
cd UI
npm ci --ignore-scripts --no-audit --no-fund
npm run build
```

The default game path is `C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II`; override it with `-p:GamePath="your installation path"`. Some managed harnesses reference that path directly. Run checks with `dotnet run --project tests/<project> -c Release` and `node tests/ui-*.cjs` individually. Managed/native stubs do not establish Unity/ECS runtime correctness.

Builds do not deploy the mod. Generated outputs, local dependencies, runtime logs and rollback copies stay excluded from Git. Packaging includes Harmony and its licence; see [third-party notices](THIRD_PARTY_NOTICES.md). Original MW source projects remain separate and unmodified by this integration.



To rebuild the self-contained Windows installer after building the mod/UI, run Installer/BuildInstaller.ps1. It embeds the four runtime files, manual, notices, release notes and installation instructions. Installer engine checks use temporary folders and the actual embedded payload; see [installer validation](docs/INSTALLER_0.4.163_VALIDATION.md).

This is an independent community tool and is not affiliated with or endorsed by Iceflake Studios or Paradox Interactive.

Cities: Skylines II and its associated names and trademarks belong to their respective owners.

