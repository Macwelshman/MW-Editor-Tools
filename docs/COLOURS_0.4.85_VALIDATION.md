# 0.4.85 automatic colour selection

VERIFIED-STATIC: Colours follows inspector selection automatically while visible and without uncommitted edits. Observed selection/world/active-panel identity produces a revision token; the backend re-observes and rejects delayed follow requests for a different selection. Existing Capture, target validation, read-only checks and write interlocks remain. Unsupported selections give existing guidance and are not retried continually.

Copy colour settings remains available across selection captures. Paste colour settings retains its named destination confirmation and deep-copy validation. Palette drafts are retained across tab/window closure in frontend memory, keyed to paletteRevision. Edited or generated palettes prevent automatic following and show an explicit discard action; returning to the original selection resumes editing. Mesh dropdown changes also ask before discarding. Capture/Choose invalidate draft revisions. No original project changed.

Release zero warnings/errors; UI build and TypeScript pass. Five UI suites pass, including automatic clean following, generated and ungenerated draft refusal, explicit discard revision and descriptive labels. 17 copy/transport and29 palette/writegate checks pass. Existing layout and recovery logic unchanged. Native selection interaction remains UNPROVEN until in-game acceptance.
