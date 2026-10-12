# 0.4.54 Read-only nested prop discovery

VERIFIED-STATIC: adds a one-time inventory after 120 editor frames using an ObjectData/PrefabData query and the public PrefabSystem.TryGetPrefab API. Lists up to 20 alphabetically sorted candidate names in the mod log, plus scanned/candidate counts. Definition checks require 2–8 distinct prop identities, child depth at most four, deterministic child links, and exclude buildings, ActivityLocation, networks, areas and effects. Generated MW Nested fixtures are excluded. This reads loaded definitions only; it does not clone, place, delete, arm recovery, save or modify prefabs/entities. Discovery exceptions only log a warning and do not clear recovery/history. No new patches, systems or UI controls.

Candidate definition eligibility does not prove typed live entity eligibility or recoverability. Existing placement/ownership/reference checks remain mandatory. Discovery and ordinary nested recovery are UNPROVEN-RUNTIME. Prior saved grill fixture remains VERIFIED-RUNTIME at 0.4.52.

Release and UI builds passed; all 453 existing safety/runner assertions and UI settings render passed. These tests do not execute the new native inventory. Public API compatibility is compiler-verified against installed assemblies; prior recorded native assembly evidence is reused.

Next: open the editor and any disposable test asset, wait a few seconds, leave CS2 open. Read the inventory from the log and identify a specific candidate before requesting placement or deletion. No deletion requested for this build. Phase 5 remains paused; Phase 6 and publishing remain unauthorised.
