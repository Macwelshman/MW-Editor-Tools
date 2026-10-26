# 0.4.230 deletion and move Undo log review

VERIFIED-RUNTIME: latest MWEditorTools.log snapshot ends 2026-10-23 23:22:41, same 0.4.230 session as the accepted outline test. Evidence docs/evidence/targeting-0.4.230/undo-test-2322. Read-only review; no source, runtime or installation changes.

Successful deletion recoveries (7): Restricted Spawn Location at23:21:35.619; Grill01 at23:21:38.805,23:21:41.938,23:21:45.188; Invisible Car Path - 1xTwoway at23:21:56.707; Walking Area at23:21:59.571; FlowerBushCity01 at23:22:05.917. Recorded completion validates source/graph/companions, or payload/object graph/recursive ownership as applicable. Detached external prefab reference checks logged matches=True. This does not independently establish save/reopen or all categories.

Failures: two FenceResidentialHigh01 deletion capture attempts, entity53048:5 (graph15) at23:21:47.945 and53047:5 (graph11) at23:21:52.169. Both time out with 'network graph or source descriptors differ' at23:21:50.042 and23:21:53.948. Each is followed by 'Lane/network deletion did not validate; no Undo entry recorded.' These are capture/validation failures before recording a recoverable action, not a logged completed recovery. The precise differing field/topology/source row is not identified by this message. Root cause, including whether rendering changes contribute, remains UNPROVEN. Do not relax graph/reference guards from this summary alone.

The later invisible car-path capture/restoration succeeds. A 'Network Undo blocked: recovery busy, disabled or dialog open' entry occurs during that restoration at23:21:56.663; completed graph/companion verification at23:21:56.707 means this is not counted as a failed recovery.

Successful move Undo (4): Grill01 at23:22:18.326 and23:22:22.381; Pedestrian Access Location at23:22:26.430; Restricted Spawn Location at23:22:30.356. Each explicitly verifies source, graph and companions. A further Restricted Spawn Location move starts capture at23:22:40.084 but has no ready/completed/cancelled result before this log snapshot ends; outcome UNPROVEN rather than labelled passed/failed.

No ERROR/CRITICAL/exception matches in checked MW/UI/Player logs, and no optional outline-renderer disabled/cleanup failure was logged. The two explicit network validation warnings still prevent a blanket Undo pass. No effect or bike-rack move/deletion completion was observed in this test segment. Accepted prior results remain historical, not newly verified by this session.

Next narrow investigation: inspect the failed network capture completion path to determine which graph/source-descriptor comparison timed out. Preserve companion data/references and working categories; do not infer that every native regeneration difference is safe to ignore.
