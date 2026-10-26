# MW Editor Tools

Tools for the **Cities: Skylines II Asset Editor**. Current release: **0.4.251**. The repository and releases remain private.

**[Download the installer or manual ZIP](https://github.com/Macwelshman/MW-Editor-Tools/releases/tag/v0.4.251)** · **[User manual](docs/USER_MANUAL.md)** · **[PDF manual](output/pdf/MW-Editor-Tools-User-Manual-0.4.251.pdf)**

Close Cities: Skylines II before installing. The unsigned .NET Framework 4.8 installer backs up the existing mod folder and verifies installed files. It installs into the current user's Mods folder and does not change game files.

## What's new since 0.4.180

- **Viewport selection:** Start replacing, element-type filters, gold hover/cyan selection, and exact Object Info row reveal with a rounded cyan highlight. Component/list opening, pagination and scrolling follow the selected element.
- **Clear copy/paste targets:** viewport shortcuts follow current hover; panel buttons follow clicked selection. Native inspector edits stop replacing before rebuilding the asset.
- **Component tasks:** separate Component copying, Localisation and Pack priorities views. Copy selected fields, use Group and Priority presets, transfer localised names/descriptions, or preview workspace priority numbering. Destination thumbnails are preserved.
- **Recovery:** guarded bound-network, spawn/access marker and supported native bike-rack improvements, retaining companion/reference checks and the shared last-action Undo.
- **Compatibility and diagnostics:** optional Asset Icon Creator integration, patch conflict isolation, tab error recovery and quieter successful-operation logging.

See [release notes](docs/RELEASE_NOTES_0.4.251.md) for scope and validation. Proposed precision transforms, group selection, duplication and filtered deletion tools are not included.

The shared window contains **Replacer**, **Components**, **Colours** and **Multi-Save**, with **Undo** in its header. Click the coloured diamond icon to open or close it. Drag its header to move it; the position is remembered. Workspace **Save All** starts Multi-Save; its tab shows results and details.

## Features

- **Undo:** one shared last-action slot, normally Ctrl+Z. Supported paths include standalone/bound/nested props and foliage deletion/moves; bound/unbound supported surface/area deletion and node moves; single-node addition/deletion, combining addition with subsequent moves of that same node; direct native effect deletion/moves; and supported directly bound lane/network deletion. Cancelled/no-op moves preserve the previous slot. Asset changes and unrelated writes can invalidate it. There is no Redo or multiple-action history.
- **Replacer:** hover an element in Object Info or the viewport and press Ctrl+C. Ctrl+V replaces the destination; Ctrl+Shift+V replaces all matching elements in its owning asset list. Destination placement and row settings are preserved. Ctrl+Z or the header Undo restores the last single or batch replacement as one action; foliage regeneration must finish first. Exact source-row/ownership matching is required; unsupported or ambiguous targets refuse. This replaces attached source-list elements, not arbitrary standalone placements.
- **Components:** copy selected components or all supported components, including settings, between assets. Paste modes are Add missing, Replace present, and Replace and add. Paste automatically checks compatibility and reports changes and skips. Existing activation is preserved; additions retain copied activation. Recover is available until later destination changes invalidate it. Opaque/native or unsupported component settings are explicitly skipped.
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

Undo does not cover network moves, standalone network/junction recovery, whole-polygon creation/deletion through node tools, multi-polygon/merged edits, whole-building/component recovery or third-party tools. Component Recover is separate from the shared Undo action. Undo history is session-only; persistence acceptance concerns the resulting saved asset, not preserving an Undo slot across reloads.

VERIFIED-RUNTIME: user accepted 0.4.251 selection, highlighting and replacement/Undo. The recorded session includes single and seven-element batch replacement/Undo. This does not establish a fresh dedicated surface material/rebuild/save-reopen pass or exhaustive mod compatibility. See [native acceptance](docs/evidence/release-review-0.4.251/native-acceptance/README.md).

Use disposable asset copies for validation. Review [release readiness](docs/RELEASE_READINESS_0.4.251.md), [release notes](docs/RELEASE_NOTES_0.4.251.md), [component transfer scope](docs/PHASE5_0.4.160_COMPONENT_SETTINGS.md) and [current project status](PROJECT_STATUS.md).

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



To rebuild the PDF, run `python tools/build_manual_pdf.py` with ReportLab and pypdf installed. After building the mod/UI and PDF, run `Installer/BuildInstaller.ps1` to create the self-contained Windows installer and ZIP. Both contain the four runtime files, PDF/Markdown manuals, notices, release notes and installation instructions. Installer engine checks use temporary folders and the actual embedded payload; see [installer validation](docs/INSTALLER_0.4.163_VALIDATION.md).

This is an independent community tool and is not affiliated with or endorsed by Iceflake Studios or Paradox Interactive.

Cities: Skylines II and its associated names and trademarks belong to their respective owners.
