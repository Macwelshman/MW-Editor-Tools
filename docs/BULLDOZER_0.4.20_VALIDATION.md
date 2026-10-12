# 0.4.20 Main component inventory correction

VERIFIED-RUNTIME: 0.4.19 Main trace targets one standalone Grill01, no Owner, no child objects, valid prefab and transform, native Apply accepted. Entity still exists immediately after Apply, absent at age1/5/30/120. Entity deletion is deferred and the prefab-list restoration route cannot recreate this standalone instance. Evidence: evidence/bulldozer-0.4.19-main-runtime.log.

The component-type ToString formatter printed null labels. Complete component inventory remains UNPROVEN. 0.4.20 diagnostic uses public ComponentType.GetManagedType().FullName and explicit buffer flags, with type-index fallback. Verified API metadata: evidence/main-object-creation-api.txt (Unity.Entities 0.0.0.0; Game 0.0.0.0 MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef). Native CreationDefinition and ObjectDefinition expose prefab/owner/transform/seed/probability-related state; invoking their creation pipeline remains UNPROVEN and is not implemented.

Release zero warnings/errors; production UI and25 lifecycle/history assertions pass. No Main recovery writes; validated Sub-element recovery retained.

Repeat one standalone Grill01 deletion in Main mode on disposable scene. No arming/Undo. Wait several seconds; reply finished. Next identify full required standalone data and review native creation lifecycle before enabling bounded Main Undo.

VERIFIED-RUNTIME 0.4.20 Main inventory: standalone Grill01 no Owner, empty SubObject buffer, accepted native Apply, entity absent by next frame. Components: PrefabRef,CullingInfo,Surface,PseudoRandomSeed,Transform,MeshColor/MeshBatch/SubNet/SubObject/SubArea/CustomMeshColor buffers,Hidden,Object,ObjectGeometry,Static,Simulate. Preserved docs/evidence/bulldozer-0.4.20-main-runtime.log. Seed/custom colours and actual related-buffer contents must be preserved or explicitly excluded; prefab+transform alone is insufficient. VERIFIED-STATIC native API inventory in main-object-pipeline-api.txt: ObjectToolBaseSystem.CreateDefinitions; GenerateObjectsSystem.CreateObjectsJob.CreateObject; ApplyObjectsSystem.HandleTempEntitiesJob.Create/Delete. No native creation route invoked yet; lifecycle/restoration UNPROVEN. Next narrow inspect CreateObject/Create and definition pipeline; design bounded ownerless simple-prop recovery with new entity IDs, exact seed/custom colours, no nonempty related buffers, deferred confirmation and redo branch. Main Undo remains disabled; 0.4.18 validated Sub-element history retained in installed 0.4.20.


