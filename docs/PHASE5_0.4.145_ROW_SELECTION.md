# Phase 5 — 0.4.145 row selection correction

VERIFIED-RUNTIME (user report): clicking an element in the native Object Info browser list only expands/collapses it; the item is never a selected object. Therefore 0.4.144's reliance on `InspectorPanelSystem.selectedObject` could not choose a replacement row.

VERIFIED-STATIC: Prop Replacer now enumerates the current inspector asset's actual underlying ObjectSubObjects, ObjectSubAreas, ObjectSubNets, EffectSource and ObjectSubLanes storage. Its own row chooser stores the exact descriptor object identity and owning asset. Every command re-enumerates the live lists and requires that identity to occur exactly once; asset changes, list reconstruction or removal invalidate the choice.

Filtering, pagination and collapsed native rows cannot redirect the operation because visible browser indices are not used. Copy, selected replacement, matching-reference batch replacement, complete-list rollback, native refresh and destination-field preservation are otherwise unchanged.

VERIFIED-STATIC: Release and shared UI builds pass; lifecycle and integrated-tab regressions pass. Native replacement behavior remains UNPROVEN.
