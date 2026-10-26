using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Colossal.Mono.Cecil;
using Colossal.Mono.Cecil.Cil;

internal static class NativeCollectionChecks {
    private static IEnumerable<TypeDefinition> Types(TypeDefinition type){yield return type;foreach(var nested in type.NestedTypes)foreach(var child in Types(nested))yield return child;}
    internal static int Run() {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../.."));
        var managed="C:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed";
        int checks=0;
        foreach(var pair in new[]{("Unity.Entities.dll","Unity.Entities.DynamicBuffer`1"),("Unity.Collections.dll","Unity.Collections.NativeList`1")}) {
            using var native=AssemblyDefinition.ReadAssembly(Path.Combine(managed,pair.Item1));
            var type=native.MainModule.Types.Single(t=>t.FullName==pair.Item2);
            foreach(var method in type.Methods.Where(m=>m.Name.Contains("IEnumerable")&&m.Name.EndsWith("GetEnumerator"))) {
                if(!method.Body.Instructions.Any(i=>i.Operand is MethodReference call&&call.DeclaringType.FullName=="System.NotImplementedException"))throw new Exception("Native collection behavior changed; re-inspect "+method);
                checks++;
            }
        }
        if(checks!=4)throw new Exception("Expected both generic and non-generic native enumeration guards.");
        using var mod=AssemblyDefinition.ReadAssembly(Path.Combine(root,"bin/Release/net48/MW.EditorTools.dll"));
        // Guard the native serialization contract used by exact companion projection,
        // and verify the production move capture reaches the complete anchor context.
        using var game=AssemblyDefinition.ReadAssembly(Path.Combine(managed,"Game.dll"));
        var apply=game.MainModule.GetType("Game.Tools.ApplyPrefabsSystem");
        var cached=apply.Methods.Single(m=>m.Name=="CheckCachedValue"&&m.Parameters[0].ParameterType.FullName=="Unity.Mathematics.float3&");
        if(!cached.Body.Instructions.Any(i=>i.OpCode.Code==Code.Ldc_R4&&i.Operand is float threshold&&threshold==0.01f)||!cached.Body.Instructions.Any(i=>i.OpCode.Code==Code.Bge_Un_S))throw new Exception("Native cached-position threshold changed; re-inspect before using companion projection.");
        checks++;
        var trace=mod.MainModule.GetType("MW.EditorTools.Modules.BulldozerRecovery.BulldozerTrace");
        var capture=trace.Methods.Single(m=>m.Name=="CaptureMoveOwner");
        var calls=capture.Body.Instructions.Where(i=>i.Operand is MethodReference).Select(i=>(MethodReference)i.Operand).ToArray();
        if(!calls.Any(m=>m.Name=="CaptureBoundCompanions")||calls.Any(m=>m.Name=="CaptureAreaNetwork"))throw new Exception("Move capture must use the full owned companion/anchor context rather than a separate anchorless network capture.");
        checks++;
        int safeCopies=0;
        foreach(var method in mod.MainModule.Types.SelectMany(Types).SelectMany(t=>t.Methods).Where(m=>m.HasBody))foreach(var instruction in method.Body.Instructions) {
            if(instruction.OpCode.Code==Code.Box&&instruction.Operand is TypeReference boxed&&
                (boxed.FullName.StartsWith("Unity.Entities.DynamicBuffer`1")||boxed.FullName.StartsWith("Unity.Collections.NativeList`1"))) {
                for(var next=instruction.Next;next!=null;next=next.Next) {
                    if(next.Operand is MethodReference call) {
                        if(call.DeclaringType.FullName=="System.Linq.Enumerable")throw new Exception("Native IEnumerable regression: "+method.FullName+" at "+instruction);
                        break;
                    }
                }
            }
            if(instruction.Operand is MethodReference copy&&copy.DeclaringType.FullName.StartsWith("Unity.Collections.NativeArray`1")&&copy.Name=="ToArray")safeCopies++;
        }
        if(safeCopies<8)throw new Exception("Production assembly does not contain the required native array copy paths. Build the mod before testing.");
        return checks+2;
    }
}
