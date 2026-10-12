# 0.4.24 Main native creation-marker guard correction

VERIFIED-RUNTIME:0.4.23 captured and recorded Main Grill01 deletion; Undo preflight refused Game.Common.Created in native prefab archetype. No fresh entity created because refusal precedes CreateEntity. Evidence evidence/bulldozer-0.4.23-main-runtime.log.

Correction: creation-only archetype guard accepts explicitly verified Game.Common.Created,Updated,Applied markers in addition to known static prop types. Full archetype inventory and all unknown types now logged together. Source-capture and final initialized-prop allowlists remain unchanged; unknown final components or property mismatch still refuse. Native marker evidence main-applied-markers.txt,ApplyObjectsSystem.OnCreate IL_00b8/00bd/00c2 and Create IL_00a1,Game0.0.0.0 MVIDe15551e9-3dea-4855-8dde-1da3cea7d7ef. No general component cloning or arbitrary unknown-type acceptance.

Release zero warnings/errors;production UI;25 lifecycle/history and29 snapshot checks pass. Live Main creation remains UNPROVEN. Same explicitly armed one simple standalone prop test as0.4.23.

Retest fresh saved disposable scene: place standalone Grill01,Arm one Main prop Undo test,Main delete,Undo. If completed,Redo thenUndo and inspect position/rotation/appearance/no duplicates. Editor scene cannot be saved; check restored appearance/position/no duplicates instead. On failure reload saved scene before saving. Reply finished.

VERIFIED-RUNTIME0.4.24 Main live PASS: evidence/bulldozer-0.4.24-main-runtime.log shows standalone Grill01 accepted deletion; native archetype has Created/Updated; Undo creates fresh Entity50520:353 (original50521:1), initialization/properties validate;Redo confirms recreated entity disappearance;furtherUndo creates50520:355 thenRedo succeeds;finalUndo creates50520:357 andvalidates. ThreeUndo/twoRedo completions,noerrors. Single ownerless simple prop,seed43081,one MeshColor,emptyCustomMeshColor. Save/reopen and user visual no-duplicates/appearance acceptance pending. No broad Main/multi-entry/customcolour claim. Installed0.4.24 retained;Sub-element recovery preserved.



VERIFIED-RUNTIME (user-confirmed):0.4.24 restored standalone Main Grill01 appearance/original position/exactly one copy confirmed after logged repeatedUndo/Redo pass. User corrected that editor scene cannot be saved; remove Main editor-scene save/reopen as a requested gate. Main persistence NOT TESTED and not claimed. Future Main tests use disposable editor session and reload asset/restart editor without saving scene; describe discarding test state rather than assuming scene save support. Bounded single simple Main prop acceptance passes;Sub-element asset save/reopen acceptance remains separate and unchanged.


