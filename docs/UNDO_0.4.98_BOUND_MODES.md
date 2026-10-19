# 0.4.98 bound prop capture modes

VERIFIED-RUNTIME: The 0.4.97 session log records standalone Grill01 capture and successful Undo at 00:05:24–00:05:25 on 2026-10-14. No bound prop capture was recorded. An area capture was separately refused for Game.Objects.Plant. A whole building deletion was refused as expected.

VERIFIED-STATIC: Automatic bound preflight and BeginNestedGroup each required Everything mode, although the outer capture route accepts SubElements as well. Thus SubElements bound deletions could not enter automatic capture. Both gates now accept SubElements and Everything. Main mode remains excluded. All owner, topology, descriptor, snapshot and deferred native completion checks remain in place; nested-child targets outside the supported direct-owner route remain unsupported.

HYPOTHESIS: The missing mode route accounts for the user's latest bound failure. The prior log does not record deletion modes, so that specific cause is not runtime-confirmed. New Apply logs include mode and automatic setting, and unarmed bound capture is explicitly logged. Unsupported exceptions during automatic preflight now report their reason instead of being swallowed.

VERIFIED-STATIC: Release build has zero warnings/errors; production UI build passes. Relevant suites pass 336 assertions (BulldozerSafety 39, Phase3Safety 153, NestedRecoveryRunner 132, LastPropHistory 12), including refusal-message delivery without enabling unsupported recovery and compiled native collection audit.

UNPROVEN: 0.4.98 native bound recovery. No computer use or in-game test performed. Package prepared; installation deferred while Cities2 is running. Installation must preserve rollback and verify copied hashes once fully exited.

VERIFIED-STATIC installation: 0.4.98 installed after Cities2 exit check; all 5 package hashes and 106 rollback file hashes verified. Rollback: outputs/rollback/MWEditorTools-before-0.4.98-20261014-000910/MWEditorTools. Native bound acceptance pending.

