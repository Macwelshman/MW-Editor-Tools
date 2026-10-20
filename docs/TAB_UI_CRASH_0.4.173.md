# 0.4.173 candidate - developer-menu UI crash

2026-10-20. User reports the game UI disappears permanently after activating the developer menu with TAB.

VERIFIED-RUNTIME: UI.log at 12:52:31.756 reports minified React error 185. The stack at 12:52:31.924 points to coui://ui-mods/MW.EditorTools.mjs:9:27561 and :9:27567. The captured 0.4.172 bundle SHA256 is 4f5049122042f62c30f28f29b36a47f782ca8412ed9e97d134326415cffc01a3; those positions resolve to the launcher's offset state update and immediate layout measurement. Evidence: evidence/tab-ui-0.4.172-react-crash.txt and evidence/tab-ui-0.4.172-bundle-location.txt. The installed React development source identifies the corresponding maximum-update-depth failure.

VERIFIED-STATIC: Launcher.useLayoutEffect depended on offset, immediately measured its DOM position and called setOffset. If native layout temporarily returns a frozen rectangle during a developer-overlay transition, each new offset changes the derived base and triggers another synchronous layout effect/state update. The actual pre-fix Launcher source reproduces maximum update depth in the hook lifecycle harness with frozen bounds. Evidence: evidence/tab-ui-0.4.173/original-loop-reproduction.txt. The log proves this mod participated in the fatal update chain. The precise native TAB/overlay layout transition remains HYPOTHESIS because that input/layout state was not captured in the log.

The launcher now reads its applied offset through a ref and does not depend on its own position state. Measurements are deferred through one coalesced animation frame, with finite-value checks and cancellation on unmount. Resize/timer bursts schedule at most one measurement; an offset update cannot synchronously schedule another. Collision avoidance, sidebar anchoring, persistent open/close toggle and existing icon appearance remain. TAB is not captured by the launcher and no game-wide UI visibility flags are modified.

VERIFIED-STATIC: Release/interface builds pass. All 26 managed suites and ten interface scripts pass. The new actual-source hook test covers 80 frozen-bound transitions, hidden native bounds, visibility recovery, collision positioning, coalesced events and frame/timer/listener cleanup. Existing native-buffer safety gate passes. Undo and all editing implementations are unchanged from the 0.4.172 candidate; the component-removal refresh fix is retained. Existing NU1900 dependency-metadata warning remains. Evidence: evidence/tab-ui-0.4.173/.

UNSUPPORTED/UNPROVEN: native developer-menu open/close acceptance is pending. Next check: use TAB to open and close the developer menu several times, confirm the normal game/editor UI returns, and check that the launcher still opens/closes its window. No computer use or publishing; stable private release remains 0.4.171.

INSTALLED: 0.4.173 with Cities: Skylines II fully closed. All 10 previous mod files backed up and hash-verified under outputs/rollback/MWEditorTools-before-0.4.173-*. All seven installed hashes and assembly version 0.4.173.0 verified. Evidence: evidence/tab-ui-0.4.173-installed-hashes.csv.

ACCEPTED-RUNTIME 2026-10-20: user confirmed the UI now returns when the developer menu is closed with TAB. User authorised updating the private GitHub repository and publishing 0.4.173 with installer, ZIP and updated PDF manual.
