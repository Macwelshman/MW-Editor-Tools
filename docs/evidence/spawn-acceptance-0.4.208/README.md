# 0.4.208 Restricted Spawn Location acceptance

VERIFIED-RUNTIME 2026-10-23: user reports first visual restoration, then subsequent action blocked. MW Mixed Test Components, target Restricted Spawn Location Entity(53035:1).

11:58:04.770 deletion recorded; 11:58:05.880 spawn/access restoration starts; 11:58:05.901 retained reference map refused in root network typed mapping; 11:58:07.461 next native bulldozer Apply skipped while recovery pending; 11:58:08.663 timeout and reload-without-saving warning. No completed Undo is recorded. The blocked deletion preceded timeout; behaviour of every possible action after timeout is not established by this log.

Diagnostic shows first unmapped expected Connection Lane Entity(53086:5), curve 10.73693,511.9453,31.45362 to 7.658996,511.9453,31.5554, length3.079617. A candidate Entity(52946:149) has exactly reversed four world controls and equal length. Both have secondary=true, connection=true, Pedestrian|Inside, restriction=Entity(52957:1), no cache/container/seed. Original endpoint owners 53035/53038; candidate owners 53036/53038. Another candidate connection has a slope to height511.9388, matching one original marker's recorded elevation, so that slope alone is not evidence of corruption.

VERIFIED-STATIC limitation: ExplainMismatch reports first still-unmapped expected item after TryMap refusal, which need not be the item that caused an earlier survivor-reservation failure. The diagnostic does not print the full anchor correspondence or all expected connection witnesses. Therefore the exact failed predicate is UNPROVEN. HYPOTHESIS: a reused generated lane identity or whole-graph anchor correspondence prevents the reversal candidate from being considered/accepted. Do not weaken validation or declare this solely a stale busy flag without proving the graph.

Native acceptance of 0.4.208 failed despite visual marker return. No code or installation changed during this investigation. Reload the disposable editor session without saving before further tests.
