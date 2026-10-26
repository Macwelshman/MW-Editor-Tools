# Components layout and JSON transport — test 0.4.184

User requests: reduce the crowded Components panel, distinguish each task, enlarge the naming-pattern editor, and investigate the empty workspace scan.

VERIFIED-STATIC implementation:

- Three task buttons show only Component copying, Localisation or Pack priorities. Field choices, text clipboards, priority selections and preview configuration survive task changes. Field choices and custom naming remain closed initially.
- Component copying separates source copy controls from destination paste controls. Existing Copy, Copy All, Select All, Deselect All, paste modes, Paste, Restore and Clear Clipboard remain available. Reports remain visible in the component task.
- Localisation pairs name and description clipboard summaries with their respective Copy/Paste actions. Priority numbering has Choose assets, Set numbering and Review/apply steps.
- Naming-pattern textarea spans the card width, shows four lines and wraps long expressions. Numeric fields share the available width. Explicit text blocks prevent the selected-asset count splitting across lines.
- `FieldChoiceReader` replaces the failing Unity DTO parse with bounded explicit JSON token validation. Empty field selections are preserved; invalid/duplicate/oversized payloads refuse without replacing the clipboard. Newtonsoft.Json 13.0.0.0 is referenced from the installed game, never modified or redistributed.
- Pack requests and state also use managed JSON (`PackJson`) rather than Unity's runtime field serializer. This includes nested workspace rows and preview arrays. Type-name handling is disabled and depth/size bounded for incoming requests. Actual-source tests roundtrip private nested row DTOs using the installed Newtonsoft assembly.
- Scan clears a stale search filter and reports resolved/eligible asset counts. Empty workspace and empty filtered results have different messages. The native hierarchy source is unchanged from the accepted Multi-Save method; diagnostics record hierarchy-row and resolved-asset counts.

VERIFIED-RUNTIME evidence: the user reported `ReadFieldChoices` returning no choices and the workspace list appearing empty in 0.4.182. HYPOTHESIS: Unity runtime serialization of the new nested DTO arrays caused both symptoms. The replacement avoids that path, but game acceptance is still needed; no claim that the user's native workspace has been reproduced.

Validation: managed/UI builds pass (existing NU1900 vulnerability-feed warning). All 14 UI suites, 98 component transfer checks and 30 managed JSON checks pass. UI-generated Copy/Copy All payloads are fed into the actual field reader. Task switching preserves field overrides, localisation/priority controls are separated, and the large textarea is present. Native rendering, copy/paste, workspace scan and priority/localisation operations remain pending in-game acceptance.

This build includes the prior 0.4.182 deletion Undo work. No publishing or computer use. The earlier 0.4.183 parser-only package was staged but installation stopped because Cities2 was running; 0.4.184 supersedes it.

Installed 0.4.184 with Cities2 fully exited. All previous installed files were backed up and hash-verified; installed DLL/MJS/CSS match the staged build. See evidence/components-layout-0.4.184/installation.txt.
