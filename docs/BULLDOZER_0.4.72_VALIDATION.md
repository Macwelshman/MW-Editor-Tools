# 0.4.72 full prop history batch

VERIFIED-STATIC: The diagnostic Check full prop history command counts the adopted roots (one to four) and runs that many Undo, Redo and final Undo steps after the user deletes each root once. The runner waits for native recovery to finish between actions and validates history counts. Existing recovery algorithms and acceptance limits are unchanged.

VERIFIED-STATIC: Release build passes with zero warnings/errors; 620 C# assertions, settings rendering and 10 recovery-panel rendering checks pass. Runner checks cover counts one through four and refusal of empty/oversized sets.

VERIFIED-RUNTIME: 0.4.71 simple-only adoption and two-deletion history passed on MW Simple Prop Undo Test; preserved in bulldozer-0.4.71-simple-only-runtime.log.

UNPROVEN: Native full three-grill history including deletion of the final prop and native list-component recreation. Reopen the saved MW Simple Prop Undo Test baseline. Select the outer asset, Show diagnostic tools, Check full prop history. Wait for Test ready, then Everything-delete Grill01, Grill02 and Grill03 once each. Wait for automatic Undo3 / Redo3 / Undo3 and PASS. Do not save test edits.

No expanded network/lane/area/effect recovery or atomic rollback claim. Phase 5 remains paused; Phase 6/publishing unauthorised. No computer use.

VERIFIED-RUNTIME 0.4.72: MW Simple Prop Undo Test completed all three grill deletions and automatic Undo3 / Redo3 / Undo3. Last deletion removed ObjectSubObjects at 16:26:43.928; Undo created a fresh component at 16:26:43.945; final Redo removed it at 16:26:44.934; final Undo created another at 16:26:45.130. All nine operations verified recorded payload and object ownership. Final PASS at 16:26:45.725 with three groups/three nodes restored. Evidence docs/evidence/bulldozer-0.4.72-full-history-runtime.log. Final simple-prop recovery and retained history across component recreation are VERIFIED-RUNTIME on this fixture. No wider recovery claim. Continued immediately into normal-panel access for existing standalone recovery.
