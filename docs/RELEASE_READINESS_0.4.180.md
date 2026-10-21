# Release readiness - 0.4.180

21 October 2026. User explicitly authorised release 0.4.180 with updated installer, ZIP and manual through the existing private GitHub repository. Repository privacy verified with GitHub before release preparation.

VERIFIED-STATIC: Release and interface builds pass. The new runtime's 2,278 method signatures/instruction bodies match the installed 0.4.180 mod. Assembly informational versions have different Git stamps (feb3e19 in the rebuilt release and e6d9fd8 in the earlier installed test); DLL byte/hash equality is not claimed. No game files changed.

VERIFIED-STATIC: all 35 installer safety checks pass against the nine actual embedded files, covering fresh/update installs, backup preservation, corrupt payload, junction/game-folder/game-running refusals, partial-write rollback, mid-install game start, final verification, read-only files and install locking. All ten ZIP entries, installer resources and release SHA256 manifests verify. The installer and ZIP include matching PDF/Markdown manuals, release notes, notices and installation instructions. Production mod directory was not changed by installer tests.

VERIFIED-STATIC: the updated manual contains 11 pages. Current version, game name, section headings and revised component/clipboard/Undo instructions pass text checks. All rendered pages were visually reviewed; a split Undo table was corrected before final packaging. Final PDF has no observed overlaps, clipping or almost-empty continuation page.

VERIFIED-STATIC: all 27 managed suites and 11 interface scripts pass. Final managed/UI matrix results are recorded under evidence/release-0.4.180. Existing dependency-metadata NU1900 and unused managed-test-field warnings remain; no new runtime dependency.

UNSUPPORTED/UNPROVEN: native acceptance of the newest selection focus/layout and report-clearing refinements is pending. Earlier user-accepted replacement Undo and launcher TAB fixes are retained. Whole-building/component Undo, other documented unsupported operations and the unresolved SR ResidentialLow hierarchy error are not advertised as fixed. Logs show duplicate prefab IDs and subscribed/local ImportedData copies; that collision remains a hypothesis.

Publication assets: MWEditorTools-0.4.180-Setup.exe, MWEditorTools-0.4.180.zip, MW-Editor-Tools-User-Manual-0.4.180.pdf and SHA256SUMS.txt. No repository visibility change or other-platform release.
