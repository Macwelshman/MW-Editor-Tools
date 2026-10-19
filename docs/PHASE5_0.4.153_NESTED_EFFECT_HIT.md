# Actual viewport hit and nested owner refresh — 0.4.153

VERIFIED-RUNTIME: the 0.4.152 log records simple viewport replacements completing, several nested attempts taking browser fallback because samples contained the entire Workspace text, and effect copy resolving an unsupported owner entity. Ownerless destination errors occurred before source mutation.

VERIFIED-STATIC: read-only Cecil inspection of installed Game.dll confirms ToolBaseSystem.GetRaycastResult(Entity&, RaycastHit&) reads RaycastResult.m_Owner at IL_0027 and m_Hit at IL_0033. DefaultToolSystem.Update stores that owner in m_LastRaycastEntity (recorded evidence/effect-default-hover-native.txt, IL_004c). RaycastHit has m_HitEntity. ToolRaycastSystem.GetRaycastResult(out RaycastResult) is public and returns native data without changing it. Temp.m_Original supplies an explicit original for temporary markers. The previous implementation incorrectly used the owner cache as the hit identity.

Copy and paste now use the actual hit after requiring its raycast owner to agree with the native hover cache. Every intervening live Owner link must occur exactly once in the parent's buffers; cyclic, missing, duplicate or over-budget paths refuse. Temporary markers require an original entity. Exact descriptor placement matching remains unchanged.

Nested destination owners may belong to a verified chain of writable prefab parents. Each nested prop parent link must match a unique ObjectSubObjects descriptor using BoundDescriptorIndex, without trusting cached descriptor indices. Successful or rollback regeneration queues the edited source and its verified ancestors. Read-only nested source/parents remain refused.

When browser samples contain no exact source prefab label, paste may resolve a valid current viewport hit rather than treating whole-application HUD text as an ambiguous row. Exact browser labels with ambiguous occurrences retain browser refusal.

VERIFIED-STATIC: Release/UI builds pass. Actual-source hover/ownership tests cover same hit, nested hit, missing owner, duplicate/missing links, cycle and depth limit. Existing snapshot/history and effect restoration regressions pass. Native nested/effect copy, paste, batch replacement and persistence remain UNPROVEN pending grouped disposable testing.
