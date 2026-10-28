param([string]$OutputDirectory=(Join-Path $PSScriptRoot '../outputs/release-0.4.257'),[string]$RuntimeDirectory=(Join-Path $PSScriptRoot '../bin/Release/net48'))
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$output=[IO.Path]::GetFullPath($OutputDirectory)
$payload=Join-Path $output 'MWEditorTools'
$runtime=(Resolve-Path -LiteralPath $RuntimeDirectory).Path
New-Item -ItemType Directory -Path $payload -Force | Out-Null
$sources=@{
 'MW.EditorTools.dll'=(Join-Path $runtime 'MW.EditorTools.dll')
 '0Harmony.dll'=(Join-Path $runtime '0Harmony.dll')
 'MW.EditorTools.mjs'='dist/MW.EditorTools.mjs'
 'MW.EditorTools.css'='dist/MW.EditorTools.css'
 'USER_MANUAL.md'='docs/USER_MANUAL.md'
 'USER_MANUAL.pdf'='output/pdf/MW-Editor-Tools-User-Manual-0.4.257.pdf'
 'THIRD_PARTY_NOTICES.md'='THIRD_PARTY_NOTICES.md'
 'RELEASE_NOTES.md'='docs/RELEASE_NOTES_0.4.257.md'
 'INSTALL.txt'='Installer/INSTALL.txt'
 'manual-replacer.png'='docs/manual-replacer.png'
 'manual-components.png'='docs/manual-components.png'
 'manual-fields.png'='docs/manual-fields.png'
 'manual-priorities.png'='docs/manual-priorities.png'
 'manual-localisation.png'='docs/manual-localisation.png'
 'manual-colours.png'='docs/manual-colours.png'
 'manual-multisave.png'='docs/manual-multisave.png'
 'manual-options.png'='docs/manual-options.png'
}
foreach($name in $sources.Keys){$source=if([IO.Path]::IsPathRooted($sources[$name])){$sources[$name]}else{Join-Path $project $sources[$name]};if(!(Test-Path -LiteralPath $source)){throw "Missing payload: $source"};Copy-Item -LiteralPath $source -Destination (Join-Path $payload $name) -Force}
if([Reflection.AssemblyName]::GetAssemblyName((Join-Path $payload 'MW.EditorTools.dll')).Version.ToString() -ne '0.4.257.0'){throw 'Unexpected mod version'}
$manifest=Join-Path $output 'payload.manifest'
$names=@($sources.Keys | Sort-Object)
$names | ForEach-Object {$_+'|'+(Get-FileHash -LiteralPath (Join-Path $payload $_) -Algorithm SHA256).Hash} | Set-Content -LiteralPath $manifest -Encoding UTF8
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if(!(Test-Path -LiteralPath $compiler)){throw '.NET Framework C# compiler not found'}
$setup=Join-Path $output 'MWEditorTools-0.4.257-Setup.exe'
$arguments=@('/nologo','/target:winexe','/optimize+','/warnaserror+','/reference:System.dll','/reference:System.Core.dll','/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll',('/out:'+$setup),('/resource:'+$manifest+',payload.manifest'))
foreach($name in $names){$arguments+=('/resource:'+(Join-Path $payload $name)+',payload.'+$name)}
$arguments+=@((Join-Path $PSScriptRoot 'InstallerCore.cs'),(Join-Path $PSScriptRoot 'Setup.cs'))
& $compiler @arguments
if($LASTEXITCODE -ne 0){throw 'Installer compilation failed'}
$names | ForEach-Object {(Get-FileHash -LiteralPath (Join-Path $payload $_)).Hash+'  '+$_} | Set-Content -LiteralPath (Join-Path $payload 'SHA256SUMS.txt')
$zip=Join-Path $output 'MWEditorTools-0.4.257.zip'
Compress-Archive -LiteralPath $payload -DestinationPath $zip -Force
$pdf=Join-Path $output 'MW-Editor-Tools-User-Manual-0.4.257.pdf'
Copy-Item -LiteralPath (Join-Path $payload 'USER_MANUAL.pdf') -Destination $pdf -Force
@($setup,$zip,$pdf) | ForEach-Object {(Get-FileHash -LiteralPath $_).Hash+'  '+[IO.Path]::GetFileName($_)} | Set-Content -LiteralPath (Join-Path $output 'SHA256SUMS.txt')
Write-Output "Built $setup"
Write-Output "Built $zip"
