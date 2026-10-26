# Bound deletion diagnostic — 0.4.193

VERIFIED-RUNTIME: 0.4.192 session at 2026-10-22 13:01 repeatedly refused bound prop and area capture: retained values preflight failed for Pedestrian Access Location; companion=False, placement=True. Full log preserved under evidence/bound-diagnostic-0.4.193.

UNPROVEN: which spawn/access value or reference differs after native rebuilding. No field-level data exists in that log, so no safe reference restoration change can yet be justified.

Adds read-only diagnostic reporting of companion categories and individual spawn/access value changes, mapped entity IDs, registry types and reference witness comparison. Refusal logic is unchanged; no reference writes or relaxed matching introduced. The next grouped native check needs one deletion on the same mixed asset, followed by the mod log. This is a diagnostic build, not a confirmed fix.

VERIFIED-STATIC: managed/UI builds and 29 bound completion checks pass; spawn/access diagnostic check covers field-name reporting while changed fields remain refused.
