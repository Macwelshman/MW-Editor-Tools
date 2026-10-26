# Release notes - 0.4.251

This release packages the accepted 0.4.251 build and development changes since 0.4.180.

Includes an updated 19-page illustrated PDF manual and Markdown manual with eight user-supplied in-game screenshots. Screenshot files are included in both installer and ZIP for offline Markdown viewing.

## Replacer and selection

- Start/Stop replacing, element-type filters, gold hover and cyan selection outlines, plus polygon/curve outlines for supported targets.
- A clicked viewport element reveals its exact owning Object Info row: component/list opening, correct page, native scrolling and a rounded cyan highlight. Matching uses ownership/source data rather than names; ambiguous rows remain unmarked.
- Ctrl+C/Ctrl+V/Ctrl+Shift+V follow current viewport hover. Panel buttons use the clicked selection. Blocked or stale hover is refused.
- Native inspector edits stop replacing and clear owned highlights before the asset rebuild. Surface hit containment and network presentation matching have focused regression coverage.
- Compatible surface/area reference replacement retains destination boundaries, elevations, settings and live references. Layout-changing conversions and nested owners remain refused. Native old-material allocations are released before retained surface material changes. Dedicated fresh visual/rebuild/persistence acceptance is not established by the latest general replacement test.

## Components, localisation and packs

- Separate Component copying, Localisation and Pack priorities tasks.
- Optional per-field copying, including a UI Object Group and Priority preset. UI Object Icon is excluded to preserve destination thumbnails.
- Independent name/description clipboards and filling missing language entries, with separate localisation restoration.
- Workspace asset scanning, optional naming-pattern order, explicit priority preview/apply/restore, search and pagination. Select matching works across pages; Apply uses the full prepared preview.
- Component Paste performs compatibility checks and the transfer immediately. Recover returns the last eligible destination to its previous state.

## Recovery and compatibility

- Guarded bound-network and companion reconstruction improvements, including accepted fence deletion recovery, supported spawn/access marker paths and native BikeRack01 move handling.
- Existing one-action Ctrl+Z model, cancellation/no-op preservation and strict ownership/geometry/reference checks retained. No Redo or multi-action history.
- Optional verified Asset Icon Creator callback integration; module/patch conflict isolation and late ownership-change detection. Unsupported integrations do not establish general third-party Undo support.
- Tab error boundaries, shared selection-state parsing and removal/opt-in gating of verbose successful-operation diagnostics.

## Validation and limits

VERIFIED-RUNTIME: user accepted 0.4.251 selection, highlighting and replacement/Undo. The recorded session contains a seven-element batch replacement and Undo plus a single replacement and Undo, reporting original prefabs, placement and settings restored. Earlier fence and bike-rack acceptance is documented separately. This does not certify every element type, persistence workflow or mod combination.

VERIFIED-STATIC: Release/UI builds and managed/interface regression checks pass; distribution checks are detailed in release readiness. NuGet advisory retrieval emitted NU1900; no fresh vulnerability clearance is claimed.

Unsupported: network moves, standalone network/junction recovery, whole-building/component Undo, whole-polygon creation/deletion through node tools, merged edits, arbitrary standalone replacement, universal component copying and guaranteed Move It/Better Bulldozer custom-operation recovery. The proposed transform/group/duplicate tools are not implemented here.

Close Cities: Skylines II and run **MWEditorTools-0.4.251-Setup.exe**, or use the manual ZIP. The unsigned .NET Framework 4.8 installer preserves a verified backup and verifies installed files. PDF manual and SHA256SUMS.txt are supplied separately. The existing GitHub repository remains private.
