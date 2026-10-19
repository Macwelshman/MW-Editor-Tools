# Viewport replacement — 0.4.152

VERIFIED-RUNTIME (user report): browser copy, paste and replace-all pass on 0.4.151. Viewport paste was not implemented in that build.

Viewport Ctrl+V and Ctrl+Shift+V now resolve the active DefaultToolSystem hover entity to its immediate owner prefab. The entity must exist, be settled, have an owner and prefab reference, and occur exactly once in the owner's child buffers. The owner must resolve to a writable registered prefab and agree with an inspected asset when present. Missing, temporary, deleted, unbound and incompatible targets refuse before mutation.

Props/foliage use the existing BoundDescriptorIndex exact prefab/local pose/parent mesh/probability/group comparison, without trusting cached descriptor indices. Effects use existing EffectEditPolicy.Descriptor including source identity, scale/intensity, pose, attachment and animation index. Areas require exact local node positions/attachments; network edges and lanes require exact prefab and cached source curve. Exactly one source descriptor must match; duplicate or absent matches refuse.

Both shortcuts reuse the existing replacement transaction: Ctrl+V replaces that single descriptor reference; Ctrl+Shift+V replaces matching references in the same owned source list. Geometry and other source settings remain unchanged. Rollback uses a full detached list snapshot. Native prefab update is queued and the asset marked dirty. Inspector refresh occurs only for the corresponding inspected root. Existing browser sample targeting is retained; viewport fallback is used when fresh browser samples are empty.

VERIFIED-STATIC: Release and UI builds pass; seven browser target checks, 428 snapshot/history checks and 20 lifecycle assertions pass. Viewport native targeting, rendering regeneration and persistence remain UNPROVEN. Standalone scene entity replacement is unsupported because it has no owning source row.

Grouped acceptance on a disposable asset: copy a source, hover a different bound viewport prop and Ctrl+V; verify only the target changes at the same position and rotation with the same probability. Repeat on a duplicated prefab occurrence, then Ctrl+Shift+V and verify only matching elements in that owning list change. Repeat supported area/effect cases as available; save under a disposable new name and reopen. No original asset overwrite.
