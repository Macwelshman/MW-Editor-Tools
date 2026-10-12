# 0.4.78 automatic standalone capture

VERIFIED-STATIC: supported single standalone prop deletion automatically enables existing Main/Everything recovery before native Apply, when recovery is on. Exact typed capture must succeed before changing mode or clearing previous history. Duplicate control points for the same entity are accepted; multiple targets, uncreated points and unsupported schemas are refused. No writes occur during preflight. Manual enable remains available. Automatic activation is blocked by save activity, dialogs, active recovery/preparation, running nested diagnostics and the standalone failure latch. Bound/nested recovery retains its existing manual enable workflow; full Undo/Redo scope remains incomplete.

Existing strict MainPropCapture and MainPropRecovery retain native archetype/schema, transform, seed, colour, ownership and deferred completion validation. No new game APIs or patches. Recorded Game.dll identity unchanged: SHA256 AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A, Game0.0.0.0 MVID e15551e9-3dea-4855-8dde-1da3cea7d7ef. Reuses prior standalone restoration runtime evidence; automatic hook timing is UNPROVEN until native acceptance.

VERIFIED-STATIC: Release zero warnings/errors; 672 C# assertions (six new automatic activation gate/capture-order/refusal checks), UI build, TypeScript and all four UI rendering/position suites pass. Managed gate tests do not simulate ECS or native hook timing.

Next native check in disposable editor session: place one standalone Grill01, without clicking Enable; Main-delete it, then Undo, Redo, Undo. Expect one restored copy at original position and named history. Do not save. No repeat Multi-Save test required. Networks, lanes, areas, effects, whole buildings and arbitrary schemas remain unsupported for bulldozer restoration.

VERIFIED-RUNTIME: preserved shortcut-0.4.78-runtime.log records automatic capture without explicit enable and native completed standalone Grill01 Undo/Redo/Undo at17:49:46..48, plus Redo17:49:56. User confirms buttons work. Subsequent user-reported shortcut crash is separate and under correction in0.4.79; native Player log has no faulting stack.
