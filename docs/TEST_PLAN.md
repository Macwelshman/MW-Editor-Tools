# Current release gate

Current distribution preparation: [0.4.251 release readiness](RELEASE_READINESS_0.4.251.md). The accepted installed runtime is unchanged; all eight screenshots and the 19-page PDF are complete, and final installer/package checks pass. The entries below preserve earlier feature-specific gates and evidence.

Current authorised feature: [0.4.224 filtered targeting and Undo feedback](TARGETING_0.4.224.md). Native targeting masks, render overlays, input/toolbar transitions and target Copy/Replace require grouped acceptance alongside existing Undo. Managed checks do not certify native input/render behaviour.

Current optional integration: [0.4.223 Asset Icon Creator thumbnail preservation](ASSET_ICON_COMPATIBILITY_0.4.223.md). Verify non-default Group/Priority across repeated thumbnail updates and save/reopen, plus grouped normal native Undo. Managed adapter/Undo regression checks do not replace this native acceptance.

Current authorised hardening: [0.4.222 conflict isolation and late patch detection](CONFLICT_HANDLING_0.4.222.md). Managed conflict/Undo regression checks and Release/UI builds are required; native grouped acceptance remains separate from the earlier accepted Undo baseline.

Current investigation: [0.4.220 bike-rack timing and intermittent failure](BIKE_RACK_TIMING_0.4.220.md). The latest grouped run is [accepted with11 completed Undos and timing evidence](evidence/bike-rack-acceptance-0.4.220/README.md). The historical intermittent failure was not reproduced; this is not a causal proof of resolution. [0.4.221](BIKE_RACK_OBSERVER_0.4.221.md) corrects diagnostic bookkeeping only.

The latest focused source review is [0.4.251 diagnostics, performance and compatibility review](RELEASE_REVIEW_0.4.251.md), extending the [0.4.218 security, stability and release review](RELEASE_REVIEW_0.4.218.md). Its remaining packaging, native persistence and compatibility checks apply before a new distribution. The 0.4.180 documents below describe the last packaged release, not current-source readiness.

Use [0.4.180 release readiness](RELEASE_READINESS_0.4.180.md) for the current component selection/UI/installer release and pending native acceptance, [0.4.173 release readiness](RELEASE_READINESS_0.4.173.md) for accepted TAB UI recovery and component-removal refresh, [0.4.171 release readiness](RELEASE_READINESS_0.4.171.md) for replacement/source-mapping fixes and the user-accepted replacement Undo, [0.4.164 release readiness](RELEASE_READINESS_0.4.164.md) for the launcher update, and [0.4.163 release readiness](RELEASE_READINESS_0.4.163.md) for the current single-action Undo and Phase 5 grouped acceptance. Historical multi-history/Redo checks below do not apply to the current supported release scope.

# Validation plan

Phase 1: inspect project XML, inert source, document links, evidence hashes and absence of deployment/runtime hooks. The inert scaffold build subsequently passed with SDK 8.0.425 and zero warnings/errors. No destructive tests performed.

Phase 2 disposable asset matrix: simple component; component with dependencies; read-only asset; different prefab categories; cancel/close; confirm once; repeated click; selection/asset switch while dialog open; editor exit/load/dispose; interception error; other mods. Compare full component/settings/entity counts before and after cancellation. Confirm must match vanilla removal, including cascades. Unsupported builds must disable the capability and explain why.

Recovery acceptance per supported operation: complete before/after data equality; no shared mutable snapshot collections; correct underlying index with search/pagination; null/empty lists; parent-child graphs; transforms/probability; external prefab references; network nodes/edges/lanes; area geometry; effects; save and reopen; multiple undo/redo; redo branch clearing; history depth/memory; world/asset invalidation. Compare semantic data and references, not merely visual appearance.

Fault injection: failed capture, missing prefab, stale Entity version, partial creation, failed serialization/save, queued ECS work and interrupted callback. Verify no orphan/duplicate entities and prove rollback/recovery behavior before enabling destructive use.

Existing module regression: preserve colour groups/bindings/randomness/alpha/external settings; Multi-Save queue, lock handling, native dialogs, cancellation and stale-generation callbacks. Test sources individually before shared service adaptation.
