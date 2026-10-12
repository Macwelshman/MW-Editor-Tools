# Version 0.4.5 — Settings UI failure correction

VERIFIED-RUNTIME (user, 0.4.4): Settings tab causes all Editor UI to disappear, requiring forced quit. Logs/UI.log at 17:54:55-56 records repeated TypeError: Cannot read properties of undefined (reading 'length'), executing JS :15:23. Logs prove a UI failure but do not identify the exact failing component.

HYPOTHESIS: HTML checkbox/select controls interacting with the native focus/navigation system triggered the error. 0.4.5 removes HTML input/select/option controls from Settings and uses native Button controls with the established editor button class/theme. Switches use On/Off; history/key choices use Previous/Next. Backend commands, settings validation, recovery and shortcut policies are unchanged.

VERIFIED-STATIC: C# Release zero warnings/errors; production UI build/type checks pass. VERIFIED-AUTOMATED: actual Settings component renders with all switches on and off using complete state and mocked native interfaces. This does not emulate Cohtml input/focus; runtime fix remains UNPROVEN. Prior 43 Phase 4 safety assertions cover unchanged backend logic.

Use a duplicate asset and test before making other edits:
1. Restart CS2, open MW Tools, then Settings. Editor UI should remain visible and responsive.
2. Switch between Settings and History repeatedly; Close/reopen. Existing MW icons and editor selection should work.
3. Toggle each setting off/on; use Previous/Next for history limit and both keys. Verify displayed values update. Restore defaults.
4. Close/reopen and restart CS2; verify persistence.
5. Continue PHASE_4_VALIDATION.md only after Settings remains stable.
6. If UI disappears again, stop testing; report the step and preserve logs after exiting. Do not treat this candidate as runtime-validated.
