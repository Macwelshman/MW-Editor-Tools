# 0.4.83 adaptive layout and colour spelling

VERIFIED-STATIC: shared body now uses natural content height rather than fixed640rem, capped85vh; the section retains overflow scrolling when screen space is insufficient. Existing tab row and opening-coordinate lifetime preserved. No claim that arbitrarily long details can fit without scrolling on every resolution.

Channel lists with0..3rows render ordinary naturally sized blocks, without a native Scrollable. Lists with4+rows retain native Scrollable; explicit42rem rows and126rem viewport fit three complete swatches. Visible colour strings standardised in controls, picker, native statuses/errors; Game ColorProperties API/type and binding/CSS identifiers unchanged. Exact source comparison confirms original engine logic unchanged outside quoted strings, entrypoint removal and namespace migration.

VERIFIED-STATIC: Release zero warnings/errors; UI build/TypeScript,27colour rendering/command/threshold checks (0,1,2,3,4,7rows),29tabchecks,19savechecks,5positionchecks andsettingsabsencecheck pass.29palette/writegate and17copy/transport assertions rerun pass. Remaining earlier740-assertion suite inherited; this release changes layout/display strings/version only. Native geometry/scroll rendering remain UNPROVEN until visual acceptance.

Selection workflow suggestion delivered, not implemented: automatically follow current selection; persistent colour clipboard; Copy colour settings on source, select destination, Paste colour settings with destination confirmation. Ask before abandoning uncommitted palette edits. Existing Use current selection flow unchanged pending user direction.

User accepts prior read-only Apply refusal as expected.0.4.82 live log reports generation9combinations; native writable Apply/Undo/paste acceptance still unreported. Shortcut0.4.79 native correction acceptance also unreported. No computer use, publishing or original-source edits.
