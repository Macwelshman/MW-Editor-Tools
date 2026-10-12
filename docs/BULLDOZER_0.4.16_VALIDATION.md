# 0.4.16 validation mismatch diagnostic

VERIFIED-RUNTIME: 0.4.15 records the deletion, dispatches Undo and reaches synchronous AfterFinalize, then rejects restored state. Exact failed comparator is UNPROVEN because 0.4.15 combines owner, payload and graph into one error. Preserved evidence: evidence/bulldozer-0.4.15-validation-failure-runtime.log.

0.4.16 adds separate owner/payload/component-shape/graph results, changed prefab field names, and expected/actual child prefab/container identities and transforms (limited to 32 children per image). This is diagnostic instrumentation; restoration behavior, strict validation, single-entry scope and fail-closed handling are unchanged. No successful Undo/Redo claim.

Builds: C# Release zero warnings/errors; production UI build; 14 bulldozer lifecycle and 22 snapshot/history assertions pass. Native restoration remains UNPROVEN.

Test on a fresh saved disposable duplicate: select owning asset; click Arm next Sub-element Undo test; delete one grill with others remaining; click Undo once. Stop after the diagnostic error and reload the saved duplicate before saving. Report finished so the diagnostic can be read. Do not attempt Redo on this failed state.

Same user-authorised bounded experiment. No phase expansion or game-file edits.
