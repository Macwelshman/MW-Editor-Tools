# Retain copied foliage during replacement — 0.4.155

VERIFIED-RUNTIME (user report): 0.4.154 effect copy/paste/replace-all passes. Replacing foliage causes the originally copied item to fade. The log records successful FlowerBushCity01 source-list replacements; it does not record the native matcher or rendering transition.

HYPOTHESIS: native SubObjectSystem nearest same-prefab matching borrows the copied source when processing a new replacement descriptor. The earlier accepted foliage Undo correction supplies an exact-placement candidate to avoid this matching path. Recorded static evidence: docs/UNDO_0.4.116_STAGE_FINISH.md and docs/evidence/undo-0.4.108-foliage-matching.txt.

Replacement now preflights leaf Plant/Tree replacement archetypes and current owning instances. When an owner has retained instances of the replacement prefab, every changed destination requires a unique settled live source row. A fresh native-archetype candidate is created at the destination using its exact transform/local attachment/probability/group/index and seed, with detached typed foliage/colour values from the retained prefab instance. Destination surface/elevation presence is preserved. Created/Applied/Updated schedule native initialization. Candidates are linked to the owner before the existing queued prefab regeneration. No existing donor is written or moved by this code.

The correction covers single/batch ObjectSubObjects replacement through browser and viewport paths. It is bounded to 256 candidates and leaf foliage with supported archetypes and no source companions. Unsupported companion/archetype cases refuse before source writes; owners without a retained same-prefab instance use existing native creation. Synchronous failure removes only fresh candidate entities/links before restoring the complete source list and requeuing refresh. Deferred failure cannot promise a full scene rollback and stops further replacement with reload guidance.

The system checks original donors retain exact entity identity, owner link, prefab and transform after candidate initialization over three settled frames. Other replacements wait for this check. Failure or timeout stops further replacement.

VERIFIED-STATIC: Release/UI builds pass. ReplacementFoliageSafety executes actual candidate-creation/cleanup source using managed doubles: fresh identity, destination pose/seed/index, ownership, source pose unchanged, optional surface, typed vegetation/colours, native initialization tags, linking, idempotent cleanup and pre-/post-link failure cleanup. Existing hover target/path checks also pass. Native scheduling, visual fade removal and persistence remain UNPROVEN.

Acceptance: copy one foliage item, replace another while watching the original source, then replace-all on another matching group. Require no fade/movement of the copied source, correct destination placement and count, candidate/retained-donor success messages, then save/reopen a disposable copy.
