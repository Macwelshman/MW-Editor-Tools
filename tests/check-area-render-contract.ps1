param([string]$ManagedPath='C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed')
$ErrorActionPreference='Stop'
Add-Type -LiteralPath (Join-Path $ManagedPath 'Colossal.Mono.Cecil.dll')
$a=[Colossal.Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $ManagedPath 'Game.dll'))
try {
 if($a.MainModule.Mvid.ToString() -ne 'e15551e9-3dea-4855-8dde-1da3cea7d7ef'){throw 'Unverified Game build'}
 $type=$a.MainModule.Types|Where-Object FullName -eq 'Game.Rendering.AreaBatchSystem'
 $release=$type.NestedTypes|Where-Object Name -eq 'CullingActionJob'
 $allocate=$type.NestedTypes|Where-Object Name -eq 'BatchAllocationJob'
 $action=$type.NestedTypes|Where-Object Name -eq 'AllocationAction'
 foreach($field in $allocate.Fields){
  if($field.Name -in @('m_BatchData','m_AllocationActions')){continue}
  $source=$type.Fields|Where-Object Name -eq $field.Name
  if(!$source -or $source.FieldType.FullName -ne $field.FieldType.FullName){throw "Native allocation job field mismatch: $($field.Name)"}
 }
 $failed=$release.Methods|Where-Object Name -eq 'FailedCulling'
 $execute=$allocate.Methods|Where-Object Name -eq 'Execute'
 if(!$failed -or !$execute){throw 'Missing native release methods'}
 $failedIl=($failed.Body.Instructions|ForEach-Object ToString) -join "`n"
 $executeIl=($execute.Body.Instructions|ForEach-Object ToString) -join "`n"
 foreach($token in @('m_AllocatedSize','m_VisibleCount','m_IsVisible','AllocationAction','::Enqueue')){if(!$failedIl.Contains($token)){throw "Release contract missing $token"}}
 foreach($token in @('m_BatchIndex','NativeHeapAllocator::Release','m_AllocatedSize','m_MetaIndex','::RemoveAtSwapBack','::GetRefRW')){if(!$executeIl.Contains($token)){throw "Allocator contract missing $token"}}
 if($action.Fields.Count -ne 2 -or ($action.Fields|Where-Object Name -eq 'm_Entity').FieldType.FullName -ne 'Unity.Entities.Entity'){throw 'Allocation action changed'}
 $inspector=$a.MainModule.Types|Where-Object FullName -eq 'Game.UI.Editor.InspectorPanelSystem'
 if(!($inspector.Methods|Where-Object {$_.Name -eq 'UpdateParent' -and $_.Parameters.Count -eq 1 -and $_.Parameters[0].ParameterType.FullName -eq 'System.Boolean'})){throw 'Inspector guard target changed'}
 "PASS: exact Game MVID, private allocation job fields, native release/allocator contract, inspector refresh signature. VERIFIED-STATIC; native acceptance pending."
}finally{$a.Dispose()}
