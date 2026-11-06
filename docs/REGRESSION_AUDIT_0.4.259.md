# 0.4.259 regression audit

VERIFIED-STATIC: audited the complete tracked 0.4.257-to-0.4.258 source delta. The update replaced immediate colour paste, component paste and browser/viewport replacement (including Ctrl+V / Ctrl+Shift+V) with mandatory previews. It also changed colour combination Apply, cleared staged combinations when Colours closed, and automatically opened/switched the tools window for replacement previews. These changes are removed by restoring the accepted 0.4.257 implementations and matching tests. Preserved a snapshot of every affected file under outputs/regression-audit-before-restoration.

All pre-existing runtime validation, rollback, recovery, settings and lifecycle implementations match the accepted source baseline. No additional runtime changes outside that preview delta were present. Version/build metadata advances to 0.4.259. Unused preview helper files are retained without callers.

VERIFIED-STATIC: Release and UI builds and all 18 UI scripts pass. Full managed suite results follow in regression-audit-managed.txt. Native acceptance remains UNPROVEN; no computer use or publishing.

VERIFIED-STATIC: all 37 managed suites pass, including 130 component transfer checks and replacement/Undo/viewport regression checks. All 18 UI scripts pass. Build warning NU1900 records unavailable NuGet vulnerability lookup; compilation succeeds. Installed while Cities2 was closed, with backup and copied hashes verified. See outputs/installation-regression-fix-0.4.259.txt.
