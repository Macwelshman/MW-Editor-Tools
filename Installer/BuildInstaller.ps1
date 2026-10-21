param([string]$OutputDirectory=(Join-Path $PSScriptRoot '../outputs/release-0.4.180'))
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$output=[IO.Path]::GetFullPath($OutputDirectory)
$payload=Join-Path $output 'MWEditorTools'
New-Item -ItemType Directory -Path $payload -Force | Out-Null
$sources=@{
 'MW.EditorTools.dll'='bin/Release/net48/MW.EditorTools.dll'
 '0Harmony.dll'='bin/Release/net48/0Harmony.dll'
 'MW.EditorTools.mjs'='dist/MW.EditorTools.mjs'
 'MW.EditorTools.css'='dist/MW.EditorTools.css'
 'USER_MANUAL.md'='docs/USER_MANUAL.md'
 'USER_MANUAL.pdf'='output/pdf/MW-Editor-Tools-User-Manual-0.4.180.pdf'
 'THIRD_PARTY_NOTICES.md'='THIRD_PARTY_NOTICES.md'
 'RELEASE_NOTES.md'='docs/RELEASE_NOTES_0.4.180.md'
 'INSTALL.txt'='Installer/INSTALL.txt'
}
foreach($name in $sources.Keys){$source=Join-Path $project $sources[$name];if(!(Test-Path -LiteralPath $source)){throw "Missing payload: $source"};Copy-Item -LiteralPath $source -Destination (Join-Path $payload $name) -Force}
if([Reflection.AssemblyName]::GetAssemblyName((Join-Path $payload 'MW.EditorTools.dll')).Version.ToString() -ne '0.4.180.0'){throw 'Unexpected mod version'}
$manifest=Join-Path $output 'payload.manifest'
$names=@($sources.Keys | Sort-Object)
$names | ForEach-Object {$_+'|'+(Get-FileHash -LiteralPath (Join-Path $payload $_) -Algorithm SHA256).Hash} | Set-Content -LiteralPath $manifest -Encoding UTF8
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if(!(Test-Path -LiteralPath $compiler)){throw '.NET Framework C# compiler not found'}
$setup=Join-Path $output 'MWEditorTools-0.4.180-Setup.exe'
$arguments=@('/nologo','/target:winexe','/optimize+','/warnaserror+','/reference:System.dll','/reference:System.Core.dll','/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll',('/out:'+$setup),('/resource:'+$manifest+',payload.manifest'))
foreach($name in $names){$arguments+=('/resource:'+(Join-Path $payload $name)+',payload.'+$name)}
$arguments+=@((Join-Path $PSScriptRoot 'InstallerCore.cs'),(Join-Path $PSScriptRoot 'Setup.cs'))
& $compiler @arguments
if($LASTEXITCODE -ne 0){throw 'Installer compilation failed'}
$names | ForEach-Object {(Get-FileHash -LiteralPath (Join-Path $payload $_)).Hash+'  '+$_} | Set-Content -LiteralPath (Join-Path $payload 'SHA256SUMS.txt')
$zip=Join-Path $output 'MWEditorTools-0.4.180.zip'
Compress-Archive -LiteralPath $payload -DestinationPath $zip -Force
$pdf=Join-Path $output 'MW-Editor-Tools-User-Manual-0.4.180.pdf'
Copy-Item -LiteralPath (Join-Path $payload 'USER_MANUAL.pdf') -Destination $pdf -Force
@($setup,$zip,$pdf) | ForEach-Object {(Get-FileHash -LiteralPath $_).Hash+'  '+[IO.Path]::GetFileName($_)} | Set-Content -LiteralPath (Join-Path $output 'SHA256SUMS.txt')
Write-Output "Built $setup"
Write-Output "Built $zip"
