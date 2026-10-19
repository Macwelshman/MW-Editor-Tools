# Undo 0.4.108 — retained surface descriptors

VERIFIED-RUNTIME: the user confirmed bound surface Undo passes on 0.4.107. The captured log records final surface payload/graph verification at 20:53:56.334, followed by Walking Area capture refusal at 20:54:01.493: retained descriptors differ (before=2, after=1). See docs/evidence/undo-0.4.107-runtime.log.

VERIFIED-STATIC: capture now verifies the complete native area placements before deletion. The immediate source-descriptor check requires exactly one removal of the captured target prefab, unchanged retained prefab identity counts, finite polygon coordinates and valid parent-mesh array shapes. It permits the retained source polygon representation to be rewritten by SaveInstance. The deleted target before-image is preserved unchanged. Deferred capture still requires complete retained native geometry, parent meshes, flags, ownership and companions to match their before-images for three stable frames before recording Undo. Pending Apply protection and surface recovery validation remain in place.

VERIFIED-STATIC: Release build has zero warnings/errors; production UI and TypeScript pass. Focused checks: Phase3Safety 209, BulldozerSafety 47, NestedRecoveryRunner 133, StandaloneNestedSafety 25, LastPropHistory 12, LifecycleSafety 20 (446 total). Six additional checks cover rewritten retained surface geometry, incorrect/multiple removals, absent target, nonfinite nodes and malformed mesh arrays.

UNPROVEN: native bound-area acceptance on 0.4.108 remains pending user testing. Managed checks do not establish in-game acceptance.
