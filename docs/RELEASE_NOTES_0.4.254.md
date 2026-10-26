# Release notes - 0.4.254

Adds a configurable shortcut for hiding and showing the Asset Editor UI through the native free-camera screen.

- Default: Backquote (`), without modifiers. Press again to show the UI; Escape also restores it.
- Configure or disable Hide / show UI in Options > MW Editor Tools, then Apply changes.
- Supports standalone letters, number-row keys, F1-F12, navigation keys and punctuation, plus modifier combinations.
- The former Ctrl+H default migrates to Backquote. Other custom bindings and the enable preference are preserved.
- Typing/dialog/save/recovery guards and MW shortcut conflict checks are retained. Restoring the UI does not depend on hidden browser heartbeats.

VERIFIED-RUNTIME: the user confirmed Backquote hide/show works on 0.4.254. This acceptance covers the reported shortcut workflow; it does not certify every key, persistence scenario or third-party binding combination. Accepted 0.4.251 editing behavior is retained.

Includes the updated illustrated PDF and Markdown manuals, all eight existing screenshots, Windows installer, manual ZIP and SHA256SUMS.txt. See [release readiness](RELEASE_READINESS_0.4.254.md) for checks. The existing unsigned .NET Framework 4.8 installer makes a verified backup and verifies installed files. Close Cities: Skylines II before installation.

All previous supported operations and limits remain as documented in [0.4.251 release notes](RELEASE_NOTES_0.4.251.md). Native/third-party shortcut conflicts remain user-configured. The repository remains private; no game files are modified.
