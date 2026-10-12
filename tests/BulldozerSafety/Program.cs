using System;
using MW.EditorTools.Modules.BulldozerRecovery;
class Program {
 static int checks;
 static void Check(bool result,string name){if(!result)throw new Exception(name);checks++;}
 static void Main(){var a=new object();var b=new object();var s=new BulldozerTraceSession<object>();
 Check(!s.CanSynchronize(a),"No transaction before a click");
 s.Begin(a,10);Check(!s.CanSynchronize(a),"Preview is not a completed action");
 s.AllowApply(false);s.EndApply();Check(s.Asset==null,"Denied apply cannot be recorded");
 s.Begin(a,20);s.AllowApply(true);Check(!s.CanSynchronize(a),"No synchronization while apply executes");
 s.EndApply();Check(s.CanSynchronize(a),"Allowed apply awaits its own asset");
 Check(!s.CanSynchronize(b),"Unrelated SaveInstance cannot match");s.Synchronize(b);Check(!s.Synchronized,"Unrelated callback does not consume action");
 s.Synchronize(a);Check(s.Synchronized&&!s.CanSynchronize(a),"Native synchronization consumed only once");
 Check(!s.Expired(140)&&s.Expired(141),"Bounded native wait timeout");
 s.Reset();Check(!s.Accepted&&!s.Synchronized&&s.Asset==null,"Asset change/dispose clears transient state");
 s.AllowApply(true);Check(!s.Accepted,"Unrelated later native validation does not activate trace");
 s.Begin(a,200);s.AllowApply(true);s.EndApply();s.Begin(b,201);Check(s.Matches(b)&&!s.Matches(a)&&!s.Accepted,"Overlapping action cannot inherit previous approval/owner");
 s.EndApply();Check(s.Asset==null,"Unapproved second action is discarded");
 s.Begin(null,250);s.AllowApply(true);Check(s.Asset==null&&!s.Accepted,"Missing owner cannot activate trace");
 var h=new MW.EditorTools.History.TransactionHistory<object>(3);var c=new object();
 h.Record(a);h.Record(b);h.Record(c);
 Check(h.UndoCount==3&&ReferenceEquals(h.Latest,c),"Three deletions retain reverse undo order");
 var pending=h.Latest;Check(h.UndoCount==3&&h.RedoCount==0,"Starting deferred restoration does not advance history");
 Check(!h.Undo(item=>false)&&h.UndoCount==3&&h.RedoCount==0,"Failed scene validation leaves cursor unchanged");
 Check(h.Undo(item=>ReferenceEquals(item,pending))&&h.UndoCount==2&&ReferenceEquals(h.NextRedo,c),"Confirmed graph commits only matching pending entry");
 Check(!h.Undo(item=>ReferenceEquals(item,pending))&&h.UndoCount==2,"Old completion cannot consume next entry");
 Check(h.Undo(item=>ReferenceEquals(item,b))&&h.Undo(item=>ReferenceEquals(item,a))&&h.UndoCount==0,"Undo all three in order");
 Check(h.Redo(item=>ReferenceEquals(item,a))&&h.Redo(item=>ReferenceEquals(item,b))&&h.Redo(item=>ReferenceEquals(item,c)),"Redo all three in forward order");
 h.Undo(item=>true);h.Record(a);Check(h.RedoCount==0&&h.UndoCount==3,"New deletion after Undo clears redo branch");
 h.Record(b);Check(h.UndoCount==3&&ReferenceEquals(h.Recent[0],b)&&ReferenceEquals(h.Latest,b),"Depth evicts oldest entry while retaining newer deletions");
 h.SetDepth(1);Check(h.UndoCount==1&&ReferenceEquals(h.Latest,b),"Configured lower limit keeps latest deletion");
 h.Clear();Check(h.UndoCount==0&&h.RedoCount==0,"Context invalidation clears both directions");
 int captures=0,enables=0;
 Func<bool> valid=()=>{captures++;return true;};Action enable=()=>enables++;
 Check(!AutomaticStandaloneCapture.TryEnable(false,false,true,valid,enable)&&captures==0&&enables==0,"Disabled recovery does not inspect or enable");
 Check(!AutomaticStandaloneCapture.TryEnable(true,true,true,valid,enable)&&captures==0&&enables==0,"Busy, disabled or diagnostic mode does not inspect or enable");
 Check(!AutomaticStandaloneCapture.TryEnable(true,false,false,valid,enable)&&captures==0&&enables==0,"Sub-element mode does not activate standalone recovery");
 Check(!AutomaticStandaloneCapture.TryEnable(true,false,true,()=>false,enable)&&enables==0,"Unsupported target leaves existing mode intact");
 Check(!AutomaticStandaloneCapture.TryEnable(true,false,true,()=>throw new NotSupportedException(),enable)&&enables==0,"Capture refusal leaves history intact");
 Check(AutomaticStandaloneCapture.TryEnable(true,false,true,valid,enable)&&captures==1&&enables==1,"Validated standalone target enables exactly once before native deletion");
 Console.WriteLine("PASS: "+checks+" bulldozer lifecycle/history assertions. Native game event timing remains unproven.");}
}
