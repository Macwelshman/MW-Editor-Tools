# 0.4.17 deferred child-graph completion

VERIFIED-RUNTIME: 0.4.16 Undo restores owner/prefab payload/component shape exactly. At immediate prefab finalization expected child count 20, actual 19: Grill01 absent, remaining 19 entries match logged identities/transforms. Evidence: evidence/bulldozer-0.4.16-mismatch-runtime.log.
VERIFIED-STATIC: Game 0.0.0.0 MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. ReplacePrefabSystem.Finalize.UpdateInstanceElementsJob.Execute updates areas IL_00df and nets IL_0108; it is not an object-creation completion gate. SubObjectSystem.OnCreate requires SubObject IL_0074 with Updated IL_008c or Deleted; uses ModificationBarrier2B IL_0051. OnUpdate schedules jobs; nested UpdateSubObjectsJob has CreateSubObject routines using deferred commands. Evidence: evidence/bulldozer-instance-elements.txt and evidence/bulldozer-subobject-system.txt.
HYPOTHESIS: missing Grill01 is regenerated after the earlier validation point. This is not yet runtime proven.

0.4.17 waits until a later frame after matching native prefab finalization to validate exact prefab payload and full semantic child graph. While owner/payload remain valid, incomplete graph may settle within the original 120-frame budget. History advances only on a complete graph match. Owner/payload failures still reject; timeout logs graph differences and disables recovery. No direct entity creation, no timeout extension, no weakened equality.

C# Release zero warnings/errors; production UI build; 14 observation and 22 snapshot/history checks pass. Native live recovery remains UNPROVEN.

Test fresh saved disposable duplicate: select asset, Arm, delete one Sub-element grill keeping others, Undo. Wait for completion. If successful, Redo then Undo, and compare positions/probability/related effects/networks/areas. On failure reload saved duplicate before saving. Reply finished.

VERIFIED-RUNTIME PASS: User reports 0.4.17 worked. Preserved docs/evidence/bulldozer-0.4.17-success-runtime.log confirms Test single Sub-element deletion followed by Undo, Redo, Undo; each completed exact prefab payload and semantic child graph validation after deferred native creation. Tested single-entry explicitly armed scope only. Save/reopen, multiple history entries, Main, Everything, last-child and nested targets remain UNPROVEN/unsupported. Installed 0.4.17 retained; no phase expansion.



VERIFIED-RUNTIME (user-reported): Following the successful logged Undo/Redo/Undo test, user confirms all good in response to saving the duplicate after Undo and reopening it. Save/reopen acceptance recorded for the tested 0.4.17 single Sub-element prop case. Current bounded manual-arm/single-entry scope remains; Main/Everything, last-child, nested targets and multi-entry history not validated. No later phase authorised by this acceptance.


