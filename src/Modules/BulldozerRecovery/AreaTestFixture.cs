using System;
using System.Linq;
using Game.Prefabs;
using Game.Tools;
using Game.UI.Editor;
using UnityEngine;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        internal void CreateAreaFixture(){
            if(!CanArm||nestedFixturePending||areaFixture){Status="Select the saved mixed test asset first.";return;}
            var source=context.Asset as ObjectPrefab;
            if(source==null||(source.GetComponent<ObjectSubAreas>()?.m_SubAreas?.Length??0)<1){Status="Select an asset containing a surface or walking area first.";return;}
            try {
                var prefabs=world.GetExistingSystemManaged<PrefabSystem>();var hierarchy=world.GetExistingSystemManaged<EditorHierarchyUISystem>();
                if(prefabs==null||hierarchy==null)throw new NotSupportedException("Native fixture services unavailable.");
                var before=JsonUtility.ToJson(source);
                var clone=source.Clone("MW Area Undo Test "+Guid.NewGuid().ToString("N").Substring(0,8)) as ObjectPrefab;
                if(clone==null||clone.isReadOnly||clone.components.Any(c=>source.components.Any(original=>ReferenceEquals(c,original))))throw new NotSupportedException("Fixture clone is not independent.");
                if(clone.GetComponent<ObjectSubObjects>()!=null)ApplyPrefabsSystem.RemoveComponent<ObjectSubObjects>(clone);
                if(clone.GetComponent<ObjectSubNets>()!=null)ApplyPrefabsSystem.RemoveComponent<ObjectSubNets>(clone);
                if(clone.GetComponent<ObjectSubLanes>()!=null)ApplyPrefabsSystem.RemoveComponent<ObjectSubLanes>(clone);
                if(clone.GetComponent<EffectSource>()!=null)ApplyPrefabsSystem.RemoveComponent<EffectSource>(clone);
                if(clone.GetComponent<ActivityLocation>()!=null)ApplyPrefabsSystem.RemoveComponent<ActivityLocation>(clone);
                if(before!=JsonUtility.ToJson(source))throw new InvalidOperationException("Source changed while cloning; stop testing.");
                if(!prefabs.AddPrefab(clone))throw new InvalidOperationException("Fixture registration refused.");
                hierarchy.AddPrefab(clone);Invalidate();nestedFixture=clone;nestedFixturePending=true;nestedFixtureFrame=Time.frameCount;areaFixture=true;
                Status="Area test copy created; waiting for native initialization. Original unchanged; nothing saved.";Mod.Log.Info(Status);
            }catch(Exception error){Status="Area fixture could not be created: "+error.Message;Mod.Log.Warn(Status);}
        }
        private bool areaFixture;
    }
}
