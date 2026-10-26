# Move acceptance — 0.4.210

VERIFIED-RUNTIME2026-10-23: user reports move/Undo works on all tested types except markers. Latest log confirms bound move Undo completion for Grill01(12:56:29.263), FlowerBushCity01(12:56:36.111), FenceIndustrialPieceLow02(12:56:56.748), and isolated bound FireBigVFX effect move(12:56:51.454). Area/surface move was recorded ready12:57:36.340; exact final completion should be assessed from preserved full log rather than inferred from the readiness message.

Restricted Spawn Location attempts12:56:21.245/12:56:38.598 and Pedestrian Access Location12:56:43.764 were refused before capture with "Spawn/access companion support is limited to deletion Undo."

VERIFIED-STATIC: BulldozerTrace.CaptureMoveGroup in PropMoveRecovery.cs explicitly rejects group.Any(n=>n.SpawnAccess.Present||n.Lanes.HasLanes). Thus marker moves were not captured; no failing restoration ran. Deletion support does not yet extend marker movement. A correct extension must account for moved-anchor derived connection geometry/topology and complete companion references, rather than simply removing the guard.

No code or installation changes in this diagnostic review. Preserve accepted repeated marker deletion Undo and ordinary prop/foliage/effect/area move behavior.

Additional tail verification: area/surface Undo completions at12:57:40.104 and12:57:45.138 both verify source, geometry and companions. The latter follows a node-deletion capture. These recorded operations passed; an unexecuted node-addition readiness message alone is not counted as completed Undo.
