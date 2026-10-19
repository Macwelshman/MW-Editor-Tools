# Undo 0.4.102 — Park/Walking Area and vegetation companion capture

VERIFIED-RUNTIME: user confirms main surface Undo works. Preserved docs/evidence/undo-0.4.101-runtime.log records successful Sand Surface 02 deletion/Undo at 00:44:00 on 2026-10-14. Park Area capture refused Game.Areas.Navigation; the complete inventory also contains HangaroundLocation. Bound Sand Surface 01 and Walking Area capture both refused UpdateFrame on GreenBushCity03 before native deletion. Main prop and nested bound group recovery continued completing.

VERIFIED-STATIC: installed Game 0.0.0.0, MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef; unchanged Game.dll SHA256 AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A. Exact metadata in docs/evidence/undo-0.4.102-native.txt: Navigation/HangaroundLocation are empty IComponentData/IEmptySerializable tags. UpdateFrame is a shared component with uint m_Index.

Implementation: standalone area capture explicitly retains Navigation and HangaroundLocation presence; creation installs the captured tags before native Updated processing and validation checks their exact presence. Bound AreaImage checks also compare these tags after owner regeneration. Surface creation/geometry restoration remains the validated 0.4.101 path.

Vegetation companion capture permits native UpdateFrame only when the entity is a Plant or Tree. It does not copy a shared scheduling index onto regenerated entities; native scheduling remains responsible for assigning it. Plant pollution, tree state/growth, transforms, colours, placement and enabled-effect identities retain their existing comparisons.

Bound area native synchronization may reorder retained ObjectSubObjects descriptors, as already observed for prop deletion. Area removal now accepts only a unique permutation of complete retained object rows, and retained companion root cache indices are resolved against current descriptors. All other prop values and nonarea fields remain checked. This addresses a static downstream guard mismatch; the latest runtime log did not reach this guard.

VERIFIED-STATIC validation: clean Release build; production UI/TypeScript pass; 409 relevant assertions pass (183 Phase3Safety, 39 BulldozerSafety, 132 NestedRecoveryRunner, 23 StandaloneNestedSafety, 12 LastPropHistory, 20 LifecycleSafety). Final area companion reorder change also passed Release and Phase3Safety again. Native ECS integration not performed.

UNPROVEN: 0.4.102 Park/Walking Area and bound surface/area capture and restoration. Unknown schemas, external/ambiguous graphs, lots/districts/map tiles and unsupported nested ownership remain refused. No computer use performed. Package ready; installation deferred while Cities2 is running.

VERIFIED-STATIC installation: 0.4.102 installed after Cities2 exit checks; five package hashes and all rollback file hashes verified. See docs/evidence/recovery-0.4.102-installed-hashes.csv. Native area/bound recovery acceptance pending.

