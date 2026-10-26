# Hover outline instead of ring — 0.4.229

User VERIFIED-RUNTIME: surface targeting passes with 0.4.228. User requested the gold hover highlight follow the element outline instead of a circle.

Object/marker/node hover no longer draws the circular overlay; it relies on the native Highlighted silhouette introduced in 0.4.228. Selected-element cyan indicators remain. Polygon boundaries and network/lane curves still use gold on hover and cyan when selected. The public hint now says outline for hover rather than promising gold for every object. Geometry-less native gizmos may vary by element; native visual acceptance pending.

VERIFIED-STATIC: Game.Tools.Highlighted has no per-entity colour fields (installed Game.dll MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef). A separately configurable gold silhouette was not established through the inspected native API. This version deliberately uses the native outline colour for object silhouettes; it does not alter shared materials, shader palettes or prefab colours. The existing exact-target tag ownership/cleanup, source mapping, Undo preflight, native restoration, companion and reference handling remain unchanged.

VERIFIED-RUNTIME (managed tests): 69 actual-source targeting assertions, including no circle on initial object hover, native Highlighted presence and retained cyan selected indicator; 24 UI targeting/dispatch assertions; tab error fallback and header-focus checks pass. Native renderer appearance still needs user acceptance. Build and installation records are under docs/evidence/targeting-0.4.229.

Release/UI builds passed; cached NU1900 advisory-feed warning remains. INSTALL DEFERRED: Cities2 running, 0.4.228 remains installed; 0.4.229 staged with hashes under docs/evidence/targeting-0.4.229. Native outline appearance acceptance pending.

INSTALL VERIFIED 2026-10-23: user confirmed game closed; elevated process checks confirmed Cities2 absent. Installed assembly 0.4.229.0 and all three copied hashes verified. Nine prior installation files backed up and hash checked at outputs/rollback/MWEditorTools-before-0.4.229-20261023-224847/MWEditorTools. See outputs/installation-0.4.229.txt. Native outline appearance acceptance remains pending.
