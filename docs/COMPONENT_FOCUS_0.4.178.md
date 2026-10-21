# Embedded checkbox focus fix — 0.4.178

21 October 2026. VERIFIED-RUNTIME (user report): selecting/rendering the new component headings logs "Button: Cannot register child Toggle. Children are not allowed in this focus node". Original heading checkbox was nested inside the native expand Button with automatic focus registration; previous static tests did not model the native focus tree.

VERIFIED-STATIC: installed UI EditorSectionBase renders its native activation Checkbox inside Button with focusKey FOCUS_DISABLED. focus-key.ts useUniqueFocusKey returns null for that exact exported singleton; Checkbox forwards it into Toggle. New selection Checkbox now uses the same singleton, retaining existing wrapper keyboard handling and native pointer onChange. No heading rebuild or transfer/Undo changes. Evidence: evidence/component-focus-0.4.178/native.txt. The installed Game.dll identity remains unchanged from component-headings-0.4.174/native.txt.

VERIFIED-STATIC: Release/webpack builds and heading/Components UI checks pass. Regression assertion now requires the exact FOCUS_DISABLED export on the embedded checkbox and still checks pointer/keyboard selection and event isolation. Existing NU1900 dependency metadata warning remains.

UNSUPPORTED/UNPROVEN: absence of the reported log in a fresh native session and native checkbox focus/selection require runtime acceptance. No computer use or publication. Check headings expanded/collapsed, checkbox toggle, keyboard Enter/Space and native expansion/activation controls, with no repeated child registration error.
