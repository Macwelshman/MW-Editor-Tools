# Existing source inventory

Read-only discovery under `C:\Users\ian\Documents\Development` on 9 October 2026. The existing `MW Editor Tools` directory was empty in the top-level listing; this new scaffold is saved in this chat's outputs and does not overwrite that location.

Colour Variation: `MW Color Variation Tool\MWColorVariationTool\Mod.cs`, `NativeUIBridge.cs`, `Palette.cs`, tests and build/UI scripts located. Inspected Mod.cs declarations and capture/clone/paste/undo sections: IMod creates a persistent MonoBehaviour host; PropertiesCopy deep-copies variation/binding/group collections; local undo state tracks target/world and whether a paste created a component. This is module-specific undo, not proof of native editor deletion history. Multiple release directories exist; the authoritative migration baseline still needs selection/version verification. No rewrite planned.

Multi-Save: `MW Multi-Save\MultiSaveSystem.cs` inspected. UISystemBase restricted to Editor; uses existing hierarchy/inspector/prefab systems, reflection for hierarchy rows, OnSaveAssetCheckDLC and dependency completion. Builds a deduplicated unlocked prefab queue; asynchronous callbacks use generation/waiting state. Preserve this lifecycle, native save dialogs and stop behavior. It saves current assets, not recovery snapshots. Original Mod.cs/build/UI files located but not fully audited.

Component Tool: source directory and AGENTS.md located; full source audit deferred until integration. Its local rules must be read before any work there. Prop Replacer source was not identified in this bounded directory search.

Neighbouring MW Editor Environment csproj inspected: net48 and Private=false game references. Useful local build precedent; not an official toolchain guarantee. Original projects remain unchanged.
