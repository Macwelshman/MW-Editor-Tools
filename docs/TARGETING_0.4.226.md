# Blank window correction — 0.4.226

VERIFIED-RUNTIME: user reported opening the MW window removed the editor UI. Current UI.log records TypeError: Cannot read properties of undefined (reading length) at 19:18:12.983, immediately after the MW open command at 19:18:12.564. Logs preserved under docs/evidence/targeting-0.4.226.

HYPOTHESIS: React DOM mounting the newly always-visible HTML select accessed an unavailable options collection in Cohtml. VERIFIED-STATIC: React DOM updateOptions reads node.options.length. Targeting now uses the public cs2/ui Dropdown, DropdownToggle and DropdownItem contract rather than HTML select/option. The extracted local declaration had a conflicting DropdownItem data-interface name; that interface is renamed DropdownItemData to expose the existing component export correctly. No runtime API invented.

A React error boundary wraps tab contents only. A render failure displays a local message and records a diagnostic; header controls and tab switching remain outside this boundary. Shared state binding, visible filter and availability explanation retained. No native Undo capture/restore, companion or reference mapping changes.

VERIFIED-RUNTIME (managed UI harness): 22 targeting assertions, tab fallback/diagnostics and boundary scope, launcher rendering/lifecycle and header-focus checks pass. Release and UI production builds pass, with the previously recorded NU1900 advisory-feed warning. Native dropdown rendering and corrected activation still require gameplay acceptance.

INSTALL VERIFIED: Cities2 absent; installed 0.4.226.0 and three package hashes verified. Nine prior files backed up and hash checked. Details outputs/installation-0.4.226.txt and docs/evidence/targeting-0.4.226/installed-hashes.csv. No game/third-party files modified, no publishing or computer use.
