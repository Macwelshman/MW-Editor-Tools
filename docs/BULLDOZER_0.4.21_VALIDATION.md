# 0.4.21 Main detached capture gate

Outstanding Phase 3 Main recovery work authorised by continuation. No Main restoration enabled; validated Sub-element history preserved.

VERIFIED-STATIC: Game 0.0.0.0 MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. GenerateObjectsSystem.CreateObjectsJob.CreateObject creates from ObjectData.m_Archetype IL_09c9/09ce; seed copy IL_0f8a or creation seed IL_0fa6. ApplyObjectsSystem.HandleTempEntitiesJob.Create removes transient types IL_000e and applies m_AppliedTypes IL_00a1. Evidence main-create-implementation.txt. Native creation route and full applied-marker/barrier completion still UNPROVEN for standalone recovery; no direct entity writes introduced.

Read-only gate captures one ownerless simple static prop before Main native Apply. Strict component allowlist based on 0.4.20 observed Grill01, no Temp/Deleted/Owner, required prefab/archetype/transform/seed/surface. Nonempty child/net/area buffers and unknown components refuse. MeshColor and CustomMeshColor arrays bounded to256 entries; captures presence via null versus empty arrays. Transform, PseudoRandomSeed, Surface and both colour buffers use actual PrefabSnapshot deep materialization/equality. Native preview Hidden and derived mesh batches are not captured for restoration. Upon accepted deletion and original disappearance, repeats detached roundtrip/reference validation and logs candidate result. Original entity ID is never reused to restore. No recovery entry/UI claim for Main.

Release zero warnings/errors; production UI;25 lifecycle/history and22 snapshot/history assertions pass. Runtime detached candidate remains UNPROVEN until trace. Previous0.4.20 standalone deletion timing/inventory verified in evidence/bulldozer-0.4.20-main-runtime.log.

Test: disposable scene, place standalone Grill01, Main bulldoze once, wait several seconds, reply finished. No arming or Main Undo. Read resulting capture verified/refused and after-deletion detached valid result before implementing live recreation. Next establish exact native creation markers/barrier and supported-property validation; then bounded Main live experiment.
