# Stage 1 — automatic one-step prop Undo (0.4.95)

Scope approved 2026-10-13: Stage 1 of the reassessed Undo plan only. Highest priority is the last mistaken bound-element bulldozer deletion. No new Redo/multiple-history development; later area/surface/effect/lane/network stages are not implemented here. User performs native checks; no computer use.

VERIFIED-RUNTIME (user): component-list recovery, standalone prop bulldozer recovery and manually enabled bound prop recovery, including the Grill02 Everything Undo/Redo/Undo check, were previously confirmed for their tested cases. Do not treat this as validation of every nested type or of this new one-step flow.

IMPLEMENTED:

- One retained completed deletion across the active recovery route. Inspector/list, standalone and bound stores use LastDeletion rather than multiple Undo/Redo stacks. New captures replace old data. Failed validation does not consume the slot; successful restoration consumes it. Native completion must finish before the bound/standalone slot advances. Redo cannot dispatch from the normal shortcut/UI.
- Every new bulldozer Apply attempt clears completed slots before selecting a capture route. Unsupported targets, failed captures and denied Apply cannot expose an older action as though it recovered the latest attempt. This intentionally also clears the slot on a denied attempt. List history is already invalidated by the native bulldozer boundary.
- Existing automatic standalone Main/Everything route retained. Existing automatic bound Everything route now handles stale root cache indices by resolving a unique descriptor using prefab identity, position, rotation, parent mesh, probability and group. Only detached snapshot data is normalized; no live cache writes, SaveInstance preparation edit or save occurs. Duplicated/missing placements refuse capture. Child cache/schema, graph/owner/reference/budget and native completion validation remain in force.
- Normal shared UI has Undo last deletion, Forget last deletion, target/status and configurable shortcut guidance. Manual enabling, Arm/test controls, history list and Redo removed from normal use. Inspector shows Undo/Forget. Legacy HistoryLimit/Redo options hidden; persisted fields and old restore code retained for compatibility, without usable Redo slots. Existing input guards and deferred shortcut dispatch retained; keyboard polling requests Undo only.

VERIFIED-STATIC: Game 0.0.0.0 / MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef remains the supported runtime identity. Descriptor/cache fields referenced directly from installed Game.dll; compile validates field names/types. Existing native synchronization/finalization evidence is reused. No game assemblies or original MW projects modified.

Verification: clean Release build, production UI build and TypeScript. Twelve relevant managed suites pass 788 assertions, including 15 new one-slot/descriptor resolution checks. All seven UI scripts pass, including single-step presentation, removal of manual/test/history controls, the window delayed-mount correction, Colours and Multi-Save. Old standalone nested-runner/stack suites remain regression coverage of their linked source/policies; they do not prove the new native integration. Exact native cache remapping and one-step shortcut behavior remain UNPROVEN until user acceptance.

## Consolidated acceptance for the next in-game batch

No Enable/Arm step is required. Recovery and its shortcut must be enabled in Options; default Ctrl+Z.

1. A disposable standalone prop: bulldozer Main or Everything deletion, then Ctrl+Z. Verify position/settings. A second Ctrl+Z must do nothing.
2. A saved disposable mixed fixture: select the owning asset in Workspace, Everything-delete one bound grill, then Ctrl+Z. Verify it returns exactly, without a preparation edit. Repeat on a supported nested prop/group.
3. Delete two supported props in succession; Undo must restore only the second. No earlier deletion or Redo becomes available.
4. After a supported deletion, attempt deletion of an unsupported target on the disposable copy. The old Undo must disappear. Switch assets/make unrelated edits and confirm stale Undo is unavailable.
5. Typing in a field, recording keys, an open dialog, saving or holding a shortcut must not dispatch recovery.

Use a disposable baseline and do not save test deletions over it. Report a refusal/status message if any supported capture fails. No repeated Redo or multi-history test is required. This checklist can be grouped with later implementation acceptance as requested.

Bound automatic scope still requires a selected writable owning asset, one target in Everything mode, existing bounded prop schemas/topologies, deterministic supported links and unambiguous placements. Unsupported nested ancillary attachments remain excluded. Whole houses/components and new bulldozer area/network/lane/effect routes remain outside Stage 1.
