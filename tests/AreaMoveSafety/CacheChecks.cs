#pragma warning disable CS0649
using System;
using System.Linq;
using System.Collections.Generic;
using Game.Tools;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;
namespace MW.EditorTools.Modules.BulldozerRecovery {
sealed partial class BulldozerTrace {
    private class Snapshot{internal int Value;internal object Materialize()=>Value;internal bool Matches(object value)=>Equals(Value,value);}
    private class Payload{internal object[] Components=Array.Empty<object>();internal bool[] Active=Array.Empty<bool>();internal Dictionary<string,Snapshot> Values=new();internal bool Valid=true;internal ObjectSubAreaInfo[] Rows;internal bool Matches(object asset)=>Valid;}
    private class Transform{internal float3 m_Position;internal quaternion m_Rotation=quaternion.identity;}
    private class AreaImage {
        internal string Metadata=>"test metadata";
        internal Entity Original;internal AreaPrefab Prefab;internal LocalNodeCache[] Local;internal Game.Areas.Node[] Nodes;
        internal AreaImage WithLocal(LocalNodeCache[] local)=>new(){Original=Original,Prefab=Prefab,Local=local,Nodes=Nodes};
    }
    private class Item{internal Entity Id;internal bool HasLocal;internal int Local;}
    private class Network{internal bool Valid=true;internal Item[] Items=Array.Empty<Item>();internal bool TryMap(Network other,bool exact,out Dictionary<Entity,Entity> map,bool a=false,bool b=false){map=new();return Valid&&other.Valid;}}
    private static class AreaCompanionNetwork{internal static bool CanRestoreLocal(int a,int b)=>a==b;}
    private class NetworkEntry{internal Payload Before;internal object Asset;internal Entity Owner;internal Transform Transform=new();internal Network Network=new(),RestoreFrom=new();internal AreaImage[] Areas=Array.Empty<AreaImage>();internal AreaMoveEntry AreaMoveEntry;internal NestedNode[][] Props=Array.Empty<NestedNode[]>();internal object[] Effects=Array.Empty<object>();internal Dictionary<object,Payload> References=new();}
    private AreaImage[] cacheActual;private Network cacheNetwork=new();private bool cacheCompanions=true,cacheIdentities=true,cacheSettled=true,cacheMetadata=true;
    private bool AreasMatch(AreaImage[] expected,Entity owner)=>expected.Length==cacheActual.Length&&expected.Select((a,i)=>AreaEqual(a,cacheActual[i])).All(v=>v);
    private static bool AreaEqual(AreaImage a,AreaImage b)=>ReferenceEquals(a.Prefab,b.Prefab)&&AreaGeometry.Matches(a.Nodes,a.Local,b.Nodes,b.Local);
    private bool NetworkNonAreaCompanionsMatch(NetworkEntry e,bool source)=>cacheCompanions;
    private Network CaptureAreaNetwork(Entity id,bool invalid)=>cacheNetwork;
    private bool NetworkSettled(Network network)=>cacheSettled;
    private bool CompanionSourcesValid(IEnumerable<Entity> a,IEnumerable<Entity> b)=>cacheIdentities;
    private AreaImage[] CaptureAreas(Entity owner)=>cacheActual;
    private static T[] NetworkRows<T>(Payload p,string key)=>(T[])(object)p.Rows;
    private bool AreaMoveSettled(Entity id)=>cacheSettled;
    private StandaloneAreaEntry CaptureStandaloneArea(Entity id,bool bound,bool invalid)=>new(){Data=new(Game.Areas.AreaFlags.Complete,cacheActual[0].Nodes,cacheActual[0].Local),Network=cacheNetwork};
    private bool StandaloneAreaMatches(StandaloneAreaEntry a,StandaloneAreaEntry b)=>cacheMetadata&&a.Data.Matches(b.Data);
    private static void CacheChecks(ref int count) {
        int n=0;void Check(bool value,string name){if(!value)throw new Exception("Area cache restore: "+name);n++;}
        var id=new Entity{Id=9};var prefab=new AreaPrefab();
        var nodes=new[]{new Game.Areas.Node{m_Position=new(2,0,0)},new Game.Areas.Node{m_Position=new(4,0,0)},new Game.Areas.Node{m_Position=new(0,0,4)}};
        var old=nodes.Select(p=>new LocalNodeCache{m_Position=p.m_Position,m_ParentMesh=-1}).ToArray();old[0].m_Position.x=0;
        var saved=nodes.Select(p=>p.m_Position).ToArray();
        (BulldozerTrace,NetworkEntry) Ready(){
            var trace=new BulldozerTrace();var expected=new AreaImage{Original=id,Prefab=prefab,Nodes=nodes,Local=old};
            var regenerated=old.ToArray();for(int i=0;i<regenerated.Length;i++)regenerated[i].m_Position=saved[i];
            trace.cacheActual=new[]{expected.WithLocal(regenerated)};
            trace.world.EntityManager.Values[typeof(LocalNodeCache)]=regenerated.ToList();
            var image=new StandaloneAreaEntry{Data=new(Game.Areas.AreaFlags.Complete,nodes,old),Network=new()};
            return(trace,new NetworkEntry{Before=new(){Rows=new[]{new ObjectSubAreaInfo{m_AreaPrefab=prefab,m_NodePositions=saved}}},Areas=new[]{expected},AreaMoveEntry=new(){Target=id,Before=image,After=image}});
        }
        var (t,e)=Ready();t.RestoreAreaMoveCaches(e);
        Check(t.world.EntityManager.GetBuffer<LocalNodeCache>(id).SequenceEqual(old),"actual restore writes original cache after preflight");
        Check(nodes[0].m_Position.x==2&&old[0].m_Position.x==0,"first move geometry and detached before-image preserved");
        foreach(var gate in new[]{"source","companions","identities","network","settled","nodes","cache","attachment","metadata","own graph"}){
            (t,e)=Ready();
            switch(gate){
                case "source":e.Before.Valid=false;break;
                case "companions":t.cacheCompanions=false;break;
                case "identities":t.cacheIdentities=false;break;
                case "network":t.cacheNetwork.Valid=false;break;
                case "settled":t.cacheSettled=false;break;
                case "nodes":t.cacheActual[0].Nodes=nodes.ToArray();t.cacheActual[0].Nodes[1].m_Position.x+=1;break;
                case "cache":t.cacheActual[0].Local[0].m_Position.x+=1;break;
                case "attachment":t.cacheActual[0].Local[0].m_ParentMesh=3;break;
                case "metadata":t.cacheMetadata=false;break;
                case "own graph":e.AreaMoveEntry.Before.Network.Valid=false;break;
            }
            var before=t.world.EntityManager.GetBuffer<LocalNodeCache>(id).ToArray();bool refused=false;
            try{t.RestoreAreaMoveCaches(e);}catch(NotSupportedException){refused=true;}
            Check(refused&&t.world.EntityManager.GetBuffer<LocalNodeCache>(id).SequenceEqual(before),gate+" failure writes no cache");
        }
        count+=n;
    }
}}
