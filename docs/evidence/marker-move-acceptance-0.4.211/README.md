# Marker move acceptance — 0.4.211

VERIFIED-RUNTIME 2026-10-23, MW Mixed Test Components. Full mod log preserved alongside this note. User reports subsequent marker move/Undos worked; first attempt possibly failed.

Seven marker move Undos completed full source, graph and companion validation:
- Pedestrian Access Location: 13:20:01.244, 13:20:13.192, 13:20:27.221.
- Restricted Spawn Location: 13:20:07.763, 13:20:17.893, 13:20:43.877, 13:20:48.092.

FireBigVFX effect move Undo also completed at 13:20:31.740. Readiness-only entries (including the final move) are not counted as completed Undos.

The first Restricted Spawn Location move capture (13:19:44.956) timed out at 13:19:52.233: retained topology typed-mapping survivor reservation failed for FenceResidentialHigh01 Entity(52976:1). No Undo entry was recorded. Later actions were not blocked.

VERIFIED-RUNTIME diagnostic: the retained fence kept the same entity, endpoints, local curve cache, seed (9364), length (24.00001), ownership and shown settings. Interior world control heights changed: b.y 511.9461 to 511.9455, c.y 511.9451 to 511.9452. The b delta is approximately 0.6 mm at printed precision. Current strict comparison rejects that change; the special regenerated-fence allowance also requires a changed entity identity.

HYPOTHESIS: native fence control-height normalization caused the first capture refusal. This is not proof that arbitrary retained-curve changes are safe. A future correction needs a narrow source/local-cache equivalence proof while preserving complete reference and companion validation.

Acceptance is partial: repeated moves of both marker types are runtime verified, but initial capture has a reproducible diagnostic failure in this log. Marker deletion, other topology, rotations and persistence are not independently established by this session. No code, package or installation changes made during this review.
