# Toolchain verification — 9 October 2026

VERIFIED: .NET SDK 8.0.425; official `csiimod` template (ColossalOrder.ModTemplate 1.0.0). The installed template imports user CSII_TOOLPATH/Mod.props and Mod.targets. Read-only inspection of Mod.props confirms Library output, net48 and C# 9.0. Installed configuration resides in the game's user .cache/Modding directory. Initial sandbox views hid that directory and user environment values; unrestricted read confirmed they exist. No repair of the environment was required.

Build of the inert scaffold passed: zero warnings, zero errors. Initial --no-restore build lacked .NET 4.8 reference assemblies. Normal package restore supplied references; a Developer Pack installation was not needed for this build. Build artifacts were produced only in the scaffold's bin/obj directories. No installed game files or mods were changed.

Official Mod.targets contains AfterBuild postprocessing and DeployWIP, which removes the deployment directory and copies build output into local mods. Publish invokes a publisher. These targets were inspected but not imported or executed. The inert project uses plain Microsoft.NET.Sdk and has no deployment targets or game entry point.

This validates the metadata scaffold's compilation, not game API compilation, mod loading, Unity/ECS postprocessing or runtime compatibility. Those remain future gates. Unity licence activation and every toolchain dependency were not independently tested; SDK/template/build verification does not establish full toolchain health.
