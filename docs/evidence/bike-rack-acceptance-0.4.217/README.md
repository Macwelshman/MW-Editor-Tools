# 0.4.217 runtime acceptance — 2026-10-23

VERIFIED-RUNTIME: User reports “All passed”. Preserved original MWEditorTools.log (last write 15:30:50).

Nine completed move Undos on MW Mixed Test Components, all reporting source, graph and companions verified:

- BikeRack01: 15:29:51.025 (first), 15:30:11.496, 15:30:18.352, 15:30:26.359, 15:30:31.605, 15:30:50.053.
- Pedestrian Access Location: 15:30:05.360, immediately after first rack Undo.
- Restricted Spawn Location: 15:30:36.490 and 15:30:40.467.

User visual acceptance covers requested rack move/rotation and marker checks. No warnings/errors, capture timeout or source descriptor mismatch logged. The 15:30:20.956 unsupported whole-building move refusal is expected and separate from supported tests.

Scope: first/repeated BikeRack01 and both marker move Undo accepted. No ordinary prop/effect/deletion/persistence attempt in this session; their earlier evidence remains applicable. No implementation, build, installation or publishing changes in this review.
