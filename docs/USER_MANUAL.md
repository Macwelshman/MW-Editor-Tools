# MW Editor Tools — User Manual

For version **0.4.163** · Cities: Skylines II Asset Editor

MW Editor Tools combines element replacement, component transfer, colour variations, workspace saving and supported Undo in one window.

## Contents

- [Install or update](#install-or-update)
- [Open the window](#open-the-window)
- [Quick start](#quick-start)
- [Undo](#undo)
- [Replacer](#replacer)
- [Components](#components)
- [Colours](#colours)
- [Multi-Save](#multi-save)
- [Options and shortcuts](#options-and-shortcuts)
- [Troubleshooting](#troubleshooting)
- [Supported limits](#supported-limits)

## Install or update

Download the installer or manual ZIP from the [0.4.163 release](https://github.com/Macwelshman/MW-Editor-Tools/releases/tag/v0.4.163). The repository contains development source; cloning it does not install the mod.

### Windows installer

Close Cities: Skylines II, run **MWEditorTools-0.4.163-Setup.exe**, and choose **Install / update**. The default is your current Windows user's Mods folder; no administrator privileges are needed there. Use **Choose folder…** only for another user Mods folder. The installer refuses the game installation and linked folders.

The installer verifies its embedded files, backs up the entire existing mod folder, and verifies every installed file. Backups are outside Mods, under **Cities Skylines II/MWEditorTools-backups** in the user data folder. The completion message gives the exact path. The installer does not change settings stored outside the mod folder. It is unsigned and requires .NET Framework 4.8.

If a write fails while the game stays closed, changed files are rolled back. If the game starts during installation, all further writes, including rollback, stop: close the game before restoring the displayed backup or running the installer again.

### Manual ZIP installation

1. Fully close Cities: Skylines II.
2. If updating, back up the existing `MWEditorTools` mod folder.
3. Place these four files in `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\MWEditorTools`:
   - `MW.EditorTools.dll`
   - `MW.EditorTools.mjs`
   - `MW.EditorTools.css`
   - `0Harmony.dll`
4. Start the game and open the Asset Editor.

Keep the supplied third-party notices with the package. Do not place these files in the game installation or replace game assemblies. If the package has a SHA256 manifest, use it to verify the files.

To roll back, fully close the game and restore the previous mod folder. This restores the mod version; it does not reverse changes already saved to an asset.

## Open the window

Click the **MW Editor Tools** sliders icon in the Asset Editor. The window has four tabs:

| Tab | Purpose |
|---|---|
| Replacer | Replace one element or all matching elements in an asset list. |
| Components | Copy components and their settings between assets. |
| Colours | Edit palettes and generate colour variations. |
| Multi-Save | Save unlocked assets in the workspace. |

**Undo** is in the window header and is also available through Ctrl+Z. It does not need a separate tab. The launcher hides while the window is open and returns when you close it.

Drag the window by its header to move it. Its position is remembered. Use **Options → MW Editor Tools → Reset window position** if needed. Hover over controls for explanations; use **Details** for transfer or save reports.

Most tools follow the asset or mesh selected in the native **Object Info** panel. Select an editable asset to populate the controls. Read-only assets require an editable copy before changes can be applied.

## Quick start

| Task | Workflow |
|---|---|
| Replace an element | Hover source → Ctrl+C → hover destination → Ctrl+V. |
| Replace matching elements | Hover source → Ctrl+C → hover destination → Ctrl+Shift+V. |
| Copy one component | Components → select source component → Copy selected → select destination → Preview → Paste. |
| Copy all components | Components → Copy all → select destination → choose mode → Preview → Paste. |
| Change colour variations | Colours → select mesh → adjust palette → Generate combinations → Apply combinations. |
| Save workspace assets | Multi-Save → Save all assets → review the result. |
| Undo a supported move or deletion | Ctrl+Z, or Undo in the header. |

Use an editable copy when trying an unfamiliar workflow. Clipboard contents and recovery controls are working-session tools, not saved backups.

## Undo

Undo automatically captures the last supported completed action when **Enable Undo** is on. The default shortcut is **Ctrl+Z**. You can also click **Undo** in the header or use the inspector control when enabled.

There is one shared last-action slot:

- A new supported completed action replaces the previous action.
- A cancelled or unchanged move preserves the previous action.
- Successful Undo consumes the action. Pressing Undo again does not walk back through older edits.
- Asset loads/changes and unrelated edits can clear or invalidate the action. Element replacement and component paste clear the shared slot.
- The Undo slot does not survive a reload. Saving and reopening preserves the resulting asset changes, not the recovery slot.

### Supported actions

| Element | Supported Undo |
|---|---|
| Props, foliage and supported nested groups | Supported standalone/bound deletion and built-in moves. |
| Surfaces and areas | Supported bound/unbound deletion and native node edits on existing supported polygons. |
| Native editor effects | Supported direct bound/unbound deletion and built-in moves. |
| Directly bound lanes/network segments | Supported deletion, subject to ownership and geometry checks. |

“Bound” means an element is attached to an owning asset. Supported recovery preserves its placement and relationships to that asset. The relevant bulldozer mode must target the element: use the native Main, Sub-elements or Everything mode as appropriate. Recovery is conditional on the selected route passing its checks; the table does not promise every possible element arrangement can be restored.

### Surface and area nodes

Keep the relevant surface/area tool active after releasing the node, then press Ctrl+Z.

Adding a node and then moving **that same added node** is treated as one action. Moving a different node starts a separate last action. Deleting one node from an existing supported polygon is also supported.

Whole-polygon creation/deletion through the node tool and edits that merge or affect multiple polygons are outside this workflow.

### Separate recovery controls

The following controls have different scopes:

| Control | Recovers |
|---|---|
| Header Undo / Ctrl+Z | The last supported captured move, deletion or node edit. |
| Components → Restore | The destination settings/components before the latest component paste. |
| Colours → Undo last edit | The last supported colour edit. |

Do not assume Ctrl+Z reverses a Replacer paste or every change made by another tool.

## Replacer

Replacer copies an element's referenced asset and uses it as a replacement. It preserves the **destination row's placement and settings**, rather than copying the source element's position.

### Copy and replace in the viewport or Object Info

1. Use the native default selection tool for viewport targeting.
2. Hover over the source element in the viewport, or its highlighted row in the current asset's **Object Info attached-element list**.
3. Press **Ctrl+C** while it is highlighted. Check the Replacer clipboard to confirm what was copied.
4. Hover over the destination element or its Object Info row.
5. Press **Ctrl+V** to replace that element.

Object Info rows often expand or collapse rather than becoming a persistent selection. You can copy and replace while hovering; clicking the row first is not required.

The Object Info attached-element list is different from the asset catalogue/browser used to find prefabs. These hover instructions refer to attached source-list elements, not copying an arbitrary catalogue card.

### Batch replacement

After copying a source element, hover over a destination and press **Ctrl+Shift+V**. This replaces all elements matching the destination's original reference **in that owning asset list**. It does not perform a workspace-wide search, and it does not mean replacing every nested occurrence throughout unrelated lists.

For example, copy a flower bush, hover over one of several matching destination bushes and use Ctrl+Shift+V. Matching rows in that asset list receive the copied bush reference while keeping their individual placements/settings.

### Use the Replacer tab

When supported rows are available, you can choose one in the tab and use **Copy**, **Replace** or **Replace all**. **Clear** empties the element clipboard. The clipboard helps identify the source even when you move the cursor away from it.

Objects must be replaced with compatible object rows, effects with effect rows, and so on. Copying a prop that contains an effect copies the prop reference; copying the effect itself copies an effect reference. They are different element types.

### Replacement limits

The destination must resolve to a unique supported source row and owning asset. Standalone destination placements without an editable attached source list are not supported. Read-only, ambiguous, incompatible or still-updating targets can be refused.

A replacement whose visible result or status is wrong should not be saved over an original. For a reported foliage regeneration failure, stop further replacements and reload the disposable session without saving; a queued rollback is not a guarantee that every visible/native state has already recovered.

## Components

Components transfers the selected components' settings between assets. It leaves the source unchanged and attaches additions to the destination asset.

### Copy one or several components

1. Select the source asset in Object Info and open **Components**.
2. Click a component to select it. Click again to deselect it. You can select several.
3. Choose **Copy selected**, or **Copy all** for all supported attached components.
4. Select the destination asset in Object Info.
5. Choose a paste mode.
6. Click **Preview** and review the proposed changes and any skips.
7. Click **Paste** to apply the preview.

### Paste modes

| Mode | Already present on destination | Missing from destination |
|---|---|---|
| Add missing | Leave unchanged. | Add with copied settings. |
| Replace present | Replace settings. | Skip. |
| Replace and add | Replace settings. | Add with copied settings. |

Destination-only components remain in every mode. Existing components retain their active/inactive state; added components retain the copied source activation.

Settings include supported nested lists, arrays, curves and referenced prefabs. Mutable settings are copied independently. Referenced prefabs retain their identities: transfer does not duplicate the referenced assets or remap them to similarly named assets.

Dependencies are checked and copied additions are ordered where possible. A required active dependency must already be available or be included as a suitable copied addition. Incompatible components and settings that cannot be safely copied are reported as skips. **Copy all** does not override those restrictions or silently substitute default settings.

### Restore a component paste

Return to the latest destination and click **Restore**. This reinstates the prior settings and removes tracked additions from that paste.

Restore is one level, not a history of every paste. It refuses if the destination has changed since the paste, to protect subsequent work. Changing the destination, clipboard or settings after Preview requires a new Preview before Paste.

Use **Details** to inspect the result or skipped-component report. **Clear** empties the component clipboard. The component clipboard is separate from Replacer's element clipboard and the Colours clipboard.

## Colours

Colours follows the object or render mesh selected in Object Info. If the asset has several meshes, choose the target mesh in the tab.

### Edit and generate variations

1. Select the target mesh and variation group. Use **Previous group** or **Next group** where available.
2. Use **Reload colours from asset** to load the selected group's current colours. This replaces uncommitted palette edits and clears generated combinations; it does not modify the asset by itself.
3. Click a swatch in Channel 0, 1 or 2 to open the colour picker. Adjust it, then choose **Use colour**.
4. Choose **Replace group** or **Append to group**.
5. Click **Generate combinations**, then inspect the result/count.
6. Click **Apply combinations** to write the variations.

Channels **0 and 1 remain paired by row**. Channel **2 combines with every pair**. For example, four paired rows and three Channel 2 entries produce twelve combinations. Changing the palette after generation requires generating again before applying.

**Replace group** replaces the selected group's variations. **Append to group** adds the generated variations to it. This tool creates combinations from your palettes; it does not promise that every unrelated colour setting on an asset is randomly regenerated.

### Transfer colour settings

1. Select the source mesh and choose **Copy colour settings**.
2. Select the destination mesh and choose **Paste colour settings**.
3. Review and confirm the paste, or cancel it.

Paste replaces the complete supported Colour Properties setup, or adds Colour Properties where supported. **Undo last edit** is the Colours tool's own recovery control.

If you change selection with uncommitted edits, the tool asks before discarding the draft. Return to the previous item to continue editing, or explicitly discard and follow the new selection. Read-only assets and busy editor operations can disable writing controls.

## Multi-Save

Multi-Save saves unlocked assets in the current workspace, including hidden entries. Repeated instances of the same asset are saved once. Locked assets are excluded.

1. Open **Multi-Save** and check the number ready to save.
2. Click **Save all assets**.
3. Follow any native editor dialogs that require a response.
4. Review the completion summary. Open **Details** for the report or any skipped/failed items.

Existing files are updated. New assets use their current names. This is workspace saving, not automatic creation of backup copies.

**Stop** requests that the batch stop after the current asset finishes. It does not reverse files already saved. Failures expand the detailed report automatically. If no assets are eligible, Save is disabled.

## Options and shortcuts

Open **Options → MW Editor Tools**. Edit the options, then choose **Apply changes** to activate and save them.

| Option | Purpose |
|---|---|
| Confirm deletions | Confirm component removal and clearing lists. This is not a confirmation for every bulldozer action. |
| Enable Undo | Capture supported recoverable actions. Changing this clears the current Undo action. |
| Show inspector Undo | Show the Undo control in Object Info. |
| Enable Undo shortcut | Enable the configured Undo key combination. |
| Undo shortcut | Default Ctrl+Z. Click the binding field and press the desired combination. |
| Reset window position | Return the shared tools window to its default location. |
| Enable browser shortcuts | Enable the category shortcuts below. Off by default. |

### Browser defaults

| Shortcut | Category |
|---|---|
| Alt+B | Buildings |
| Alt+D | Props |
| Alt+F | Foliage |
| Alt+A | Areas |
| Alt+E | Effects |
| Alt+S | Surfaces |

The category shortcut opens the browser at that category and retains its search text. A retained search can still filter the displayed items.

Previous default Ctrl+Alt bindings migrate to these defaults. Other customised bindings are preserved. Duplicate combinations with other enabled MW shortcuts are suppressed; choose distinct bindings and Apply changes. Bindings use a letter A–Z with the recorded modifiers.

Shortcuts pause while typing, recording a binding, using dialogs, saving or recovering. Modifier combinations must match exactly: holding an extra modifier can prevent a shortcut from firing.

## Troubleshooting

| Symptom | What to check |
|---|---|
| Empty tab or disabled controls | Select a supported editable asset or mesh in Object Info. Wait for loading/saving/recovery or dialogs to finish. |
| Ctrl+C copies the wrong thing | Check Replacer's clipboard. Hover the intended prop or effect itself; a prop containing an effect and the effect are different targets. |
| “Does not match copied element type or an owned source row” | Source and target must be compatible row types, and the target must have a supported unique owner/source row. Try the corresponding Object Info row. Do not force a mismatched paste. |
| Browser shortcut does nothing | Enable browser shortcuts and Apply changes. Check custom/conflicting bindings, typing focus and exact modifiers. |
| Browser category looks empty | Check the browser's retained search text. |
| Undo is disabled or does nothing | Check Enable Undo/shortcut options and supported scope. The slot may have been consumed, cleared or replaced. Close typing/dialog focus. |
| Area/surface node Undo does not trigger | Complete/release the node edit, keep its native area/surface tool active, then use Ctrl+Z. |
| Component Paste asks for another Preview | Destination settings, selection or clipboard changed. Preview again. |
| Component is skipped | Review Details for compatibility, dependencies or unsupported settings. An editable destination alone does not make every component transferable. |
| Restore refuses | A later destination edit, identity change or different destination prevents safe restoration. |
| Colours asks to generate again | The palette changed after the last generation. Generate combinations again before Apply. |
| Save is disabled or some assets were excluded | Check the ready count and Details. Locked assets are excluded; saving may already be running. |
| Window is out of reach | Use Options → MW Editor Tools → Reset window position. |

An intermittent viewport type/source-row mismatch was observed during development and did not recur in the accepted effect tests. It remains a documented limitation rather than a claim that every targeting case is fixed.

For a reproducible problem, record the mod version, source/target names, tool and action sequence, and any displayed message. The mod log is normally at:

```text
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\MWEditorTools.log
```

Include whether the issue persists on a fresh disposable copy. Avoid saving a visibly failed operation over the original asset.

## Supported limits

The current release does **not** provide:

- Redo or multiple-action Undo history.
- Whole-building or whole-component recovery through the shared Undo slot.
- Network moves, standalone network/junction recovery or guaranteed third-party-tool recovery.
- Whole-polygon creation/deletion through node tools, multi-polygon edits or merged-edit recovery.
- Arbitrary standalone viewport destination replacement or a global workspace replacement operation.
- Universal component copying: opaque/private serialized payloads, native/runtime objects, unsupported third-party components and oversized/cyclic settings can be skipped.
- Undo of files already written by Multi-Save.

For candidate status and validation details, see [release readiness](https://github.com/Macwelshman/MW-Editor-Tools/blob/main/docs/RELEASE_READINESS_0.4.163.md). For a shorter overview, return to the [README](https://github.com/Macwelshman/MW-Editor-Tools/blob/main/README.md).


