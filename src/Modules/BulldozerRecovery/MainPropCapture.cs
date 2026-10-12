using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using MW.EditorTools.History;
using Unity.Collections;
using Unity.Entities;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private sealed class MainPropImage {
            internal PrefabBase Prefab;
            internal Entity Original;
            internal MainPropData Data;
            internal bool DetachedValid=>Prefab!=null&&Data!=null&&Data.Matches(Data.Clone());
        }
        private MainPropImage mainImage;
        private static readonly HashSet<string> MainKnownTypes=new HashSet<string>{
            "Game.Prefabs.PrefabRef","Game.Rendering.CullingInfo","Game.Objects.Surface","Game.Common.PseudoRandomSeed","Game.Objects.Transform",
            "Game.Rendering.MeshColor","Game.Rendering.MeshBatch","Game.Net.SubNet","Game.Objects.SubObject","Game.Areas.SubArea","Game.Rendering.CustomMeshColor",
            "Game.Tools.Hidden","Game.Objects.Object","Game.Objects.ObjectGeometry","Game.Objects.Static","Unity.Entities.Simulate"
        };
        private MainPropImage CaptureMainProp(Entity entity,bool log=true) {
            var em=world.EntityManager;
            if(!em.Exists(entity)||em.HasComponent<Game.Common.Owner>(entity)||em.HasComponent<Game.Common.Deleted>(entity)||em.HasComponent<Game.Tools.Temp>(entity)||
                !em.HasComponent<Game.Objects.Static>(entity)||!em.HasComponent<Game.Objects.Transform>(entity)||!em.HasComponent<Game.Common.PseudoRandomSeed>(entity)||!em.HasComponent<Game.Objects.Surface>(entity)||!em.HasComponent<PrefabRef>(entity))throw new NotSupportedException("Not a simple standalone static prop.");
            using(var types=em.GetComponentTypes(entity,Allocator.Temp))foreach(var type in types)if(!MainKnownTypes.Contains(type.GetManagedType()?.FullName??"unknown"))throw new NotSupportedException("Unhandled component: "+type.GetManagedType()?.FullName);
            if((em.HasBuffer<Game.Objects.SubObject>(entity)&&em.GetBuffer<Game.Objects.SubObject>(entity,true).Length!=0)||
                (em.HasBuffer<Game.Net.SubNet>(entity)&&em.GetBuffer<Game.Net.SubNet>(entity,true).Length!=0)||
                (em.HasBuffer<Game.Areas.SubArea>(entity)&&em.GetBuffer<Game.Areas.SubArea>(entity,true).Length!=0))throw new NotSupportedException("Prop has related objects, networks or areas.");
            var prefabs=world.GetExistingSystemManaged<PrefabSystem>();var reference=em.GetComponentData<PrefabRef>(entity);
            if(!em.HasComponent<ObjectData>(reference.m_Prefab))throw new NotSupportedException("Missing native object archetype.");
            var colours=default(NativeArray<Game.Rendering.MeshColor>);
            var custom=default(NativeArray<Game.Rendering.CustomMeshColor>);
            try {
                if(em.HasBuffer<Game.Rendering.MeshColor>(entity)){if(em.GetBuffer<Game.Rendering.MeshColor>(entity,true).Length>256)throw new NotSupportedException("Colour buffer exceeds bounded test size.");colours=em.GetBuffer<Game.Rendering.MeshColor>(entity,true).ToNativeArray(Allocator.Temp);}
                if(em.HasBuffer<Game.Rendering.CustomMeshColor>(entity)){if(em.GetBuffer<Game.Rendering.CustomMeshColor>(entity,true).Length>256)throw new NotSupportedException("Colour buffer exceeds bounded test size.");custom=em.GetBuffer<Game.Rendering.CustomMeshColor>(entity,true).ToNativeArray(Allocator.Temp);}
                if(colours.Length>256||custom.Length>256)throw new NotSupportedException("Colour buffer exceeds bounded test size.");
                var image=new MainPropImage{Original=entity,Prefab=prefabs.GetPrefab<PrefabBase>(reference),
                    Data=new MainPropData(em.GetComponentData<Game.Objects.Transform>(entity),em.GetComponentData<Game.Common.PseudoRandomSeed>(entity),em.GetComponentData<Game.Objects.Surface>(entity),colours.IsCreated?colours.ToArray():null,custom.IsCreated?custom.ToArray():null)};
                if(!image.DetachedValid)throw new NotSupportedException("Detached snapshot round trip failed.");
                if(log)Mod.Log.Info("Main prop capture round trip verified; prefab="+image.Prefab.name+" seed="+em.GetComponentData<Game.Common.PseudoRandomSeed>(entity).m_Seed+" meshColours="+colours.Length+" customColours="+custom.Length+"; derived mesh batches and preview Hidden are not copied.");
                return image;
            }finally{if(colours.IsCreated)colours.Dispose();if(custom.IsCreated)custom.Dispose();}
        }
    }
}
