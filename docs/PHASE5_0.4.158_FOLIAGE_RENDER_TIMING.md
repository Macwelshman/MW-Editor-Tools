# Foliage native rendering timing — 0.4.158

VERIFIED-RUNTIME: 0.4.157 candidate Entity(50873:199) survives but has MeshBatch length 0 and CullingInfo radius 0, with Hidden absent. Destination validation fails and source rollback is queued. UI refresh did not correct rendering.

VERIFIED-STATIC: Game 0.0.0.0 MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef, PreCullingSystem.OnCreate IL_00b8–010a initializes its rendering query from CullingInfo plus Updated/BatchesUpdated. Evidence: foliage-preculling-create.txt. Accepted effect Undo already sends these tags at the PreCullingSystem.OnUpdate prefix after intervening cleanup.

0.4.158 reuses that existing callback to submit foliage replacement tags once at the native rendering consumer, after Created has cleared. No renderer allocations are copied. Destination and donor checks remain active. HYPOTHESIS: tags submitted in UIUpdate were cleared before native rendering consumed them. Runtime correction remains UNPROVEN.

Acceptance: disposable asset, single foliage paste then replace-all; require visible destinations and unchanged copied source. Discard without saving on failure.
