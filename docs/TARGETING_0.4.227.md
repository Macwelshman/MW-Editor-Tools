# Targeting state transfer and dropdown — 0.4.227

VERIFIED-RUNTIME: screenshot displays the missing-state fallback while backend MWEditorTools.log reports targeting available for MW Mixed Test Components at 19:24:59.704. The 0.4.226 dropdown renders, confirming recovery from the blank-window issue, but popup items draw behind siblings with default styling. Previous subscription-only hypothesis did not resolve the missing state.

State serialization now uses the existing Newtonsoft.Json dependency for the shared public-field DTO, explicitly retaining nested TargetingState. Tools passes that exact targeting snapshot into Replacer as a prop; Replacer no longer creates another shared-state hook. Availability/activation safety rules and all Undo, companion and reference code remain unchanged. Frontend diagnostics record targeting presence, availability and reason to distinguish wire-state problems from real readiness failures on the next native run.

Dropdown supplies scoped native theme classes for an opaque background, light text, popup stacking and bounded scrolling. Target controls establish a local stacking level. No browser select/option introduced. Native renderer stacking still requires acceptance.

VERIFIED-RUNTIME (managed harness): 61 actual-source targeting/feedback assertions, including the actual installed Newtonsoft serializer retaining nested availability/reason; 23 UI targeting/dispatch assertions; launcher render/lifecycle, header-focus and local tab error fallback checks pass. Release/UI builds pass; cached NU1900 advisory-feed warning persists. Focused diff whitespace checks pass.

Build staged at outputs/test-package-0.4.227/MWEditorTools. Installation status is recorded at docs/evidence/targeting-0.4.227/install-status.txt. If deferred, 0.4.226 remains installed until the game exits. No native gameplay acceptance claimed. Next grouped check: open window, select editable owning asset with native Select active, confirm live readiness text, activate targeting, choose type, check hover/selection and existing guarded copy/replace/Undo.

INSTALL VERIFIED 2026-10-23: user confirmed game closed; elevated absence checks passed. Installed assembly 0.4.227.0 and all three copied hashes verified. Nine prior installation files backed up and hash checked at outputs/rollback/MWEditorTools-before-0.4.227-20261023-192830/MWEditorTools. See outputs/installation-0.4.227.txt. Native acceptance pending.
