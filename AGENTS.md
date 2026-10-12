# Working rules

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


