# Asset Icon Creator thumbnail compatibility — 0.4.223

User-authorised IMPLEMENT scope: thumbnail assignment preserves existing Group/Priority while keeping accepted Undo and companion/reference validation intact. No third-party/game files edited, publishing, playset changes or computer use.

## Supported integration

VERIFIED-STATIC: installed `AssetIconCreator, Version=1.4.1.0`, MVID `2c652cbe-d9b5-46ec-8850-c7a39cf7d8a6`, SHA256 `A77E25A6EE15891D7457444014AE0DA02B57A2482076A60EF1C87BC5B7A47004`. The verified method is `ScreenshotUtility/<>c__DisplayClass33_0.<ProcessScreenshot>b__0`, token `0x060000a1`, instance void/no parameters. Original selected IL is preserved in `docs/evidence/editor-compatibility-0.4.222/selected-details.json` and described in `EDITOR_MOD_COMPATIBILITY_0.4.222.md`.

The optional adapter waits for this assembly to load, verifies assembly version/MVID, current Game.dll MVID `e15551e9-3dea-4855-8dde-1da3cea7d7ef`, Harmony2.4.2, method signature/token and a unique ordered pair of typed Remove/Add calls. It refuses an already patched callback. It attaches only to that third-party thumbnail completion callback under its own Harmony owner; it does not patch Game/Undo methods or broaden MW's native owner allowlist.

The original `Remove<UIObject>` call at IL_0082 becomes a receiver `pop`; `AddComponent<UIObject>` at IL_008d is redirected to a helper that returns the existing `TryGet<UIObject>` result or creates the component if missing. The stack effects, branch labels and exception boundaries are retained. The helper uses the same TryGet semantics as Asset Icon Creator, including derived components. Existing component identity, Group reference, Priority and other settings remain in place. Asset Icon Creator's own assignments still set its new icon and invoke its usual single UpdatePrefab/thumbnail-result flow. Automatic assignment disabled in Asset Icon Creator remains disabled; no preference is changed.

The adapter does not restore cached values after refresh and does not replace or clone any component. Therefore it avoids briefly applying default Group/Priority and avoids exchanging the component identity tracked by MW priority/component restore checks.

## Isolation and limits

An absent dependency is normal; discovery polls every five unscaled seconds until loaded. Unknown versions/builds or unexpected original IL disable only this optional integration with an explanatory log; the rest of MW continues. No third-party assembly is bundled or modified on disk. Cleanup removes only this adapter's owner from its target.

The existing ownership monitor also checks this callback for late foreign patches/removal of MW's owner. A conflict retires the adapter and explicitly warns that automatic UI Object setup is again unprotected and can reset Group/Priority. A later unfamiliar instruction stream is left untouched rather than throwing into another mod's patch installation; the preflight/owner checks handle refusal and retirement. Unverified future Asset Icon Creator builds require inspection before support.

VERIFIED-STATIC: native Undo source, captured payloads, companion/reference maps, source matching, geometry tolerances and completion checks are unchanged. `undo-source-comparison.json` compares their hashes against0.4.222. Thumbnail edits are still Asset Icon Creator operations, not a new MW Undo action. External asset refresh may invalidate earlier snapshots under existing guards; this adapter does not bypass those guards or coordinate another mod's asynchronous work with an in-flight MW restoration.

## Validation and grouped native check

VERIFIED-RUNTIME (managed harnesses):35 actual-adapter/Harmony checks cover reproduced original priority loss; retained identity, exact Group reference, integer Priority boundaries, other settings, new icon, missing/derived UI Object, disabled assignment, repeated/different assets, refresh/completion counts, stack/label/exception metadata, unchanged input, unknown IL refusal/no-op, late foreign-owner retirement, other-owner retention, absent dependency and idempotent disposal. Lifecycle24 checks include optional adapter startup failure without losing Undo/UI/other modules. Existing native Undo managed regression suites also pass. Total1,686 assertions across12 harnesses; Release/UI builds pass. The existing cached NU1900 advisory-feed network warning remains; no dependency audit completion is claimed.

UNPROVEN: real screenshot generation, compatibility attachment under the game's loader and save/reopen with this build. Next grouped check on a disposable asset: assign a non-default Group and Priority; generate/update its thumbnail twice with automatic UI Object setup enabled; verify both values and other UI settings remain; verify a normal prop move/deletion Undo and marker/bike-rack Undo as applicable; save/reopen. Check `MWEditorTools.log` for the successful Asset Icon Creator compatibility installation message and any unexpected conflict refusal. Previous accepted Undo/persistence remains the baseline, not fresh native acceptance for0.4.223.

Evidence: `docs/evidence/asset-icon-compatibility-0.4.223`.

INSTALL VERIFIED2026-10-23:0.4.223.0 installed after elevated Cities2 absence checks. Three DLL/MJS/CSS hashes match staging; nine previous files backed up/hash-verified at `outputs/rollback/MWEditorTools-before-0.4.223-20261023-173641/MWEditorTools`. Installed hash evidence: `docs/evidence/asset-icon-compatibility-0.4.223/installed-hashes.csv`. No third-party files or game files were replaced. Native acceptance remains pending.
