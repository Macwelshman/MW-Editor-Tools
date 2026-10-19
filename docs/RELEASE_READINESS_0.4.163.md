# Release readiness — 0.4.163

Status: automated regression and verified private candidate packaging complete. User confirms UI appearance and Options already checked. Prior feature/persistence acceptances are retained. No public publishing authorised.

## Evidence

VERIFIED-STATIC: all 23 managed test projects and seven UI render/interaction scripts pass on the current source. Per-suite output is in docs/evidence/release-0.4.163/. Checks cover Undo capture/restore and cancellation, node geometry, effects/foliage/nested recovery, component transfer modes/settings/rollback, confirmation, lifecycle, Colours, Multi-Save, Options/default migration/conflicts, tab commands, tooltips' child controls and window positioning/resize. Managed doubles do not prove native scheduling, Cohtml layout or actual saving.

VERIFIED-RUNTIME (prior user acceptance): Undo stage including effect persistence accepted on 0.4.143; viewport/browser element replacement and batch including foliage/effects/nested props and persistence accepted through 0.4.159; component transfer modes, settings, Restore and persistence accepted on 0.4.160. These are prior-build/user-reported gates, not a fresh combined native run of 0.4.163.

VERIFIED-STATIC: source/document credential-pattern scan found no matches before the repository snapshot. Generated outputs, game assemblies, dependencies, runtime logs and rollback folders are excluded from Git. GitHub reports Macwelshman/MW-Editor-Tools private. Existing repository reused rather than duplicated. Public distribution remains unauthorised.

## Optional combined native smoke check

The user has already checked UI appearance and Options, and accepted the feature workflows/persistence previously. Do not repeat these as new blocking micro-tests. The following is an optional combined smoke sequence for future release testing; no fresh combined native run or individual Alt key dispatch is claimed by this pass. Use disposable copies and do not overwrite originals:

1. Inspect all four populated/empty tabs, introductory lines, tooltips, standard buttons and compact Options. Close/reopen and drag the window; verify launcher and saved position.
2. Exercise all six Alt browser shortcuts, search retention, a custom binding after restart, and no dispatch while typing/dialogs. Confirm Apply persists changes.
3. With window open and closed, Undo a prop move/deletion, an effect action and a surface/area node edit; verify header Undo and Ctrl+Z. Include one cancelled move retaining the prior slot.
4. Replace one element and batch replace; include foliage with original source retained and an effect/nested prop. Component paste in one mode, then Restore; verify source/destination ownership/settings.
5. Edit/apply a colour variation, exercise its local Undo and draft guard. Save disposable copies through Multi-Save, inspect results, and reopen to compare the final content. Locked assets must remain excluded; Stop should finish the current item.

Readiness is based on the recorded per-feature user acceptances, latest UI/Options check and automated regression; it is not a claim of a fresh exhaustive native run. Existing scope exclusions remain in README and release notes. Earlier intermittent type/source-row mismatch remains an open documented limitation, with refusal diagnostics retained.

## Build/package

Release .NET and production UI builds are rerun and logged for packaging. NU1900 means vulnerability metadata could not be fetched, so no clean online vulnerability-audit claim is made. Package contains only the four runtime files, README, release notes/readiness and third-party notice, plus SHA256 manifest. Native game assemblies and development/evidence/rollback files are excluded. Archive entries and their content hashes are verified against the package directory. The installed 0.4.163 file hashes are compared with runtime package files; installation is not repeated for documentation-only work.

VERIFIED-RUNTIME (user report during this pass): UI appearance and Options already checked. No repeated UI/Options acceptance requested.

