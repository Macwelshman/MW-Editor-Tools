# 0.4.19 Main-mode standalone-object diagnostic

User authorised continuation after multi-entry Sub-element validation. Outstanding Phase 3 recovery only. 0.4.18 validated recovery is preserved; Main recovery remains UNSUPPORTED/UNPROVEN.

Read-only diagnostic on existing bulldozer callbacks: actualMode MainElements, up to eight actions/targets, captures native ControlPoint original entity; logs existence, Deleted/Temp flags, Owner, prefab identity, Transform, SubObject count and component type inventory before native Apply; accepted/denied apply; state after Apply and at elapsed frames 1,5,30,120. No Main restoration, entity recreation, component writes or extra ECS systems. Native game deletion proceeds normally. Deleted entity IDs are used only for bounded existence observations, never reused for restoration. Other edits/deletions continue to invalidate Sub-element history as before.

VERIFIED-STATIC: Game 0.0.0.0 MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef; reuse recorded bulldozer mode/control-point evidence. Release zero warnings/errors; production UI build; 25 lifecycle/history and22 snapshot/history assertions pass. Main runtime trace pending.

Test in disposable editor scene/duplicate: place ONE standalone prop not attached to an asset (as described by user). Use Main bulldozer to delete it. No need to Arm Sub-element history, and no Main Undo should be expected. Wait several seconds. Reply finished. Reload the disposable scene afterward if needed. Do not delete buildings, networks or groups in this first trace.

Next: establish standalone prefab/reference/child and deferred deletion boundaries from preserved trace, then review native creation route before any Main restoration implementation. Everything remains untested; Prop Replacer paused; no migration/publishing.
