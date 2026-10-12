# Proposed architecture

Proposal only; no runtime services implemented.

Core owns module lifecycle and a small constructor-injected service registry. Services own selection/asset context, capability validation, prefab inspection, interception coordination, logging and editor-thread dispatch. History owns immutable snapshots, transaction metadata and undo/redo lifecycle. Modules depend on contracts, not on other module implementations. UI owns one persistent draggable native-style window with History & Recovery, Prop Replacer, Components, Colour Variations, Multi-Save and Settings sections. Settings owns module enablement, shortcuts and limits.

Each capability is disabled until the installed-build checks and restoration tests pass. Editor context contains world identity, stable asset identity and an incrementing generation. History/pending dialogs invalidate on asset switch, load, world disposal or unexpected external edits. No cross-asset history until proven safe.

Proposed transactions: component/entity/surface/network deletion, prop replacement, component changes and colour changes. Each stores operation kind, versioned schema, owning asset, dependency closure, immutable before/after data and validation fingerprints. History commits only after verified success; redo clears on a new branch. Initial target 20–50 steps with a memory cap. No raw Unity object references as durable snapshots.

Future apply sequence: validate → capture complete before state → prepare graph/reference remap → execute on correct editor boundary → validate result → commit. On failure use a proven rollback path or stop with a recovery copy; never promise atomic rollback without evidence. Preview capture/restore on copies before destructive use.

Prop Replacer must preserve destination probability exactly, position, rotation, compatible scale and other compatible properties; source unchanged. Pivot and compatibility checks precede replacement. Colour Variation migrates existing copy/undo behavior incrementally; Multi-Save retains its asynchronous save lifecycle.
Phase 3 implements native inspector recovery controls and a bounded transaction service; the remaining shared window and module architecture below remain proposals.
