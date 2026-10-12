using System; using System.Linq;
namespace UnityEngine {static class Time {public static int frameCount;}}
namespace Game.Prefabs {class PrefabBase {}}
namespace MW.EditorTools {static class Mod {internal static Logger Log=new Logger();internal class Logger {internal void Info(string value){}}}}
namespace MW.EditorTools.Modules.BulldozerRecovery {
internal sealed partial class BulldozerTrace {
class Context {internal bool EditorAvailable=true,DialogBusy;internal Game.Prefabs.PrefabBase Asset;}
class Session {internal object Asset;}
class Record {internal object ForestBefore;internal Game.Prefabs.PrefabBase Asset;}
class Records {internal int UndoCount,RedoCount;}
Context context=new Context();Session session=new Session();Records records=new Records();
Game.Prefabs.PrefabBase nestedFixture,armedAsset;Record record;object restoring;bool disabled,armed,nestedArmed,nestedForestArmed;bool CanArm=true;string Status;int undos,redos;
bool preparationRequested; bool BeginNestedTestPreparation()=>preparationRequested;
int adoptedRootCount=2;int AdoptedRootCount()=>adoptedRootCount;
bool savedAvailable=true,ordinaryAvailable,ordinaryNested; bool SelectedAssetUsesNestedGroup()=>true;
bool UseOrdinaryNestedGroup(){if(!ordinaryAvailable)return false;nestedFixture=context.Asset;ordinaryNested=true;return true;}
bool UseSavedNestedFixture(){if(!savedAvailable)return false;nestedFixture=context.Asset;ordinaryNested=false;return true;}
void ArmNestedForest(){armed=nestedArmed=nestedForestArmed=true;armedAsset=context.Asset;records.UndoCount=records.RedoCount=0;}
bool CanUndo=>records.UndoCount>0;bool CanRedo=>records.RedoCount>0;
void Undo(){undos++;records.UndoCount--;records.RedoCount++;}
void Redo(){redos++;records.UndoCount++;records.RedoCount--;}
static int checks;static void Check(bool value){checks++;if(!value)throw new Exception("Assertion "+checks);}
static BulldozerTrace Ready(){var t=new BulldozerTrace();t.context.Asset=new Game.Prefabs.PrefabBase();t.StartNestedRecoveryTest();return t;}
void Deleted(){record=new Record{Asset=context.Asset,ForestBefore=new object()};records.UndoCount=1;UnityEngine.Time.frameCount+=20;}
void Step(){UnityEngine.Time.frameCount+=20;TickNestedRecoveryTest();}
public static void Main(){
MixedTypeChecks();
foreach(int size in new[]{1,2,3,4}){
var full=new BulldozerTrace();full.context.Asset=new Game.Prefabs.PrefabBase();full.adoptedRootCount=size;full.StartNestedRecoveryTest(0);Check(full.NestedTestRunning&&full.nestedTestCount==size);
full.Deleted();full.records.UndoCount=size;full.Step();full.restoring=new object();full.Step();Check(full.undos==1&&full.redos==0);full.restoring=null;
for(int action=0;action<3*size;action++)full.Step();
Check(full.Status.StartsWith("PASS: "+size)&&full.undos==2*size&&full.redos==size);Check(full.records.UndoCount==0&&full.records.RedoCount==size&&!full.NestedTestRunning);
}
foreach(int size in new[]{0,5}){var full=new BulldozerTrace();full.context.Asset=new Game.Prefabs.PrefabBase();full.adoptedRootCount=size;full.StartNestedRecoveryTest(0);Check(!full.NestedTestRunning&&full.Status.StartsWith("Undo test stopped:"));}
Check(EmptyPreparationField("ObjectSubAreas","m_SubAreas",null));Check(EmptyPreparationField("ObjectSubNets","m_SubNets",new int[0]));Check(EmptyPreparationField("ObjectSubLanes","m_SubLanes",new int[0]));Check(EmptyPreparationField("EffectSource","m_Effects",new System.Collections.ArrayList()));Check(EmptyPreparationField("EffectSource","m_AnimationCurves",null));Check(EmptyPreparationField("ObjectSubNets","m_InvertWhen",0));Check(!EmptyPreparationField("ObjectSubNets","m_InvertWhen",1));Check(!EmptyPreparationField("ObjectSubAreas","m_SubAreas",new[]{1}));Check(!EmptyPreparationField("EffectSource","unknown",null));Check(!EmptyPreparationField("Unknown","m_SubLanes",null));
var manual=new BulldozerTrace();manual.context.Asset=new Game.Prefabs.PrefabBase();manual.ordinaryAvailable=true;manual.EnableNestedHistory();Check(manual.NestedHistoryEnabled&&!manual.NestedTestRunning);manual.Deleted();manual.Step();manual.Step();Check(manual.undos==0&&manual.redos==0&&manual.records.UndoCount==1);
manual=new BulldozerTrace();manual.context.Asset=new Game.Prefabs.PrefabBase();manual.ordinaryAvailable=true;manual.preparationRequested=true;manual.EnableNestedHistory();Check(!manual.NestedHistoryEnabled&&!manual.NestedTestRunning);manual.BeginArmedNestedTest();Check(manual.NestedHistoryEnabled&&!manual.NestedTestRunning);manual.Deleted();manual.Step();Check(manual.undos==0);
manual=new BulldozerTrace();manual.EnableNestedHistory();Check(!manual.NestedHistoryEnabled&&!manual.NestedTestRunning);
manual=new BulldozerTrace();manual.context.Asset=new Game.Prefabs.PrefabBase();manual.ordinaryAvailable=true;manual.EnableNestedHistory();manual.Deleted();manual.records.RedoCount=2;manual.EnableNestedHistory();Check(manual.records.UndoCount==1&&manual.records.RedoCount==2);Check(manual.NestedHistoryEnabled&&!manual.NestedTestRunning);manual.restoring=new object();manual.EnableNestedHistory();Check(manual.records.UndoCount==1&&manual.records.RedoCount==2&&manual.restoring!=null);
var batch=new BulldozerTrace();batch.context.Asset=new Game.Prefabs.PrefabBase();batch.StartNestedRecoveryTest(2);batch.Deleted();batch.Step();Check(batch.undos==0&&batch.Status.Contains("1 of 2"));batch.records.UndoCount=2;batch.Step();Check(batch.undos==1);batch.restoring=new object();batch.Step();Check(batch.undos==1);batch.restoring=null;batch.Step();Check(batch.undos==2&&batch.redos==0);batch.Step();batch.Step();Check(batch.redos==2&&batch.records.UndoCount==2);batch.Step();batch.Step();batch.Step();Check(batch.Status.StartsWith("PASS: 2")&&batch.undos==4&&batch.records.RedoCount==2);batch.Step();Check(batch.undos==4);
batch=new BulldozerTrace();batch.context.Asset=new Game.Prefabs.PrefabBase();batch.StartNestedRecoveryTest(5);Check(!batch.NestedTestRunning&&batch.Status.Contains("unsupported"));
batch=new BulldozerTrace();batch.context.Asset=new Game.Prefabs.PrefabBase();batch.StartNestedRecoveryTest(2);batch.Deleted();batch.records.UndoCount=3;batch.Step();Check(batch.undos==0&&batch.Status.StartsWith("Undo test stopped:"));
batch=new BulldozerTrace();batch.context.Asset=new Game.Prefabs.PrefabBase();batch.StartNestedRecoveryTest(2);batch.Deleted();batch.records.UndoCount=2;batch.Step();batch.records.UndoCount=2;batch.Step();Check(batch.undos==1&&batch.Status.StartsWith("Undo test stopped:"));
var deselected=Ready();Check(deselected.NestedTestRunning);deselected.context.Asset=null;deselected.Step();Check(deselected.NestedTestRunning&&deselected.undos==0);deselected.Deleted();deselected.record.Asset=deselected.nestedTestAsset;deselected.Step();Check(deselected.undos==1);
Check(NestedElevationPolicy.MatchesState(false,1,false,3));Check(!NestedElevationPolicy.MatchesState(false,0,true,0));Check(!NestedElevationPolicy.MatchesState(true,0,false,0));Check(NestedElevationPolicy.MatchesState(true,3,true,3));Check(!NestedElevationPolicy.MatchesState(true,3,true,2));
Check(NestedElevationPolicy.Matches(false,0,0,false,9,7));Check(!NestedElevationPolicy.Matches(false,0,0,true,0,0));Check(!NestedElevationPolicy.Matches(true,0,0,false,0,0));Check(NestedElevationPolicy.Matches(true,2.5f,3,true,2.5f,3));Check(!NestedElevationPolicy.Matches(true,2.5f,3,true,2.6f,3));Check(!NestedElevationPolicy.Matches(true,2.5f,3,true,2.5f,2));
var t=Ready();t.Step();Check(t.undos==0&&t.redos==0);t.Deleted();t.Step();Check(t.undos==1&&t.redos==0);t.restoring=new object();t.Step();Check(t.redos==0);t.restoring=null;t.Step();Check(t.redos==1);t.Step();Check(t.undos==2);t.Step();Check(t.Status.StartsWith("PASS:")&&t.records.RedoCount==1);t.Step();Check(t.undos==2&&t.redos==1);
t=Ready();t.armed=false;t.Step();Check(t.Status.StartsWith("Undo test stopped:")&&t.undos==0);
t=Ready();t.context.Asset=new Game.Prefabs.PrefabBase();t.Step();Check(t.nestedTestAsset==null&&t.undos==0);
t=Ready();t.Deleted();t.records.UndoCount=2;t.Step();Check(t.undos==0&&t.Status.StartsWith("Undo test stopped:"));
t=Ready();t.Deleted();t.session.Asset=new object();t.Step();Check(t.undos==0);t.session.Asset=null;t.context.DialogBusy=true;t.Step();Check(t.undos==0);t.context.DialogBusy=false;t.disabled=true;t.Step();Check(t.undos==0&&t.nestedTestAsset==null);
t=new BulldozerTrace();t.context.Asset=new Game.Prefabs.PrefabBase();t.preparationRequested=true;t.StartNestedRecoveryTest();Check(!t.armed&&t.nestedTestAsset==null&&t.undos==0);t.BeginArmedNestedTest();Check(t.armed&&t.nestedTestStep==1&&t.undos==0);
t=new BulldozerTrace();t.context.Asset=new Game.Prefabs.PrefabBase();t.savedAvailable=false;t.ordinaryAvailable=true;t.StartNestedRecoveryTest();Check(t.armed&&t.nestedTestStep==1);
t=new BulldozerTrace();t.context.Asset=new Game.Prefabs.PrefabBase();t.savedAvailable=false;t.StartNestedRecoveryTest();Check(!t.armed&&t.nestedTestAsset==null);
Func<int[],int[],bool> accepted=(parents,depths)=>NestedPropPolicy.Accept(parents,depths,parents.Select(_=>new object()).ToArray(),parents.Select(_=>true).ToArray());
Check(accepted(new[]{-1,0},new[]{0,1}));Check(accepted(new[]{-1,0,1},new[]{0,1,2}));Check(accepted(new[]{-1,0,0},new[]{0,1,1}));Check(accepted(new[]{-1,0,0,1,1,2,2,3},new[]{0,1,1,2,2,2,2,3}));
Check(!accepted(new[]{-1},new[]{0}));Check(!accepted(new[]{-1,0,0,0,0,0,0,0,0},new[]{0,1,1,1,1,1,1,1,1}));Check(!accepted(new[]{-1,0,1,2,3,4},new[]{0,1,2,3,4,5}));Check(!accepted(new[]{-1,1},new[]{0,1}));Check(!accepted(new[]{-1,-1},new[]{0,1}));Check(!accepted(new[]{-1,0},new[]{0,2}));Check(!accepted(new[]{-1,0},new[]{1,2}));
var same=new object();Check(!NestedPropPolicy.Accept(new[]{-1,0},new[]{0,1},new[]{same,same},new[]{true,true}));Check(!NestedPropPolicy.Accept(new[]{-1,0},new[]{0,1},new[]{new object(),null},new[]{true,true}));Check(!NestedPropPolicy.Accept(new[]{-1,0},new[]{0,1},new[]{new object(),new object()},new[]{true,false}));Check(!NestedPropPolicy.Accept(new[]{-1,0},new[]{0},new[]{new object(),new object()},new[]{true,true}));Check(!NestedPropPolicy.Accept(null,new[]{0},new[]{new object()},new[]{true}));
Console.WriteLine("PASS: "+checks+" automatic runner assertions against actual source; native timing remains unproven.");
}
}}


