# 0.4.255 beta editor-load check

VERIFIED-RUNTIME (recorded 28 October 2026, 19:11 session): MW Editor Tools 0.4.255.0 loaded; component-header/list Clear confirmation installed; prop and area/surface move Undo hooks attached; sub-element deletion hooks and prefab-list recovery loaded; per-instance outlines initialized; shared UI mounted and editor availability became true. No MW warning, error or unavailable-hook message was logged.

VERIFIED-RUNTIME: current-session log files (modified after 19:10) contain zero matches for ERROR, Exception, unverified, incompatible, conflict or failed. Player.log separately contains one Mono gpath.c assertion ('filename != NULL' failed), without an MW stack trace or established cause. This is not evidence of a mod compatibility failure.

VERIFIED-RUNTIME: FileSystem.log has 2,092 warnings (1,923 missing ObjectPrefab references, 168 missing AreaPrefab references, one unmatched asset filter). SceneFlow.log has 2,402 warnings, including null subobject prefabs in Spanish Revival house assets. UI.log has 26 CSS warnings. These are separate asset/UI diagnostic findings; their cause and relationship to the beta are UNPROVEN.

Targeting reports 'Select an editable asset in Object Info first', a normal availability condition. No editable selection or action was recorded at the snapshot. Startup attachment is verified; actual confirmation, deletion/move Undo, targeting selection/render behavior and persistence acceptance remain pending. No game interaction or changes performed.
