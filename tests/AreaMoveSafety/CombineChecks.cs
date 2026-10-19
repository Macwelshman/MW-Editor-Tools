using System;
using System.Linq;
using System.Collections.Generic;
using Game.Tools;
using Unity.Entities;
using Unity.Mathematics;
namespace MW.EditorTools.Modules.BulldozerRecovery {
sealed partial class BulldozerTrace {
    private readonly MW.EditorTools.History.LastDeletion<AreaMoveEntry> areaMoveHistory=new();
    private class NestedNode{internal Entity Original;internal int Value;}
    private static class AreaCompanionEffect{internal static bool Matches(object[] a,object[] b)=>a.SequenceEqual(b);}
    private bool combineReferences=true;
    private bool NestedReferenceMatches(object key,Payload value)=>combineReferences;
    private static NestedNode[][] MatchForest(NestedNode[][] a,NestedNode[][] b)=>a.Length==b.Length?b:null;
    private static bool ExactNestedGroup(NestedNode[] a,NestedNode[] b)=>a.Length==b.Length&&a.Select((n,i)=>n.Value==b[i].Value).All(v=>v);
    private static IEnumerable<Entity> GroupSources(NestedNode[] group)=>group.Select(n=>n.Original);
    private static void CombineChecks(ref int count) {
        int n=0;void Check(bool value,string name){if(!value)throw new Exception("Node combination: "+name);n++;}
        var nodes=new[]{new Game.Areas.Node{m_Position=new(0,0,0)},new Game.Areas.Node{m_Position=new(4,0,0)},new Game.Areas.Node{m_Position=new(0,0,4)}};
        var added=nodes.Concat(new[]{new Game.Areas.Node{m_Position=new(2,0,2)}}).ToArray();
        StandaloneAreaEntry Image(Game.Areas.Node[] v)=>new(){Data=new(Game.Areas.AreaFlags.Complete,v,v.Select(p=>new LocalNodeCache{m_Position=p.m_Position,m_ParentMesh=-1}).ToArray()),Network=new()};
        (BulldozerTrace,AreaMoveEntry,AreaMoveEntry) Ready(bool bound=false){
            var t=new BulldozerTrace();var moved=added.ToArray();moved[3].m_Position.x=3;
            var prior=new AreaMoveEntry{Target=new(){Id=1},Live=new(){Id=1},Before=Image(nodes),After=Image(added),AddedNode=3};
            var next=new AreaMoveEntry{Target=prior.Live,Live=prior.Live,Before=Image(added),After=Image(moved)};
            if(bound){var source=new Payload();var transform=new Transform();var asset=new object();
                prior.Owner=next.Owner=new(){Id=2};prior.BoundBefore=new(){Before=source,Asset=asset,Transform=transform};prior.BoundAfter=new(){Before=source,Asset=asset,Transform=transform};
                next.BoundBefore=new(){Before=source,Asset=asset,Transform=transform};next.BoundAfter=new(){Before=new(),Asset=asset,Transform=transform};}
            t.areaMoveHistory.Record(prior);return(t,prior,next);
        }
        foreach(bool bound in new[]{false,true}){
            var (t,prior,next)=Ready(bound);var merged=t.CombineAddedNodeMove(next);
            Check(!ReferenceEquals(merged,next)&&ReferenceEquals(merged.Before,prior.Before)&&ReferenceEquals(merged.After,next.After),"addition and same-node move share original before and final after images");
            Check(merged.AddedNode==3&&merged.Before.Data.Nodes.Length==3&&merged.After.Data.Nodes.Length==4,"combined Undo removes the originally added node");
            Check(t.areaMoveHistory.Latest==prior,"preparation never mutates previous slot");
            t.areaMoveHistory.Record(merged);next.Before=merged.After;var again=next.After.Data.Nodes;again[3].m_Position.z+=1;next.After=Image(again);
            var repeated=t.CombineAddedNodeMove(next);Check(ReferenceEquals(repeated.Before,prior.Before)&&repeated.AddedNode==3,"successive moves remain restricted to the same added node");
            Check(t.areaMoveHistory.UndoCount==1&&t.areaMoveHistory.RedoCount==0,"combination retains one-action slot");
        }
        foreach(var gate in new[]{"other node","multiple nodes","different target","stale before","no-op","no addition","owner source","references","props","prop identity","areas","network"}){
            var (t,prior,next)=Ready(true);
            switch(gate){
                case "other node":var v=added.ToArray();v[1].m_Position.x=9;next.After=Image(v);break;
                case "multiple nodes":v=next.After.Data.Nodes;v[1].m_Position.x=9;next.After=Image(v);break;
                case "different target":next.Target=new(){Id=99};break;
                case "stale before":v=next.Before.Data.Nodes;v[0].m_Position.x=9;next.Before=Image(v);break;
                case "no-op":next.After=next.Before;break;
                case "no addition":prior.AddedNode=-1;break;
                case "owner source":next.BoundBefore.Before=new();next.BoundBefore.Before.Values["changed"]=new(){Value=1};break;
                case "references":prior.BoundBefore.References[new object()]=new();t.combineReferences=false;break;
                case "props":prior.BoundAfter.Props=new[]{new[]{new NestedNode{Original=new(){Id=3},Value=1}}};next.BoundBefore.Props=new[]{new[]{new NestedNode{Original=new(){Id=3},Value=2}}};break;
                case "prop identity":prior.BoundAfter.Props=new[]{new[]{new NestedNode{Original=new(){Id=3}}}};next.BoundBefore.Props=new[]{new[]{new NestedNode{Original=new(){Id=4}}}};break;
                case "areas":prior.BoundAfter.Areas=new[]{new AreaImage()};break;
                case "network":next.Before.Network.Valid=false;break;
            }
            Check(ReferenceEquals(t.CombineAddedNodeMove(next),next),gate+" starts independent action instead of combining");
        }
        count+=n;
    }
}}
