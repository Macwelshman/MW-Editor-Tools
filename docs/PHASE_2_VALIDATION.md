# Phase 2 validation and test build

## Implemented scope

Only component-header RemoveComponent is intercepted. A Harmony prefix blocks the initial body; the game's own dependency collector supplies the dialog list. On affirmative action 0, a queued reply is processed on the host's Unity Update thread; context is revalidated and the original handler is invoked with a consumed one-shot bypass. No lower-level component-removal or entity destruction patch is installed.

Guarded against: wrong Game.dll MVID, different loaded Harmony version, pre-existing deletion handler patches, read-only/stale objects, non-editor/loading context, mismatched inspector ownership, unavailable/busy native dialog, duplicate clicks, expired requests (60 seconds), changed component sequence/closure, selection changes including away-and-back, game load/mode changes and mod disposal. Another native dialog invalidates the pending request instead of replacing its callback with a deletion action.

Unsupported build/startup failure logs an explicit error and does not install protection. While installed, validation/dialog errors block the requested deletion. If the vanilla deletion itself throws after confirmation, the mod logs it; atomic rollback is not provided.

Invalidation does not force-close the native dialog or erase another UI callback. An old dialog can remain visible until dismissed, but its token cannot authorise removal. No recovery history exists.

## Verified automated checks

- Release net48 build passes with zero warnings/errors against installed game assemblies.
- 15 safety assertions pass: duplicate request rejection; wrong/stale reply isolation; cancel/dismiss; timeout boundary; one-shot consumption; managed Harmony cancellation, original invocation, nested bypass rejection, exception cleanup and owner unpatch.
- The Harmony smoke test runs under .NET 8 against a synthetic method. It does not verify the Unity Mono runtime or the actual inspector handler.
- No installed game assembly was written and no automatic deployment target was executed.

Harmony behavior follows its [prefix documentation](https://harmony.pardeike.net/v2/articles/patching-prefix.html) and the pinned [Lib.Harmony 2.4.2 package](https://www.nuget.org/packages/Lib.Harmony/2.4.2). Static API checks and successful compilation are not runtime proof.

## Manual acceptance steps — user-reported results recorded below

Install the contents of the separate test-build package into a new MWEditorTools folder under CS2's local Mods directory, then restart the game. Preserve existing mods and original assets. This project does not install itself. The package includes MW.EditorTools.dll, 0Harmony.dll and the Harmony licence. Use the log to confirm that protection installed; a failed startup means the vanilla controls remain unprotected.

1. Open a duplicate writable asset and select a simple optional component. Click its header delete button. Verify a native dialog appears, names the component and warns that undo is unavailable.
2. Cancel, close and Escape separately. Compare component settings, collections and associated entities before/after; all must remain unchanged. Confirm the component survives save and reopen after cancellation.
3. Confirm on a fresh copy. Verify the original component removal occurs once and matches vanilla behavior. Repeat with dependent components; displayed and removed closures must agree.
4. Repeat clicks while pending; verify no extra dialog/removal. Change selection away and back, or change/load assets while pending; later confirmation must perform no deletion. Let the dialog expire for 60 seconds and confirm; no deletion should occur.
5. Exercise another native dialog, editor exit, mod disposal and read-only assets. No stale callback may delete data. Review errors before saving.
6. Verify Object/Area/Net/Effect row deletion and bulldozer behavior are unaffected; do not expect protection on those paths.

Record game version/MVID, load status, asset category, dependency types, action, before/after counts and logs. Phase 2 completes only after this matrix passes in-game. Compatibility with additional deletion patches is deliberately disabled pending review.

## Known limits

English literal dialog text; no settings toggle or unified window yet. Conservative selection invalidation may cancel a request during benign inspector refresh. Only the inspected Game.dll MVID is allowed. Display uses C# type names rather than localised component labels. Other Harmony patches can affect lifecycle methods; compatibility requires live checks. No release installer, native postprocessing or publishing was attempted.

## User-reported in-game validation

The user reports normal operation without errors and explicitly confirms the 60-second timeout and save/reopen after cancellation pass. The native dialog blocks selection changes until dismissal; the normal-UI selection-change test is not applicable. These are user-reported results, not independently observed runtime traces. Specific dependent-component cascades, other-mod compatibility and forced lifecycle interruption remain undocumented.

Phase 2 is complete for the tested component-header confirmation scope on the installed build. These test results do not authorise Phase 3 implementation.
