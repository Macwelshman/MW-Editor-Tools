# Native 0.4.240 surface test — user reports failure

VERIFIED-RUNTIME (logs, 2026-10-24): hover transitions distinguish Sand Surface 01, Tiles Surface 02 and Tiles Surface 03, including multiple entities sharing a prefab name. The ray's hit entity remains terrain 20987:1 while m_Owner identifies the area, consistent with the recorded native ground-area contract. Logs have no cursor positions/boundary coordinates, so correct spatial hover identification is UNPROVEN.

At 18:50:38.800 hover is entity 53148:35 (Tiles Surface 02), while selected is 53149:35 (Tiles Surface 03). Ctrl+C at 18:50:39.588 copies the selected Tiles Surface 03 entity. This confirms that existing explicit-selection priority still governs clipboard commands while hovering another surface. The user reports that both hover and Copy/Replace remain incorrect; native acceptance is FAILED, not inferred from successful source writes.

At 18:50:42.458 batch replacement updates rows 4,5 (two retained entities) to Tiles Surface 03. Material batches 24,24 become 25, with native completion logged at 18:50:42.505. At 18:50:57.857 a single replacement updates row 5 to Sand Surface 01; material batch 25 becomes 21, completing at 18:50:57.890. No renderer timeout, crash or replacement refusal is recorded.

No Undo shortcut queue/block/dispatch, replacement Undo completion, or restoration exception is recorded in this run. Undo entries are recorded, then the first is superseded by the next replacement. Surface Undo readiness currently returns false without a detailed reason; these logs do not establish which check blocked a button attempt or whether a shortcut reached the mod. Undo success remains UNPROVEN/user-reported failed.

UI log contains startup CSS compatibility warnings and Move It icon-rectangle messages, without a matching new focus exception. No matching error/exception was found in the inspected Player.log entries. No code or installation changes made during this log inspection.

Next investigation: distinguish viewport-hover shortcut intent from pinned selection without silently redirecting panel-button actions; capture precise ray hit position and polygon/source identity for disputed hover; expose individual surface Undo readiness refusals while retaining strict geometry/settings/reference checks.
