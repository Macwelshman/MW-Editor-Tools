using System;
using System.Collections.Generic;
using System.Linq;
using MW.EditorTools.Modules.BulldozerRecovery;
using Unity.Entities;
internal static class BoundCompletionChecks {
    internal static int Run(){
        int count=0;void Check(bool value,string label){if(!value)throw new Exception(label);count++;}
        Entity Id(int i)=>new Entity{Index=i,Version=1};
        var map=new Dictionary<Entity,Entity>();
        Check(EntityReferenceMap.Add(map,Id(1),Id(101)),"new verified mapping");Check(EntityReferenceMap.Add(map,Id(1),Id(101)),"same verified mapping reusable");
        Check(!EntityReferenceMap.Add(map,Id(1),Id(102))&&!EntityReferenceMap.Add(map,Id(2),Id(101)),"conflicting and noninjective mappings refused");
        Check(!EntityReferenceMap.Add(map,Entity.Null,Id(2)),"null mapping refused");Check(EntityReferenceMap.Resolve(Id(7),map)==Id(7),"unmapped external identity preserved");
        var gate=new BoundCompletionGate();Check(!gate.Observe(10,map)&&!gate.Observe(10,map),"callbacks within one frame do not count twice");
        Check(!gate.Observe(11,map)&&gate.Observe(12,map),"three distinct stable frames required");
        var churn=new Dictionary<Entity,Entity>{{Id(1),Id(102)}};Check(!gate.Observe(13,churn)&&!gate.Observe(14,churn)&&gate.Observe(15,churn),"entity churn resets stability");
        gate.Reset();Check(!gate.Observe(16,churn),"failed preflight invalidates stability");
        gate.Reset();var scope=EntityReferenceMap.WithOwnerPrefab(map,Id(50),Id(60),Id(61));
        Check(!gate.Observe(20,scope)&&!gate.Observe(21,scope)&&gate.Observe(22,scope),"owner scope stable across three frames");
        var changedScope=EntityReferenceMap.WithOwnerPrefab(map,Id(50),Id(60),Id(62));Check(!gate.Observe(23,changedScope),"owner prefab transition churn resets stable gate");
        Check(!gate.Observe(24,map),"removing owner scope resets stable gate");
        int[] live={1,2,3},items={0,1,2};int writes=0;
        Check(!RetainedValueTransaction.Apply(items,i=>i!=2,i=>{writes++;live[i]=99;},i=>live[i]=i+1)&&writes==0&&live.SequenceEqual(new[]{1,2,3}),"all preflights finish before any write");
        bool failed=false;int rollback=0;
        try{RetainedValueTransaction.Apply(items,i=>true,i=>{live[i]=99;if(i==1)throw new InvalidOperationException("injected partial write");},i=>{rollback++;live[i]=i+1;});}catch(InvalidOperationException){failed=true;}
        Check(failed&&rollback==2&&live.SequenceEqual(new[]{1,2,3}),"partial write rolls back failing item and all preceding items");
        Check(RetainedValueTransaction.Apply(items,i=>true,i=>live[i]=10+i,i=>live[i]=i+1)&&live.SequenceEqual(new[]{10,11,12}),"successful typed value transaction");
        bool aggregate=false;var attempted=new List<int>();
        try{RetainedValueTransaction.Apply(items,i=>true,i=>{if(i==2)throw new InvalidOperationException();},i=>{attempted.Add(i);if(i==1)throw new Exception("rollback failure");});}catch(AggregateException e){aggregate=e.InnerExceptions.Count==2;}
        Check(aggregate&&attempted.SequenceEqual(new[]{2,1,0}),"rollback attempts every earlier item and reports failure");
        return count;
    }
}
