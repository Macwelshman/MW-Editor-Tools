# MW Editor Tools 0.4.255 — performance-test compatibility

Adds explicit support for CS2 Steam `performance-test` build 26269843 alongside the previously verified stable build. Component confirmation, list/native Undo, filtered targeting, inspector-edit cleanup and custom outlines now accept the audited beta assembly profiles. The associated surface replacement, component heading, localization, workspace and existing Asset Icon Creator guards use the same verified Game identities.

Recovery, outline rendering and area allocation reject mismatched dependency profiles. Unknown game builds still refuse the guarded operations. Confirmation, ownership, snapshot, single-action history, conflict isolation and completion behavior are unchanged. Targeting startup errors no longer imply that Undo initialized successfully.

VERIFIED-STATIC: 39 selected native bodies exactly match recorded stable IL; private field types, rendering job fences, outline properties, area release routines and PreCulling scheduling were checked against installed assemblies. Release/UI builds, 36 managed suites, 18 UI scripts and the native API contract checks pass. The existing NU1900 advisory-feed warning remains.

UNSUPPORTED/UNPROVEN: native acceptance on this beta. On disposable asset copies, check startup, confirmation cancellation/approval, list/native deletion and move Undo, and targeting/outline cleanup during an inspector edit. This update does not expand the supported editing or recovery operations. No public release is made by installing this local test build.

See [the API audit and acceptance checklist](PERFORMANCE_TEST_0.4.255.md).
