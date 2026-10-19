# Bound foliage transient refresh — 2026-10-14

VERIFIED-RUNTIME: user reports all tested recovery types now work on 0.4.108, with another same-prefab bound foliage instance fading then returning during foliage Undo. The same retained instance is affected. Standalone foliage is not the reported case.

VERIFIED-STATIC: bound recovery restores the owning asset payload then calls PrefabSystem.UpdatePrefab(asset, Entity.Null) in BulldozerTrace.Restore. Native Game, Version=0.0.0.0, MVID=e15551e9-3dea-4855-8dde-1da3cea7d7ef, SubObjectSystem.UpdateSubObjectsJob.FindOldSubObject(Entity, Transform, int, ref UpdateSubObjectsData) selects the nearest available same-key old object when alignIndex is negative: IL_0060–007e initial candidate, IL_00b1–00da nearest comparison, IL_00fb–0104 removes selected candidate. There is no exact-position requirement. Evidence: docs/evidence/undo-0.4.108-foliage-matching.txt.

HYPOTHESIS: while processing the reinstated descriptor, native matching borrows the nearest surviving same-prefab instance, moves it to the restored placement, then regenerates the retained placement. This explains a repeatable neighbour fading even though the final graph validates. Logs confirm successful final nested recovery, but do not record native matching decisions or rendering transitions. This is not yet proof of the visual cause.

No runtime fix or new package installed for this report. 0.4.108 remains installed. A correction must preserve retained identity during native regeneration and retain exact descriptor/payload validation; post-hoc typed value restoration alone cannot prevent an earlier render transition. No computer use performed.
