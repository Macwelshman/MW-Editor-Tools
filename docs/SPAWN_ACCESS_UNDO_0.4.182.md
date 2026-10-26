# Spawn/access companion deletion Undo — local test 0.4.182

User-authorised implementation, 2026-10-21. No game files changed and no computer use or publishing.

VERIFIED-STATIC: `SpawnAccessImage` captures every field of `Game.Objects.SpawnLocation`, `Game.Routes.TakeoffLocation`, `Game.Routes.AccessLane` and `Game.Routes.RouteLane`. The nested schema accepts these typed components and their `Game.Objects.Marker` tag. Unrecognised components still refuse capture. Ordinary prop capture and area companion capture use the same image.

The image is a witness of native state. Undo uses the existing native descriptor restoration and regeneration path. It does not clone, clear or overwrite any lane/restriction reference. Every existing nested comparison and restore preflight now requires full companion equality, including flags, positions, group, component/tag presence, exact versioned reference targets and each target's owner, prefab, native lane endpoints and curve. Null native links remain null. Missing, temporary, deleted, self-referencing and wrong-type lane targets refuse capture. Nonfinite positions refuse capture.

The owner's native `SpawnLocationElement` buffer is also witnessed: for each spawn marker the native regenerated buffer must point to the actual current marker with the same multiset of spawn types. Marker IDs may change through native regeneration; the old marker ID is never written back into the owner's registry. Registry and reference equality are required before restoring ordinary typed prop values and during the existing exact post-restore validation. Pending native initialization may settle before the normal validation timeout.

SUPPORTED IMPLEMENTATION SCOPE: deletion of already supported props/groups and areas on an asset whose spawn/access companions retain their exact lane/restriction targets through native regeneration. Existing network topology and external-reference safeguards remain. Moving spawn/access markers remains refused. Whole-building/component deletion is not added.

UNSUPPORTED/UNPROVEN: remapping a replaced lane/restriction to a new entity; ambiguous or external network topologies previously refused; arbitrary third-party incoming references; game scheduling and save/reopen acceptance on the user's SR buildings. A changed lane entity is refused even if it looks similar. This is deliberately bounded reference preservation, not general graph reconstruction. Existing fallback/error reporting remains in place.

VERIFIED-STATIC native evidence: Game.dll SHA256 `AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A`, MVID `e15551e9-3dea-4855-8dde-1da3cea7d7ef`. See [native evidence](evidence/spawn-access-0.4.182/native.txt) for exact fields and serialization methods with IL offsets. For example `Game.Routes.RouteLane.Serialize<TWriter>` loads start/end entities at IL_0003 / IL_0016 and curve positions at IL_0029 / IL_003c. This confirms they require typed reference preservation rather than marker-only handling.

Validation: focused actual-source managed tests cover unchanged/fresh markers, detached values, target deduplication, changed owner/prefab/lane endpoints/curve, stale entity versions, deleted/temporary/mistyped targets, orphan route schema, invalid floats, null links and native registry reassociation/type changes. Existing Undo, lifecycle and component transfer suites and all 14 UI suites pass. Managed doubles do not establish native acceptance. Build succeeds with the existing NU1900 vulnerability-feed warning.

Grouped native acceptance on disposable duplicates: delete/Undo an ordinary prop, an area/surface and a spawn/access marker if the native tool allows it; verify placements and every companion reference/registration, then save/reopen. Include a second deletion and an asset switch. A separately changed lane/reference must refuse recovery. No original assets should be overwritten for these checks.

Installation: 0.4.182 installed with Cities2 fully exited; previous mod files backed up and verified, then all three installed runtime hashes verified. See evidence/spawn-access-0.4.182/installation.txt. No release published.
