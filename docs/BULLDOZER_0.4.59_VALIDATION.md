# 0.4.59 test status visibility

VERIFIED-STATIC: Active nested tests and existing recovery history take priority over the Start-test selection warning. Start guards and native recovery remain unchanged. Regression runner now checks that clearing inspector selection leaves the test armed and permits deletion detection.

Release build: zero warnings/errors. UI build and settings render pass. All 467 assertions pass, including 47 nested runner assertions. Runtime visibility check pending.

VERIFIED-RUNTIME: Test ready remains visible while bulldozer is selected. Assistant tested sole Carport01 at X12. Native deletion removed ObjectSubObjects entirely; nested path refused before mutation. This exposes missing last-prop routing for nested recovery; no Undo attempted. Evidence docs/evidence/bulldozer-0.4.59-computer-use-runtime.log. 0.4.60 routes sole nested root through existing LastRemoval/AlignLast checks and still requires exact unchanged retained fields/references and full native graph validation.
