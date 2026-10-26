# Priority lists — local test 0.4.185

2026-10-21 user-reported preview stacking, followed by concern about overwhelming large lists.

VERIFIED-RUNTIME: the user's 0.4.184 screenshot shows asset name, current priority and proposed priority stacked on separate full-width lines. HYPOTHESIS: the native UI renderer does not preserve standard HTML table layout here.

VERIFIED-STATIC changes: replace HTML table elements with explicit flex rows and fixed-width numeric columns, retaining accessible table/row/cell roles. Asset names use the remaining width. The header stays above a body capped at 320rem with its own scrolling.

Both the workspace asset chooser and priority preview render at most 25 assets per page, with Previous/Next and visible-range counts. The chooser retains selected IDs across pages and search changes. Select shown adds eligible assets on the visible page; Select matching adds eligible assets on every matching page. Neither clears previous selections. Deselect pack clears them all. A fresh scan resets selections through the existing scan-token guard.

The preview has a separate search and page count including the complete preview total. Paging/search never changes the prepared batch. An explicit caption states that Apply affects the complete preview, including assets hidden by paging or filtering. Existing backend preview tokens, stale-input guards, ordering, rollback and persistence behavior are unchanged.

Validation: build and all 14 UI suites pass; existing NU1900 feed warning remains. Actual-source UI checks cover a 200-asset preview limited to 25 rows, three cells per visible asset, next-page ranges, preview search, selection retained across pages, and Select matching across 60 assets. Native rendering and interaction remain pending user acceptance. No computer use or publishing.

Installed 0.4.185 with Cities2 closed; previous files backed up and hash-verified, then all installed runtime hashes verified. See evidence/priority-layout-0.4.185/installation.txt.
