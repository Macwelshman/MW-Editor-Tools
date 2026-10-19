# 0.4.90 preserve vertical position while resizing

VERIFIED-STATIC: installed game UI index.js, game-ui/common/panel/draggable-panel/draggable-panel.tsx, DraggablePanel computes top as position.y * (viewport height - measured panel height). This explains movement with content height. Shared wrapper now pins the pixel top after native initial placement; native header dragging updates the pin. Resize corrections do not persist a changed fractional position. Content-based sizing retained.

Release build zero warnings/errors; UI build, TypeScript and existing UI suites pass. Additional actual observer test verifies resize preservation, drag updates and persistence exclusion for resize. Native interaction remains UNPROVEN pending user test. No computer use per user instruction.
