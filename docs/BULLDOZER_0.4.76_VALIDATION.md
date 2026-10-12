# 0.4.76 named recovery targets

VERIFIED-STATIC: Normal recovery panel now shows Next Undo and Next Redo descriptions from the actual latest and next-redo records. Bound single props include prefab and asset names; nested groups are identified as groups; standalone props use their prefab name. Existing list row recovery uses its recorded label. History entries now identify the deleted prefab instead of labelling simple props as nested groups. Labels disappear when their corresponding cursor has no entry. No recovery mutation, acceptance limit, ordering or native timing change.

VERIFIED-STATIC: Release zero warnings/errors; 646 C# assertions pass, UI builds and settings rendering pass. 17 recovery-panel rendering checks include empty history, named Undo and a named Redo after all Undo entries are consumed.

VERIFIED-RUNTIME inherited: user confirms 0.4.75 Clear dialog works. Preserved log proves actual Sub Areas/Nets/Lanes dialogs and confirmed lane Clear, plus normal standalone control Grill01 Undo and Redo. Final Undo absent from that log and not claimed. Full three-grill Undo3/Redo3/Undo3 including component recreation passed on0.4.72. New label rendering is verified statically; live appearance pending normal use, no destructive repeat required.

Scope remains bounded supported prop/list recovery. No bulk Clear Undo, generic network/lane/area/effect bulldozer recovery, atomic rollback, phase expansion, publishing or computer use.
