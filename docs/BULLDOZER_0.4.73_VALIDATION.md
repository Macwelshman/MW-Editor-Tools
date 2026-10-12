# 0.4.73 standalone recovery controls

VERIFIED-STATIC: Enable standalone prop Undo is now available in the normal History & Recovery panel through the existing standalone Main/Everything recovery path. The published enabled state disables repeated enable clicks. Both the public command and native arming entry preserve history when already enabled. Switching from asset recovery to standalone recovery still explicitly clears previous history; the panel explains this. Main enabled status is displayed even without an asset selection. Native capture, restoration, schema guards and limits are unchanged.

VERIFIED-STATIC: Release build has zero warnings/errors. All 620 C# assertions, settings rendering and 14 recovery-panel rendering checks pass. Rendering covers standalone enable availability, unavailable state, enabled state and visibility outside diagnostics. Native interaction of the newly exposed control remains UNPROVEN; existing standalone recovery runtime evidence remains separate.

VERIFIED-RUNTIME: 0.4.72 full three-grill history passed all nine recovery steps, including native ObjectSubObjects component removal and recreation. Preserved evidence bulldozer-0.4.72-full-history-runtime.log.

Small interaction check only: normal panel / Enable standalone prop Undo; Main-delete one separately placed Grill01; Undo, Redo, Undo. The button should read Standalone prop Undo enabled and be disabled while enabled. Use a disposable editor session; do not save. No asset fixture setup or repeated three-grill test required.

No expanded network/lane/area/effect recovery or atomic rollback claim. Phase 5 paused; Phase 6/publishing unauthorised. No computer use.

VERIFIED-RUNTIME 0.4.75: User confirms Clear works well. Log shows dialogs for ObjectSubAreas (2 entries) at16:49:55.884, ObjectSubNets (1) at16:50:03.872 and ObjectSubLanes (1) at16:51:40.981; confirmed lane Clear dispatched at16:51:43.481. No claim that every dialog was confirmed. Normal standalone control enabled16:52:46.825; standalone Grill01 capture/deletion recorded16:52:59.517; native Undo verified16:53:04.985 and Redo verified16:53:06.276. The preserved log does not contain a final Undo, so that final step is not claimed for this run. Evidence docs/evidence/bulldozer-0.4.75-clear-standalone-runtime.log. Prior bounded standalone recovery evidence remains valid. No need to repeat these passed operations for a display-only change.
