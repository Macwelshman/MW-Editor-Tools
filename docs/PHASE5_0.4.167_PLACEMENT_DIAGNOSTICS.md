# 0.4.167 - repeated viewport refusal diagnostics

VERIFIED-RUNTIME: the user's 18:18 screenshot and current 0.4.166 log show repeated wall paste refusals at 18:17:05, 18:17:28, 18:17:37 and 18:17:40. The log records a valid copied Stucco wall and target entities, but not their live/owner/cache transforms or source-row comparison values. The 0.4.165/166 correction has not resolved this reported case. Evidence: `evidence/viewport-prop-0.4.166-refusal.txt`.

HYPOTHESIS: remaining causes include live/source position drift, quaternion representation, or parent mesh/probability/group mismatches. The available log cannot distinguish them. No tolerance increase, source-index fallback or inferred target is justified by that evidence.

This candidate adds bounded detailed logging whenever object source resolution refuses: target and owner identities, asset and prefab names, live and owner transforms, cached placement/settings/index, projected placement, and up to eight nearest same-prefab source rows with per-field deltas. Quaternion deltas include the opposite-sign representation. The UI no longer suggests repeatedly waiting for a mismatch that has already persisted.

The projection is factored into one shared read-only implementation for matching and diagnostics. Matching predicates, safety refusals, mutation behavior, foliage/network/area changes and shared Undo behavior remain unchanged. Logging cannot replace the original refusal with an exception from diagnostic collection.

VERIFIED-STATIC: Release/UI builds, 24 managed suites and nine interface scripts pass. The projection harness includes 56 policy/diagnostic checks plus 10 actual network-adapter checks, including bounded logs, late repeated-row candidates, live/projected/settings fields and invalid-data reporting. The production assembly passes the native-buffer guard. Shared BulldozerRecovery/History implementation files have no changes. The existing NU1900 metadata warning remains.

UNSUPPORTED/UNPROVEN: this is a diagnostic candidate, not confirmation of a repair for the latest native wall case. A single repeat of that failing wall paste is needed to capture the missing values. No computer use or publishing.
