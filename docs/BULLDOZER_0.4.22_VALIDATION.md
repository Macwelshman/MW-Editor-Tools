# 0.4.22 typed standalone Main capture

VERIFIED-RUNTIME: 0.4.21 refused capture before any recovery writes with Unsupported snapshot type Game.Objects.Transform. Evidence: evidence/bulldozer-0.4.21-main-runtime.log. PrefabSnapshot intentionally excludes ECS component structs; do not relax its general allowlist.

Correction: isolated MainPropData typed value snapshot (Game.Objects.Transform, Common.PseudoRandomSeed, Objects.Surface, MeshColor[], CustomMeshColor[]). Constructor and getters independently copy colour arrays. Clone produces a fresh image; exact typed values and array sequences compared. Missing versus empty buffers retained. MainPropImage uses this typed image; prefab reference validity remains required. No raw Entity serialized in snapshot, no Main restoration enabled. Original Entity is only a bounded deletion observation identity.

VERIFIED-STATIC: Release zero warnings/errors; production UI;29 actual-source Phase3 assertions including7 new typed standalone checks (roundtrip, array isolation, seed/surface/transform mutation detection, null-empty buffer distinction,256-entry budget). Native live Main capture confirmation pending. Sub-element prefab snapshot/history unchanged.

Repeat disposable standalone Grill01 Main deletion, wait several seconds, reply finished. No arming, no Main Undo yet. Expected capture roundtrip verified plus detached candidate valid after original entity disappears. Strict unknown-component and nonempty-related-buffer refusal preserved. Next live recreation remains UNPROVEN pending native marker/barrier review.

VERIFIED-RUNTIME 0.4.22 typed Main capture PASS: docs/evidence/bulldozer-0.4.22-main-runtime.log confirms standalone Grill01 transform/seed/surface/colour image roundtrip verified (seed24160,MeshColor1,CustomMeshColor0), native Apply accepted, original entity absent by nextframe, detached candidate valid=True after confirmed deletion. No live Main restoration performed. Nonempty custom-colour preservation not runtime validated; typed array isolation tested statically. Next native creation marker/barrier review then bounded simple standalone Main live recovery; use new entity IDs, preserve typed image, reject unknown components/nonempty related buffers. Installed0.4.22 retained; validated Sub-element history preserved; Everything/last/nested still unsupported.


