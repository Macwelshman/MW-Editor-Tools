# Editor Environment integration — 0.4.256

The user authorized integrating MW Editor Environment and organizing Options into Keyboard Shortcuts, Tools and Environment on 28 October 2026. The original project is unchanged. No computer use or publishing is included in this work.

## Settings and behavior

- Keyboard Shortcuts: hide/show UI, Undo and the six browser category bindings, including their enable switches.
- Tools: component/list Clear confirmation, Undo capture, inspector Undo and window reset.
- Environment: time of day, time of year, precipitation, cloud coverage and snow. Apply changes on any tab saves drafts from all tabs. Saved environment changes apply at the next editor load, retaining the original one-time application and photo-mode-return behavior.

VERIFIED-STATIC: source review used standalone v0.1.3 EditorEnvironmentSystem, SnowCoverage, Settings and Mod. [Original source hashes](evidence/environment-integration-0.4.256/original-source-hashes.json) record the reviewed source. The runtime was adapted into the existing mod entry point; there is no second IMod or reference to the standalone DLL.

EnvironmentSettings retains the exact `MWEditorEnvironment` named settings fragment, `ModsSettings/MWEditorEnvironment` FileLocation, five property names and defaults (12:00, day 91, precipitation 0%, cloud coverage 15%, snow off). It is loaded by AssetDatabase and never registered as a second Options entry. Existing settings are populated by property name into the integrated store. Keyboard/tool settings retain their existing JSON storage and binding behavior. Draft edits and resets do not persist until Apply changes. Environment-only changes do not clear Undo.

The integrated preset requires a verified stable/beta Game/Unity.Entities pair. A loaded standalone MWEditorEnvironment assembly refuses integrated initialization/application. Local deployment backs up the standalone installation and moves it out of the active Mods directory to prevent competing presets and a duplicate Options menu. Its source project and saved settings are preserved.

Normal preload cleanup restores editor overrides before deserialization, where values remain owned. Stop clears the settings reference so retained native lifecycle callbacks cannot reapply a disposed module. If a preload callback was missed, the fallback discards editor ownership and snow snapshots without restoring old values into a city. Snow buffer restoration still requires the original buffer identities; gameplay snow visuals remain respected.

## API review and validation

VERIFIED-STATIC: installed Game MVID e8325128-556a-460d-a950-497d90b3627c. [Options API IL](evidence/environment-integration-0.4.256/options-api.txt) verifies two-argument SettingsUISection(tab, group), SettingsUITabOrder and GetOptionTabLocaleID. AssetDatabase.LoadSettings(name, object, defaults, bool) resolves the named SettingAsset fragment and uses JSON.WriteInto on the supplied object (IL_0045 source assignment, IL_005b population), preserving scalar settings independently of the old class identity.

[Environment API evidence](evidence/environment-integration-0.4.256/environment-api.txt) verifies planetary accessors, float climate override properties, private SnowSystem.m_SnowHeights RenderTexture[] and native two-buffer allocation/read selection. Native PlanetarySystem remains in PreCulling (27), matching the integrated system's UpdateBefore registration. Game files were only read.

Release/UI builds pass. All 37 managed suites and 18 UI regression scripts pass. Focused suites cover 100 Options/tab/preset assertions, 29 actual-source environment lifecycle/settings/snow assertions, and 31 startup/disposal assertions including isolated environment failure. The existing beta editor, area allocation and surface scheduling checks pass, as does the new environment contract check. The existing NU1900 vulnerability-feed warning remains; no new dependency was introduced.

The tests use managed/GPU doubles and metadata inspection. They do not certify native Options layout, persistence through game restart, appearance, paused-editor rendering or photo-mode transitions. [Evidence/results](evidence/environment-integration-0.4.256) retain the build and test outputs.

## Grouped native acceptance — pending

1. Restart the game and confirm one MW Editor Tools Options entry with the requested three tabs and no separate MW Editor Environment entry.
2. Check existing bindings/tool switches and saved environment values. Edit drafts, Apply changes, restart and verify persistence.
3. Load a disposable editor workspace while paused. Verify time, seasonal appearance, cloud/precipitation settings and optional snow. Check photo-mode entry/return and that current-session changes remain unchanged until a reload.
4. Return to a disposable city/main menu and verify environment isolation; exercise normal supported Undo/confirmation/targeting together.

VERIFIED-STATIC: 0.4.256 was installed while Cities2 was absent. The previous MWEditorTools installation was copied and hash-verified under outputs/rollback/MWEditorTools-before-0.4.256-live-20261028-193012/MWEditorTools. Runtime DLL/JavaScript/CSS and release notes match staged SHA256 hashes; see [installed-hashes-live.csv](evidence/environment-integration-0.4.256/installed-hashes-live.csv). The standalone Environment directory was already absent at deployment, so no live standalone folder was moved. A disposable deployment smoke test separately verified the legacy-folder backup/relocation path, including preservation of an extra legacy file.

The published 0.4.254 distribution and its PDF/installer remain unchanged. This is a local test build; outputs/installation-0.4.256-live.txt records deployment details. Native acceptance remains pending.
