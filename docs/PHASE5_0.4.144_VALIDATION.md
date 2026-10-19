# Phase 5 — 0.4.144 validation

## Scope

VERIFIED-STATIC: the shared Prop Replacer tab copies the referenced prefab from a selected Object/Area/Net/Effect/Lane inspector row. Single replace changes only the reference field. Batch replace changes rows in the same owned list whose prior reference matches the selected destination. Complete list snapshots provide synchronous rollback; the native prefab update and asset dirty routes are queued after writes.

VERIFIED-STATIC: the original MW Component Tool 0.4.0 transfer engine was source-reviewed and migrated without changing its transfer rules. It keeps clipboard data across asset selection changes, requires preview before Apply, verifies selection/component identities and typed fingerprints, checks public compatibility/dependency metadata, and rolls back completed changes after a synchronous failure.

Settings are copied only for Color Properties and Building Terraform Override. Other component types support **Add only missing** with native defaults and are reported as unsupported for settings replacement. No mode removes destination-only components. New components are inactive for inspection. Viewport element replacement is deferred.

## Checks

- Release DLL compiles against the installed assemblies.
- Shared TypeScript/UI production build passes.
- Original Component Tool validation passes 77 checks: 17 selection/JSON, 17 clipboard/revision, 16 typed paste and 27 transfer-mode/rollback checks.
- Existing MW Editor Tools safety suites must pass before packaging.

## Grouped native acceptance

Use disposable writable copies.

1. **Element replace:** copy one Object row, select a different Object row in the same or another asset, replace it, and confirm destination position/rotation/probability/scale remain unchanged. Repeat once for a non-Object row available on the fixture.
2. **Batch replace:** select one destination occurrence, run batch replace, confirm every occurrence of that prior referenced prefab in that list changed and unrelated rows did not.
3. **Single component settings:** copy Color Properties or Building Terraform Override, select another compatible asset, choose Replace only present, preview/apply, and confirm full settings plus preserved destination activation. Test Restore once.
4. **All components:** Select all and copy. On a compatible destination, inspect previews for Add only missing, Replace only present, and Replace and add. Confirm unsupported settings are reported rather than silently copied and new native components are inactive.
5. Save each successful disposable destination under a new name, reopen it, and confirm replacements/components persist. Confirm accepted Undo 0.4.143 operations still work afterward.
