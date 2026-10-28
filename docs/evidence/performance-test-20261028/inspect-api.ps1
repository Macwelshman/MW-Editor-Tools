param([string]$ManagedPath='C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed')
$ErrorActionPreference='Stop'
Add-Type -LiteralPath (Join-Path $ManagedPath 'Colossal.Mono.Cecil.dll')
$out=$PSScriptRoot
$targets=@{
 'Game.UI.Editor.InspectorPanelSystem'=@('RemoveComponent','CollectRequiringComponents','UpdateParent','RefreshSections','SelectObject','SelectPrefab','SelectEntity','OnValueChanged','OnCreate','BuildLocalizationFields','IsLocalizationReadOnly')
 'Game.UI.Editor.EditorHierarchyUISystem'=@('GetHierarchy','OnUpdate')
 'Game.Tools.DefaultToolSystem'=@()
 'Game.UI.Editor.EditorPanelSystemBase'=@('get_children')
 'Game.UI.Widgets.ArrayAdapter'=@('DeleteElementInternal')
 'Game.UI.Widgets.ListAdapter'=@('DeleteElementInternal')
 'Game.UI.Widgets.PagedList'=@('Clear','CreateElement')
 'Game.Tools.BulldozeToolSystem'=@('Apply','GetAllowApply')
 'Game.Tools.ObjectToolSystem'=@('StartMoving','Apply','GetAllowApply')
 'Game.Tools.AreaToolSystem'=@('set_recreate','Apply','Cancel')
 'Game.Tools.ToolBaseSystem'=@('GetAllowApply','InitializeRaycast','OnUpdate')
 'Game.Tools.ToolRaycastSystem'=@('GetRaycastResult','OnUpdate')
 'Game.Tools.ApplyPrefabsSystem'=@('OnUpdate')
 'Game.Prefabs.PrefabSystem'=@('OnUpdate','UpdatePrefab','UpdatePrefabs')
 'Game.Prefabs.ReplacePrefabSystem'=@('ReplacePrefab')
 'Game.Prefabs.ReplacePrefabSystem/Finalize'=@('OnUpdate')
 'Game.Rendering.PreCullingSystem'=@('OnUpdate')
 'Game.Rendering.BatchManagerSystem'=@('GetNativeBatchGroups','GetNativeBatchInstances')
 'Game.Rendering.AreaBatchSystem/CullingActionJob'=@('FailedCulling')
 'Game.Rendering.AreaBatchSystem/BatchAllocationJob'=@('Execute')
 'Game.Rendering.AreaBatchSystem/AllocationAction'=@()
 'Game.Common.SystemOrder'=@('Initialize')
 'Game.Tools.ApplyAreasSystem/HandleTempEntitiesJob'=@('Execute')
 'Game.Tools.ApplyObjectsSystem/HandleTempEntitiesJob'=@('Execute')
 'Game.Tools.ControlPoint'=@()
 'Game.Common.RaycastResult'=@()
 'Game.Tools.Temp'=@()
 'Game.Tools.EditorContainer'=@()
 'Game.Rendering.MeshBatch'=@()
 'Game.Rendering.InstanceData'=@()
 'Game.Rendering.GroupData'=@('GetPropertyIndex')
 'Game.UI.AppBindings'=@('ShowConfirmationDialog')
 'Colossal.Rendering.NativeBatchInstances`4/ParallelInstanceWriter'=@('GetPropertyValue','SetPropertyValue')
 'Colossal.Rendering.NativeBatchInstances`4'=@('AsParallelInstanceWriter','GetInstanceData','GetInstanceCount')
 'Colossal.Rendering.NativeBatchGroups`4'=@('GetGroupData','IsValidGroup','GetGroupCount')
 'Colossal.Rendering.UnsafeBatches`4'=@('GetPropertyValue','SetPropertyValue')
 'Colossal.Collections.NativeHeapAllocator'=@('Release')
 'Colossal.Collections.NativeHeapBlock'=@()
 'Unity.Collections.NativeQueue`1'=@('Enqueue','Dequeue','AsParallelWriter')
 'Unity.Entities.DynamicBuffer`1'=@('GetEnumerator','ToNativeArray')
 'Unity.Entities.DynamicBuffer`1/Enumerator'=@('MoveNext','get_Current')
}
function AllTypes($items){foreach($t in $items){$t;AllTypes $t.NestedTypes}}
$assemblies=@('Game','Colossal.Core','Colossal.Collections','Unity.Collections','Unity.Entities')|ForEach-Object{[Colossal.Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $ManagedPath ($_+'.dll')))}
try {
 $methods=@{}; $fields=@();$dump=[System.Collections.Generic.List[string]]::new()
 $identities=foreach($a in $assemblies){@{Assembly=$a.Name.FullName;MVID=$a.MainModule.Mvid.ToString();SHA256=(Get-FileHash $a.MainModule.FileName).Hash}}
 $identities|ConvertTo-Json|Set-Content (Join-Path $out 'assembly-identities.json')
 foreach($a in $assemblies){foreach($t in (AllTypes $a.MainModule.Types)){
  if(!$targets.ContainsKey($t.FullName)){continue}
  $dump.Add('TYPE '+$t.FullName)
  foreach($f in $t.Fields){$fields+=@{Type=$t.FullName;Name=$f.Name;FieldType=$f.FieldType.FullName;Attributes=$f.Attributes.ToString()};$dump.Add('FIELD '+$f.FullName)}
  foreach($m in $t.Methods){
   if($m.Name -notin $targets[$t.FullName]){continue}
   $il=@($m.Body.Instructions|ForEach-Object ToString)
   $methods[$m.FullName]=@{IL=$il;Attributes=$m.Attributes.ToString();Baseline=@();Exact=$false}
   $dump.Add('METHOD '+$m.FullName);foreach($line in $il){$dump.Add($line)};$dump.Add('')
  }
 }}
 $dump | Set-Content (Join-Path $out 'beta-selected-il.txt')
 $fields | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $out 'beta-fields.json')
 # Reuse recorded stable IL only; do not load or copy old game assemblies.
 foreach($file in (Get-ChildItem (Join-Path $out '..') -Filter '*.txt' -Recurse)){
  if($file.FullName.StartsWith($out)){continue}
  $key=$null;$lines=[System.Collections.Generic.List[string]]::new()
  foreach($line in [System.IO.File]::ReadLines($file.FullName)){
   if($line -match '^IL_[0-9a-f]+: '){if($key){$lines.Add($line.TrimEnd())};continue}
   if($key -and $lines.Count -gt 0){
    $entry=$methods[$key];$same=($entry.IL -join "`n") -ceq ($lines -join "`n")
    $entry.Baseline+=@{File=$file.FullName.Substring((Get-Location).Path.Length+1);Exact=$same;Instructions=$lines.Count}
    if($same){$entry.Exact=$true}
   }
   $key=$null;$lines.Clear()
   $candidate=($line -replace '^METHOD ','') -replace ' attributes=.*$',''
   if($methods.ContainsKey($candidate)){$key=$candidate}
  }
  if($key -and $lines.Count -gt 0){$entry=$methods[$key];$same=($entry.IL -join "`n") -ceq ($lines -join "`n");$entry.Baseline+=@{File=$file.FullName.Substring((Get-Location).Path.Length+1);Exact=$same;Instructions=$lines.Count};if($same){$entry.Exact=$true}}
 }
 $report=@($methods.GetEnumerator()|Sort-Object Name|ForEach-Object{[pscustomobject]@{Method=$_.Key;ExactStableIL=$_.Value.Exact;StableEvidence=$_.Value.Baseline;BetaInstructions=$_.Value.IL.Count}})
 $report|ConvertTo-Json -Depth 8|Set-Content (Join-Path $out 'stable-comparison.json')
 $report|Select-Object Method,ExactStableIL,@{n='RecordedBodies';e={$_.StableEvidence.Count}}|Format-Table -AutoSize|Out-String -Width 240
}finally{foreach($a in $assemblies){$a.Dispose()}}
