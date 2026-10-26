# Add localised description — test 0.4.187

User-authorised addition, 2026-10-21. VERIFIED-STATIC: Add missing description opens an optional language ID/multiline description form, default en-US. Add description fills a blank or missing language entry and refuses to overwrite existing text. The name and description additions share bounded validation, native WIP write/dirty/refresh, rollback and guarded Restore. Name, other translations, prefab name and thumbnail are preserved. Copy description can subsequently copy the entered text; save normally to persist it. Draft text is tied to the selected asset token.

Native field API/evidence is unchanged from 0.4.186 and `evidence/pack-editing-0.4.181/native.txt` (`InspectorPanelSystem.BuildLocalizationFields`, `m_DescriptionLocalization` at IL_0097 and Initialize at IL_009d). Native rendering/save acceptance remains pending. No publishing or computer use.

Validation evidence is in `evidence/localised-description-0.4.187/`: build, all 14 UI suites and component transfer checks, including multiline description creation/copy, blank/missing entry Restore, existing text protection, read-only rejection, refresh rollback and stale-draft guarding.

118 transfer checks passed. Installed with Cities2 closed; previous files backed up and verified, all three installed runtime hashes verified. See evidence/localised-description-0.4.187/installation.txt.
