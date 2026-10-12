# Version 0.4.2 — independent icon launcher

VERIFIED-RUNTIME: 0.4.1 logs show UI bindings created, frontend mounted and editor availability true. User screenshot confirms the launcher remains absent. Layout-only correction did not resolve it.

VERIFIED-STATIC: reviewed the existing Multi-Save UI/src/index.tsx and style.scss, Colour Variation UI/src/window.tsx and Component Tool UI/src/launcher.tsx. Their launchers are separate Editor append registrations with fixed-size icon buttons, independent of window rendering.

0.4.2 uses a separate Launcher registered before Tools, gated only on editor availability, with a 56rem icon button and tooltip. It does not require settings to show the launcher. Essential position/size styles are inline as well as styled by the package; an original SVG sliders symbol identifies the tool. The icon is below the existing icons to avoid their fixed slots. Backend/frontend state and computed launcher bounds are logged on availability/settings/width changes, not every heartbeat.

HYPOTHESIS: window/settings rendering or missing stylesheet application prevented visibility. The exact cause is not proven. No recovery/shortcut policy changes; no module migration.

Test on a duplicate asset:
1. Restart CS2 and select an asset in the Asset Editor.
2. Look below the existing blue and coloured icons for a dark square with three white lines and cyan sliders. Hover: tooltip should read MW Editor Tools.
3. Click to open, close, reopen, and test Close within the window.
4. Verify Multi-Save and Colour Variation icons still work.
5. If successful, continue PHASE_4_VALIDATION.md. If the icon is absent or the window does not open, report which; preserve logs after closing CS2.

VERIFIED-STATIC: C# Release zero warnings/errors; pinned production UI build/type checks pass. Existing 79 safety assertions passed on 0.4.1; their covered recovery policies are unchanged. UNPROVEN: 0.4.2 runtime visibility/window behavior.
