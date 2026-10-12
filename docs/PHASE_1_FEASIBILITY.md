# Phase 1 feasibility assessment

9 October 2026 • INSPECT • static evidence only

The smallest defensible next step is component-header confirmation. The installed managed handler provides a pre-destruction boundary and the native confirmation dialog API exists. Full vanilla deletion undo is not yet implementation-ready: list ownership, regeneration, entity graphs and persistence remain unproven.

| Capability | Assessment | Gate |
|---|---|---|
| Header confirmation | Conditional go for narrow proof of concept | Verify interception and dialog lifecycle in disposable asset |
| Header complete undo | Unproven | Capture dependency closure before DestroyImmediate; restore references and generated entities |
| Object list undo | Candidate, unproven | Live accessor mapping, immutable deep snapshot, regeneration and reopen |
| Area/surface undo | Candidate, unproven | Nodes, parent meshes, ownership and entity reconstruction |
| Net undo | Higher risk, unproven | Node/edge connectivity, lanes and reference remapping |
| Effect undo | Candidate, unproven | Settings plus source/parent references and regeneration |
| Bulldozer undo | Defer | Precommit graph boundary and ECS ordering not established |
| 20–50 history steps | Design target only | Memory budget, stable asset identity and transaction validation |
| Recovery snapshots | Candidate fallback | Non-overwriting save/export fidelity and reload validation |
| Existing module migration | Sources located; defer changes | Baseline and regression tests before adaptation |

## Smallest safe Phase 2 proof of concept

On a duplicate writable asset, intercept only `InspectorPanelSystem.RemoveComponent(ComponentBase)` before its body. Check editor mode, asset identity, writable state, component validity and supported method signature/build. Preserve the vanilla body via a narrowly scoped one-shot bypass for confirmation; do not reproduce its dependency removal algorithm.

Suppress the initial call, show a native confirmation listing the actual dependency closure, and perform no prefab/ECS mutation while waiting. Cancellation/close must leave components, settings and entities unchanged. Confirmation must revalidate the asset/world/selection generation, component and dependency closure; invalidate stale requests. A scoped bypass must permit exactly one original call, avoid recursion and reset in finally. Cancel duplicate pending requests and dispose callbacks on unload/asset switch. If interception cannot be safely established, disable protection explicitly instead of displaying a misleading dialog.

This is a proposal, not implemented code or a runtime guarantee. A handler patch with a mod-owned Harmony version is one candidate; delegate wrapping is another. Select only after probing compatibility. Avoid patching the lower RemoveComponent helper: it is also used during normal synchronisation and would prompt per dependent component.

## Recovery alternatives and limits

Prefer restore from a verified immutable prefab snapshot plus the game's supported regeneration path over raw ECS byte copies, if experiments prove fidelity. Preserve complete collections, indices, settings, prefab identities, parent-child relationships and external references. Restore must allocate valid new entity identities and remap every internal reference; old Entity values cannot be treated as persistent identities.

If faithful operation-level reversal cannot be proven, offer a separate recovery copy/checkpoint with explicit whole-asset reload scope. Do not overwrite the original or silently undo unrelated later edits. Multi-Save saves current data and is not a pre-delete backup or undo service. Automatic snapshot cadence, storage limits and naming remain design decisions pending save/reopen tests.

Full undo, atomic rollback, snapshot fidelity, cross-asset recovery, network reconstruction and save/reopen persistence are unsupported in this scaffold. No feature is enabled.

## Phase 1 limits

Installed assemblies were read with bundled Colossal.Mono.Cecil, not loaded as game runtime types. No game assembly was changed. No editor deletion was performed. Initial SDK absence prevented compilation; after toolchain installation the inert scaffold built successfully with SDK 8.0.425, zero warnings/errors. Runtime compatibility is still unproven. UI bundle event dispatch, exact list ownership and complete system update ordering were not exhaustively inspected. Phase 1 supplies concrete method boundaries and an implementation plan with explicit gates, not blanket feature feasibility.

