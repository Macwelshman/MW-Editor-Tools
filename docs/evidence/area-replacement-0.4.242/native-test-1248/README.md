# Native test — 0.4.242, 25 October 12:48

VERIFIED-RUNTIME: user reports Area replacement failed. Log shows Park Area copied from 53054:117 and Walking Area selected at 53055:117. Attempts at 12:48:48.139 and 12:48:54.355 were refused before writes: native component layouts differ. This is the explicit layout compatibility guard, not failure of an applied transaction. Arbitrary Space-to-Space conversion remains unsupported when archetype sets differ. The UI error says Surface although the operation is Area; this wording needs correction with the next implementation.

Native inspector edit at 12:48:28.449 triggered the new targeting shutdown before native refresh, then returned to DefaultToolSystem. Later targeting resolved regenerated Park/Walking Area identities. No crash is recorded in this captured mod log; do not infer general crash acceptance solely from its absence.

Surface row 6 replacement at 12:48:12 released its old render allocation, updated the source/live reference, completed batch 24 material refresh and passed retained identity/boundary/source/reference Undo readiness. Actual visible success and Undo acceptance require the user's result.

Requested follow-up: correlate viewport selection with the native component-list element row. Reuse exact ownership/source-row matching rather than prefab-name matching, since repeated instances can have identical names. Display a cyan row marker for selection without changing component-copy checkboxes or source data. Clear on selection/context/edit/filter invalidation; do not mark an ambiguous row. Native row binding/extension still needs inspection before implementation.
