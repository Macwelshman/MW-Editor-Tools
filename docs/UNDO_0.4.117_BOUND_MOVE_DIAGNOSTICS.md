# Undo 0.4.117 — bound move marker correction and blocker reporting

## Acceptance retained from 0.4.116

VERIFIED-RUNTIME (user, 2026-10-16): standalone move Undo, cancelled-move preservation, foliage neighbour refresh and bound deletion passed. Bound move attempts failed: Ctrl+Z did nothing; the Undo button was not checked. Persistence was not attempted because bound moves could not be undone. Do not repeat accepted tests as a new full batch.

VERIFIED-RUNTIME log: Carport03 moves reached “Prop move ready to Undo” at 18:15:41.267, 18:17:43.513 and 18:18:51.315, but no bound move restoration dispatch followed. Grill01 move observation timed out at 18:15:53.112 with “Prop placement does not match an asset descriptor.” Capture refusals also explicitly identify Game.Common.BatchesUpdated at 18:16:45.691 and 18:21:10.471. Standalone move recovery completed at 18:17:27.738. Foliage candidate creation and successful complete forest validation occur at 18:16:23.841–23.904 and 18:16:27.422–27.490. Network deletion completed at 18:16:29.256 and 18:16:36.379. Preserved log: docs/evidence/undo-0.4.116-acceptance-runtime.log.

## Changes

VERIFIED-STATIC: shared standalone/nested prop schema now recognizes Game.Common.BatchesUpdated as derived refresh state. This marker was already written by the standalone move Undo path; it has no instance fields and is not copied as saved prop data. Deleted/Temp/SaveInstance and unknown components remain excluded. The actual registry is shared through PropComponentSchema and linked into managed safety tests.

VERIFIED-STATIC assembly: Game, Version=0.0.0.0, MVID=e15551e9-3dea-4855-8dde-1da3cea7d7ef; Game.Common.BatchesUpdated is a value type implementing IComponentData with zero fields. Evidence: docs/evidence/undo-0.4.117-batches-marker-native.txt. No game assembly was modified.

VERIFIED-STATIC: move readiness reports a changing blocker once rather than swallowing exceptions: referenced prefab changes, owner/context/pending updates, saved after-image fields, prop/area/effect companions, network mapping or capture schema. An explicit configured Undo keypress with an existing slot reports readiness, typing/focus/heartbeat/dialog/shortcut-conflict gates when blocked. No typed text is logged. Descriptor refusal now includes cache values and up to four same-prefab saved rows (position/rotation/parent mesh/probability/group/index). Exact descriptor equality is unchanged; no approximate placement/index fallback was introduced.

HYPOTHESIS: the ready-but-silent bound cases may be blocked by keyboard focus or a later after-state eligibility mismatch. The preserved log cannot distinguish these. Marker recognition fixes the observed schema refusals but is not proof that every bound move blocker is fixed.

VERIFIED-STATIC validation: clean Release/UI/TypeScript; 572 focused assertions (Phase3Safety 274, MoveUndoSafety 36, BoundFoliageSafety 25, BulldozerSafety 47, NestedRecoveryRunner 133, StandaloneNestedSafety 25, LastPropHistory 12, LifecycleSafety 20), plus recovery/window/settings/Multi-Save/Colours UI regressions. Four additional checks exercise the actual schema registry, confirm native marker has zero fields, retain unsafe marker exclusions and require detailed descriptor mismatch reporting while preserving strict rejection. Native bound move restoration remains UNPROVEN.

## Next grouped check

On the same disposable mixed asset, move a simple bound prop, a nested group and foliage, pressing Ctrl+Z after exiting each move. If Ctrl+Z does nothing, check the shared window’s “Undo last action” button once: note whether it is disabled, or use it if enabled. Stop on a recovery error, and reload without saving. The new log distinguishes keyboard blocking from capture/readiness failure; do not repeat already-passed deletion and standalone checks. After bound moves pass, save only a new disposable restored copy and reopen it for persistence acceptance.
