# Area conversion and viewport/list correlation — 0.4.243

Implemented; native acceptance pending. User preference: do not open component lists automatically. Reveal only in an already-open list, leaving search and keyboard focus unchanged.

## Evidence

VERIFIED-RUNTIME: 0.4.242 Park Area -> Walking Area attempts were refused at 12:48:48 and 12:48:54 because their native archetype component sets differed. No replacement writes occurred for those attempts. The log did not enumerate the differing components. Actual compatibility of those particular loaded prefab instances still needs native confirmation.

VERIFIED-STATIC: installed Game.dll 0.0.0.0, MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef:

- NavigationArea.GetArchetypeComponents IL_0001 adds Areas.Navigation; IL_000d adds Net.SubLane.
- HangaroundArea.GetArchetypeComponents IL_0001 adds Areas.HangaroundLocation. Both Navigation and HangaroundLocation have zero instance fields.
- Net.SubLane contains both m_SubLane (Entity) and m_PathMethods (PathMethod). Preserve the complete struct, not only its entity reference.
- ListAdapterBase.ResolveIndex IL_0001 reads m_FilteredIndices; IL_0023 maps filtered position to source index. PagedList publishes childStartIndex; native BoundPagedList uses childStartIndex + child offset as EditorListItem.focusKey. The native UI export is game-ui/editor/widgets/list/list.tsx.
- Native BoundPagedList composes group/path with parent.path plus its own path. EditorSectionBase.sectionId uses the same dot-joined identity. Native page dispatch is setCurrentPageIndex; it is a UI operation, not a source edit.

Inspection files and marker/filter contract evidence are in evidence/area-row-highlight-0.4.243. The 0.4.242 material allocation release and inspector-edit guard remain in place.

## Area conversion

Space-to-Space replacement accepts differences limited to Navigation, HangaroundLocation and the SubLane buffer. Known marker tags are added/removed in place. Existing nonempty lane buffers, complete path-method values, ownership and references are retained even when the destination no longer requires navigation. A missing required buffer is added empty. Undo restores original marker state and original buffer presence; it refuses to discard newly created external lane references. Source-marker and required-buffer consistency, layout/geometry/render compatibility, source ownership, unique boundary mapping, all-owner coverage and budgets remain required. Unknown component differences remain refused.

All retained lane structs are captured by indexed buffer copies. Undo readiness compares their full payload as well as existing node/cache/owner witnesses. The native collection regression gate prevents boxing/enumerating DynamicBuffer through LINQ. Source snapshots, row settings and the existing surface render release path are retained.

## Highlight/reveal

The selection observer uses the clicked native target, exact ownership/source-row mapping, and the matching native list accessor. It does not depend on clipboard contents and does not claim a target by name. Lists are correlated by exact native paths. A native filtered index is accepted only if unique and consistent; mismatched, filtered-out and over-budget indices are not marked.

The owning component and matching element row have a cyan marker; the row has a small Selected label. Component-copy checkboxes remain independent. An already-expanded list may change to the selected row's page and scroll that row into view. Closed lists/components stay closed, existing searches stay intact, and there are no keyboard-focus calls or new focus controllers. Manual page changes are not repeatedly overridden for the same selection. Selection/context/filter invalidation clears the correlation. Nested/unmapped/ambiguous selections are not guessed.

## Checks and native acceptance

Managed checks cover known area-layout changes and reverse Undo, retained nonempty lane/path payload, new empty-buffer restoration, refusal after external lane creation, filtered/paged index mapping, duplicate names and paths, closed-list/search preservation, scroll without focus, independent copy checkboxes, launcher lifecycle and existing replacement/Undo/outline regressions. Release/UI builds and installed-assembly contracts are checked before staging. NU1900 remains the unavailable advisory-feed warning, not a completed security audit.

Grouped native test: replace Walking Area with Park Area and reverse, single/batch/Undo, comparing boundaries, row settings, companions and references. With the component list already open, click distinct viewport elements (including duplicate prefab names) and confirm the correct row/page is revealed in cyan. Close the list and select again: it must stay closed. Keep a search active: it must stay unchanged. Include vanilla area edits while Replace is active, surface material replacement/Undo, accepted deletion/move Undo and gold/cyan outlines. No new native pass is claimed.

Validation passed: 88 area/layout/list-index checks, 92 targeting, 22 outline, 45 replacement Undo, 1204 Phase 3, 34 viewport retry, 76 projection, 10 network adapter, 28 lifecycle checks and UI row/headings/launcher/focus tests. Release/UI builds and native contracts passed. Installed with Cities2 absent; 9 rollback files and all 3 copied hashes verified. See outputs/installation-0.4.243.txt and evidence/area-row-highlight-0.4.243/installed-hashes.csv. Native acceptance remains pending.
