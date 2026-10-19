# 0.4.89 content-sized shared window

VERIFIED-STATIC: shared body now uses height:auto with max-height:85vh. Its section uses flex:0 1 auto and overflow-y:auto, allowing the active tab to determine height and long content to scroll within the viewport cap. Native position/dragging code unchanged.

Release build passes with zero warnings/errors. UI build, TypeScript and all five UI suites pass. Native tab-switch sizing remains UNPROVEN pending user testing. User requested no computer use and will close and test the game. Installation awaits confirmed process exit.

Installation complete after verifying Cities2 exited. Previous installation preserved under outputs/rollback/MWEditorTools-before-0.4.89-20261012-160836; every copied file SHA-256 matched. User will reopen and test; no further computer use.
