Builds: Release and UI pass. NU1900 advisory feed warning only.
Managed suites: ViewportPropSafety (29 adapter + 76 projection), ReplacementUndoSafety (45), SurfaceReplacementSafety (91), TargetingSafety (92), OutlineColourSafety (22), ViewportRetrySafety (34), LifecycleSafety (28), Phase2Safety (37), AssetIconCompatibilitySafety (35), MoveUndoSafety (44), AreaMoveSafety (100), EffectUndoSafety (41), SpawnAccessSafety (60), BoundCompletionSafety (77), Phase3Safety (1204) passed.
UI: all ui-*.cjs scripts passed after updating two stale import stubs; targeted row suite additionally checks shared parsing and zero diagnostic bridge messages on success.
Native acceptance: pending for 0.4.251. User confirmed 0.4.250 highlighting before this review.
ComponentTransferSafety: 130 passed (one existing test-only unused-field warning).

Native acceptance update: user confirms all requested checks passed. Full session logs and interpretation retained in native-acceptance/README.md; supersedes pending status above.
