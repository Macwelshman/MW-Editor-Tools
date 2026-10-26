# Targeting availability — 0.4.225

User reported Target elements disabled, showing its fallback tooltip rather than the backend availability reason.

VERIFIED-STATIC: window and Replacer previously constructed separate bindings for mwEditorTools/state. Both now import a single binding instance. HYPOTHESIS: duplicate subscription registration left Replacer on its fallback state; native confirmation is pending. Activation checks are unchanged. Filter remains visible while inactive; availability status is displayed below the controls, with an explicit missing-state message. Backend logs availability transitions without logging every hover change.

VERIFIED-RUNTIME (managed harness): 60 actual-source targeting/feedback assertions, 22 UI targeting assertions, launcher rendering/lifecycle and header focus checks pass. Release and UI builds pass; cached NU1900 advisory-feed warning remains. No Undo capture/restoration, companion or reference mapping changes.

INSTALL VERIFIED: game absence checked; 0.4.225.0 installed and all three copied hashes verified. Nine previous files backed up and hash verified. See outputs/installation-0.4.225.txt and docs/evidence/targeting-0.4.225/installed-hashes.csv.

UNPROVEN: native activation after the subscription correction. Select an editable asset in Object Info and use native Select, then open Replacer. The filter and readiness/refusal message should remain visible before activation. If disabled, report the displayed message; availability transitions are also recorded in MWEditorTools.log. Preserve native grouped Undo acceptance requirements from 0.4.224.
