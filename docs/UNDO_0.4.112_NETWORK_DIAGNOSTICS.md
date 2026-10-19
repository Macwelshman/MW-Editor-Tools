# Undo 0.4.112 — precise network mismatch diagnostics

VERIFIED-RUNTIME: the user reports network visually passed and fence/lane failed. The 0.4.111 session log shows Invisible Car Path - 1xTwoway capture and restoration followed by timeout at 20:11:39.988 on generic graph/source mismatch. A subsequent attempt was refused by the fail-stop guard. Thus the log does not establish final network validation or a separately captured fence failure.

VERIFIED-STATIC: this release adds bounded exact diagnostic reporting without relaxing recovery checks. Restoration distinguishes source-field mismatch, typed graph mapping, finite local-cache mismatch, surviving source containment and strict final graph mismatch. Graph diagnostics distinguish count, root memberships, owner, edge endpoint, lane path bits and connectivity failures. Typed candidates report node transforms, world/local curves, lengths, seeds, container fields, entity identities and path-node bits. Detailed explanations are evaluated only at timeout. Diagnostics preserve successful mappings and never alter snapshots or native entities.

VERIFIED-STATIC: Release zero warnings/errors; 464 focused assertions pass (Phase3Safety 259, BulldozerSafety 47, NestedRecoveryRunner 133, StandaloneNestedSafety 25). Four added checks cover successful, graph-count, typed-geometry and connection explanations. UI production build and TypeScript pass.

UNPROVEN: this is a diagnostic package, not a confirmed fence or network fix. Full lane/network native acceptance remains outstanding; do not treat visual return as completion. Foliage neighbour fade remains unresolved.

Next grouped observation is deliberately reduced: in a fresh disposable session, delete the fence first, press Ctrl+Z and wait three seconds. Report the result before further deletion if it fails. This isolates fence behavior from the network fail-stop condition. No computer use.
