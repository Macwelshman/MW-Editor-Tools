# 0.4.219 bike-rack and subsequent move Undo — 2026-10-23

VERIFIED-RUNTIME: loaded assembly0.4.219.0. User reports first bike-rack Undo stuttery, all subsequent checks pass. Full log preserved locally (last write16:24:28,8291 bytes).

Five completed bound move Undos, all source, graph and companions verified on MW Mixed Test Components:

- BikeRack01 first: dispatch16:23:57.020, complete16:23:57.159 (139ms).
- BikeRack01 repeat: dispatch16:24:03.622, complete16:24:03.742 (120ms).
- BikeRack01 repeat: dispatch16:24:12.264, complete16:24:12.421 (157ms).
- Pedestrian Access Location: dispatch16:24:19.789, complete16:24:19.904 (115ms).
- FenceIndustrialPieceLow02 ordinary prop: dispatch16:24:26.780, complete16:24:26.905 (125ms).

No warning/error, capture timeout, typed graph mismatch or source-association refusal in this run. First rack restoration reports zero changed/new groups and60 unchanged groups; later two rack operations report2 changed/new and58 unchanged. These counts describe restoration work and do not measure frame time.

UNPROVEN: first-Undo stutter cause. The logged dispatch-to-completion interval is similar to later runs; timestamps cannot identify frame stalls, rendering or native-job/GC/JIT costs. Do not ascribe the stutter to a particular subsystem without profiling.

Scope: this fresh-load first/repeated rack, pedestrian marker and ordinary-prop sequence passed. The0.4.218 failure was not reproduced and remains an intermittent unresolved case;0.4.219 changes diagnostics only. No deletion, Restricted Spawn, effect or save/reopen attempt logged in this session. Previous colour/priority/component persistence acceptance remains applicable. No code/build/install/publish changes in this review.
