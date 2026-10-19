# Component settings transfer between assets — 0.4.160

User authorised continuation of Phase 5 component transfer, preserving settings and providing the three bulk modes. No publishing or computer use.

The Components tab supports Copy selected (including a single component) and a direct Copy all components action. Destination modes are Add only missing, Replace only present, and Replace and add. All additions carry their copied settings and activation; settings replacement preserves destination activation. Extra destination components are retained in every mode.

Public settings on installed game component types now use detached bounded settings snapshots. Arrays, lists, game settings DTOs, primitive/enumerated values, mathematical/Unity value structs, animation curves and live prefab references are supported. Prefab references retain identity, mutable settings are reconstructed, and the destination component's prefab owner is never copied. Typed colour/terraform validation remains. Reconstruction is checked before writes. Unsupported opaque/private serialized fields, native/runtime objects, arbitrary Unity objects, third-party components, cycles and excess budgets are reported as skips rather than added with defaults.

Bulk planning checks native menu compatibility, filters unavailable requirements and orders copied dependencies before dependants. Existing requirements remain active; copied additions can satisfy requirements. Apply/Restore check destination/clipboard identity and settings freshness. Failed apply rolls back tracked changes and queues regeneration. Source copies remain independent of later source edits. Shared busy-state guards apply, and component writes invalidate the prior single-action bulldozer Undo slot. One-level component Restore remains available if no later destination edit intervenes.

VERIFIED-STATIC: installed Game 0.0.0.0 MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. PrefabBase.AddComponent(Type), IL_0014–003a creates a fresh ScriptableObject, assigns its prefab owner and appends it. No automatic dependency creation occurs in that method. Evidence: docs/evidence/phase5-component-native-add.txt. Public settings examples and assembly SHA256: docs/evidence/phase5-component-settings-schema.txt. No installed game files modified.

VERIFIED-STATIC: Release and production UI builds passed. Managed checks cover detached nested lists/arrays, curves, prefab identity, three modes, activation and owner preservation, rollback of partial additions/failed refresh, stale clipboard/settings and Restore refusal, dependencies and read-only/opaque/cyclic payload refusal. Component UI command/render checks and accepted element-replacement/shared Undo UI regressions pass. These are managed doubles; native refresh, persistence and component compatibility acceptance remain UNPROVEN. Release has NU1900 because online vulnerability metadata is unavailable.

Grouped native acceptance on disposable asset copies:

1. Give a source component distinctive scalar/list/reference settings. Copy selected, switch assets, preview/apply and compare all fields; verify the source remains unchanged. Restore and compare destination's prior values.
2. Copy all from a source with both shared and source-only components. Add only missing must leave existing settings unchanged; Replace only present must leave missing components absent; Replace and add must do both. Retain destination-only components in each case.
3. Include inactive components and a dependency pair; check addition activation, unchanged existing activation and correct dependencies. Review every skip in preview. Exercise ordinary scalar components plus Effect Source/Object SubObjects if applicable to the asset.
4. Change destination settings after preview and after apply: stale Apply and stale Restore must refuse. Test a read-only destination and an incompatible component.
5. Save a disposable destination copy, reopen, compare settings, reference identities and component counts. Do not overwrite original assets.

Final verification: 32 component-transfer assertions, 20 actual Mod lifecycle assertions, 428 snapshot/history assertions, 14 viewport targeting assertions and 18 foliage replacement assertions passed. Component UI and shared Undo render checks passed. Native acceptance remains pending.


## Native acceptance — 2026-10-19
VERIFIED-RUNTIME (user report): all three component-transfer modes passed, including settings preservation, Restore and persistence. The tested 0.4.160 component-transfer scope is accepted. Unsupported schemas remain explicit skips; this acceptance does not establish support for skipped component types. No publishing authorised.

