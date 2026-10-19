using System;
using System.Linq;
using Unity.Entities;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private bool mainNestedValuesRestored;
        private int mainNestedStableFrames;
        private bool MainSourcesGone(MainPropImage image)=>!world.EntityManager.Exists(image.Original)&&
            (image.Group==null||GroupSources(image.Group).All(id=>!world.EntityManager.Exists(id)));
        private bool MainNestedMatches(MainPropImage expected,MainPropImage actual) {
            if(expected.Group==null)return actual.Group==null;
            return actual.Group!=null&&ExactNestedGroup(expected.Group,actual.Group)&&
                !GroupSources(expected.Group).Intersect(GroupSources(actual.Group)).Any()&&expected.DetachedValid;
        }
        // Let native object initialization create descendants from the unchanged
        // prefab. Only restore captured values after the complete tree matches.
        private bool RestoreMainNestedValues() {
            int previousStableFrames=mainNestedStableFrames;mainNestedStableFrames=0;
            if(mainRecord.Group==null)return true;
            if(!mainRecord.DetachedValid)return false;
            var actual=CaptureNestedImage(mainLive,false,true);var expected=mainRecord.Group;
            if(actual.Length!=expected.Length||GroupSources(actual).Intersect(GroupSources(expected)).Any())return false;
            if(mainNestedValuesRestored){if(!ExactNestedGroup(expected,actual))return false;mainNestedStableFrames=previousStableFrames+1;return mainNestedStableFrames>=3;}
            for(int i=0;i<expected.Length;i++) {
                var e=expected[i];var a=actual[i];
                var values=new MainPropData(e.Data.Transform,e.Data.Seed,a.Data.Surface,e.Data.Colours,a.Data.CustomColours);
                if(e.Parent!=a.Parent||e.Depth!=a.Depth||!ReferenceEquals(e.Prefab,a.Prefab)||e.HasSurface!=a.HasSurface||
                    !SameNestedElevation(e,a,false)||!e.Local.Equals(a.Local)||!SameNestedPlacement(e,a)||!e.Data.Matches(values)||
                    (e.Data.Colours==null)!=(a.Data.Colours==null)||(e.Data.Colours?.Length??0)!=(a.Data.Colours?.Length??0))return false;
            }
            for(int i=0;i<expected.Length;i++) {
                var e=expected[i];var id=actual[i].Original;
                world.EntityManager.SetComponentData(id,e.Data.Seed);world.EntityManager.SetComponentData(id,e.Data.Transform);
                if(e.HasColor)world.EntityManager.SetComponentData(id,e.Color);if(e.HasDecoration)world.EntityManager.SetComponentEnabled<Game.Objects.Decoration>(id,e.DecorationEnabled);
                if(e.HasPlant)world.EntityManager.SetComponentData(id,e.Plant);if(e.HasTree)world.EntityManager.SetComponentData(id,e.Tree);
                var colours=e.Data.Colours;if(colours!=null){var buffer=world.EntityManager.GetBuffer<Game.Rendering.MeshColor>(id);for(int c=0;c<colours.Length;c++)buffer[c]=colours[c];}
            }
            mainNestedValuesRestored=true;mainNestedStableFrames=0;
            Mod.Log.Info("Standalone nested native tree matched; typed values restored; waiting for three exact validation frames.");
            return false;
        }
    }
}

