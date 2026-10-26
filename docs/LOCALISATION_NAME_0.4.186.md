# Localised name — local test 0.4.186

User request, 2026-10-21: show/copy the localisation name rather than the prefab name, and permit adding a name when the localisation entry is empty.

VERIFIED-STATIC: Copy name already snapshots `m_NameLocalization.ValidEntries()` from the native Object Info WIP fields, without using the prefab name as copied text. The previous clipboard summary used the prefab name as its source label. The summary now displays copied language IDs and actual detached localisation text. Up to three languages are shown with per-entry text truncated for display only; the clipboard retains the complete entries. Empty fields never fall back to the prefab name and cannot replace an existing clipboard.

Add missing name opens an optional language ID/name form (default language ID en-US). Add name fills a blank/whitespace name or creates a missing language entry on the selected asset, refusing to overwrite an existing nonempty name. It uses the same native WIP write, dirty/refresh, failure rollback and guarded Restore path as localisation paste. The internal prefab name, descriptions, other languages and thumbnails remain unchanged. It does not change the clipboard; Copy name can copy the newly entered localisation afterwards. Save the asset normally to persist it. Typed name drafts cannot be reused on a different selection token.

Native field evidence is reused from `evidence/pack-editing-0.4.181/native.txt`, Game.dll SHA256 `AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A`, MVID `e15551e9-3dea-4855-8dde-1da3cea7d7ef`: `InspectorPanelSystem.BuildLocalizationFields` IL_006e loads `m_NameLocalization`, IL_0074 initializes it with localisation entries. No new native API or assembly changes.

Validation: managed/UI builds and all 14 UI suites pass. 109 component transfer checks include actual-text clipboard display, detached copy, blank/new-language name creation, nonempty-name refusal, Restore, read-only rejection, invalid input and refresh-failure rollback. UI checks cover the add request and stale-draft guard. Native rendering and save/reopen acceptance remain pending. No publishing or computer use.

Installed 0.4.186 with Cities2 closed; rollback and installed runtime hashes verified. See evidence/localised-name-0.4.186/installation.txt.
