# Marker move acceptance — 0.4.212

VERIFIED-RUNTIME 2026-10-23: latest mod log ends 13:32:10.350. User reports first and repeated move/Undo tests for both markers appeared worse. This log contains two Restricted Spawn Location captures and zero completed marker Undos. No Pedestrian Access Location capture or Undo shortcut dispatch is recorded, so its specific outcome cannot be established from this session log.

1. Capture 13:31:25.653, timeout 13:31:33.088. Retained FenceResidentialHigh01 Entity(52977:3), owner Entity(52957:3), same full identity, edge endpoints, local curve, seed27664 and length24.04335. World b.y 511.942 to511.9455 (~3.5mm); c.y511.9372 to511.9452 (~8mm). Endpoints Y511.9453 unchanged. Recorded local b.y -0.003326416, c.y -0.008117676 unchanged. MarkerHeightWitness rejects these residuals beyond its one-millimetre ceiling. No Undo entry was recorded. This is greater drift than the0.4.211 first-capture example (~0.6mm); its different length/local geometry also means the logs do not establish that the new comparison itself caused a regression on identical input.

2. Capture13:31:41.155, timeout13:32:10.350. Refusal: marker move has unsupported generated connection topology, target Entity(53037:3). The source guard rejects zero incident lanes, more than two incident lanes, or a non-generated incident item. The log does not record the count/schema or whether before/after witness failed; exact branch is UNPROVEN. Do not assume a specific branching topology or relax graph preservation from this message alone.

Tool/selection changes occur during each pending capture. BeforeMoveStart and BeforeObjectApply block new actions while Busy; the second timeout has an interval of about29seconds. HYPOTHESIS: pending-capture guards explain why some attempted repeated actions were not separately captured. The log does not report each blocked interaction, so this is not independent proof of every attempted operation.

Result: native acceptance FAILED. Zero completed restorations means this log does not demonstrate companion/reference corruption during Undo, or validate restoration. The earlier0.4.211 session had seven completed marker Undos; the latest session has none. Preserve source/reference/companion safeguards. A further fix needs proof of native fence interior-height reconstruction from preserved source data, and precise before/after incident-topology diagnostics/support. Merely raising the height tolerance is not justified by these observations.

Review only: no source behavior, version, installation or published artifacts changed.
