# Last bound prop — read-only diagnostic on installed 0.4.28

VERIFIED-STATIC: Game.Tools.ApplyPrefabsSystem.UpdateObjectSubObjects removes ObjectSubObjects at IL_004f when no object entries remain, and can remove EffectSource IL_00a3/ActivityLocation IL_00ea. Game0.0.0.0 MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef, SHA256 AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A; evidence/detail.txt. Existing recovery requires unchanged component identities and at least one remaining prefab object. Last-prop Undo remains UNSUPPORTED.

Existing Everything diagnostic already captures component shape/names and before/after payload at matched SaveInstance, and inventories owner/child existence. Reuse it without adding code or weakening recovery checks. Actual last-item component presence/ownership and regeneration are UNPROVEN.

Test on a fresh disposable duplicate, never original Test:

1. Use Object Sub Objects list to remove every object entry except Grill01. Leave other components as they are. Confirm exactly one entry remains in that list (effect editor containers are separate).
2. Do not arm either recovery test. Clear history if the setup list edits produced entries.
3. Select Everything and bulldoze the sole bound Grill01. Do not attempt Undo.
4. Wait a few seconds, report finished, keep CS2 open until log copied. Note whether the Object Sub Objects component disappeared.
5. Discard these unsaved edits by fully restarting CS2 afterwards; do not save diagnostic changes.

Read-only experiment only; no last-item recovery or general component reconstruction advertised. Next use exact observed delta to design separate component-presence snapshots/native reattachment with full payload/reference/deferred graph guards, or retain unsupported status if not proven safe. Preserve accepted Main/Sub/Everything behavior.
