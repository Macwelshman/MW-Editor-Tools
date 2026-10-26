$ErrorActionPreference='Stop'
$managed='C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed'
Add-Type -LiteralPath (Join-Path $managed 'Colossal.Mono.Cecil.dll')
$a=[Colossal.Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $managed 'Game.dll'))
function Walk($types){foreach($t in $types){$t;Walk $t.NestedTypes}}
$types=@(Walk $a.MainModule.Types)
foreach($t in $types | Where-Object { $_.FullName -match '^Game.Common.RaycastSystem$' }){
  $t.FullName
  foreach($f in $t.Fields){$f.FullName}
  foreach($m in $t.Methods){
    if($m.HasBody -and ($m.Name -in @('GetResult','OnValueChanged','UpdateParent','Execute','UpdateArea','Update','LateInitialize','Initialize','OnUpdate','GetArchetypeComponents') -or $t.FullName -match 'SpacePrefab')){$m.FullName; foreach($i in $m.Body.Instructions){$i.ToString()}}
  }
}
$a.Dispose()


