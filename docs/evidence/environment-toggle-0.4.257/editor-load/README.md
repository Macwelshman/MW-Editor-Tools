# 0.4.257 session log check — 28 October 2026

VERIFIED-RUNTIME (recorded 19:36–19:38 session): 0.4.257.0 loaded; Environment integrated, Options registered, confirmation/list/native deletion/prop and area move Undo hooks and outlines initialized without warnings or errors. The enabled Environment preset applied once at 19:37:02: noon, day 91, precipitation 0%, clouds 15%, snow off. Climate refreshed before rendering without simulation playback. Cleanup logged previous override restoration at 19:38:19; Modding.log records normal mod disposal.

VERIFIED-RUNTIME: current-session category logs contain zero ERROR/CRITICAL, Exception, unverified, incompatible, conflict, failed or unavailable matches. Player.log contains no exception/crash/error matches in the focused scan, but does retain the same Mono gpath.c:115 filename != NULL assertion seen in the earlier beta session. Its cause is UNPROVEN and no MW stack trace is present. Player.log also contains shutdown allocator statistics with Failed Allocations/Failed count entries; these are not category ERROR lines or a reported out-of-memory exception.

Separate warnings: SceneFlow.log has 311 duplicate-prefab warnings; FileSystem.log has one unmatched asset-filter warning; UI.log has 19 CSS warnings. No standalone MW Editor Environment initialization occurred in this session. The old standalone log has an earlier timestamp and was excluded.

This verifies enabled preset startup, paused climate refresh and logged cleanup. Native menu appearance, enable-checkbox persistence/disabled-session behavior, supported Undo actions and environment visuals are not established by these logs alone. No game interaction, source changes or installation performed.
