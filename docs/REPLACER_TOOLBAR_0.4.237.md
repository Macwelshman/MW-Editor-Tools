# Anchored Replacer toolbar — 0.4.237

VERIFIED-STATIC: the toolbar has a fixed minimum row height, vertically centered controls and fixed Start/Stop button dimensions. Element type is inline immediately left of the dropdown, instead of above it. The dropdown no longer inherits action-button flex/centering styles: it retains the native indicator/layout with a left-aligned value, distinct border/background and sufficient width. Action buttons retain consistent dimensions. No runtime selection, replacement, Undo or outline behavior changed.

VERIFIED-RUNTIME (managed): Release/UI builds and 32 UI render/dispatch checks pass. Focused whitespace check passes. Release build retains the NU1900 advisory-feed warning; no new dependency audit claimed. Evidence: `evidence/replacer-toolbar-0.4.237`.

VERIFIED-RUNTIME (deployment check): Cities2 was running, so installation was deferred. The checked package is staged in `../outputs/test-package-0.4.237/MWEditorTools` with conditional backup/hash-verifying installer `../outputs/install-replacer-toolbar-0.4.237.ps1`. Installed version remains 0.4.236.

UNPROVEN: native toolbar appearance and position stability remain pending installation and user confirmation. No game interruption or publishing performed.
