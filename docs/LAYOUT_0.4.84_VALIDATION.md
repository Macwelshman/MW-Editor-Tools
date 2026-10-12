# 0.4.84 stable tab geometry

VERIFIED-STATIC: the shared body uses a consistent 85vh height across tabs, retaining the screen cap and internal section scrolling. This corrects the variable-height layout introduced in 0.4.83. Native dragging, opening coordinates and reset behaviour are unchanged. Swatch scrolling still starts only above three rows. Selection behaviour unchanged.

Release build: zero warnings/errors. UI build, TypeScript, five UI suites pass, including two new geometry CSS regression checks. Recovery and colour engine logic unchanged. Native tab-switch appearance remains UNPROVEN pending visual acceptance.
