# Browser capture correction — 0.4.148

VERIFIED-RUNTIME (user report): viewport Ctrl+C succeeds; browser copying does not. The log records viewport captures and an expected-reference exception during row enumeration; no browser keyboard callback is recorded.

VERIFIED-STATIC: ObjectSubLaneInfo uses m_LanePrefab (see NetworkDescriptorPolicy), whereas the original reader requested m_Lane. That exception discarded all enumerated rows. The corrected schema restores row enumeration.

Browser hover samples now arrive at 100 ms intervals using mousemove coordinates. The native Ctrl+C handler attempts the fresh browser candidate before viewport capture, so browser keyboard-event delivery is no longer required. Missing/ambiguous candidates preserve the existing clipboard. Own mod panels are excluded. Release/UI builds and shared tab regression pass. Native browser copy acceptance remains UNPROVEN.
