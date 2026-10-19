# Undo 0.4.105 — main trees and serialized network node indices

VERIFIED-RUNTIME: user and docs/evidence/undo-0.4.104-runtime.log confirm bound GreenBushCity03 recovery, including six captured groups, now passes. Main NA_LondonPlaneTree01 was refused at capture for Game.Objects.Color; inventory also includes enableable Decoration. Bound Sand Surface 01 still fails only ObjectSubNets.m_SubNets; effects now pass. Main surfaces, main areas and bound areas remain confirmed.

VERIFIED-STATIC implementation: typed object group snapshots retain Color (byte Index/Value and bool SubColor), Decoration presence and enabled state. Native creation sets captured root values before initialization; typed restoration preflights complete group state before writing Color/Decoration and final validation compares them exactly. Existing Plant/Tree/seed/colour-buffer/effect/ownership checks remain. Normal bound foliage behavior is preserved.

Area-only network descriptor comparison now normalizes m_NodeIndex on a detached copy. All remaining fields use the existing native geometry tolerance and exact reference/discrete comparisons. This comparison can only reach recording if the complete retained native graph separately matches: node/edge/lane membership, geometry, endpoints, owners, path nodes, references, seeds and source-survival. Wrong endpoint/ownership/reference changes still refuse recovery. No live descriptor or prefab is changed to prepare capture.

VERIFIED-STATIC evidence: same Game 0.0.0.0 MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. Color is IComponentData/IEmptySerializable with byte m_Index, byte m_Value, bool m_SubColor. Decoration is empty IComponentData/IEnableableComponent/IEmptySerializable. Reused docs/evidence/stage2-companions-native.txt: ApplyPrefabsSystem.ListObjectSubNets calls GetNodeIndex at IL_042f/IL_043e and stores ObjectSubNetInfo.m_NodeIndex at IL_0448, with another assignment at IL_05f2. Serialized indices can be reassigned; the graph is the topology authority.

HYPOTHESIS: serialized node-index reassignment is the remaining bound surface descriptor difference. The log names the net descriptor list but does not expose individual differing fields; remaining mismatches remain refused. Native bound surface acceptance is still UNPROVEN.

VERIFIED-STATIC checks: clean Release, production UI and TypeScript; 422 assertions across six relevant managed suites pass (Phase3Safety 193, BulldozerSafety 39, NestedRecoveryRunner 133, StandaloneNestedSafety 25, LastPropHistory 12, LifecycleSafety 20). Tests cover detached normalization, unchanged exact parent meshes, absent descriptors, tree Color/Decoration restoration and exact completion. Existing graph tests reject changed endpoints, ownership and external references. Native ECS initialization is not simulated.

UNPROVEN: 0.4.105 main tree and bound surface recovery. No computer use or game assembly edits. User performs runtime acceptance. Automatic installation with full rollback and copied hash checks is authorised after game exit.

VERIFIED-STATIC installation: 0.4.105 installed after Cities2 exit checks; five package hashes and 113 rollback file hashes verified. Rollback: outputs/rollback/MWEditorTools-before-0.4.105-20261014-203243/MWEditorTools. See docs/evidence/recovery-0.4.105-installed-hashes.csv. Native main-tree and bound-surface acceptance pending.

