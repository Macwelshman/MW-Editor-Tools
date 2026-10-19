# Undo 0.4.114 — generated lane curve precision

VERIFIED-RUNTIME: fence visually returned on 0.4.113, but final validation still timed out. The precise mismatch is the retained Invisible Car Twoway Lane 3 generated curve: original b.y=511.9462/c.y=511.945, regenerated b.y=511.9459/c.y=511.9453, identical endpoints and length 30.60623. This exceeds the source-edge allowance while remaining below the half-millimetre cap. The subsequent network attempt was blocked by the fail-stop guard, not independently restored. See docs/evidence/undo-0.4.113-generated-lane-runtime.log.

VERIFIED-STATIC: bound restoration world curves on generated Game.Net.Lane entities use eight scaled float machine epsilons, still capped at 0.0005 world units. Source edges and nodes keep four; source capture/deletion comparisons, local cache values, curve lengths, references, exact topology/path bits, seeds and ownership retain their prior checks. This reflects the additional native lane curve derivation after parent regeneration. No geometry is rewritten by the tolerance change; guarded cache/seed restoration and full final validation remain mandatory. Every other caller retains strict default comparisons.

VERIFIED-STATIC: clean Release/UI/TypeScript; 475 focused assertions pass (Phase3Safety 270, BulldozerSafety 47, NestedRecoveryRunner 133, StandaloneNestedSafety 25). Four new checks reproduce observed 0.3mm generated lane rounding, enforce the cap, retain strict capture defaults and preserve the smaller source-edge allowance.

UNPROVEN: native final lane/network acceptance awaits user testing. Visual fence return is confirmed; strict completion has not yet been confirmed. Foliage neighbour fade remains unresolved. No computer use.

## 2026-10-15 — 0.4.114 native acceptance
VERIFIED-RUNTIME: user confirms both fence and network Undo now work. Native log confirms complete graph/companion validation at 20:23:53.403 and 20:24:12.173, with guarded local-cache repair and nine retained prop groups unchanged. Acceptance covers these tested bound segments on MW Mixed Undo Test; broader network/lane prefab coverage remains unproven. Foliage neighbour fade remains unresolved. No new runtime changes or installation.
