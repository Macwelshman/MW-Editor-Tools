# Bound deletion owner prefab transition — 0.4.195

VERIFIED-RUNTIME: the preserved 2026-10-22 13:07 log shows a retained Pedestrian Access Location still pointing to owner Entity(52651:1), while that owner's PrefabRef changed from Entity(22096:1) to Entity(52636:191). All logged placement, lane, effect, presence, elevation and light checks passed. The exact prefab witness equality prevented recording.

VERIFIED-STATIC: BoundCompanions now captures the exact owner and its prefab entity. After the full companion graph comparison and OwnerMatches resolve the live owner to the guarded editable asset, an owner-specific comparison scope carries the captured/live prefab pair. SpawnAccessImage.Reference permits that pair only on that exact unchanged root entity with no parent owner, lane or curve. Other witness identity, ownership, component presence, registration, scalar and lane geometry rules remain unchanged. It never remaps prefab entities generally and never writes companion data or references. Scope metadata participates in the three-frame stability gate.

The same scope is produced by BoundRecordMap for pre-Undo and restoration checks, including the deleted node's regenerated companion. Before and after snapshots retain their original witnesses; comparison does not mutate them. Later owner/asset, payload or graph edits still invalidate Undo.

Validation: managed/UI builds pass (existing NU1900 feed warning); 37 actual-source spawn/access checks, 33 bound completion orchestration checks, 452 Phase3 checks, 25 bound foliage and 97 area move checks pass. Tests cover the logged same-owner prefab transition, immutable images/live references, unrelated owners, unexpected initial/third prefab, schema/lane and scalar changes, transition proof construction and stable-gate churn.

UNPROVEN: native mixed-asset deletion and Undo scheduling after this fix. Acceptance: delete one prop and Undo on the mixed asset, then one area and Undo; confirm companions/references and retained values remain intact. Do not save disposable test edits.
