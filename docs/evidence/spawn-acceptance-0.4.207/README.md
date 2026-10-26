# Restricted Spawn Location acceptance — 0.4.207

VERIFIED-RUNTIME: 2026-10-23 session, MW Mixed Test Components. User observed first deleted marker return, then next deletion did nothing. The log records one deletion/Undo attempt, not a completed successful Undo.

- 11:42:15.699: nested deletion recorded.
- 11:42:17.239: bound spawn/access restoration started.
- 11:42:17.264: root network typed mapping refused for Connection Lane. Original curve runs (-12.43059,511.9453,110.4058) to (-9.477299,511.9453,110.3995). Candidate has exactly reversed four control points and equal length 2.9533, with rebuilt object path-owner IDs.
- 11:42:19.286: next bulldozer Apply deferred while recovery pending; native deletion skipped.
- 11:42:20.041: retained prop/reference preflight timeout; warning instructs reload disposable session without saving.

VERIFIED-STATIC: AreaCompanionNetwork.TryMap requires forward SameWorldCurve and directed start/middle/end SamePath. The logged reversed lane cannot pass the current mapping. There is no successful completion message in this session.

HYPOTHESIS: native regenerated connection lane direction depends on rebuilt object ordering. Supporting that representation safely requires proving curve reversal together with corresponding path-node semantics, ownership, all membership/reference links, and uniqueness. Reversing geometry alone or skipping graph validation is insufficient. No recovery code changed in this investigation.

Native acceptance remains incomplete: restored visual marker does not establish restored companion/reference graph. Reload disposable test session without saving before another test.
