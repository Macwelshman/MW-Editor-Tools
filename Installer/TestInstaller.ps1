param([string]$ReleaseDirectory=(Join-Path $PSScriptRoot '../outputs/release-0.4.164'))
$ErrorActionPreference='Stop'
$output=[IO.Path]::GetFullPath($ReleaseDirectory)
$manifest=Join-Path $output 'payload.manifest'
if(!(Test-Path -LiteralPath $manifest)){throw 'Build the installer payload first.'}
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$test=Join-Path $output 'InstallerTests.exe'
$arguments=@('/nologo','/target:exe','/optimize+','/warnaserror+','/reference:System.dll','/reference:System.Core.dll',('/out:'+$test),('/resource:'+$manifest+',payload.manifest'))
foreach($line in Get-Content -LiteralPath $manifest){if(!$line){continue};$name=($line -split '\|')[0];$arguments+=('/resource:'+(Join-Path $output ('MWEditorTools/'+$name))+',payload.'+$name)}
$arguments+=@((Join-Path $PSScriptRoot 'InstallerCore.cs'),(Join-Path $PSScriptRoot 'InstallerTests.cs'))
& $compiler @arguments
if($LASTEXITCODE -ne 0){throw 'Installer harness compilation failed.'}
$fixture=Join-Path $output ('test-fixtures/'+[Guid]::NewGuid().ToString('N'))
$linkHome=Join-Path $output ('link-fixtures/'+[Guid]::NewGuid().ToString('N'))
$actual=Join-Path $linkHome 'actual'
$linked=Join-Path $linkHome 'Mods/MWEditorTools'
New-Item -ItemType Directory -Path $actual,(Split-Path $linked) -Force | Out-Null
Set-Content -LiteralPath (Join-Path $actual 'untouched.txt') -Value 'junction target must remain unchanged'
New-Item -ItemType Junction -Path $linked -Target $actual | Out-Null
& $test $fixture $linked
if($LASTEXITCODE -ne 0){throw 'Installer harness failed.'}
Write-Output "Test fixtures retained at $fixture"
