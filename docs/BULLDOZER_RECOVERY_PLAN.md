VERIFIED-RUNTIME (user-confirmed)0.4.30 last-prop persistence PASS: saved restored disposable asset,fullCS2restart/reopen,ObjectSubObjects and exactlyoneGrill01 correct. Together logged UndoRedoUndo freshcomponent/payload/graph pass this accepts only explicitLastarm/singleentry/exactObjectSubObjects removal. Automatically next safety gate: asset-context change must clear last-prop recovery, returning to original must not revive staleUndo. Same installed0.4.30; no new build or installation. Do notsave diagnostic deletion;keepCS2open afterfinished tocopylog,thenfullrestartnosave. Nested/linked/othercomponents/mixedhistory remain unsupported; Phase5paused/6publishingunauthorised.

# Bulldozer recovery — active priority

User steering: all 0.4.9 tests pass; prioritise bulldozer Undo before Prop Replacer. Prop Replacer work paused. No runtime code or installed package changed in this priority update.

VERIFIED-STATIC: recorded Game.dll SHA256 AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A remains unchanged; MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. Reuse existing evidence.
- BulldozeToolSystem.Apply calls GetAllowApply at IL_0001 and sets applyMode at IL_00a2; it does not perform a synchronous prefab-list deletion (evidence/inspection.txt).
- ApplyAreasSystem.HandleTempEntitiesJob.Delete adds Deleted to original and temporary entities at IL_0028 / IL_003e; deletion is deferred (evidence/finaltrace.txt).
- ApplyPrefabsSystem.OnUpdate consumes SaveInstance (IL_0021), reconstructs Object/Net/Area data at IL_00aa/00b7/00c4, updates prefab IL_00f1 (evidence/detail.txt).
- UpdateObjectSubObjects can remove the whole ObjectSubObjects component when the last element is gone (IL_004f), EffectSource (IL_00a3), and updates ActivityLocation (IL_00d5). Existing row-history requires unchanged component identities, so cannot simply be used for bulldozer removal.

HYPOTHESIS: capturing owning prefab payload and relevant component presence before bulldozer apply, then recording only after a verified native synchronization boundary, can provide regeneration-based recovery without reusing stale entity IDs. Exact capture timing, complete related payload and deferred graph restoration remain UNPROVEN.

Next focused experiment:
1. Trace editor-only selected original/owner and actual commit/synchronization boundary; distinguish preview, denied apply, cancelled click and successful deletion.
2. Capture complete supported parent payload and component presence before mutation. Detect unhandled related components and refuse recovery claims for those operations.
3. First runtime test: one simple prop removed from a duplicate asset that retains another prop. Verify exact before/after data, parent/child ownership, external prefab references, probability and transforms. Do not claim last-item/component reconstruction until separately proven.
4. Prove Undo/Redo waits for deferred regeneration and does not produce duplicates/orphans; validate save/reopen. Include cancellation, rapid consecutive clicks, asset switch and failed restoration.
5. Expand separately to last prop, nested props, surfaces, networks and effects. Preserve validated list-deletion and shortcut behavior. Unsupported cases remain labelled explicitly.

UNSUPPORTED/UNPROVEN: bulldozer recovery in installed 0.4.9. No general entity resurrection, atomic ECS rollback or network reconstruction claim.

Current experiment: 0.4.10 diagnostic installed; see BULLDOZER_0.4.10_VALIDATION.md. No restoration writes are enabled. Native capture/synchronization trace awaiting user test.


## First successful Sub-element runtime trace — 0.4.12
VERIFIED-RUNTIME: user completed one-grill deletion. Preserved trace in evidence/bulldozer-0.4.12-sub-element-runtime.log. Inspector root is null while bulldozing; native target ownership resolves a writable candidate whose payload has 7 objects before, 6 after. Native Apply accepted → matching SaveInstance before/after at frame 2263 → queued prefab rebuild dispatch at frame 2264. No components removed. EffectSource.m_Effects and ObjectSubNets.m_SubNets values also change although counts remain 13 and 2. Areas stay at count 3 and their captured fields compare unchanged.

UNPROVEN: identities of individual changed effect/network fields, final ECS graph completion, last-element reconstruction, Main and Everything recovery. Count stability is not value stability. The log does not identify the candidate prefab by name; Test mapping is consistent with the user test and counts, not an independent named identity check. Next implementation must record complete before/after supported related payload and retain owning-entity context without requiring inspector selection; preserve strict asset/world and external-edit invalidation. Do not write recovery based solely on a one-row removal comparison.

