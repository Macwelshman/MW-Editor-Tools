# Undo 0.4.110 — native network endpoint selection

VERIFIED-RUNTIME: both lane/network attempts on 0.4.109 stopped before capture, at 22:41:18.717 and 22:41:24.818, with "Delete one bound lane or network segment at a time." No network Undo entry was created. The installed log did not record the raw control-point identities, so their exact runtime shapes are UNPROVEN.

VERIFIED-STATIC: Game Version=0.0.0.0, MVID=e15551e9-3dea-4855-8dde-1da3cea7d7ef. BulldozeToolSystem.SnapJob.Execute invokes CreatePath and AddControlPoints at IL_0195 and IL_019f. CreateDefinitionsJob.Execute accepts network control-point paths (IL_001b-0066), and resolves connected edges between consecutive original node identities (IL_010f-021b). A single segment can therefore have multiple original control-point identities. Evidence: docs/evidence/undo-0.4.110-network-points-native.txt.

The corrected capture resolves two directly owned endpoint nodes to their unique directly bound connecting edge. A direct edge, repeated edge point, or one edge accompanied by its own endpoints also resolves to one segment. Disconnected endpoints, ambiguous parallel edges, generated-lane targets, junction-only points, multiple edges/segments, missing entities and different owners remain refused. All original full-graph deletion, source-list, companion and native regeneration guards stay mandatory. Control-point identities are now logged before resolution for any remaining capture mismatch.

VERIFIED-STATIC: clean Release build; production UI and TypeScript pass. Phase3Safety 247 assertions includes 12 new actual-source target-selection checks. BulldozerSafety 47, NestedRecoveryRunner 133 and StandaloneNestedSafety 25 pass (452 focused assertions this correction), plus UI recovery render regression. Existing LastPropHistory and LifecycleSafety checks are unchanged from 0.4.109.

UNPROVEN: native lane/network Undo acceptance on 0.4.110 awaits user testing. This correction fixes the observed pre-capture point-count refusal; it does not establish that every later native recovery stage passes. Bound-foliage fade remains unresolved. No computer use performed.
