# Release readiness - 0.4.173

20 October 2026. The user authorised publishing 0.4.173 as the latest release in the existing private repository, with installer, ZIP and updated PDF manual.

VERIFIED-RUNTIME: user confirmed the normal UI returns when closing the developer menu with TAB. The original UI log recorded React error 185 with its stack pointing at this mod's launcher state update. See TAB_UI_CRASH_0.4.173.md. Accepted 0.4.171 replacement Undo remains preserved.

VERIFIED-STATIC: Release and interface builds pass. All 26 managed suites and ten interface scripts pass, including original-loop reproduction, 80 simulated frozen-layout transitions and cancellation/cleanup checks. The component-removal/Clear harness has 37 checks covering deferred confirmation, cancellation, duplicate callbacks, retained components and native refresh ordering. Evidence: evidence/tab-ui-0.4.173/ and evidence/effect-removal-0.4.172/. Existing NU1900 dependency-metadata warning remains. No new runtime dependency was added.

The final release runtime is compared with the installed accepted 0.4.173 files. PDF/Markdown manuals are embedded in the installer and ZIP, with the PDF also attached separately. Final installer/payload checks and PDF visual verification are recorded under evidence/release-0.4.173/.

UNSUPPORTED/UNPROVEN: the user confirmed TAB UI recovery, not every developer-menu/third-party layout combination. Native cleanup of the previously reported fire marker following component removal and a fresh persistence run remain unconfirmed. Whole-component removal is not advertised as Undo-recoverable. No visibility change, other-platform publication or computer use.

VERIFIED-STATIC: all 35 installer checks pass against the actual nine-file embedded payload. EXE resources, ten ZIP entries and release checksums pass verification; all four runtime files match the accepted installed 0.4.173 build. The 11-page updated PDF passed text/version/game-name checks and full-page visual inspection; it is included in both packages and as a standalone asset.
