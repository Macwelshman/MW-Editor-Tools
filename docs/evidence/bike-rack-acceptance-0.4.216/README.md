# Bike rack and pedestrian move acceptance — 0.4.216

VERIFIED-RUNTIME 2026-10-23 on MW Mixed Test Components: first BikeRack01 capture14:50:56.394 timed out14:51:00.046, no Undo entry recorded. Error: Prop placement does not match an asset descriptor. Bike-rack local position(16.65672,-0.01623535,-3.368393), probability100, group0, parentMesh-1, index54 agree with source row54; cached local rotation(0,1,0,0) differs from source rotation(-0.008925325,0.9999602,0,0). Nearby rows are other bike-rack placements and do not match the target position.

Pedestrian Access Location capture14:51:04.054 was refused before capture with the identical bike-rack descriptor mismatch. The parent capture validates all companion props; the immediate pedestrian failure was not a logged pedestrian restoration failure. No Undo entry was recorded for that attempt. Native settling/serialization is a HYPOTHESIS for the rotation mismatch, not a proved causal instruction or justification for a loose angle tolerance.

Later seven Undos completed:
- FireBigVFX isolated bound effect move14:51:12.919, placement/values verified.
- Restricted Spawn Location14:51:16.933, source/graph/companions verified.
- Pedestrian Access Location14:51:20.841, source/graph/companions verified.
- Grill0114:51:30.393, source/graph/companions verified.
- BikeRack0114:51:37.703,14:51:43.950,14:51:48.941, source/graph/companions verified.

An attempted whole-building move14:51:25.619 was explicitly excluded by the existing supported prop scope. Effect preflight busy=True while restoration pending was followed by successful completion, not a failed effect Undo. No completion is inferred from readiness alone.

Partial acceptance: lane-only eligibility/dispatch now reaches capture and repeated native bike-rack move Undo completes. Initial rack capture still fails descriptor rotation association and can temporarily prevent other companion-aware moves on the same asset. Existing position-only native serialization mapping is insufficient to establish this rotation case. A correction should prove exact native cached-versus-world-derived rotation serialization, retain unique row association, geometry/typed-data/reference/source checks, and avoid accepting arbitrary source rotation edits. Full original log retained. Review only: no code/version/install changes.
