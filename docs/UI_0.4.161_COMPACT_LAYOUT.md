# Compact shared UI — 0.4.161

User requested less verbose tabs, tooltips, Undo outside a dedicated tab, Replacer naming, component alignment/spacing, British colour spelling and consistent Save buttons. Reference: supplied Greenshot screenshots dated 2026-10-19 13:30.

Four tabs: Replacer, Components, Colours, Multi-Save. Undo is a compact shared header action with configured shortcut and availability in its tooltip. Routine recovery transcripts, action confirmations and obsolete support prose are removed from the panel. Ctrl+Z/backend recovery remains unchanged. Launcher hides while the panel is open to avoid covering navigation, and returns on close.

Replacer has a compact shortcut strip, source rows, clipboard and short action labels. Detailed shortcut/placement explanations use tooltips. Normal copy feedback is represented by the clipboard; replacement success is concise, full details in its tooltip, and refusal/error text stays visible.

Components uses one text node for its heading/count and explicit block layout to avoid native flex splitting. The empty list/heading and source-only copy controls disappear when no asset is selected. Mode descriptions and workflow guidance are tooltips. Preview opens details for review; errors/skips remain visible when details are collapsed. Paste/Restore command contracts and guards are unchanged.

Colours removes duplicate selection prose and hides inactive footer actions without a target. Visible colour spelling remains British, including incoming status lines. Draft-discard and full colour-paste confirmations, busy state, mesh selection, palette channels and local edit Undo remain. Footer buttons use natural widths. Multi-Save uses ordinary native button dimensions/styling, retains concise progress, Stop, optional details and automatic failure detail expansion.

VERIFIED-STATIC: Release and production UI builds pass (NU1900: online vulnerability metadata unavailable). React render/command checks pass for header Undo/four tabs, component modes/copy/preview gating, 19 Multi-Save checks, 27 Colours checks plus draft protections, Options separation, and panel drag/resize/position persistence. Native Cohtml visual appearance/tooltips and shortcut interaction remain UNPROVEN until grouped acceptance. No computer use or publishing.

Acceptance: inspect all four tabs with no selection and with a populated asset; check compact heading/count, tooltip access, no clipped rows and consistent button sizes. Verify Undo via header and Ctrl+Z with window open/closed, Replacer shortcuts, component preview/paste/Restore, Colours picker/draft/paste confirmations and Multi-Save Save/Stop/details. Drag the panel and reopen it to verify saved position and returning launcher.
