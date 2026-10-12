# 0.4.88 — browser category full-path lookup

VERIFIED-RUNTIME 0.4.87: native log reports Buildings, Surfaces, Effects and Foliage selection; Props reports unavailable or ambiguous and user observes default All. Saved shortcuts are enabled (Shift+Alt combinations). Exact same-name path count was not logged in0.4.87, so duplicate names remain HYPOTHESIS until the corrected build's runtime check.

VERIFIED-STATIC: Game Version0.0.0.0, MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. EditorAssetCategorySystem.AddCategory IL_0015–0026 constructs full parent/child paths; GetLocalizationID IL_0006 uses path; CreateCategory splits full paths and permits recurring leaf names. Previous code incorrectly required a globally unique leaf ID. Corrected lookup uses the requested full root path, so nested categories called Props cannot conflict with root Props. Missing/duplicate full paths still refuse selection. Log now includes actual match count and same-name paths on refusal. Successful selection is read back through GetSelectedItem before being logged.

VERIFIED-STATIC: Release zero warnings/errors; production UI build; 15 navigation regression assertions including duplicate leaf name under Buildings/Props beside root Props; 35 Options assertions and330 settings/input/history assertions pass. Settings, configured bindings, guards, shared-window UI and recovery behavior unchanged.

UNPROVEN pending installation/native retest: Props full-path resolution in this running asset set. Acceptance: configured Props shortcut from closed browser and after selecting another category; Props must highlight and show prop results. No saves or asset mutations required. Regression-check the other five category bindings.
