# Foliage companion preflight correction — 0.4.156

VERIFIED-RUNTIME: 0.4.155 logs Ctrl+C success for FlowerBushCity01 and FlowerBushCity03. Paste refuses before source writes with the new leaf-foliage companion restriction. No foliage candidate was created in those attempts. Therefore fade correction was not exercised.

The blanket source-companion restriction is replaced by bounded validation of supported Object/Area/Net/Effect/Lane source lists and live reference/ownership checks. Donor runtime companion buffers are bounded rather than required empty. New candidates still use the actual replacement native prefab archetype and Created/Applied/Updated initialization. Existing child entities and native EnabledEffect handles are never copied into the candidate; the native systems initialize the replacement prefab's declared companions. Existing copied-source identity/pose checks, unknown archetype refusal and candidate rollback remain intact.

VERIFIED-STATIC: Release/UI build and snapshot/history checks pass. Actual candidate tests now also supply a donor with a child and an allocated EnabledEffect handle, verifying that neither identity/handle is transferred or changed. Native creation with declared companions and fade correction remain UNPROVEN.

Acceptance: repeat FlowerBushCity01/03 copy and paste, then replace-all. Require successful candidate/retained-source log messages, no fade or movement of the copied source, correct replacement placement/count and intact native effects. Use a disposable asset and do not save over the original.
