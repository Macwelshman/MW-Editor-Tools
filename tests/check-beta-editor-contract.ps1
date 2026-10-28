param([string]$ManagedPath='C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed')
$ErrorActionPreference='Stop'
Add-Type -LiteralPath (Join-Path $ManagedPath 'Colossal.Mono.Cecil.dll')
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$evidence=Join-Path $root 'docs/evidence/performance-test-20261028'
$identities=Get-Content (Join-Path $evidence 'assembly-identities.json') -Raw|ConvertFrom-Json
$assemblies=@{};$types=@{};$checks=0
function Types($items){foreach($item in $items){$item;Types $item.NestedTypes}}
try {
 foreach($identity in $identities){
  $name=$identity.Assembly.Split(',')[0];$path=Join-Path $ManagedPath ($name+'.dll')
  if((Get-FileHash $path).Hash -ne $identity.SHA256){throw "Assembly changed since API audit: $name"}
  $a=[Colossal.Mono.Cecil.AssemblyDefinition]::ReadAssembly($path);$assemblies[$name]=$a
  if($a.MainModule.Mvid.ToString() -ne $identity.MVID){throw "MVID changed: $name"}
  foreach($t in (Types $a.MainModule.Types)){$types[$t.FullName]=$t}
  $checks++
 }
 # Every previously recorded complete stable body used by the audit must still
 # match the installed beta. Read the original stable evidence independently.
 $comparison=Get-Content (Join-Path $evidence 'stable-comparison.json') -Raw|ConvertFrom-Json
 foreach($entry in $comparison|Where-Object ExactStableIL){
  $signature=$entry.Method;$typeName=($signature -split ' ')[1] -replace '::.*$',''
  # Return types containing spaces are absent in these selected Cecil signatures.
  $m=@($types[$typeName].Methods|Where-Object FullName -ceq $signature)
  if($m.Count -ne 1){throw "Native method missing/ambiguous: $signature"}
  $current=(@($m[0].Body.Instructions|ForEach-Object ToString) -join "`n")
  $matched=$false
  foreach($baseline in $entry.StableEvidence|Where-Object Exact){
   $recorded=Get-Content (Join-Path $root $baseline.File) -Raw
   $pattern='(?m)^(?:METHOD )?'+[regex]::Escape($signature)+'(?: attributes=[^\r\n]*)?\r?\n((?:IL_[^\r\n]*\r?\n)+)'
   foreach($block in [regex]::Matches($recorded,$pattern)){
    $old=($block.Groups[1].Value.TrimEnd() -replace "`r`n","`n")
    if($old -ceq $current){$matched=$true;break}
   }
   if($matched){break}
  }
  if(!$matched){throw "Stable IL comparison failed: $signature"}
  $checks++
 }
 $fields=@{
  'Game.UI.Editor.InspectorPanelSystem'=@{m_ParentObject='System.Object';m_SectionObjects='System.Collections.Generic.List`1<System.Object>'}
  'Game.UI.AppBindings'=@{m_ConfirmationDialogCallback='System.Action`1<System.Int32>';m_DismissibleConfirmationDialogCallback='System.Action`2<System.Int32,System.Boolean>'}
  'Game.Tools.BulldozeToolSystem'=@{m_ControlPoints='Unity.Collections.NativeList`1<Game.Tools.ControlPoint>'}
  'Game.Tools.DefaultToolSystem'=@{m_LastRaycastEntity='Unity.Entities.Entity'}
  'Game.Tools.ObjectToolSystem'=@{m_MovingObject='Unity.Entities.Entity'}
  'Game.Tools.AreaToolSystem'=@{m_State='Game.Tools.AreaToolSystem/State';m_ControlPoints='Unity.Collections.NativeList`1<Game.Tools.ControlPoint>';m_TempAreaQuery='Unity.Entities.EntityQuery'}
  'Game.Tools.ApplyPrefabsSystem'=@{m_SaveInstanceQuery='Unity.Entities.EntityQuery'}
  'Game.Prefabs.PrefabSystem'=@{m_UpdateMap='System.Collections.Generic.Dictionary`2<Game.Prefabs.PrefabBase,Unity.Entities.Entity>'}
  'Game.Prefabs.ReplacePrefabSystem'=@{m_UpdateInstances='Unity.Collections.NativeQueue`1<Unity.Entities.Entity>'}
  'Game.UI.Editor.EditorHierarchyUISystem'=@{m_Hierarchy='Unity.Collections.NativeList`1<Game.UI.Editor.EditorHierarchyUISystem/HierarchyItem>'}
 }
 foreach($typeName in $fields.Keys){foreach($name in $fields[$typeName].Keys){
  $field=@($types[$typeName].Fields|Where-Object Name -ceq $name)
  if($field.Count -ne 1 -or $field[0].FieldType.FullName -cne $fields[$typeName][$name]){throw "Private field contract changed: ${typeName}::$name"}
  if($field[0].IsStatic){throw "Expected instance field: ${typeName}::$name"};$checks++
 }}
 # The outline writer operates in GPU property bytes and honors native job fences.
 foreach($name in @('GetNativeBatchGroups','GetNativeBatchInstances')){
  $m=$types['Game.Rendering.BatchManagerSystem'].Methods|Where-Object Name -eq $name
  $il=($m.Body.Instructions|ForEach-Object ToString) -join "`n"
  foreach($token in @('ReadDependencies','WriteDependencies','JobHandle::CombineDependencies')){if(!$il.Contains($token)){throw "Renderer job fence missing: $name $token"}}
  $checks++
 }
 $writer=$types['Colossal.Rendering.UnsafeBatches`4'].Methods|Where-Object {$_.Name -eq 'GetPropertyValue' -and $_.GenericParameters.Count -eq 1}
 $il=($writer.Body.Instructions|ForEach-Object ToString) -join "`n"
 foreach($token in @('m_GPUAllocation','m_SizeBytesGPU','CopyPtrToStructure','m_UpdateMask')){if(!$il.Contains($token)){throw "Outline property read contract changed: $token"};$checks++}
 foreach($typeName in @('Game.Rendering.ObjectProperty','Game.Rendering.NetProperty','Game.Rendering.LaneProperty')){
  $field=$types[$typeName].Fields|Where-Object Name -eq 'OutlineColors'
  $attribute=$field.CustomAttributes|Where-Object {$_.AttributeType.Name -eq 'InstancePropertyAttribute'}
  if(!$attribute -or $attribute.ConstructorArguments[0].Value -ne '_Outlines_Color'){throw "Outline attribute changed: $typeName"};$checks++
 }
 "PASS: $checks exact beta identities, stable IL comparisons, private field types and renderer dependency/property contracts. VERIFIED-STATIC; native acceptance pending."
}finally{foreach($a in $assemblies.Values){$a.Dispose()}}
