# 0.4.210 repeated deletion acceptance — 2026-10-23

VERIFIED-RUNTIME: user reports repeated Restricted Spawn Location deletion/Undo including interior marker all passed. Preserved MWEditorTools.log independently confirms four complete spawn/access Undo operations on MW Mixed Test Components:

- 12:55:06.003 — source, graph and companions verified.
- 12:55:09.442 — source, graph and companions verified; preceding capture at12:55:08.338 proves interior connection projection, removed2, before15/after14.
- 12:55:12.268 — source, graph and companions verified; preceding capture at12:55:11.019 independently proves another interior projection, removed2, before15/after14.
- 12:55:14.648 — source, graph and companions verified.

No WARN/ERROR/CRITICAL, recovery refusal, unavailable or timeout lines in this mod log. Two INFO "Network Undo blocked: recovery busy, disabled or dialog open" lines occur during active network restoration and are each immediately followed by successful validation; these are eligibility guards, not failed Undo.

Subsequent successful deletion/Undo regression checks in the same session:

- FlowerBushCity01 — 12:55:17.271, payload/object graph verified.
- Grill01 — 12:55:19.971, payload/object graph verified.
- BikeRack01 — 12:55:26.810, payload/object graph verified.
- Walking Area — 12:55:28.754, payload/object graph verified.
- Sand Surface01 — 12:55:30.223, payload/object graph verified.
- Invisible Car Path -1xTwoway — 12:55:31.834, network graph/companions verified.
- FenceResidentialHigh01 — 12:55:34.282, network graph/companions verified.
- EU_FenceResidentialPieceLow06 prop — 12:55:37.072, payload/object graph verified.

Result: repeated supported spawn/access deletion capture and Undo, including interior two-neighbour replacement and subsequent editing, accepted for this tested mixed asset. Twelve successful completed Undo operations total. This session contains no direct effect move/deletion test and does not establish save/reopen persistence or universal asset/topology support. Previous scope guards remain applicable. No code, build or installation changes made in this log-review turn.
