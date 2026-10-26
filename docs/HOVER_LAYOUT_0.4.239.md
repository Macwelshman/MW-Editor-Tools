# Stable hover feedback layout — 0.4.239

VERIFIED-STATIC: the active Replacer feedback block conditionally displays hover, selected, owner, outline and Undo details. Previously these rows changed the window's outer height as hover appeared/disappeared. A window directly above an element could expand over it, lose the hover and shrink back repeatedly.

The active feedback now has a fixed 220rem height and an internal scrollbar for longer details. The viewport hit-test boundary therefore does not expand/shrink with these feedback rows. Existing Start/Stop anchoring and inline dropdown layout remain intact. No selection, replacement, Undo or outline implementation changed.

Native flicker acceptance remains UNPROVEN until tested with an element directly beneath the window, including long names and Undo eligibility feedback. The inactive panel retains its compact layout.

VERIFIED-RUNTIME (managed/build/install): Release/UI builds and32 UI render/dispatch checks pass; focused whitespace checks pass. NU1900 advisory-feed warning remains. Cities2 absence verified; 0.4.239 installed with nine-file rollback backup and matching staged/installed hashes. Evidence: docs/evidence/hover-layout-0.4.239 and outputs/installation-0.4.239.txt. Native layout confirmation pending.
