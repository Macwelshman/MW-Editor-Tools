param([string]$ManagedPath='C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed')
$ErrorActionPreference='Stop'
Add-Type -LiteralPath (Join-Path $ManagedPath 'Colossal.Mono.Cecil.dll')
$assembly=[Colossal.Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $ManagedPath 'Game.dll'))
try {
    if ($assembly.MainModule.Mvid.ToString() -ne 'e15551e9-3dea-4855-8dde-1da3cea7d7ef') { throw 'Unverified native scheduling build.' }
    $order=$assembly.MainModule.Types | Where-Object FullName -eq 'Game.Common.SystemOrder'
    $initialize=$order.Methods | Where-Object Name -eq 'Initialize'
    $calls=@($initialize.Body.Instructions | Where-Object { $_.Operand -is [Colossal.Mono.Cecil.GenericInstanceMethod] -and $_.Operand.FullName -match 'UpdateAt<Game.Rendering.AreaBatchSystem>' })
    if ($calls.Count -ne 1) { throw 'Native area batching registration is ambiguous.' }
    $phase=[int]$calls[0].Previous.Operand
    $enum=$assembly.MainModule.Types | Where-Object FullName -eq 'Game.SystemUpdatePhase'
    $expected=$enum.Fields | Where-Object Name -eq 'PreCulling'
    if ($phase -ne [int]$expected.Constant) { throw 'Native area batching phase changed.' }
    $source=Get-Content (Join-Path $PSScriptRoot '../src/Core/Mod.cs') -Raw
    if (!$source.Contains('SurfaceRenderSystem,Game.Rendering.AreaBatchSystem>(SystemUpdatePhase.PreCulling)')) { throw 'Surface callback must use the same phase as its native reference system.' }
    'PASS: exact native area batching phase and production refresh registration agree: PreCulling (27).'
} finally { $assembly.Dispose() }
