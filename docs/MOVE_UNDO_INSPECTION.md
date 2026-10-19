# Move Undo — native inspection, 2026-10-15

User confirmed the built-in move/relocate tool is the intended entry point. This inspection adds no runtime hooks, changes no installed mod package and preserves user-validated 0.4.114 deletion recovery.

VERIFIED-STATIC assembly: Game Version=0.0.0.0 MVID=e15551e9-3dea-4855-8dde-1da3cea7d7ef, unchanged from deletion recovery evidence.

ObjectToolSystem.StartMoving(Entity) stores the moving entity at IL_0000-0002, has an explicit editor branch at IL_0007-0017, redirects service-upgrade targets to their owner at IL_0019-004b, selects Move mode (enum value 2) at IL_0070-0072 and resolves the moving object's prefab at IL_007e-0097. A prefix can capture an original typed before-image before native preview generation; service upgrades/whole-building relocation must be excluded from initial prop scope rather than misidentified as a prop move.

ObjectToolSystem.Apply(JobHandle, Boolean) is shared by move/placement/brush/upgrade operations. Its ordinary final path checks GetAllowApply at IL_02c4-02ca and sets applyMode value 1 at IL_02cf-02d1, then clears control points at IL_0357-035d. Therefore a generic Apply callback alone is insufficient: require Move actualMode, a matching armed move session, native acceptance and subsequent changed-state verification. Reuse detached typed snapshots; never retain NativeList/DynamicBuffer views. Do not treat preview entities as committed originals.

SelectionToolSystem.Apply handles selection toggling/marquees and is not evidence of a move commit. EditorGizmoSystem is rendering; no transform authoring API was identified there. Full selected method IL: docs/evidence/move-undo-tool-entrypoints.txt and docs/evidence/move-undo-native-commit.txt.

Proposed first implementation:
- One standalone or directly bound supported prop/group, including foliage and captured nested children. Capture world position/rotation, relevant local/attachment/elevation values, original prefab/ownership identities and child graph before movement.
- Observe one accepted native move, wait for deferred updates and saved bound descriptor changes to settle, then record both before/after images as a single last-action entry. Ignore no-op/cancelled moves. Invalidate stale deletion entries after unsupported committed edits; do not clear a valid deletion slot merely on entering or cancelling the move tool.
- Share Ctrl+Z and the single-slot action order with deletion Undo. Require exact current after-state before restoration, apply original placement through native update semantics, verify complete restored graph/owner data across stable frames and consume the slot only on success. Preserve child relationships and external references. No Redo or history expansion.
- Guard loaded-world/asset changes, read-only assets, missing/reused entities, dialogs, saves, conflicting hooks, interrupted operations and native move refusal. Unsupported buildings/service upgrades/areas/networks stay outside initial move scope.

UNPROVEN: native event timing, whether original or replacement entities survive every built-in prop move, bound/nested ownership edits and the final native restore route require implementation-level inspection and subsequent user acceptance. The Boolean Apply argument/release behavior must be confirmed before drag grouping is coded. These static entry points establish a plausible capture/commit route, not runtime move Undo support.
