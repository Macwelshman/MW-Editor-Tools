# Compact Options and browser shortcuts — 0.4.163

Removes the two visible multiline guidance blocks from Options; short option labels and concise native tooltip descriptions replace repeated instructions. Existing switches, shortcut fields, Apply buttons and Reset window position remain.

Browser defaults: Alt+B Buildings, Alt+D Props, Alt+F Foliage, Alt+A Areas, Alt+E Effects, Alt+S Surfaces. Native keyboard attributes, fresh settings, invalid-value fallback, missing legacy settings and Reset Defaults agree. Saved bindings matching the former Ctrl+Alt defaults migrate on load; other customised bindings are retained. The browser-shortcut enable switch keeps its existing value. Exact modifier matching, conflict handling, search retention and typing/dialog/busy guards are unchanged.

VERIFIED-STATIC: Release and production UI build passed. Options checks cover draft/Apply, resets, all six Alt defaults and hidden verbose guidance. Settings/shortcut tests cover migration, custom preservation, conflicts and serialization. No native Options appearance or key-dispatch acceptance claimed. NU1900 remains due to unavailable online vulnerability metadata.

Acceptance: inspect compact Options and the six bindings; enable browser shortcuts if needed, Apply and try all six in the editor. Confirm search is retained and typing does not trigger them. A customised shortcut should remain customised across restart.

VERIFIED-RUNTIME (user report): UI appearance and Options already checked, confirmed during release-readiness pass. Individual Alt key dispatch was not separately enumerated in that confirmation.

