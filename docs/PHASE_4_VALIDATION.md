# Phase 4 in-game checklist

Use a duplicate asset. Open the Asset Editor and click MW Tools beside the editor sidebar. In 0.4.1 it is below the existing launcher icons; first follow PHASE_4_0.4.1_VALIDATION.md.

1. Window: opens, closes/reopens, drags, scrolls and changes tabs. Undo/Redo remain visible on every tab. Change the selected asset: window stays open and its asset label updates; previous asset history clears.
2. Recovery: delete a supported Object row, press Ctrl+Z once, then Ctrl+Shift+Z once. Verify the row and all original values return/disappear exactly once. Repeat for Area, Net and Effect; compare with the window and inspector buttons.
3. Focus: with a recoverable deletion pending, click an editable native field, search field and any text input, then press both shortcuts. The asset deletion history must not move while typing. Close the input/focus another control and verify shortcuts work again. Hold Z: no repeated undo.
4. Dialogs: open a component-removal confirmation, try shortcuts, then Cancel. Neither deletion history nor components should change. Confirmation still applies once when accepted. Save/reopen and editor exit must clear recovery history.
5. Settings: switch shortcuts off/on; change key letters and test exact Ctrl / Ctrl+Shift combinations. Alt/Meta combinations must not trigger recovery. Disable list recovery: supported deletions are not recorded. Re-enable: start a fresh history. Hide inspector controls: shared window remains usable.
6. Persistence: change history limit, shortcut keys and switches; drag the window, close/reopen it, then restart CS2. Check settings and position persist. Restore defaults after testing.
7. Regression: Phase 2 Cancel/Confirm/dependency removal; Phase 3 multiple Undo/Redo, redo invalidation after new deletion, save/reopen of restored data. Other original MW mod windows must remain usable.

Changing recovery or history limit clears history intentionally. Whole-component and bulldozer undo remain unsupported. If MW Tools is missing or shortcuts fail, report which step and retain Player.log / Logs; automated checks do not prove native UI/input behaviour.
