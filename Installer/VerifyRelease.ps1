param([string]$ReleaseDirectory=(Join-Path $PSScriptRoot '../outputs/release-0.4.251'),[string]$InstalledFolder)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($ReleaseDirectory)
$payload=Join-Path $root 'MWEditorTools'
$exe=Join-Path $root 'MWEditorTools-0.4.251-Setup.exe'
$assembly=[Reflection.Assembly]::LoadFile($exe)
foreach($line in Get-Content -LiteralPath (Join-Path $root 'payload.manifest')){
 if(!$line){continue};$parts=$line -split '\|'
 $stream=$assembly.GetManifestResourceStream('payload.'+$parts[0]);if(!$stream){throw 'Missing embedded resource'}
 $sha=[Security.Cryptography.SHA256]::Create()
 try{if([BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','') -ne $parts[1]){throw 'Installer embedded hash mismatch'}}finally{$sha.Dispose();$stream.Dispose()}
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=Join-Path $root 'MWEditorTools-0.4.251.zip'
$archive=[IO.Compression.ZipFile]::OpenRead($zip)
try{
 $entries=@($archive.Entries | Where-Object Name)
 if($entries.Count -ne 18){throw 'Unexpected ZIP content count'}
 foreach($entry in $entries){
  if($entry.FullName -notmatch '^MWEditorTools/[^/]+$'){throw 'Unexpected ZIP entry path'}
  $stream=$entry.Open();$sha=[Security.Cryptography.SHA256]::Create()
  try{if([BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','') -ne (Get-FileHash -LiteralPath (Join-Path $payload $entry.Name)).Hash){throw 'ZIP content hash mismatch'}}finally{$sha.Dispose();$stream.Dispose()}
 }
}finally{$archive.Dispose()}
foreach($line in Get-Content -LiteralPath (Join-Path $root 'SHA256SUMS.txt')){
 $parts=$line -split '  ',2
 if($parts.Length -ne 2 -or (Get-FileHash -LiteralPath (Join-Path $root $parts[1])).Hash -ne $parts[0]){throw 'Release checksum manifest mismatch'}
}
if($InstalledFolder){foreach($name in @('MW.EditorTools.dll','MW.EditorTools.mjs','MW.EditorTools.css','0Harmony.dll')){if((Get-FileHash -LiteralPath (Join-Path $payload $name)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $InstalledFolder $name)).Hash){throw "Installed runtime mismatch: $name"}}}
Write-Output 'PASS: actual EXE resources, all ZIP contents and release checksum manifest verified.'
if($InstalledFolder){Write-Output 'PASS: release runtime files match the installed mod.'}
