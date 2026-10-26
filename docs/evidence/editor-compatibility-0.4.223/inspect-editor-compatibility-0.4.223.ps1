$ErrorActionPreference='Stop'
$managed='C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed'
Add-Type -LiteralPath (Join-Path $managed 'Colossal.Mono.Cecil.dll')
$cacheRoot=Join-Path $env:USERPROFILE 'AppData\LocalLow\Colossal Order\Cities Skylines II\.cache\Mods'
$selected=@('Anarchy.dll')
$assemblies=@(Get-ChildItem -LiteralPath $cacheRoot -File -Recurse -Filter '*.dll' | Where-Object {$_.Name -in $selected})
function Read-Types($items){foreach($item in $items){$item;Read-Types $item.NestedTypes}}
function Format-Argument($arg){if($arg.Value -is [Colossal.Mono.Cecil.CustomAttributeArgument[]]){return '['+(($arg.Value|ForEach-Object{Format-Argument $_}) -join ',')+']'};return [string]$arg.Value}
$records=foreach($file in $assemblies){
 $resolver=[Colossal.Mono.Cecil.DefaultAssemblyResolver]::new()
 $resolver.AddSearchDirectory($managed)
 $resolver.AddSearchDirectory($file.DirectoryName)
 $resolver.AddSearchDirectory((Join-Path $env:USERPROFILE 'AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\MWEditorTools'))
 $parameters=[Colossal.Mono.Cecil.ReaderParameters]::new();$parameters.AssemblyResolver=$resolver
 $assembly=[Colossal.Mono.Cecil.AssemblyDefinition]::ReadAssembly($file.FullName,$parameters)
 try{
  $patches=@();$calls=@();$modes=@()
  foreach($type in @(Read-Types $assembly.MainModule.Types)){
   foreach($attribute in $type.CustomAttributes){if($attribute.AttributeType.FullName -like 'HarmonyLib.*'){$patches+=@{Owner=$type.FullName;Attribute=$attribute.AttributeType.FullName;Arguments=@($attribute.ConstructorArguments|ForEach-Object{Format-Argument $_})}}}
   foreach($method in $type.Methods){
    foreach($attribute in $method.CustomAttributes){if($attribute.AttributeType.FullName -like 'HarmonyLib.*'){$patches+=@{Owner=$method.FullName;Attribute=$attribute.AttributeType.FullName;Arguments=@($attribute.ConstructorArguments|ForEach-Object{Format-Argument $_})}}}
    if(!$method.HasBody){continue}
    if($method.Name -eq 'get_gameMode'){$modes+=@{Method=$method.FullName;IL=@($method.Body.Instructions|ForEach-Object{$_.ToString()})}}
    foreach($instruction in $method.Body.Instructions){
     $operand=$instruction.Operand
     if(($operand -is [Colossal.Mono.Cecil.MethodReference]) -and (($operand.DeclaringType.FullName -like 'HarmonyLib.*' -and $operand.Name -match 'Patch|Unpatch') -or ($operand.DeclaringType.FullName -match '^Game\.(Tools\.(ObjectToolSystem|AreaToolSystem|BulldozeToolSystem|ToolBaseSystem)|Prefabs\.(ApplyPrefabsSystem|PrefabSystem)|Rendering\.PreCullingSystem|UI\.Editor\.InspectorPanelSystem)$'))){
      $calls+=@{Caller=$method.FullName;Offset=('IL_{0:x4}' -f $instruction.Offset);Target=$operand.FullName}
     }
    }
   }
  }
  [pscustomobject]@{File=$file.FullName;Identity=$assembly.Name.FullName;MVID=$assembly.MainModule.Mvid.ToString();SHA256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash;HarmonyReferences=@($assembly.MainModule.AssemblyReferences|Where-Object Name -Match 'Harmony'|ForEach-Object{$_.FullName});Patches=$patches;RelevantCalls=$calls;GameModeGetters=$modes}
 }finally{$assembly.Dispose();$resolver.Dispose()}
}
$destination='C:\Users\ian\Documents\Development\MW Editor Tools\docs\evidence\editor-compatibility-0.4.223\assembly-overlap.json'
$records | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $destination -Encoding UTF8
$records | ForEach-Object{[pscustomobject]@{Assembly=$_.Identity;Patches=$_.Patches.Count;RelevantCalls=$_.RelevantCalls.Count;GameModes=$_.GameModeGetters.Count}}



