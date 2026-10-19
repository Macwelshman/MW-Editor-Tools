# 0.4.91 single position owner

VERIFIED-RUNTIME (user screenshots/report): 0.4.90 jumps between tabs and flickers while moving. The observer correction failed native acceptance; mocked observer tests did not model native React measurement feedback.

VERIFIED-STATIC: replaces DraggablePanel plus style mutation correction with native Panel and native DragHandle (installed UI index.js exports game-ui/common/panel/panel.tsx Panel and game-ui/common/input/drag-handle.tsx DragHandle). Shared wrapper owns pixel coordinates in React state. Saved fractional coordinates convert once after initial layout; resizing never recomputes position. Drag uses native grab offsets; only drag completion persists normalized coordinates. Native panel theme, focus scope, title bar and content-sized body retained. No MutationObserver or competing position writer.

Release build zero warnings/errors; UI build and TypeScript pass. UI suites pass including actual hook-driven initialization, height changes, successive drags and persistence checks. Native visual behaviour remains UNPROVEN pending user test. No computer use. Package prepared; installation pending game exit.

Installation complete after verifying Cities2 exited. Previous installation preserved under outputs/rollback/MWEditorTools-before-0.4.91-20261012-162017. All copied SHA-256 hashes verified. User will reopen and perform native testing; no computer use.
