# Version 0.4.12 — bulldozer target ownership diagnostic

VERIFIED-RUNTIME: user finished the 0.4.11 Sub-element retest. Its log records diagnostic startup but no action captures. It does not establish whether a capture was skipped for missing inspector asset, Apply never ran, or some other gating condition. No successful synchronization trace is claimed.

VERIFIED-STATIC: 0.4.11 BeginApply silently returned if InspectorPanelSystem.m_ParentObject was null/read-only. 0.4.12 logs every in-scope attempt before that selection gate and resolves candidate owning prefab from BulldozeToolSystem.m_ControlPoints original entities and bounded Game.Common.Owner chains (cycle detection, maximum 32 links). Multiple distinct owners, missing/destroyed entities, missing prefab references and read-only assets are refused. Inspector parent is logged for comparison but no longer required. Later matching SaveInstance/asset liveness checks use the observed owner instead of current inspector selection. Main/Everything deletion of that owner still cannot yield a restoration claim.

Installed Game.dll identity/evidence remains the recorded Game 0.0.0.0 MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. Native Apply reads/clears m_ControlPoints (inspection.txt IL_00b4/00b9); native ControlPoint.m_OriginalEntity and Game.Common.Owner.m_Owner metadata and reads are recorded in detail.txt. HYPOTHESIS: the topmost valid owning entity identifies Test in this editor case; runtime trace must establish that mapping. This diagnostic performs no entity/prefab writes or Undo entry creation.

Checks: C# Release zero warnings/errors; pinned UI/type build passes. 14 observation lifecycle and 22 snapshot/history regressions pass. Native control-point access/ownership mapping still UNPROVEN.

Minimal retest on a fresh duplicate of Test:
1. Choose Sub-element. Delete one grill and wait two seconds.
2. Tell Codex when finished so it can read the log. Do not repeat all three deletions unless requested after this capture succeeds.
3. Bulldozer Undo is still unsupported; ordinary list Undo remains available.
