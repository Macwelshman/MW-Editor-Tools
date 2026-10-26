# Fence Undo readiness and child-lane mapping — 0.4.232

VERIFIED-STATIC: direct fence deletion readiness now revalidates late marker reconnections against the immutable original capture and the verified surviving network graph. Connections outside that graph, changed marker metadata and unrelated source-row changes remain rejected. Other move/effect/area paths retain their existing after-images.

VERIFIED-STATIC: restoration can match a source-free child lane through its uniquely verified parent fence and recorded local source curve. This uses the existing bounded owner-space height witness, with mandatory topology, endpoint, horizontal geometry, typed metadata and reference checks. A pending nonfinite native local cache is permitted only during preflight; strict final validation still requires its repair. The source payload must match before this direct-deletion witness is enabled. No blanket tolerance increase or companion-data removal was introduced.

VERIFIED-RUNTIME (managed checks): Release and UI builds passed. Phase 3: 1,204 assertions; spawn/access: 60; bound completion: 77; move: 44; effect: 41; area move: 100; outlines: 22; targeting: 69; UI targeting: 25. These checks do not exercise Unity native regeneration. Build reported NU1900 because the package vulnerability advisory feed was unavailable; no fresh dependency audit is claimed.

VERIFIED-STATIC: all targeting/outline source hashes match the recorded pre-change hashes. Focused whitespace checks passed. Existing unrelated checkout whitespace findings were left unchanged.

VERIFIED-RUNTIME (installation): Cities2 absence checked, all nine previous installed files backed up and verified, then the three 0.4.232 package files installed with matching SHA-256 hashes. See `evidence/fence-undo-0.4.232/installed-hashes.csv` and `../outputs/installation-0.4.232.txt`.

VERIFIED-RUNTIME (user-reported native acceptance): following the requested grouped checks of first and repeated fence deletion/Undo and prop/marker Undo, the user reported “All pass”. This accepts the tested 0.4.232 behavior; no independent log inspection or exhaustive companion/reference audit is claimed for this report. Targeting/outline source remains unchanged. No publishing performed.
