# Undo 0.4.134 — complete read-only parent preflight

VERIFIED-RUNTIME: 0.4.133 first deletion is blocked by a four-node companion group failing AllowedBoundTopology. Exact constraint was absent from the previous log. Tree size alone is not a proved cause: BoundPropPolicy delegates multi-node groups to NestedPropPolicy, which also requires unique prefab identities, supported parent/depth ordering and deterministic probability/group values. No rule is relaxed without evidence.

VERIFIED-STATIC: added a bounded read-only preflight to the existing effect update path. Selecting a writable inspected parent with EffectSource for 45 quiet frames resolves exactly one live root instance and audits its network/object anchors plus every direct non-effect prop group (maximum 128 roots). Each group is inspected independently, so one rejection does not hide other unsupported groups. Failed captured groups log full node identities, owners, parent/depth, prefab, local probability/group/mesh/index/pose, up to 32 source child descriptors per node, duplicate prefab identity indices and non-deterministic-link indices. Groups that cannot be captured log the existing bounded raw schema inventory. Summary reports passed groups and all failed checks. Pending owners wait; ambiguous instance counts and budget limits are explicit refusals.

This preflight writes only diagnostic logs and internal once-per-selected-asset bookkeeping. It does not write entities/prefabs, invoke native tools, arm Undo, change capture policy or restore anything. Dialogs, active previews and recovery block the audit. Exceptions are contained inside the diagnostic. Existing effect lifecycle doubles stub out this added independent audit; native scheduling remains unproven.

Pending step: load/select the fresh disposable cargo harbour parent, leave it selected briefly, then inspect the audit log. No delete/move/Undo test is needed to obtain evidence. Implement preservation for the proved group constraint only after that evidence; complex harbour support, native bound Undo and persistence remain UNPROVEN. This is a diagnostic build, not a claimed recovery fix. No computer use or publishing.

VERIFIED-STATIC validation: Release net48 build zero warnings/errors; 568 existing managed assertions passed (Phase3Safety 410, NestedRecoveryRunner 133, EffectUndoSafety 25). Diagnostics have been reviewed for absence of game/prefab writes; native audit execution remains pending. Checked UI assets unchanged.

VERIFIED-STATIC installation: Cities2 absent; five package hashes and 142 rollback files verified. Evidence docs/evidence/recovery-0.4.134-installed-hashes.csv; rollback outputs/rollback/MWEditorTools-before-0.4.134-20261017-234656/MWEditorTools.

VERIFIED-RUNTIME preflight completed: network passes, 28 groups pass, 15 fail across multiple unsupported data classes. Full findings docs/UNDO_0.4.134_PREFLIGHT_FINDINGS.md; no recovery change or native acceptance claimed.
