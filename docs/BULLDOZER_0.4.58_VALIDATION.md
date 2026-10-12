# 0.4.58 nested Placeholder marker

VERIFIED-RUNTIME: computer-use set up disposable Test 2 with Grill01 and Carport01, then clicked Start test. Refused Game.Objects.Placeholder before deletion or preparation. Entire two-node inventory captured in docs/evidence/bulldozer-0.4.57-computer-use-runtime.log. Both node component lists now covered by the nested schema. No saving or bulldozing occurred.

VERIFIED-STATIC: Game.dll Version 0.0.0.0, MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. Game.Objects.Placeholder is an empty value type: zero fields and zero declared methods. Evidence docs/evidence/bulldozer-placeholder-type.txt and outputs/placeholder-inspect/Program.cs. Nested capture now records marker presence and existing auxiliary validation requires identical presence at all preflight, retained, preparation and completion checks. No marker writes; Main capture unchanged.

Release build zero warnings/errors; UI build/settings render pass; all 464 existing assertions pass. Native Carport01 readiness/recovery remains UNPROVEN. Install only while CS2 closed with full rollback and hash verification. Resume computer-use setup/testing automatically after restart; do not ask user to repeat manual setup.

## Runtime result — 2026-10-11

VERIFIED-RUNTIME: assistant performed the entire disposable Test 2 setup using computer-use. Exactly one placed outer asset; cleared its 76 Object Sub Objects in memory only, added Grill01 plus Carport01, Carport local X=12, probability100/group0/parentmesh0. Start reached Test ready, adopted one two-node Carport01/RoofLightRandom01 group. Everything-mode deletion recorded one history entry. Automatic Undo -> Redo -> Undo PASS at 00:47:50.736, exact payload/native graph validation after three stable frames each, original deleted entities gone, referenced Carport01 payload unchanged. Restored list contains exactly Grill01 and Carport01 at X12; visually one carport returned. No saving. Evidence docs/evidence/bulldozer-0.4.58-computer-use-runtime.log. This verifies this exact ordinary two-node group, not arbitrary nested props.

UI defect observed: ListRecovery.BulldozerDiagnostic prioritises StartTestBlockReason when not Busy; switching to bulldozer clears inspector context.Asset so displayed Test ready/PASS is masked by Cannot start test even though nestedTestAsset remains armed and runner succeeds. Correct status priority for active/completed test without weakening Start guards next.

Startup: direct Cities2.exe launch failed platform initialization before editor; user explicitly authorised force closing that failed instance once. Launch using existing Steam-associated Paradox launcher Play succeeded and reopened directly in editor. Prefer that path for subsequent restarts. Automatic normal close/reopen is now authorised by AGENTS.md; force termination still requires permission.
