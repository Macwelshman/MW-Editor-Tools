$ErrorActionPreference='Stop'
$managed='C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed'
Add-Type -LiteralPath (Join-Path $managed 'Colossal.Mono.Cecil.dll')
$a=[Colossal.Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $managed 'Game.dll'))
foreach($t in $a.MainModule.Types){
 $methods=@($t.Methods|Where-Object {$_.HasBody -and $_.Name -in @('GetArchetypeComponents','Initialize','LateInitialize')})
 if($t.FullName -match '^Game.Prefabs.' -and ($methods|Where-Object {($_.Body.Instructions.Operand.FullName -join ' ') -match 'ReadWrite<Game.Areas.(Park|Space|Walk|Pedestrian|Attraction|Lot|Hangaround)'})){
  $t.FullName;foreach($f in $t.Fields){$f.FullName};foreach($m in $methods){$m.FullName;$m.Body.Instructions|ForEach-Object ToString}
 }
 if($t.FullName -in @('Game.UI.Widgets.PagedList','Game.UI.Widgets.EditorSection','Game.UI.Widgets.ExpandableSection','Game.Areas.Park','Game.Areas.Walkable','Game.Prefabs.ParkData','Game.UI.Widgets.ArrayAdapter','Game.UI.Widgets.ListAdapter','Game.UI.Widgets.IListAdapter','Game.UI.Widgets.ReflectionValueAccessor')){$t.FullName;$t.Fields.FullName;$t.Properties.FullName;foreach($m in $t.Methods|Where-Object {$_.HasBody -and $_.Name -in @('Write','WriteProperties','Update','GetChildren','CreateElement','BuildElementsInRange','BuildElement','GetValue','GetIndex','get_visibleChildren')}){$m.FullName;$m.Body.Instructions|ForEach-Object ToString}}
}
$a.Dispose()

