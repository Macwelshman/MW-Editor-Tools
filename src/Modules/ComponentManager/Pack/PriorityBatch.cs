using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Game.Prefabs;
using Unity.Entities;
using MW.EditorTools.Modules.ComponentManager.Transfer;
namespace MW.EditorTools.Modules.ComponentManager.Pack {
 internal sealed partial class PriorityBatch {
  internal sealed class Change {internal PrefabBase Asset;internal UIObject Component;internal TransferTarget Target;internal int Before,After;internal string Fingerprint,Name;}
  private readonly World world;internal Action BeforeWrite;internal Change[] Preview=new Change[0];private Change[] applied;
  internal PriorityBatch(World world){this.world=world;}
  internal void Plan(PrefabBase[] assets,int start,int step,string pattern){
   Preview=new Change[0];var available=Workspace(world);if(assets.Any(a=>!available.Any(b=>ReferenceEquals(a,b))))throw new InvalidOperationException("Workspace changed. Scan again.");
   var order=PriorityOrder.Sort(assets.Select(a=>a.name).ToArray(),pattern);var values=PriorityOrder.Values(order.Length,start,step);
   Preview=order.Select((index,i)=>{var asset=assets[index];var target=new TransferTarget(world,asset,asset);target.Validate();var component=target.Exact(typeof(UIObject)) as UIObject;if(component==null)throw new InvalidOperationException(asset.name+": UI Object component missing.");return new Change{Name=asset.name,Asset=asset,Target=target,Component=component,Before=component.m_Priority,After=values[i],Fingerprint=target.Fingerprint()};}).ToArray();
  }
  internal void Invalidate(){Preview=new Change[0];}
  internal void Apply(){
   if(Preview.Length==0)throw new InvalidOperationException("Preview priorities first.");var plan=Preview;var available=Workspace(world);
   foreach(var c in plan){c.Target.Validate();if(c.Asset.name!=c.Name||!available.Any(a=>ReferenceEquals(a,c.Asset))||!ReferenceEquals(c.Target.Exact(typeof(UIObject)),c.Component)||c.Target.Fingerprint()!=c.Fingerprint)throw new InvalidOperationException("Asset settings/workspace changed. Preview again: "+c.Asset.name);}
   if(plan.All(c=>c.Before==c.After)){Invalidate();return;}
   BeforeWrite?.Invoke();
   try{foreach(var c in plan)c.Component.m_Priority=c.After;foreach(var c in plan){c.Asset.isDirty=true;c.Target.Refresh();}}
   catch(Exception error){var failures=new List<string>();foreach(var c in plan)try{c.Component.m_Priority=c.Before;c.Target.Refresh();}catch(Exception rollback){failures.Add(rollback.Message);}Invalidate();throw new InvalidOperationException("Priority batch failed; "+(failures.Count==0?"prior values restored, refresh requeued.":"rollback incomplete: "+string.Join("; ",failures)),error);}
   applied=plan;foreach(var c in applied)c.Fingerprint=c.Target.Fingerprint();Invalidate();
  }
  internal bool CanRestore=>applied!=null;
  internal void Restore(){
   if(applied==null)throw new InvalidOperationException("No priority batch to restore.");foreach(var c in applied){c.Target.Validate();if(c.Asset.name!=c.Name||!ReferenceEquals(c.Target.Exact(typeof(UIObject)),c.Component)||c.Target.Fingerprint()!=c.Fingerprint)throw new InvalidOperationException("Later asset changes prevent priority restore: "+c.Asset.name);}
   BeforeWrite?.Invoke();
   try{foreach(var c in applied)c.Component.m_Priority=c.Before;foreach(var c in applied){c.Asset.isDirty=true;c.Target.Refresh();}applied=null;}
   catch(Exception error){var failures=new List<string>();foreach(var c in applied)try{c.Component.m_Priority=c.After;c.Target.Refresh();}catch(Exception rollback){failures.Add(rollback.Message);}if(failures.Count!=0)applied=null;throw new InvalidOperationException("Priority Restore failed; "+(failures.Count==0?"prior applied values reinstated, refresh requeued.":"rollback incomplete: "+string.Join("; ",failures)),error);}
  }
 }
}
