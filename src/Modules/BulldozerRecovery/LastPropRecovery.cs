using System;
using System.Linq;
using Game.Prefabs;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private bool lastArmed;
        internal void ArmLast(){if(!CanArm)return;Arm();lastArmed=true;Status="Armed last bound prop test on "+armedAsset.name+". Exactly one Object Sub Objects entry required.";Mod.Log.Info(Status);}
        private static bool LastRemoval(Payload before,Payload after,PrefabBase asset){
            if(before.Objects!=1||after.Objects!=0||!after.Shape(asset))return false;
            var slots=before.Components.Select((value,index)=>new{value,index}).Where(pair=>pair.value is ObjectSubObjects).ToArray();
            if(slots.Length!=1||slots[0].value.GetType()!=typeof(ObjectSubObjects)||after.Components.Any(component=>component is ObjectSubObjects))return false;
            var retained=before.Components.Select((value,index)=>new{value,index}).Where(pair=>pair.index!=slots[0].index).ToArray();
            return retained.Length==after.Components.Length&&retained.Select((pair,index)=>ReferenceEquals(pair.value,after.Components[index])&&before.Active[pair.index]==after.Active[index]).All(value=>value)
                &&before.Values.All(pair=>pair.Value.ReferencesValid&&pair.Value.Matches(pair.Value.Materialize()));
        }
        private static bool LastTargetShape(Payload target,PrefabBase asset){
            var actual=asset.components.Where(component=>!(component is ObjectSubObjects)).ToArray();
            var expected=target.Components.Where(component=>!(component is ObjectSubObjects)).ToArray();
            return actual.Length==expected.Length&&actual.Zip(expected,(a,b)=>a!=null&&ReferenceEquals(a,b)).All(value=>value)
                &&target.Components.Count(component=>component is ObjectSubObjects)<=1&&asset.components.Count(component=>component is ObjectSubObjects)<=1;
        }
        private void AlignLast(Payload target,PrefabBase asset){
            if(!LastTargetShape(target,asset))throw new InvalidOperationException("Last-prop component context changed.");
            int slot=Array.FindIndex(target.Components,component=>component is ObjectSubObjects);
            var current=asset.components.OfType<ObjectSubObjects>().SingleOrDefault();
            if(slot<0){if(current!=null){Mod.Log.Info("Last-prop Redo native component removal; instance="+current.GetInstanceID());Game.Tools.ApplyPrefabsSystem.RemoveComponent<ObjectSubObjects>(asset);}}
            else if(current==null){
                var previous=target.Components[slot];
                var created=asset.AddComponent<ObjectSubObjects>();
                asset.components.Remove(created);asset.components.Insert(slot,created);
                // Native last-object removal replaces the component identity. Retarget
                // every retained image of that exact component, including redo entries.
                foreach(var payload in records.All.Where(entry=>ReferenceEquals(entry.Asset,asset)).SelectMany(entry=>new[]{entry.Before,entry.After}).Concat(new[]{target}).Distinct()){
                    for(int i=0;i<payload.Components.Length;i++)if(ReferenceEquals(payload.Components[i],previous))payload.Components[i]=created;
                    foreach(var field in payload.Fields.Where(field=>ReferenceEquals(field.Owner,previous)))field.Owner=created;
                }
                Mod.Log.Info("Last-prop Undo fresh component created; instance="+created.GetInstanceID()+" slot="+slot);
            }else if(!ReferenceEquals(current,target.Components[slot]))throw new InvalidOperationException("Unexpected last-prop component identity.");
            if(!target.Shape(asset))throw new InvalidOperationException("Last-prop component shape did not validate.");
        }
    }
}


