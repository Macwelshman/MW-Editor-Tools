# Native acceptance — 0.4.251

VERIFIED-RUNTIME (2026-10-25): user reports selection, highlighting and replacement/Undo all passed. Current MWEditorTools.log confirms 0.4.251.0 loaded at 23:25:08. Batch replacement of seven elements completed at 23:28:14; Undo completed at 23:28:17. Single replacement completed at 23:28:57; Undo completed at 23:29:04. Both restoration messages report original prefabs, placement and settings restored.

Full MWEditorTools/UI/Player logs contain no [ERROR], [CRITICAL], exception, focus registration failure or conflict match in this session. Asset Icon Creator 1.4.1 adapter installed normally. Successful row/layout diagnostics removed by the cleanup are absent.

HYPOTHESIS supported by timing/source comparison: informational target/source mismatch dumps 2–3 ms after replacements describe the old live prefab while editable source rows already contain the replacement. These are transient validation refusal reports during native regeneration, not failed replacement/Undo messages. No subsequent failure is logged. They remain a diagnostic-noise cleanup candidate, not a reason to relax matching.

UI.log contains CSS compatibility warnings and Move It's own 'Error reading Icon rectangle [MIT04]' informational messages. Their origin/impact is not established as an MW defect by these logs. Native performance and arbitrary mod combinations remain unproven.
