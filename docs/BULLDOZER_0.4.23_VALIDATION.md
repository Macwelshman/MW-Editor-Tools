# 0.4.23 bounded live Main prop experiment

User authorised continuation from verified detached Main capture to bounded Main recovery. Explicit UI arm required; one simple ownerless static prop in disposable saved editor scene. Sub-element history remains validated and separate. No expansion to Everything/buildings/networks/nested/last-child recovery or publishing.

VERIFIED-RUNTIME baseline:0.4.22 capture roundtrip and detached image valid after accepted deletion of standalone Grill01 (seed24160,one MeshColor,empty CustomMeshColor). Evidence bulldozer-0.4.22-main-runtime.log. Nonempty custom-colour live case not yet validated.
VERIFIED-STATIC: Game0.0.0.0 MVIDe15551e9-3dea-4855-8dde-1da3cea7d7ef. Native GenerateObjectsSystem.CreateObjectsJob.CreateObject uses ObjectData.m_Archetype IL_09c9/09ce and seed copy/create IL_0f8a/0fa6. ApplyObjectsSystem.OnCreate constructs Applied,Created,Updated marker set IL_00b8/00bd/00c2,stored00cc; Create applies set IL_00a1. Evidence main-create-implementation.txt,main-applied-markers.txt.

Implementation: explicit Arm one Main prop Undo test clears other histories, captures only one approved native Main target with strict component allowlist, valid prefab/archetype, no owner/Temp/Deleted,empty child/net/area buffers. Unknown components/refused captures create no Undo. Record appears only after accepted Apply and original entity disappearance. Undo creates a fresh entity using current prefab archetype, sets PrefabRef, exact Transform/seed/Surface and copied mesh/custom colour buffers, then native Applied/Created/Updated markers. No old ID resurrection. It waits at least2 frames for native initialization,compares strict typed properties, and only then enables Redo. Redo first validates the recreated entity and marks that exact entity Deleted; completes only after disappearance. One entry;no mixed-history ordering. Native scene saving semantics remain runtime UNPROVEN.

Preflight current editor/dialog/busy/prefab/record state; repeat on each command. Context/load/settings/list edits invalidate. Another bulldozer action clears the Main record; action during pending recovery disables it. Failure/120-frame timeout disables Main recovery and asks reload before saving. Only synchronous creation errors attempt immediate cleanup of just-created entity; asynchronous rollback is UNPROVEN. No broad rollback claim.

Release zero warnings/errors;production UI/Settings render;420 regression assertions pass (25 lifecycle/history,29 snapshots including typed ECS value copies,18 protection,311 options/shortcuts,11 lifecycle,26 Options). These checks do not validate native standalone entity creation.

## Focused test
1. Fresh saved disposable scene/duplicate; place one standalone Grill01 unattached to an asset, wait for placement to settle.
2. Open MW Editor Tools History & Recovery; click Arm one Main prop Undo test. Do not arm Sub-element history.
3. Delete that Grill01 using Main mode. Wait for recorded standalone deletion.
4. Undo once. Wait for completion. Check exactly one grill at original position/rotation/appearance.
5. If successful,Redo then Undo,waiting each completion. Check no duplicates and no changes to surrounding objects.
6. Check recreated prop position, rotation, appearance and exactly one copy. Editor scene cannot be saved; persistence is untested.
7. On failure/timeout,reload saved scene before saving and report status. Reply finished after testing.

Next only after result: address native initialization mismatch or validate exact captured properties; Main multi-entry and nonempty custom-colour cases remain unsupported/unproven.
