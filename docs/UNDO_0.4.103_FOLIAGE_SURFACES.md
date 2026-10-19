# Undo 0.4.103 — foliage and retained bound surface validation

VERIFIED-RUNTIME: user confirms main and bound areas working. docs/evidence/undo-0.4.102-runtime.log records main Walking Area and bound Walking Area exact restoration. Bound Sand Surface 01 capture failed its immediate removal check before recording. Bound GreenBushCity03 was refused by attached-payload policy; standalone GreenBushCity01 and FlowerBushCity03 were refused for Plant. Nested prop recovery continued completing.

VERIFIED-STATIC implementation:
- Standalone Plant/Tree instances now enter typed group capture, including single-node vegetation. Plant pollution, tree state/growth, transforms, seeds, colour buffers, placement data and unchanged native effects remain checked. Root values are supplied before native initialization. Native shared UpdateFrame scheduling is not copied between entity IDs.
- Native effect-container children are captured as effect companions rather than ordinary prop descendants. Referenced prefab snapshots include those effect identities. Native built-in effects that do not create editor-container children remain represented by the unchanged source prefab and semantic enabled-effect image. Allocation indices are never copied. Companion checks compare transforms/scale/intensity/group/cache/elevation; source-survival checks include effect entity IDs. Attached areas/networks/lanes/activity and animation curves remain unsupported.
- Bound area removal validates the descriptor change immediately and defers retained typed geometry to the existing native completion/stability window. Retained polygon descriptor positions use the same squared-distance <1e-8 tolerance already used by AreaGeometry; prefab identity and parent meshes remain exact. Native float rounding is accepted; substantive retained-node edits, missing/extra polygons and ambiguous matches remain refused. Exact component/field refusal diagnostics added.

HYPOTHESIS: native float roundoff and/or transient retained area geometry caused the bound surface immediate check to fail. The prior log does not identify the precise subguard. Both guards have been corrected without dropping retained geometry validation; final geometry still has to match across three stable frames before an Undo record exists.

VERIFIED-STATIC native descriptor layout: Game.Prefabs.ObjectSubAreaInfo has AreaPrefab m_AreaPrefab, float3[] m_NodePositions and int[] m_ParentMeshes on the same Game 0.0.0.0/MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef build. Previous Plant/Tree/native effect allocation evidence is reused; no game assembly edited.

VERIFIED-STATIC checks: clean Release, production UI and TypeScript; 411 relevant managed assertions pass (Phase3Safety 185, BulldozerSafety 39, NestedRecoveryRunner 132, StandaloneNestedSafety 23, LastPropHistory 12, LifecycleSafety 20). New retained-polygon regressions distinguish tiny native rounding from actual geometry changes. Managed standalone recovery checks use ECS doubles, not native initialization. Compiled native collection audit passes.

UNPROVEN: 0.4.103 bound surface and foliage recovery, including effect companion regeneration. Main/bound area acceptance remains recorded for 0.4.102. Package prepared; installation deferred while Cities2 is running. User performs in-game acceptance; no computer use performed.

VERIFIED-STATIC installation: 0.4.103 installed after Cities2 exit checks; five package hashes and all rollback file hashes verified. See docs/evidence/recovery-0.4.103-installed-hashes.csv. Native foliage and bound-surface acceptance pending.

