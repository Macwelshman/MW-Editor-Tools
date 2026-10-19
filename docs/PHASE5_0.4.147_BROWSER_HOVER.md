# Phase 5 — 0.4.147 browser hover copy

VERIFIED-RUNTIME: 0.4.146 received Ctrl+C, but `DefaultToolSystem.m_LastRaycastEntity` was null. The highlight was a browser UI hover rather than a 3D viewport entity.

The frontend now captures the element under the pointer at exact Ctrl+C and submits bounded aria-label, title and visible-text samples. The backend resolves these only against referenced prefab names in the current asset's live supported rows. All matches must have the same exact descriptor type and prefab identity; zero or ambiguous matches preserve the clipboard and are logged. Text inputs/content-editable fields remain excluded. Viewport hover capture remains available.

Release and UI builds pass. Native browser DOM text shape and successful resolution remain UNPROVEN.
