# Release readiness — 0.4.164

19 October 2026. Scope: supplied launcher logo, transparent corners, top-row placement with collision avoidance, and persistent open/close toggle. Editing and recovery implementations are unchanged. Previous feature acceptance and limitations remain recorded in [0.4.163 readiness](RELEASE_READINESS_0.4.163.md).

- VERIFIED-STATIC: mod Release build succeeds. NU1900 reports unavailable online vulnerability metadata; no new dependency was introduced.
- VERIFIED-STATIC: production UI build succeeds without warnings. All nine `tests/ui-*.cjs` scripts pass, including 12 placement scenarios and actual launcher visibility/open/close command checks.
- VERIFIED-STATIC: the logo was rendered and visually inspected at 256 × 256. All four corner alpha samples are zero; the centre alpha is 255. The supplied SVG is retained, with an exact outer-path clip, alongside the derived PNG used in the UI.
- VERIFIED-STATIC: installer compilation and all 34 engine checks pass against the actual embedded 0.4.164 payload, including linked-folder refusal, backup/hash verification and rollback. Tests use temporary workspace folders.
- VERIFIED-STATIC: `Installer/VerifyRelease.ps1` passes for the EXE resources, every ZIP entry and the release checksum manifest.
- UNSUPPORTED/UNPROVEN: this release has no new native-game acceptance run. Automated layout checks do not establish compatibility with every third-party launcher, screen size or native UI renderer.

The original MW Component Tool launcher already yields to other icons, so it is excluded from this launcher's obstacles to prevent both launchers repeatedly exchanging positions. Other visible buttons occupying the same vertical row are avoided. Placement is rechecked on state/sidebar changes, resize and a 500 ms timer; the timer and listener are removed on unmount.

Distribution: versioned installer, manual ZIP and SHA256SUMS.txt in the existing private repository. No repository visibility changes or publication to other platforms.
