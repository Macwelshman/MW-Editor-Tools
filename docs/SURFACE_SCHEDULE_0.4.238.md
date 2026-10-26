# Correct native surface-refresh scheduling — 0.4.238

VERIFIED-RUNTIME (logs): the 0.4.236 session copied Tiles Surface 02 and recorded source row 6 replacement at 00:49:56 on 2026-10-24. Neither material refresh submission nor completion was logged. The user reported failure. Original log retained in `evidence/surface-schedule-0.4.238/native-0.4.236.log`.

VERIFIED-STATIC: Game.dll MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. Game.Common.SystemOrder.Initialize loads phase 27 at IL_11ae and registers AreaBatchSystem at IL_11b0. The enum identifies 27 as PreCulling, while Rendering is 14. The previous relative registration incorrectly used Rendering. Game.UpdateSystem.UpdateBefore<T,Other> passes its explicit phase into relative Register at IL_0039; matching the native consumer phase is required. Narrow scheduling IL and native phase registration evidence are retained alongside the log.

VERIFIED-STATIC: the SurfaceRenderSystem relative registration now uses PreCulling. The existing retained-entity replacement, geometry/settings/reference safeguards and native batch completion checks are unchanged. An independent UI-clock deadline detects a missing or incomplete render callback even if the callback is never scheduled; it fails closed with feedback instead of waiting forever. No changes to native allocation fields, search trees, recovery implementation or outline rendering.

VERIFIED-RUNTIME (managed): the new exact-build scheduling regression reads the native registration with Cecil and compares it to the production registration. It verifies PreCulling (27). Surface transaction/render tests pass 59 assertions, including the missing-callback watchdog. UI checks 32, replacement Undo 45, Phase3 1,204 and outline 22 pass. Release/UI builds and focused whitespace checks pass. Build retains NU1900 advisory-feed warning; no new dependency audit claimed. Native scheduling/render execution is not exercised by managed tests.

The package includes the staged 0.4.237 toolbar layout: fixed Start/Stop position, inline Element type label and native dropdown appearance.

VERIFIED-RUNTIME (deployment check): Cities2 is running, so installation is deferred. 0.4.238 is staged at `../outputs/test-package-0.4.238/MWEditorTools`; conditional backup/hash-verifying installer: `../outputs/install-surface-schedule-0.4.238.ps1`. This supersedes the staged 0.4.237 package. Installed version remains 0.4.236.

UNPROVEN: native surface material update and Undo completion. After installation, test Replace/Undo then Replace all/Undo on a duplicate asset, confirming the texture changes, geometry stays fixed and accepted outlines remain intact. No game interruption or publishing performed.

VERIFIED-RUNTIME (installation follow-up): user confirmed game closed and requested installation. Cities2 absence verified; 0.4.238 installed with nine-file rollback backup and matching DLL/UI/CSS hashes. See outputs/installation-0.4.238.txt and evidence installed-hashes.csv. Native material/Undo acceptance remains pending.
