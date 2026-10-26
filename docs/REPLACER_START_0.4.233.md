# Replacer start/stop workflow — 0.4.233

VERIFIED-STATIC: the Replacer activation button is now Start replacing / Stop replacing. Start uses the existing guarded native targeting activation immediately; there is no second targeting toggle. The element filter appears while active, alongside instructions for hover, selection, Copy, Replace and Replace all. Escape and Stop use the existing native Select transition and cleanup. Browser-list replacement remains available while inactive.

VERIFIED-STATIC: no native targeting, outline, replacement or Undo implementation changed. The user manual describes the start/stop workflow.

VERIFIED-RUNTIME (managed): 27 UI render/dispatch assertions pass, including direct activation, stop dispatch, disabled availability, target replacement and retained browser-list commands. UI and Release builds pass; focused whitespace checks pass. Release build reports NU1900 advisory-feed unavailability; this is not a new dependency audit. Evidence: `evidence/replacer-start-0.4.233`.

VERIFIED-RUNTIME (installation): Cities2 was absent; nine installed files were backed up and hash-verified, then the three 0.4.233 package files installed with matching hashes. See `../outputs/installation-0.4.233.txt` and `evidence/replacer-start-0.4.233/installed-hashes.csv`.

UNPROVEN: native acceptance of the revised UI remains pending. Check Start replacing, filtered hover/selection, Copy/Replace, Stop/Escape and existing Undo. The accepted 0.4.232 Undo implementation is unchanged. No publishing performed.
