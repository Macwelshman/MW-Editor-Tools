# Object Info hover replacement — 0.4.151

VERIFIED-RUNTIME (user report): 0.4.150 Object Sub Objects hover Ctrl+C passes.

Ctrl+V now resolves the destination from fresh Object Info hover samples and the current asset, requiring the copied descriptor type and exact displayed prefab name. Repeated prefab occurrences require an unambiguous Element number matching the live underlying index. Container samples containing several Element numbers cannot choose an occurrence. Ctrl+Shift+V uses the same target to batch-replace matching references in its owned list.

Replacement writes only the reference field and preserves all other row values. Existing full-list rollback/native refresh remain active. Clipboard type and field assignability, writable root, focus/typing/dialog checks and recovery/save busy interlocks apply. The hover candidate is bound to its asset at receipt. Viewport paste is not yet supported.

Managed target-policy checks cover exact repeated-row targeting, missing row numbers, concatenated container labels, unique names, stale indices, separate number/name labels and unknown labels. Release/UI builds and shared tab regression checked. Native replacement and save/reopen acceptance remain UNPROVEN.

Grouped acceptance on a disposable duplicate: copy a different Object row; hover the collapsed header of one repeated destination prefab and Ctrl+V; confirm only that occurrence changes and position/rotation/probability remain. Copy another source and Ctrl+Shift+V on a destination; confirm all and only matching entries change. Save as a new disposable asset and reopen to confirm persistence.
