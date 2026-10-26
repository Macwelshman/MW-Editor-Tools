$ErrorActionPreference='Stop'
$managed='C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed'
Add-Type -LiteralPath (Join-Path $managed 'Colossal.Mono.Cecil.dll')
$root='C:\Users\ian\AppData\LocalLow\Colossal Order\Cities Skylines II'
function Types($items){foreach($item in $items){$item;Types $item.NestedTypes}}
$result=foreach($path in @("$root\.cache\Mods\pdx_mods\74604_40\Anarchy.dll")){
 $assembly=[Colossal.Mono.Cecil.AssemblyDefinition]::ReadAssembly($path)
 try{
  $details=@();$contracts=@()
  foreach($type in @(Types $assembly.MainModule.Types)){foreach($method in $type.Methods){
   if(!$method.HasBody){continue}
   if(($type.Namespace -eq 'Anarchy.Patches') -or ($type.FullName -eq 'Anarchy.AnarchyMod' -and $method.Name -eq 'OnLoad') -or ($type.Name -in @('PrefabBase_MarkCurrentPrefabObsolete_Patch','Patch_EditorAssetCategorySystem_AddOverrides')) -or ($type.FullName -eq 'StarQWorkflowKit.Systems.WorkflowSystem' -and $method.Name -in @('OnCreate','SaveAndContinue','AddSpawableObject','AddAssetPack')) -or ($type.FullName -eq 'StarQWorkflowKit.Systems.PackageSystem' -and $method.Name -eq 'SaveAsset') -or ($type.FullName -like 'AssetIconCreator.ScreenshotUtility/*' -and $method.Name -like '*ProcessScreenshot*')){
    $details+=@{Method=$method.FullName;IL=@($method.Body.Instructions|ForEach-Object{$_.ToString()})}
   }
   foreach($i in $method.Body.Instructions){
    if(($i.OpCode.Code.ToString() -eq 'Ldstr' -and [string]$i.Operand -match 'mwEditor|mwComponent|mwColors|mwMulti|StarQ|AssetIcon|[Cc]trl|[Ss]hortcut|[Ss]ave|[Uu]ndo|[Bb]inding') -or ($i.Operand -is [Colossal.Mono.Cecil.MethodReference] -and $i.Operand.DeclaringType.FullName -match 'Binding|InputAction|ProxyAction|EditorAsset|SaveAsset|EditorPanel|Shortcut')){
     $contracts+=@{Method=$method.FullName;Instruction=$i.ToString()}
    }
   }
  }}
  @{Assembly=$assembly.Name.FullName;Details=$details;Contracts=$contracts}
 }finally{$assembly.Dispose()}
}
$result|ConvertTo-Json -Depth 10|Set-Content 'docs/evidence/editor-compatibility-0.4.223/selected-details.json'
$result|ForEach-Object{[pscustomobject]@{Assembly=$_.Assembly;Methods=$_.Details.Count;ContractInstructions=$_.Contracts.Count}}

