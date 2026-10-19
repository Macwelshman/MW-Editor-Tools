# 0.4.92 inspector lane and bulk Clear recovery

IMPLEMENTED: inspector row deletion and bulk Clear for ObjectSubObjects.m_SubObjects, ObjectSubAreas.m_SubAreas, ObjectSubNets.m_SubNets, ObjectSubLanes.m_SubLanes and EffectSource.m_Effects. Clear captures the full underlying collection, independent of search/pagination, as one history operation. Empty Clear creates no entry. Existing shape, active-state, reference, context, budget, save/dialog and rollback guards apply. Lane fields are now also preservation targets for other supported inspector operations. Ambiguous shared storage is refused before mutation.

VERIFIED-STATIC: Game.dll SHA256 AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A is unchanged; Game 0.0.0.0 MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. Recorded evidence reused: docs/evidence/clear-list-inspection.txt, PagedList.Clear IL_0006 delegates to IListAdapter.Clear; ArrayAdapter.ClearInternal IL_0007 creates a typed zero-length array and IL_0014 assigns it; ListAdapter.ClearInternal IL_0010 clears the underlying list. docs/evidence/clear-widget-inspection.txt, InspectorPanelSystem.OnValueChanged IL_0050 calls UpdateParent. Lane descriptor snapshot evidence and tests predate this change; this build connects that schema to inspector capture.

Clear recovery prefix runs at Harmony Priority.Last after the confirmation owner, checks __runOriginal, and captures only an allowed native Clear. Existing confirmation suppression protects pending/cancelled clicks. Accepted confirmation invokes native Clear and its native change notification; history commits only if the exact after-image and every retained field/component match. Clear failures invalidate history. Unsupported lists retain their existing confirmation and vanilla behavior.

VERIFIED-STATIC: Release build zero warnings/errors. Nine managed suites pass 781 assertions: Phase2 18, Phase3 95, Phase4 330, Bulldozer 31, last-component history 12, nested runner 132, Clear confirmation 26, lifecycle 20, Multi-Save 17. Phase3 adds 24 schema/deletion/isolation checks over all five element types. Production UI build, TypeScript and all seven UI test scripts pass. These checks do not prove native Harmony ordering, Unity object lifetime, deferred ECS regeneration or save/reopen.

UNPROVEN: runtime acceptance of lane row and bulk Clear recovery, and the outstanding 0.4.86 area/surface bulldozer route. No in-game deletion was performed. User explicitly requests no computer use and will run native checks.

## User test batch

Use saved disposable fixtures; never overwrite their baselines. Enable recovery in Options.

1. Inspector lane list: delete one fence/lane row; Undo, Redo, Undo. Check prefab, curve, node indices, parent meshes and preview.
2. For each of Objects, Areas, Nets, Lanes and Effects: Clear -> Cancel must preserve data and prior history. Clear -> Confirm should record one labelled Clear operation. Undo -> Redo -> Undo must restore every entry, including hidden/search-filtered rows. Check geometry, references, settings and preview.
3. Mix supported row and Clear operations; traverse history both ways. Delete after Undo and confirm the redo branch clears. Switch assets and confirm old history cannot act on the new asset.
4. Save the restored result under a new disposable name and reopen it. Check all content/settings/relationships and empty session history. Retest existing prop bulldozer Undo and shortcuts.
5. Run the outstanding area-only surface/walking-area bulldozer deletion -> Undo -> Redo -> Undo using Create area test copy as described in docs/AREAS_0.4.86_VALIDATION.md.

## Remaining all-types work

Whole-component recovery, bulldozer networks/nodes/edges, lane and effect targets, standalone areas, and mixed area companion graphs remain UNSUPPORTED/UNPROVEN. Existing prop mixed fixtures preserve some ancillary descriptors; that does not establish recovery when those ancillary entities are the deletion target. Do not widen target support by removing ownership/schema guards without graph reconstruction and deferred-completion evidence. The next implementation batch must address those native graph paths after the pending area test identifies any regeneration faults.
