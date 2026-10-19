# 0.4.94 hidden window correction

VERIFIED-RUNTIME: user reports the launcher does not display a window. Logs at 00:52:39–00:52:56 on 2026-10-13 contain repeated open/close commands; the command binding receives input. No relevant JavaScript exception appears in the inspected Player.log tail.

VERIFIED-STATIC: 0.4.93 hides the native panel until a one-shot layout effect finds a .mw-tools-panel descendant of its wrapper. Missing/delayed native DOM leaves position null permanently. HYPOTHESIS: native panel mounting or DOM ancestry caused that measurement to miss on this run. Runtime DOM was not inspected; no computer use.

Correction: use the native Panel forwarded ref directly for measurement/drag/persistence. Retry missing/zero-size initial measurements for up to 60 animation frames, cancel on unmount, then show at a fixed visible fallback rather than staying hidden. Clamp and sanitize initial normalized coordinates. Position remains owned by React pixel state; no competing position writer or resize repositioning added.

VERIFIED-STATIC installed native UI source: Game/UI/index.js SHA256 62EA0EC8E695DDBB29F9CDD54077EB343CD80E25177B733E44C3918625170B20 exports Panel as forwardRef and forwards the reference to its native root. Focus/transition wrappers surround that root. Narrow excerpt retained in docs/evidence/panel-0.4.94-native.txt; game files read only.

Automated verification covers delayed mount, direct ref, bounded visible fallback, unmount cancellation, invalid saved coordinates, existing tab height changes, repeated drags and persistence. All seven UI scripts pass; Release build zero warnings/errors. Production UI build/TypeScript must pass before packaging. Native result remains UNPROVEN.

User check after installation: click MW Editor Tools once and confirm the window appears. Close/reopen, switch History/Colours/Multi-Save and drag it. No recovery retesting is needed for this frontend positioning correction.
