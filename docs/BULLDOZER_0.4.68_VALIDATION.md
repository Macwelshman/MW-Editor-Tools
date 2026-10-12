# 0.4.68 everyday nested Undo panel

VERIFIED-STATIC: EnableNestedHistory is idempotent once manual nested history is armed. Both the public ListRecovery wrapper (before its list-history Clear) and the trace command preserve existing history on another Enable request. An automatic test may still be explicitly replaced by manual mode when ordinary CanArm allows it; native busy guards remain unchanged.

The published UI state now includes nestedHistoryEnabled. The main panel shows a disabled Nested prop Undo enabled button while armed, and disables initial enabling when CanArm is false. The two-carport automatic check and its instructions are available under Show diagnostic tools instead of the everyday card. Undo/Redo, explicit experimental/disposable-copy messaging, shortcuts and Settings remain available.

VERIFIED-STATIC: Release zero warnings/errors, UI build pass, all 564 C# assertions pass (three additional actual-source idempotence assertions preserve undo/redo and in-flight state). Settings rendering passes, plus eight new actual-panel rendering checks covering enabled/unavailable buttons, history visibility and diagnostic control placement. No recovery schema, native patches or supported deletion scope changed.

UNPROVEN: native Cohtml interaction with the revised panel and current build's successful preparation path. Combine ordinary use/smoke acceptance with the next useful in-game batch rather than repeating prior persistence and six-operation tests solely for this UI change. Prior versions' runtime evidence remains separate.

Current bounded use: explicitly enable nested history on a disposable writable selected asset containing one supported nested prop type; Everything-delete a supported group, then use Undo/Redo. Ordinary Carport03 multi-history/mixed payload and restored-save persistence passed on preceding versions. Unsupported nested attachment data refuses; network/lane/area/effect deletion recovery and atomic rollback remain unsupported. Phase 5 paused; Phase 6/publishing unauthorised; no computer use.
