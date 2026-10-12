# 0.4.35 Temporary nested group Undo test

VERIFIED-RUNTIME: 0.4.34 captured all three fixture descendants, preserved their detached typed images, and confirmed all original entities gone after deletion. Root and parent prefab payloads remained unchanged. Evidence: evidence/bulldozer-0.4.34-nested-runtime.log.

0.4.35 adds a separately armed, single-entry Everything-mode experiment for the uniquely placed, generated three-node fixture only. Another bound prop must remain. The target must be directly owned by the active asset; the parent indices must describe root -> parent -> leaf. Other nested targets remain refused by the existing routes.

Deletion must remove exactly the fixture's ObjectSubObjects entry, preserving all retained entries and other captured payload fields. Referenced prefab images are retained with a 250000-weight budget and checked before recovery and during completion. Existing component identities, payload shape and deferred native completion checks remain mandatory. No new ECS system or native hook is introduced.

Undo uses the existing native prefab regeneration path. Before history cursor movement, recursively capture and compare every node: exact prefab reference, parent index/depth and current Owner chain, transform, seed, optional Surface presence/value, colour buffers, and full LocalTransformCache. A unique root is required. Redo must remove that root and all tracked live descendants. No old Entity ID is recreated. Native creation may change seed/colour/cache: this remains UNPROVEN; mismatches are logged and fail closed rather than weakening equality. Deferred rollback remains UNPROVEN.

VERIFIED-STATIC: Release build zero warnings/errors; 420 existing safety assertions and UI settings render pass. These checks do not simulate native nested regeneration. Game.dll SHA256 unchanged: AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A, Game 0.0.0.0, MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. Recorded native ownership/probability semantics: ApplyPrefabsSystem.ListObjectSubObjects, detail.txt IL00f4..0101, IL0129, IL0183/018d/0195. Existing evidence reused.

Test:
1. Restart CS2 without saving temporary changes; open Test containing Grill01.
2. Create nested fixture; place exactly once; Escape. Keep the original bound prop.
3. Arm nested group test; Everything bulldoze the three-grill group.
4. Undo, wait for completion; Redo, wait; Undo, wait.
5. All three must return to their original position and appearance, exactly one copy each. Reply finished and keep CS2 open for log capture. Do not save the temporary fixture.
If an error occurs, stop and keep CS2 open for log capture; fully restart without saving after the log is copied.

Existing validated Main/Sub-element/Everything/last-prop scopes are retained. No individually selectable nested leaf, general nested component reconstruction, networks, areas, mixed history or nested persistence claim. Phase 5 remains paused; Phase 6/publishing unauthorised.

INSTALLED 0.4.35 with CS2 closed; all 7 package hashes verified; entire previous installation backed up and hashes verified at C:\Users\ian\Documents\Development\MW Editor Tools\outputs\rollback\MWEditorTools-before-0.4.35-20261010-134247\MWEditorTools. Release/UI/settings render/420 existing checks pass. Live nested GROUP Undo/Redo remains UNPROVEN. Next: open Test containing Grill01, Create nested fixture/place once/Escape, keep another bound prop, Arm nested group test, Everything delete three-grill group, Undo -> Redo -> Undo waiting each completion; finished keep CS2 open/no save. See docs/BULLDOZER_0.4.35_VALIDATION.md. Continue automatically after test; Phase 5 paused/6 publishing unauthorised.
