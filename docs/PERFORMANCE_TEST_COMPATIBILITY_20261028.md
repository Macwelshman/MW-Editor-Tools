# performance-test compatibility check — 2026-10-28

Scope: DEBUG/INSPECT. No source changes, guard bypass, installation, game interaction or publishing.

VERIFIED-STATIC: Steam appmanifest_949230.acf identifies installed branch `performance-test`, build 26269843. MW Editor Tools source and loaded runtime are 0.4.254.

VERIFIED-RUNTIME (recorded logs, 18:43 session): ComponentConfirmation.Initialize rejects the build (logged IL 0x00091); ListRecovery.Initialize rejects it (0x0009f); EditorTargetingTool.Initialize rejects it (0x0002e). These are deliberate NotSupportedException checks, not missing-method exceptions. The targeting message saying Undo remains active is misleading in this session: recovery initialization already failed.

VERIFIED-STATIC: Game, Version=0.0.0.0, MVID e8325128-556a-460d-a950-497d90b3627c, SHA256 5885642745A77D39E6E34A3B61A78239F5267E1C8BDA8FD7819809630E14CBB5. Source guards expect Game MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. Additional exact-build guards cover component headings, localization/workspace operations, surface replacement, outline rendering and Asset Icon Creator integration; these must be audited before claiming beta support.

VERIFIED-STATIC: Unity.Entities MVID 17879230-8daa-4d82-a555-54adcb15b372; Colossal.Collections MVID f4863c05-b364-48ed-a003-9c869d32c740; Unity.Collections MVID 249ce684-a5c2-4e09-bd3b-cb6d8d7a862e. Reading installed assemblies used Colossal.Mono.Cecil without loading game runtime.

VERIFIED-STATIC: dotnet build MW.EditorTools.csproj -c Release --no-restore -o outputs/compatibility-performance-test/build succeeds, zero errors. NU1900 warning: NuGet vulnerability service unavailable. This establishes compile compatibility only.

VERIFIED-RUNTIME (recorded logs): shared UI mounted; Multi-Save completed 10 saves with 0 failed and 0 skipped. This does not certify all other operations. Current-session log files contain no other ERROR/Exception lines beyond the three MWEditorTools failures. UI.log contains CSS warnings. Historical logs were excluded by modification time.

UNSUPPORTED/UNPROVEN: full beta support, Undo, component confirmation, targeting and guarded dependent operations. Do not remove build checks merely because compilation succeeds. Smallest next experiment: compare the selected guarded native methods, field layouts and ECS dependencies against recorded stable evidence, then implement explicit dual-build support only for verified paths and perform grouped native acceptance on disposable assets.
