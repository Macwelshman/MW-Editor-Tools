# Undo 0.4.104 — exact foliage guard failures and surface companion normalization

VERIFIED-RUNTIME: docs/evidence/undo-0.4.103-runtime.log identifies three separate failures. Main FlowerBushCity03 captured and recorded successfully, then StartMain refused the native archetype's Game.Effects.EnabledEffect before creating any entity. Bound GreenBushCity03 exceeded the legacy four-root forest limit. Bound Sand Surface 01 failed because native sync changed ObjectSubNets.m_SubNets and EffectSource.m_Effects. Working bound Walking Area and nested props still completed exact native recovery.

VERIFIED-STATIC fixes:
- Main native archetype and capture schemas both accept EnabledEffect. Typed foliage/group snapshots continue to compare semantic enabled-effect identities, without copying native allocation slots. A simple prop without a typed group still refuses a nonempty enabled-effect buffer.
- A preflight rejection before entity creation clears the operation state, preserves the Undo record and reports a status/warning rather than calling FailMain and disabling the entire main recovery route. Post-creation failures retain cleanup/error handling.
- Root identity and placed-root limits are now 128, matching the existing mixed-area prop budget. Cycle, descendant topology, ambiguous placement, reference and retained snapshot budgets remain enforced. The old diagnostic automatic test runner's one-to-four test sequence is unchanged; it is not the normal Undo workflow.
- Bound area companion net/effect descriptor lists accept unique complete permutations and small native float differences (<0.0001 per float). Discrete indices, enum values, flags, parent meshes, array shapes, strings and prefab identities remain exact. This opt-in comparison is limited to native ancillary descriptors; PrefabSnapshot.Matches remains exact, including final prefab restore validation. Typed companion graph/placement/reference/source-survival checks still must pass before a record exists.

HYPOTHESIS: reordering/float normalization accounts for the observed net/effect descriptor changes. The runtime log proves those fields changed, but does not identify their individual values; changes beyond this conservative comparison remain refused. Native bound surface acceptance remains UNPROVEN.

VERIFIED-STATIC checks: Release zero warnings/errors; production UI and TypeScript pass. 765 assertions across eight relevant managed suites pass: Phase2Safety 18, Phase3Safety 190, Phase4Safety 330, BulldozerSafety 39, NestedRecoveryRunner 133, StandaloneNestedSafety 23, LastPropHistory 12, LifecycleSafety 20. Added checks retain exact snapshot semantics, reject nonfinite/substantive changes and changed animation indices, and accept the 128-root identity boundary while rejecting 129.

UNPROVEN: 0.4.104 foliage and bound surface restoration. No computer use or game assembly edits. Game has exited; automatic installation is authorised with rollback and hash checks.

VERIFIED-STATIC installation: 0.4.104 installed after Cities2 exit checks; 5 package hashes and 112 rollback file hashes verified. Rollback outputs/rollback/MWEditorTools-before-0.4.104-20261014-010118/MWEditorTools. Native acceptance pending.

