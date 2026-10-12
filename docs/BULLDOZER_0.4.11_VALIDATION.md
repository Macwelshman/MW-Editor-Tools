# Version 0.4.11 — corrected bulldozer trace target

VERIFIED-RUNTIME (user): Sub-element mode deleted grill01, grill02 and grill03 on Test; ordinary list Undo works. 0.4.10 trace captures ObjectSubObjects counts 7, 6, 5 before successive actions, but labels actions denied and discards pending observations. This proves the diagnostic result is inconsistent with the observed asset mutations, not that native deletion was denied. Last after-image was not captured.

VERIFIED-STATIC: Game.dll MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. BulldozeToolSystem.Apply calls virtual ToolBaseSystem.GetAllowApply at IL_0001; BulldozeToolSystem declares an override that calls OriginalDeletedSystem.GetOriginalDeletedResult at IL_0007 and returns at IL_000f (evidence/inspection.txt). The diagnostic patched the base declaration, so its Allowed callback did not observe this override. Corrected to the derived declaration; all patch target lookup now uses DeclaredOnly. Compiled target independently verified with Cecil (bulldozer-0.4.11-target-check.txt).

VERIFIED-STATIC: Release zero warnings/errors; UI build/type checks pass; 14 diagnostic lifecycle, 22 snapshot/history and 11 constructor-bypass lifecycle regression assertions pass. No asset/entity mutation or bulldozer history entries introduced. This remains a read-only diagnostic; runtime timing and recovery UNPROVEN.

Retest on a fresh duplicate of Test:
1. Restart CS2. Choose Sub-element.
2. Bulldoze grill01, wait two seconds, then grill02 and grill03 with the same waits.
3. Report the diagnostic message shown in History & Recovery and tell Codex when finished for log inspection.
4. Hold off on Everything until the accepted-action → matching SaveInstance trace works. Main/standalone objects need a separate recovery scope.

Bulldozer Undo remains UNSUPPORTED in this diagnostic version. Existing list Undo remains available.
