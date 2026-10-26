using System;
using MW.EditorTools.Modules.BulldozerRecovery;

internal static class RecoveryTimingChecks {
    internal static int Run(){
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception("Recovery timing: "+name);count++;}
        long ticks=0;int[] gc={2,1,0};
        var timing=new RecoveryTiming(()=>ticks,1000,generation=>gc[generation]);
        gc[0]++;ticks=10;timing.BeginUndo(100);
        var first=timing.Measure("readiness",4,101);ticks+=7;first.Dispose();first.Dispose();
        using(timing.Measure("readiness",8,101))ticks+=3;
        using(timing.Measure("readiness",8,102))ticks+=5;
        gc[0]+=2;gc[2]++;
        var report=timing.Summary(105);
        Check(report.Contains("readiness count=3 totalMs=15.000 maxMs=7.000"),"aggregates repeated checks and counts disposed samples once");
        Check(report.Contains("maxPerFrameMs=10.000 maxPerFrameCount=2"),"same-frame repeated readiness cost is distinguished from single-call cost");
        Check(report.Contains("undoWallMs=15.000 undoFrames=5"),"wall duration and deferred frames separate from stage time");
        Check(report.Contains("gcSinceCapture=3/0/1")&&report.Contains("gcSinceUndo=2/0/1"),"GC observations distinguish idle capture lifetime from Undo");
        Check(report.Contains("previousFrameMaxMs=8.000"),"records observed engine frame duration without claiming own CPU time");
        timing.Measure("invalid-frame",float.NaN)?.Dispose();timing.Measure("invalid-frame",float.PositiveInfinity)?.Dispose();timing.Measure("invalid-frame",-100)?.Dispose();
        Check(timing.Summary(105).Contains("previousFrameMaxMs=8.000"),"invalid frame measurements cannot contaminate summary");
        timing.BeginUndo(1000);Check(timing.Summary(105).Contains("undoFrames=5"),"repeated begin does not reset restoration window");
        Check(timing.ObserveMismatch("curve"),"first refusal recorded");Check(!timing.ObserveMismatch("curve"),"identical refusal suppressed");
        Check(timing.ObserveMismatch("source")&&timing.ObserveMismatch("schema")&&!timing.ObserveMismatch("extra")&&timing.RefusedSamples==5,"first three unique failures bounded while rejection count retained");
        for(int i=0;i<100;i++)timing.Measure("stage"+i,0)?.Dispose();
        Check(timing.Summary(105).Length<3400&&!timing.Summary(105).Contains("stage99"),"stage storage bounded");
        bool thrown=false;try{using(timing.Measure("work-failure",0))throw new InvalidOperationException("native work refused");}catch(InvalidOperationException){thrown=true;}
        Check(thrown,"measurement never swallows the observed operation's failure");
        bool failClock=false;var safe=new RecoveryTiming(()=>failClock?throw new InvalidOperationException("clock"):0,1000,generation=>0);
        var sample=safe.Measure("work",0);failClock=true;sample.Dispose();Check(true,"diagnostic sampling failure cannot escape disposal");
        return count;
    }
}
