# Undo 0.4.106 — retained networks during bound surface deletion

VERIFIED-RUNTIME: user confirms main foliage passes. docs/evidence/undo-0.4.105-runtime.log records successful EU_AlderTree01 regeneration. Bound Sand Surface 01 capture still refused ObjectSubNets.m_SubNets; bound Walking Area and foliage continued completing.

VERIFIED-STATIC: source descriptor node-index normalization alone was insufficient. Bound area/surface ancillary network comparison now treats serialized curve/index values as representations of the retained network. Captured typed live graph equivalence remains mandatory before a recovery entry exists: complete nodes, curves, edges, lanes, endpoints, ownership, path-node data, memberships, prefab/container identities, seeds and source-survival. Serialized descriptors separately retain exact prefab, attachment-mesh and upgrade requirement comparisons. Null/empty upgrade arrays are equivalent only when neither contains a requirement. Invalid/nonfinite curves, missing/extra rows and changed metadata remain refused. Repeated descriptor metadata is compared by counts; native graph mapping still requires unique geometry/topology matching.

No source descriptor is changed to prepare Undo. Both before/after prefab snapshots stay exact. CanUndo still requires the full after payload and graph to match; restoration writes the exact before payload and validates the complete before graph before consuming the slot. Main foliage, bound foliage and other restoration paths are unchanged.

HYPOTHESIS: the remaining saved net curve representation/empty upgrade array difference is the observed capture blocker. The log identifies the descriptor list, not individual fields. This change removes serialized geometry as a duplicate authority while preserving full live graph checks; it does not assert in-game success.

VERIFIED-STATIC checks: clean Release build and 428 relevant managed assertions pass (Phase3Safety 199, BulldozerSafety 39, NestedRecoveryRunner 133, StandaloneNestedSafety 25, LastPropHistory 12, LifecycleSafety 20). Six new tests cover source immutability, empty upgrade normalization, changed requirements, repeated metadata counts, missing rows and nonfinite source geometry. Existing graph tests reject geometry changes, endpoint changes, external ownership/references and orphan sources. UI production build/TypeScript are checked before packaging. Native bound surface acceptance remains UNPROVEN.

Native assembly evidence is reused from the unchanged Game 0.0.0.0/MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef build. No computer use, original asset edits, or game assembly changes.

VERIFIED-STATIC installation: 0.4.106 installed after Cities2 exit checks; five package hashes and all rollback file hashes verified. See docs/evidence/recovery-0.4.106-installed-hashes.csv. Production UI/TypeScript passed. Native bound surface acceptance pending.

