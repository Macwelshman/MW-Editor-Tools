using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using MW.EditorTools.Modules.BulldozerRecovery;
namespace Unity.Entities {internal struct Entity {internal int Index;} internal class World {internal Manager EntityManager=new();} internal class Manager {internal bool HasComponent<T>(Entity e)=>false;}}
namespace Unity.Jobs {internal struct JobHandle {internal int Value;}}
namespace UnityEngine {internal static class Time {internal static int frameCount=40;}}
namespace Game.Common {internal struct Owner {}}
namespace Game.Objects {internal struct Transform {internal float3 m_Position;internal quaternion m_Rotation;}}
namespace Game.Tools {
internal enum ApplyMode {None,Apply,Clear}
internal class ObjectToolSystem {internal enum Mode {Create,Move} internal Unity.Entities.World World;internal Mode actualMode;internal ApplyMode applyMode;}
}
namespace MW.EditorTools {internal static class Mod {internal static Logger Log=new();internal class Logger {internal void Info(string s){} internal void Warn(string s){}}}}
namespace MW.EditorTools.Modules.BulldozerRecovery {
internal sealed partial class BulldozerTrace {
private static BulldozerTrace active;
private Unity.Entities.World world=new();private Context context=new();private bool Busy,moveCommitted,moveRestoring,objectInApply,objectAccepted,objectMoveApplying,moveDisabled;
private bool movePreviewEntered;private Unity.Entities.Entity movePreviewTarget;private int moveFrame,moveStable,moveLastFrame;private string Status;private MoveEntry moveCandidate;private Func<bool> AutomaticCaptureAllowed=()=>true;
private bool effectRoute,effectFinished;private bool TryBeginEffectMove(Unity.Entities.Entity id)=>effectRoute;private bool FinishEffectMove(Game.Tools.ObjectToolSystem tool)=>effectFinished;
private string previous="delete";private int clearCount;private bool refuse,settled=true;
private class Context {internal bool EditorAvailable=true,DialogBusy;}
private class Prefab {internal string name="Tree";}
private class NestedNode {internal Prefab Prefab=new();internal Unity.Entities.Entity Owner;}
private class Payload {internal int Weight=1;}
private class NetworkEntry {}
private class MoveEntry {internal Unity.Entities.Entity Target;internal NestedNode[] Before;internal Dictionary<Prefab,Payload> References;internal NetworkEntry BoundBefore;}
private NestedNode[] CaptureMoveGroup(Unity.Entities.Entity id){if(refuse)throw new NotSupportedException("unsupported");return new[]{new NestedNode()};}
private bool MoveGroupSettled(NestedNode[] group)=>settled;private bool MoveMatchesTool(Game.Tools.ObjectToolSystem tool)=>movePreviewEntered;
private static IEnumerable<Prefab> GroupReferences(NestedNode[] group)=>group.Select(n=>n.Prefab);
private static Payload Capture(Prefab p)=>new();private static bool NestedReferenceMatches(Prefab p,Payload image)=>true;
private NetworkEntry CaptureMoveOwner(Unity.Entities.Entity id)=>new();
private void ClearMovePrevious(){previous=null;clearCount++;}
private void ClearMove(){moveCandidate=null;movePreviewEntered=moveCommitted=moveRestoring=objectInApply=false;}
internal static int RunHooks(){
int n=0;void Check(bool v,string s){if(!v)throw new Exception("Move hooks: "+s);n++;}
var tool=new Game.Tools.ObjectToolSystem();var id=new Unity.Entities.Entity{Index=7};active=null;
Check(BeforeMoveStart(tool,id),"unattached pass through");
active=new();tool.World=active.world;active.Busy=true;
Check(!BeforeMoveStart(tool,id)&&active.previous=="delete","busy move start blocked without clearing slot");
active.Busy=false;Check(BeforeMoveStart(tool,id)&&active.moveCandidate!=null&&active.previous=="delete","move start captures but preserves slot");
Check(!active.moveCommitted&&!active.moveRestoring,"preview does not commit");
var deps=new Unity.Jobs.JobHandle{Value=7};var result=new Unity.Jobs.JobHandle{Value=99};tool.actualMode=Game.Tools.ObjectToolSystem.Mode.Move;
Check(BeforeObjectApply(tool,deps,ref result,out var observe)&&observe&&active.objectInApply&&!active.objectAccepted,"prefix opens acceptance observation");
tool.applyMode=Game.Tools.ApplyMode.Apply;AfterObjectApply(tool,observe);
Check(!active.moveCommitted&&active.previous=="delete"&&!active.objectInApply,"native denial preserves slot");
BeforeObjectApply(tool,deps,ref result,out observe);MoveAllowed(tool,true);tool.applyMode=Game.Tools.ApplyMode.Clear;AfterObjectApply(tool,observe);
Check(!active.moveCommitted&&active.previous=="delete","preview/clear is not a commit");
BeforeObjectApply(tool,deps,ref result,out observe);MoveAllowed(tool,true);tool.applyMode=Game.Tools.ApplyMode.Apply;AfterObjectApply(tool,observe);
Check(active.moveCommitted&&active.moveCandidate!=null&&active.previous=="delete"&&active.clearCount==0,"accepted move awaits verified completion before slot replacement");
Check(active.moveFrame==40&&active.moveStable==0&&active.moveLastFrame==-1,"stable window starts once native commit accepted");
active.Busy=true;result.Value=99;
Check(!BeforeObjectApply(tool,deps,ref result,out observe)&&!observe&&result.Value==7,"busy Apply preserves dependency and blocks mutation");
AfterObjectApply(tool,observe);Check(active.previous=="delete"&&active.moveCandidate!=null,"blocked postfix cannot consume action");
active=new();tool.World=active.world;active.refuse=true;
Check(BeforeMoveStart(tool,id)&&active.moveCandidate==null&&active.previous=="delete","unsupported start passes native and preserves slot until commit");
BeforeObjectApply(tool,deps,ref result,out observe);MoveAllowed(tool,true);tool.applyMode=Game.Tools.ApplyMode.Apply;AfterObjectApply(tool,observe);
Check(active.previous==null&&active.clearCount==1&&!active.moveCommitted,"unsupported committed move clears stale slot");
active=new();tool.World=active.world;tool.actualMode=Game.Tools.ObjectToolSystem.Mode.Create;
BeforeObjectApply(tool,deps,ref result,out observe);MoveAllowed(tool,true);tool.applyMode=Game.Tools.ApplyMode.Apply;AfterObjectApply(tool,observe);
Check(active.previous==null&&active.clearCount==1,"accepted placement invalidates old action");
active=new();tool.World=active.world;active.context.EditorAvailable=false;
Check(BeforeMoveStart(tool,id)&&active.moveCandidate==null&&active.previous=="delete","non-editor start untouched");
result.Value=99;Check(BeforeObjectApply(tool,deps,ref result,out observe)&&!observe&&result.Value==99,"non-editor Apply untouched");
active.context.EditorAvailable=true;tool.World=new();
Check(BeforeObjectApply(tool,deps,ref result,out observe)&&!observe,"other world Apply untouched");
active=new();tool.World=active.world;active.AutomaticCaptureAllowed=()=>false;
Check(BeforeMoveStart(tool,id)&&active.moveCandidate==null&&active.previous=="delete","disabled capture leaves native start untouched");
active.AutomaticCaptureAllowed=()=>true;active.settled=false;
Check(BeforeMoveStart(tool,id)&&active.moveCandidate==null,"pending native initialization refused");
active.objectInApply=false;active.objectAccepted=false;MoveAllowed(tool,true);Check(!active.objectAccepted,"acceptance query outside native Apply ignored");active.objectInApply=true;tool.World=new();MoveAllowed(tool,true);Check(!active.objectAccepted,"other-world acceptance ignored");tool.World=active.world;active.objectInApply=false;active.settled=true;active.moveDisabled=true;Check(BeforeMoveStart(tool,id)&&active.moveCandidate==null,"disabled move module passes native start");active.moveDisabled=false;active.context.DialogBusy=true;Check(BeforeMoveStart(tool,id)&&active.moveCandidate==null,"dialog gate prevents capture");active.context.DialogBusy=false;active.CaptureMoveGroup(id)[0].Owner=new Unity.Entities.Entity{Index=1};active=new();tool.World=active.world;active.effectRoute=true;Check(BeforeMoveStart(tool,id)&&active.moveCandidate==null&&active.previous!=null,"effect route bypasses prop capture without clearing prior action");
active.effectFinished=true;BeforeObjectApply(tool,deps,ref result,out observe);MoveAllowed(tool,true);tool.applyMode=Game.Tools.ApplyMode.Apply;AfterObjectApply(tool,observe);
Check(!active.moveCommitted&&active.clearCount==0,"completed effect route bypasses prop commit/clear policy");
active=null;return n;
}
}}
class Program {
static void Main(){int n=BulldozerTrace.RunHooks();void Check(bool v,string s){if(!v)throw new Exception(s);n++;}
Game.Objects.Transform Pose(float3 p,quaternion q)=>new(){m_Position=p,m_Rotation=q};
var before=Pose(new float3(1,2,3),quaternion.identity);var after=Pose(new float3(4,2,3),quaternion.identity);
Check(PropMovePolicy.Changed(before,after),"translation changed");Check(!PropMovePolicy.Changed(before,before),"no-op retains action");
var same=before;same.m_Rotation.value=-same.m_Rotation.value;Check(!PropMovePolicy.Changed(before,same),"quaternion sign is no-op");
Check(PropMovePolicy.ChildPose(before,after,Pose(new float3(2,2,3),quaternion.identity),Pose(new float3(5,2,3),quaternion.identity)),"child translation follows root");
var rotated=Pose(new float3(4,2,3),quaternion.RotateY(math.PI/2));var childBefore=Pose(before.m_Position+new float3(1,0,0),quaternion.identity);var childAfter=Pose(rotated.m_Position+math.mul(rotated.m_Rotation,new float3(1,0,0)),rotated.m_Rotation);
Check(PropMovePolicy.ChildPose(before,rotated,childBefore,childAfter),"child position and rotation follow root");
childAfter.m_Position.x+=1;Check(!PropMovePolicy.ChildPose(before,rotated,childBefore,childAfter),"independent child edit refused");
var invalid=before;invalid.m_Position.x=float.NaN;Check(!PropMovePolicy.Finite(invalid),"nonfinite placement refused");
invalid=before;invalid.m_Rotation=default;Check(!PropMovePolicy.Finite(invalid),"zero quaternion refused");
Check(PropMovePolicy.Commit(false,true,true,true)==PropMovePolicy.CommitAction.Ignore,"denied commit ignored");
Check(PropMovePolicy.Commit(true,false,true,true)==PropMovePolicy.CommitAction.Ignore,"preview not committed");
Check(PropMovePolicy.Commit(true,true,true,true)==PropMovePolicy.CommitAction.Wait,"supported commit waits for native stability");
Check(PropMovePolicy.Commit(true,true,true,false)==PropMovePolicy.CommitAction.Clear,"uncaptured move clears stale slot");
Check(PropMovePolicy.Commit(true,true,false,true)==PropMovePolicy.CommitAction.Clear,"placement cannot become move Undo");
Console.WriteLine("PASS: "+n+" actual-source move hook/commit/pose assertions. Managed doubles; native regeneration acceptance pending.");}
}
