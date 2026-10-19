# Phase 5 — 0.4.146 viewport hover copy

User requirement: copying must occur while the element remains highlighted, so it requires a keyboard shortcut. Clicking a UI action loses the highlight.

VERIFIED-STATIC: exact Ctrl+C is polled only in an active, focused Editor session with a fresh frontend focus heartbeat, no text/input focus and no native dialog. Normal text copying remains untouched. The active tool must be `DefaultToolSystem`; its existing `m_LastRaycastEntity` is read at the keypress, without changing selection, tool state or hover state.

The live highlighted entity must exist and not be pending deletion. Object, Area, Net edge and Lane targets use their exact `PrefabRef`. Effect editor containers use `EditorContainer.m_Prefab` and require an `EffectPrefab`. Unsupported or ambiguous entities are refused with a visible status and do not replace the existing clipboard.

This build adds hover **copy** only. Destination replacement still uses an explicit underlying row in Prop Replacer. Ctrl+V viewport replacement remains pending until exact live-entity-to-owned-descriptor matching and rollback are implemented.

VERIFIED-STATIC: Release and UI builds pass; lifecycle coverage passes. Native hover identity and Ctrl+C dispatch remain UNPROVEN.
