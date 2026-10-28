param([string]$ManagedPath='C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed')
$ErrorActionPreference='Stop'
Add-Type -LiteralPath (Join-Path $ManagedPath 'Colossal.Mono.Cecil.dll')
$a=[Colossal.Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $ManagedPath 'Game.dll'))
$db=[Colossal.Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $ManagedPath 'Colossal.IO.AssetDatabase.dll'))
$evidence=Join-Path $PSScriptRoot '../docs/evidence/environment-integration-0.4.256/environment-api.txt'
$details=[System.Collections.Generic.List[string]]::new()
try {
 if($a.MainModule.Mvid.ToString() -notin @('e15551e9-3dea-4855-8dde-1da3cea7d7ef','e8325128-556a-460d-a950-497d90b3627c')){throw 'Unknown environment Game build'}
 $details.Add('ASSEMBLY '+$a.Name.FullName+' MVID='+$a.MainModule.Mvid)
 $snow=$a.MainModule.Types|Where-Object FullName -eq 'Game.Simulation.SnowSystem'
 $field=$snow.Fields|Where-Object Name -eq 'm_SnowHeights'
 if(!$field.IsPrivate -or $field.IsStatic -or $field.FieldType.FullName -ne 'UnityEngine.RenderTexture[]'){throw 'Snow buffer contract changed'}
 $details.Add('FIELD '+$field.FullName)
 $pair=$false
 foreach($m in $snow.Methods|Where-Object HasBody){
  foreach($i in $m.Body.Instructions){
   if($i.OpCode.Code.ToString() -eq 'Newarr' -and $i.Operand.FullName -eq 'UnityEngine.RenderTexture' -and $i.Previous.OpCode.Code.ToString() -eq 'Ldc_I4_2'){$pair=$true;$details.Add('METHOD '+$m.FullName);$details.Add($i.Previous.ToString());$details.Add($i.ToString())}
  }
 }
 if(!$pair){throw 'Native paired snow buffer allocation changed'}
 foreach($m in $snow.Methods|Where-Object Name -in @('get_Loaded','get_SnowDepth')){
  $details.Add('METHOD '+$m.FullName);foreach($i in $m.Body.Instructions){$details.Add($i.ToString())}
 }
 $depth=$snow.Methods|Where-Object Name -eq 'get_SnowDepth'
 if((($depth.Body.Instructions|ForEach-Object ToString)-join "`n") -notmatch 'SnowSystem::get_Read'){throw 'Snow read-buffer getter changed'}
 $planet=$a.MainModule.Types|Where-Object FullName -eq 'Game.Simulation.PlanetarySystem'
 foreach($name in @('get_day','set_day','get_time','set_time')){
  $m=@($planet.Methods|Where-Object Name -eq $name);if($m.Count -ne 1){throw "Missing planetary accessor: $name"}
  $details.Add('METHOD '+$m[0].FullName);foreach($i in $m[0].Body.Instructions){$details.Add($i.ToString())}
 }
 $climate=$a.MainModule.Types|Where-Object FullName -eq 'Game.Simulation.ClimateSystem'
 foreach($name in @('currentDate','precipitation','cloudiness','temperature')){
  $prop=$climate.Properties|Where-Object Name -eq $name
  if($prop.PropertyType.FullName -ne 'Game.OverridableProperty`1<System.Single>'){throw "Climate override contract changed: $name"}
  $details.Add('PROPERTY '+$prop.FullName)
 }
 $order=$a.MainModule.Types|Where-Object FullName -eq 'Game.Common.SystemOrder'
 $initialize=$order.Methods|Where-Object Name -eq 'Initialize'
 $calls=@($initialize.Body.Instructions|Where-Object {$_.Operand -is [Colossal.Mono.Cecil.GenericInstanceMethod] -and $_.Operand.FullName -match 'UpdateAt<Game.Simulation.PlanetarySystem>'})
 if($calls.Count -ne 1 -or [int]$calls[0].Previous.Operand -ne 27){throw 'Planetary rendering phase changed'}
 $details.Add('METHOD '+$initialize.FullName);$details.Add($calls[0].Previous.ToString());$details.Add($calls[0].ToString())
 $source=Get-Content (Join-Path $PSScriptRoot '../src/Core/Mod.cs') -Raw
 if(!$source.Contains('EditorEnvironmentSystem,Game.Simulation.PlanetarySystem>(SystemUpdatePhase.PreCulling)')){throw 'Environment system must precede native planetary rendering'}
 $load=$db.MainModule.Types|Where-Object FullName -eq 'Colossal.IO.AssetDatabase.AssetDatabase'|ForEach-Object Methods|Where-Object {$_.Name -eq 'LoadSettings' -and $_.Parameters.Count -eq 4}
 $il=($load.Body.Instructions|ForEach-Object ToString)-join "`n"
 foreach($token in @('TryGetNamedAsset','SettingAsset/Fragment::source','JSON::WriteInto<System.Object>')){if(!$il.Contains($token)){throw "Settings migration API changed: $token"}}
 $details.Add('ASSEMBLY '+$db.Name.FullName+' MVID='+$db.MainModule.Mvid)
 $details.Add('METHOD '+$load.FullName);foreach($i in $load.Body.Instructions){$details.Add($i.ToString())}
 $details|Set-Content $evidence
 'PASS: installed environment APIs, private paired snow buffers, climate override types, PreCulling order and named JSON settings population. VERIFIED-STATIC; native appearance/persistence pending.'
}finally{$a.Dispose();$db.Dispose()}
