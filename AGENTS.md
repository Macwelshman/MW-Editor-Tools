# Working rules

2026-10-28: user explicitly authorised updating the 0.4.257 installer and user manual and publishing that version through the existing private GitHub repository. Preserve repository visibility, original source projects and installed runtime behavior; validate the installer/manual/release assets before publication.

2026-10-28: user explicitly authorised integrating MW Editor Environment into this mod's Options menu, organised into Keyboard Shortcuts, Tools and Environment tabs. Preserve its saved preset values and editor/photo-mode lifecycle, preserve original source projects, and avoid competing standalone/integrated environment systems. Existing automatic backup/install and no-computer-use rules apply; this request does not publish a release.

2026-10-19: user explicitly authorised viewport paste and replace-all. Extend the accepted browser Ctrl+C/Ctrl+V/Ctrl+Shift+V workflow using exact highlighted entity ownership and unique native placement-to-source-row mapping. Preserve destination placement/settings and accepted Undo; no computer use or publishing.

2026-10-18: user explicitly started Prop Replacer Phase 5. Authorised scope is browser-list element copy/paste replacement, batch replacement of the chosen element in the current asset, single-component copy with settings, and all-component copy with add-missing / replace-present / replace-and-add modes. Viewport element replacement is a later option and remains deferred. Preserve accepted Undo 0.4.143 behavior; no computer use; group native acceptance.

2026-10-13: user also approved Stage 2. Implement bound area/surface Undo on mixed assets, with companion ownership/geometry/reference preservation, using the one-step shortcut flow. See docs/UNDO_STAGE2_0.4.96.md. Stage 3/4 deletion targets remain pending. No computer use; group native acceptance rather than requesting repeated micro-tests.

2026-10-13 scope update: user approved Stage 1 of the reassessed Undo plan. Implement automatic supported standalone/bound/nested prop Undo with the existing guarded shortcut, one last-deletion slot and no normal manual-enable/history/Redo workflow. Preserve tested capture/restore behavior. Later area/surface/effect/lane/network stages are pending separate implementation; whole-house and whole-component recovery are deprioritised. Batch acceptance checks; no computer use. See docs/UNDO_STAGE1_0.4.95.md. This update supersedes earlier multi-history/Redo priorities.

Current mode: IMPLEMENT, priority changed by the user to bulldozer Undo (outstanding Phase 3 recovery scope). Phase 4 version 0.4.9 is user-validated. Prop Replacer Phase 5 is authorised but paused behind bulldozer recovery. Preserve validated Phase 2/3/4 behavior. The user explicitly authorised migration of the working Multi-Save functionality into this mod on 2026-10-11. This narrow Phase 6 Multi-Save integration and shared-window tab/layout work are authorised; The user also explicitly authorised Colour Variation module integration on 2026-10-11. Other module migration and publishing remain unauthorised. Multi-Save integration and the shared tab layout are user-accepted. Expanded Undo/Redo is authorised and active again.

The user authorised automatic installation of subsequent versions. After successful relevant build/checks, preserve the installed version under outputs/rollback and install the test package into the existing local MWEditorTools mod folder without asking again. Verify copied hashes. Do not overwrite while Cities2 is running; verify that it has fully exited before installation. This does not authorise later development phases or publishing.

The user authorises automatically closing and reopening Cities: Skylines II as needed for the authorised development and testing, without asking for each restart. Prefer normal application exit through the UI. Disposable test edits may be discarded without saving; do not save them or overwrite original assets. If unrelated unsaved user work is present or its status is unclear, ask before discarding it. This permission does not authorise force termination of an unresponsive game. Resume computer-use setup and testing after reopening. This rule supersedes earlier notes prohibiting automatic game closure.

- Read installed assemblies only. Never edit or replace game files.
- Do not add mod entry points, Harmony patches, ECS systems, runtime UI, installers or deployment targets in INSPECT.
- Label facts VERIFIED-STATIC, VERIFIED-RUNTIME, HYPOTHESIS or UNSUPPORTED/UNPROVEN. Cite assembly identity, exact method and IL offset when available.
- Reuse recorded evidence until assembly hashes change. Inspect narrowly.
- Preserve original MW projects; integration needs source review and regression validation.
- Require explicit phase change before implementation. Do not infer that toolchain installation authorises implementation.
- Unsupported operations must not be advertised as recoverable. Preserve parent/child relationships, references and deferred ECS semantics.

See docs/AI_WORKFLOW.md for mode definitions and docs/TEST_PLAN.md for release gates.



2026-10-14: user explicitly authorised Undo for bound and unbound surfaces/areas. Standard Surface/Space standalone recovery implemented in 0.4.100; native acceptance pending. No computer use.

2026-10-14: user explicitly authorised lane/network Undo implementation, with bound elements the priority. Implement one last-deletion slot and Ctrl+Z for directly bound segments, preserving validated prop/area/surface recovery. Standalone network and junction-only recovery remain unsupported. No computer use.

2026-10-15: user explicitly authorised implementing move Undo for built-in move/relocate, starting with supported standalone/bound props, foliage and nested groups. Share one last-action Ctrl+Z slot with deletion recovery; cancelled/no-op moves preserve the previous slot. Move Undo for areas/networks and other tools remains outside this stage. No computer use.

2026-10-16: user explicitly authorised built-in move Undo for areas and surfaces, including bound and unbound supported Surface/Space geometry. Preserve validated prop/nested/foliage move and deletion recovery, use one shared last-action Ctrl+Z slot, preserve cancelled/no-op moves, and retain ownership/geometry/reference/companion safeguards. Network moves and other tools remain outside this stage. No computer use.

2026-10-17: user explicitly authorised Undo for adding surface/area nodes, moving added nodes and deleting nodes. Extend the existing supported bound/unbound native area tool path with one shared last-action Ctrl+Z slot. Preserve accepted 0.4.125 ordinary/snapped/two-move/persistence behavior and ownership/reference/companion safeguards. Whole-polygon creation/deletion through node tools, multi-polygon/merged edits and network moves remain outside this implementation. No computer use.
2026-10-17 clarification: combine node addition and subsequent moves of that same added node into one Undo action. Moving another node starts a separate last-action entry; never combine unrelated node edits.

2026-10-17: user explicitly authorised direct effect deletion and move Undo, preserving existing accepted prop/foliage moves. Implement supported bound/unbound native effect editor containers through the existing single last-action Ctrl+Z flow, exact source/ownership/geometry/reference/companion preservation, cancelled/no-op preservation, and grouped acceptance. No computer use or publishing.

2026-10-19: user explicitly authorised continuing Phase 5 component copy/paste between assets with settings preservation and bulk Add only missing / Replace only present / Replace and add modes. Preserve accepted element replacement and Undo. No publishing or computer use.


2026-10-19: user explicitly authorised creating a release of the mod with an installer. Implement/package/test a Windows installer and publish the versioned assets through the existing private GitHub repository. This does not authorise changing repository visibility or publishing to other platforms.

