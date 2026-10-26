using System;
namespace Unity.Jobs {internal struct JobHandle {internal int Value;}}
namespace Game.Tools {internal class BulldozeToolSystem {internal object World;}}
namespace MW.EditorTools {internal static class Mod {internal static Logger Log=new();internal class Logger {internal int Lines;internal void Info(string s){Lines++;}}}}
namespace MW.EditorTools.Modules.BulldozerRecovery {
internal sealed partial class BulldozerTrace {
private static BulldozerTrace active;private bool compatible=true;private bool PatchesCompatible(bool force=false)=>compatible;private object world=new();private Context context=new();private bool Busy;private string Status;private int observed;private int pending=42;
private class Context {internal bool EditorAvailable=true;}
private static void ObserveApply(Game.Tools.BulldozeToolSystem tool){active.observed++;}
internal static int GuardChecks(){int checks=0;void Check(bool v){if(!v)throw new Exception("Apply guard assertion "+checks);checks++;}
var tool=new Game.Tools.BulldozeToolSystem();var input=new Unity.Jobs.JobHandle{Value=17};var output=new Unity.Jobs.JobHandle{Value=99};active=null;
Check(BeginApply(tool,input,ref output,out var observe)&&!observe&&output.Value==99);
active=new BulldozerTrace{Busy=true};tool.World=active.world;Mod.Log.Lines=0;
Check(!BeginApply(tool,input,ref output,out observe)&&!observe&&output.Value==17);
Check(active.Busy&&active.pending==42&&active.observed==0&&Mod.Log.Lines==1&&active.Status.Contains("wait"));
Check(!BeginApply(tool,input,ref output,out observe)&&Mod.Log.Lines==1&&active.pending==42);
tool.World=new object();output.Value=99;Check(BeginApply(tool,input,ref output,out observe)&&!observe&&output.Value==99&&active.observed==0);
tool.World=active.world;active.context.EditorAvailable=false;Check(BeginApply(tool,input,ref output,out observe)&&!observe&&active.observed==0);
active.context.EditorAvailable=true;active.Busy=false;Check(BeginApply(tool,input,ref output,out observe)&&observe&&active.observed==1&&active.pending==42);
active.Busy=true;Check(!BeginApply(tool,input,ref output,out observe)&&!observe&&Mod.Log.Lines==2&&active.pending==42);
active=new BulldozerTrace{compatible=false};tool.World=active.world;Check(BeginApply(tool,input,ref output,out observe)&&!observe&&active.observed==0&&active.pending==42);active.Busy=true;Check(!BeginApply(tool,input,ref output,out observe)&&!observe&&output.Value==17&&active.pending==42);active=null;return checks;
}
}}
