# Component controls cleanup — 0.4.176

21 October 2026. User requested removing blue information/control boxes from component headings, retaining checkmarks as the selection indicator, checkbox tooltips and a separate Clear Clipboard button disabled when empty. Follow-up explicitly requested restoring Copy and Copy All.

VERIFIED-STATIC: Object Info headings now contain only the native selection checkbox and original title. Repeated Select All/Copy controls and blue selection outlines are removed. Native Tooltip explains selection and deselection, distinguishing selection from activation. The Components toolbar contains Copy, Copy All and Select All, without a redundant component list. Paste modes, Paste/Restore and result information remain. Clear Clipboard is a separate button in the transfer action row; it is disabled for an absent/empty clipboard summary and enabled when copied entries exist. Backend clipboard summary is derived from the actual entry count. Component identity, transfer safeguards and Undo behavior are unchanged.

VERIFIED-STATIC: Release and webpack builds pass; focused heading and Components UI checks pass, including native checkbox/theme, tooltip content, event isolation, absent/ambiguous mapping, copy/copy-all/select-all commands and clipboard button gating. Existing NU1900 metadata warning remains. No computer use or publication.

UNSUPPORTED/UNPROVEN: native tooltip placement/focus and final layout require grouped in-game acceptance. Check checkbox selection/tooltip without activation/expansion, Copy and Copy All, Select All, and Clear Clipboard disabled-empty/enabled-populated/disabled-after-clear.
