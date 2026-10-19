using System;
using System.Collections;
using System.Linq;
using Game.Prefabs;
using MW.EditorTools.History;

namespace MW.EditorTools.Modules.PropReplacer {
 public sealed partial class PropReplacerSystem {
  internal Func<bool> CanRecordUndo;
  internal Action<ReplacementUndoAction> RecordUndo;
  private string replacementUndoBlockReason;
  private bool ReplacementUndoBlocked(string reason){if(reason!=replacementUndoBlockReason){replacementUndoBlockReason=reason;MW.EditorTools.Mod.Log.Info("Replacement Undo blocked: "+reason);}return false;}
  private sealed class ReplacementSourceGuard {
   internal PrefabBase Root;internal ComponentBase[] Components;internal bool[] Active;
   internal ComponentBase[] Owners;internal System.Reflection.FieldInfo[] Fields;internal PrefabSnapshot[] Values;
   internal bool Matches()=>Root!=null&&!Root.isReadOnly&&Root.components.Count==Components.Length&&Root.components.Zip(Components,(a,b)=>ReferenceEquals(a,b)).All(x=>x)&&Components.Select((c,i)=>c!=null&&c.active==Active[i]).All(x=>x)&&Owners.Select((c,i)=>ReferenceEquals(c.prefab,Root)&&Values[i].ReferencesValid&&Values[i].Matches(Fields[i].GetValue(c))).All(x=>x);
  }
  private ReplacementSourceGuard[] ReplacementGuards(Row row) {
   return new[]{row.Root}.Concat(row.Ancestors??new PrefabBase[0]).Distinct().Select(root=>{
    var owners=new System.Collections.Generic.List<ComponentBase>();var fields=new System.Collections.Generic.List<System.Reflection.FieldInfo>();
    for(int i=0;i<RecoveryListSchema.Components.Length;i++)foreach(var c in root.components.Where(c=>c!=null&&c.GetType()==RecoveryListSchema.Components[i])){owners.Add(c);fields.Add(c.GetType().GetField(RecoveryListSchema.Members[i]));}
    return new ReplacementSourceGuard{Root=root,Components=root.components.ToArray(),Active=root.components.Select(c=>c!=null&&c.active).ToArray(),Owners=owners.ToArray(),Fields=fields.ToArray(),Values=owners.Select((c,i)=>PrefabSnapshot.Take(fields[i].GetValue(c))).ToArray()};
   }).ToArray();
  }
  private ReplacementUndoAction MakeReplacementUndo(Row row,PrefabSnapshot before,PrefabSnapshot after,PrefabBase original,PrefabBase replacement,int[] indices,PrefabBase selected,ReplacementSourceGuard[] guards) {
   if(before.Weight+after.Weight+guards.Sum(g=>g.Values.Sum(v=>v.Weight))>500000)throw new NotSupportedException("Replacement Undo exceeds the snapshot budget.");
   // Regeneration may clear the inspector, or select the replacement prefab.
   // The entry remains pinned to the owning source, never the current selection.
   var related=new[]{selected,row.Root,original,replacement}.Concat(row.Ancestors??new PrefabBase[0]).Where(p=>p!=null).ToArray();
   Func<bool> contextValid=()=>context?.EditorAvailable==true&&(context.Asset==null||related.Any(p=>ReferenceEquals(p,context.Asset)));
   Func<bool> ready=()=>{
    if(!contextValid())return ReplacementUndoBlocked("editor or owning asset context changed");
    if(context.DialogBusy)return ReplacementUndoBlocked("editor dialog open");
    if(foliageFailed||foliageWitnesses.Length>0)return ReplacementUndoBlocked("foliage regeneration pending or failed");
    if(!before.ReferencesValid||!after.ReferencesValid)return ReplacementUndoBlocked("captured prefab reference unavailable");
    if(!guards.All(g=>g.Matches()))return ReplacementUndoBlocked("owning source/component data differs from the completed replacement");
    if(!after.Matches(row.ListField.GetValue(row.Owner)))return ReplacementUndoBlocked("replacement list no longer matches the completed action");
    replacementUndoBlockReason=null;return true;
   };
   return new ReplacementUndoAction((indices.Length==1?"single replacement":"batch replacement ("+indices.Length+" elements)"),ready,()=>{
    if(!ready())return false;
    var restored=before.Materialize();var current=after.Materialize();
    var list=row.ListField.GetValue(row.Owner) as IList;
    var target=new Row{Root=row.Root,Ancestors=row.Ancestors,Owner=row.Owner,ListField=row.ListField,ReferenceField=row.ReferenceField,List=list,Index=indices[0],Value=list[indices[0]],Label=row.Label};
    var plan=PlanFoliageReplacement(target,true,replacement,original,indices);bool dirty=row.Root.isDirty;
    try{
     CreateFoliageCandidates(plan);row.ListField.SetValue(row.Owner,restored);row.Root.isDirty=true;Refresh(row.Root,row.Ancestors);
     if(!before.Matches(row.ListField.GetValue(row.Owner)))throw new InvalidOperationException("Restored replacement source did not verify.");
     WatchFoliageCandidates(plan,target,after);
     selectedValue=null;pendingViewportClipboard=null;status="Undid replacement of "+indices.Length+" element(s). Original prefabs, placement and settings restored.";MW.EditorTools.Mod.Log.Info(status);return true;
    }catch{
     foliageWitnesses=new FoliageWitness[0];foliagePlan=null;
     try{RemoveFoliageCandidates(plan);row.ListField.SetValue(row.Owner,current);row.Root.isDirty=dirty;Refresh(row.Root,row.Ancestors);}catch(Exception rollback){foliageFailed=true;MW.EditorTools.Mod.Log.Error(rollback,"Replacement Undo rollback failed; reload without saving.");}
     throw;
    }
   },contextValid);
  }
 }
}
