using System;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Game.UI.Editor;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private ObjectPrefab nestedFixture;
        private bool nestedFixturePending;
        private int nestedFixtureFrame;
        internal void CreateNestedFixture(){
            if(!CanArm||nestedFixture!=null){Status="Select an asset containing Grill01; create one fixture per session.";return;}
            var source=context.Asset.GetComponent<ObjectSubObjects>()?.m_SubObjects?.FirstOrDefault(value=>value!=null&&value.m_Object!=null&&value.m_Object.name=="Grill01")?.m_Object;
            if(source==null){Status="Select a disposable asset with Grill01 in Object Sub Objects first.";return;}
            if(source.GetComponent<ObjectSubObjects>()?.m_SubObjects?.Length>0){Status="Fixture source must be a simple Grill01 without children.";return;}
            Invalidate();
            try{
                var prefabs=world.GetExistingSystemManaged<PrefabSystem>();
                var hierarchy=world.GetExistingSystemManaged<EditorHierarchyUISystem>();
                if(prefabs==null||hierarchy==null)throw new NotSupportedException("Native fixture services unavailable.");
                string sourceBefore=JsonUtility.ToJson(source);
                string suffix=Guid.NewGuid().ToString("N").Substring(0,8);
                var parent=(ObjectPrefab)source.Clone("MW Nested Parent "+suffix);
                var root=(ObjectPrefab)source.Clone("MW Nested Fixture "+suffix);
                if(parent.isReadOnly||root.isReadOnly)throw new NotSupportedException("Native clones are read-only.");
                (parent.GetComponent<ObjectSubObjects>()??parent.AddComponent<ObjectSubObjects>()).m_SubObjects=new[]{NestedLink(source)};
                (root.GetComponent<ObjectSubObjects>()??root.AddComponent<ObjectSubObjects>()).m_SubObjects=new[]{NestedLink(parent)};
                if(JsonUtility.ToJson(source)!=sourceBefore)throw new InvalidOperationException("Source changed during native clone; stop testing.");
                if(!prefabs.AddPrefab(parent))throw new InvalidOperationException("Parent fixture registration refused.");
                if(!prefabs.AddPrefab(root))throw new InvalidOperationException("Root fixture registration refused.");
                hierarchy.AddPrefab(parent);hierarchy.AddPrefab(root);
                areaFixture=false;nestedFixture=root;nestedFixturePending=true;nestedFixtureFrame=Time.frameCount;
                Status="Nested fixture created; waiting for native prefab initialization…";
                Mod.Log.Info(Status+" root="+root.name+" parent="+parent.name+" leaf="+source.name+" source unchanged; no asset saved.");
            }catch(Exception error){nestedFixturePending=false;Status="Nested fixture creation failed; restart CS2 without saving the disposable test.";Mod.Log.Error(error,Status);}
        }
        private static ObjectSubObjectInfo NestedLink(ObjectPrefab child)=>new ObjectSubObjectInfo{m_Object=child,m_Position=new float3(3,0,0),m_Rotation=quaternion.identity,m_ParentMesh=-1,m_GroupIndex=0,m_Probability=100};
        private void TickNestedFixture(){
            if(!nestedFixturePending)return;
            if(!context.EditorAvailable){nestedFixturePending=false;return;}
            var prefab=world.GetExistingSystemManaged<PrefabSystem>().GetEntity(nestedFixture);
            if(Time.frameCount-nestedFixtureFrame>=2&&world.EntityManager.Exists(prefab)&&world.EntityManager.HasComponent<ObjectData>(prefab)){
                nestedFixturePending=false;
                if(!world.GetExistingSystemManaged<ToolSystem>().ActivatePrefabTool(nestedFixture)){Status="Fixture created in workspace Prefabs; native placement refused.";Mod.Log.Warn(Status);return;}
                Status=areaFixture?"Place the area test copy once on clear ground, press Escape and select its outer asset. Everything-delete one surface or walking area, then Undo, Redo, Undo. Do not save test edits.":"Place the nested fixture once on clear ground, then press Escape. Keep another bound prop, then use Arm nested group test for the temporary Everything-mode experiment.";Mod.Log.Info(Status);
            }else if(Time.frameCount-nestedFixtureFrame>120){nestedFixturePending=false;Status="Nested fixture native initialization timed out; restart without saving.";Mod.Log.Warn(Status);}
        }
    }
}

