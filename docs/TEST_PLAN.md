# Current release gate

Use [0.4.171 release readiness](RELEASE_READINESS_0.4.171.md) for replacement/source-mapping fixes and the user-accepted replacement Undo, [0.4.164 release readiness](RELEASE_READINESS_0.4.164.md) for the launcher update, and [0.4.163 release readiness](RELEASE_READINESS_0.4.163.md) for the current single-action Undo and Phase 5 grouped acceptance. Historical multi-history/Redo checks below do not apply to the current supported release scope.

# Validation plan

Phase 1: inspect project XML, inert source, document links, evidence hashes and absence of deployment/runtime hooks. The inert scaffold build subsequently passed with SDK 8.0.425 and zero warnings/errors. No destructive tests performed.

Phase 2 disposable asset matrix: simple component; component with dependencies; read-only asset; different prefab categories; cancel/close; confirm once; repeated click; selection/asset switch while dialog open; editor exit/load/dispose; interception error; other mods. Compare full component/settings/entity counts before and after cancellation. Confirm must match vanilla removal, including cascades. Unsupported builds must disable the capability and explain why.

Recovery acceptance per supported operation: complete before/after data equality; no shared mutable snapshot collections; correct underlying index with search/pagination; null/empty lists; parent-child graphs; transforms/probability; external prefab references; network nodes/edges/lanes; area geometry; effects; save and reopen; multiple undo/redo; redo branch clearing; history depth/memory; world/asset invalidation. Compare semantic data and references, not merely visual appearance.

Fault injection: failed capture, missing prefab, stale Entity version, partial creation, failed serialization/save, queued ECS work and interrupted callback. Verify no orphan/duplicate entities and prove rollback/recovery behavior before enabling destructive use.

Existing module regression: preserve colour groups/bindings/randomness/alpha/external settings; Multi-Save queue, lock handling, native dialogs, cancellation and stale-generation callbacks. Test sources individually before shared service adaptation.


