# Workspace Save All — 0.4.191

VERIFIED-STATIC: installed Game/UI/index.js SHA256 62EA0EC8E695DDBB29F9CDD54077EB343CD80E25177B733E44C3918625170B20 exports game-ui/editor/components/hierarchy/hierarchy.tsx Hierarchy. HierarchyPanel renders the search and this component inside its vertical navigation scope. The extension preserves the original hierarchy and puts Save All and running Stop below it in a flex footer. No game files are modified.

Multi-Save retains progress and Details and explicitly locates both buttons at the bottom of Workspace. Footer mounting owns workspace count refresh; tab changes no longer close it. Existing save queue and native dialogs remain unchanged. This version includes the 0.4.190 default naming update.

VERIFIED-STATIC: managed/UI builds and all 14 UI suites pass, including 23 Multi-Save presentation/command checks. Native workspace layout, scrolling, collapsed/resized panels and controller focus acceptance remain UNPROVEN.
