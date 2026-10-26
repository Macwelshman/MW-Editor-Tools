# Conflict isolation and late patch detection — 0.4.222

User-authorised IMPLEMENT scope: isolate startup conflicts and detect Harmony patches introduced after Undo attaches. No publishing, playset changes or computer use.

## Behaviour

VERIFIED-STATIC: `Mod.OnLoad` now isolates component confirmation initialisation and cleans up its partially installed hooks. A confirmation conflict or unsupported runtime no longer reaches the outer whole-mod teardown. Shared UI, recovery, Components, Replacer, Colours, Multi-Save and Options continue their existing independent startup paths. The existing protected-components UI flag remains false when confirmation installation fails; the log explicitly says vanilla component removal is unprotected. Recovery failure reporting no longer incorrectly promises that confirmation is installed.

VERIFIED-STATIC: `PatchConflictMonitor` records each successfully attached native Undo target. Optional move/area attachment rollback removes its targets from monitoring. Only the existing native Undo owner and list-recovery owner are accepted; no third-party owner was added to the allowlist. All Harmony patch categories are included through their registered owner metadata. Removal of the entire MW owner from a watched target is also refused.

Undo checks ownership before native deletion capture, object move/Apply, area recreate/Apply/Cancel and Undo/Redo dispatch. Routine callbacks, readiness and recovery ticks share a once-per-frame check; action boundaries force a fresh check even if another check ran in the same frame. The monitor does not patch Harmony itself, remove foreign patches, change patch order or swallow another mod's exceptions. Ordinary successful checks do not build diagnostic owner arrays/lists.

A detected conflict is latched for the lifetime of that recovery instance. The diagnostic names the affected method and foreign owner(s); a warning is emitted once. Undo and new capture are refused until restart with the conflict resolved. If no recovery is pending, ordinary native editing is left available without claiming MW recovery. If a recovery was already pending, MW callbacks stop without clearing captured data, companion snapshots or reference maps; the existing busy interlock continues blocking MW saves and further supported tool edits, and the status requests a reload without saving. There is no speculative rollback through newly conflicting hooks.

The shared native Undo subsystem is conservatively refused when any of its watched targets conflicts. Other independent modules remain loaded. A conflict discovered during an active recovery necessarily requires reloading that editor state; continued saving cannot be declared safe.

## Preservation and validation

VERIFIED-STATIC: no changes to companion capture/materialisation, source-row matching, entity/reference mapping, transform or height tolerances, native regeneration/completion gates, snapshot schemas, or successful Undo dispatch selection. Existing context invalidation/disposal semantics remain in place. Protection against non-Harmony detours, arbitrary external data mutation or patches arriving inside an already executing native call is not guaranteed by ownership monitoring. Existing data/reference validation remains necessary.

VERIFIED-RUNTIME (managed harnesses, not Unity gameplay): real Harmony patches added after attachment are detected for prefix, postfix, transpiler and finalizer categories. Tests cover same-frame forced checking, own/explicit compatible owners, removed MW owner, optional rollback, latched refusal, actionable status, retained busy interlock and one warning. Actual `Mod` source is tested with injected confirmation startup failure, proving independent module startup/disposal. Native tool hook harnesses cover idle pass-through without capture and blocked edits during an in-flight conflict.

Focused results and Release/UI build outputs are preserved in `docs/evidence/conflict-hardening-0.4.222`. Native cross-mod conflict injection has not been performed. Prior accepted Undo and save persistence remain the baseline, not a new runtime acceptance claim for this build. Next grouped native check: ordinary prop/effect/area and marker/bike-rack move/deletion Undo, followed by save/reopen where applicable; check the startup log for unexpected conflict refusals. Do not enable a known incompatible mod simply to test on valuable assets.

## Relationship to compatibility review

This addresses the startup-coupling and attach-only native Undo checks identified in `EDITOR_MOD_COMPATIBILITY_0.4.221.md`. Its wider compatibility limitations, standalone Components binding collision and need for pairwise native acceptance still apply. This is a local test build, not a newly published distribution.

## Checked build and installation

VERIFIED-RUNTIME (managed): 1,649 assertions across Phase2Safety37, LifecycleSafety22, Phase3Safety1,167, MoveUndoSafety44, EffectUndoSafety41, AreaMoveSafety100, BoundCompletionSafety77, SpawnAccessSafety55, BikeRackTimingSafety20, ListClearSafety37 and BulldozerSafety49. Release and UI builds pass. NuGet advisory lookup returned the existing NU1900 network warning; this is not a completed dependency-vulnerability audit.

INSTALL VERIFIED 2026-10-23: installed0.4.222.0 after elevated Cities2 absence checks. All three staged DLL/MJS/CSS hashes match the installed files. All nine previous installation files were backed up and hash-verified under `outputs/rollback/MWEditorTools-before-0.4.222-20261023-171814/MWEditorTools`. Hash evidence: `docs/evidence/conflict-hardening-0.4.222/installed-hashes.csv`. No game files, Harmony dependency, playset or published release changed.
