using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace MW.EditorTools.Modules.BulldozerRecovery {
    // Observations only. This class has no game state, callbacks that perform
    // recovery, or authority to cache/approve a validation decision.
    internal sealed class RecoveryTiming {
        private sealed class Metric { internal int Count,Frame,FrameCount,MaxFrameCount; internal double Total,Max,FrameTotal,MaxFrameTotal; }
        private readonly Func<long> clock;
        private readonly Func<int,int> collections;
        private readonly double frequency;
        private readonly Dictionary<string,Metric> metrics=new Dictionary<string,Metric>(StringComparer.Ordinal);
        private readonly HashSet<string> mismatches=new HashSet<string>(StringComparer.Ordinal);
        private readonly int[] initialCollections;
        private int[] undoCollections;
        private long undoStarted;
        private bool undo;
        private int undoFrame;
        private float previousFrameMax;
        internal int RefusedSamples { get; private set; }

        internal RecoveryTiming(Func<long> clock=null,double frequency=0,Func<int,int> collections=null){
            this.clock=clock??Stopwatch.GetTimestamp;
            this.frequency=frequency>0?frequency:Stopwatch.Frequency;
            this.collections=collections??GC.CollectionCount;
            initialCollections=Enumerable.Range(0,3).Select(this.collections).ToArray();
        }
        internal IDisposable Measure(string phase,float previousFrameMs,int frame=0){
            if(!float.IsNaN(previousFrameMs)&&!float.IsInfinity(previousFrameMs)&&previousFrameMs>=0)previousFrameMax=Math.Max(previousFrameMax,previousFrameMs);
            if(!metrics.ContainsKey(phase)){if(metrics.Count>=16)return null;metrics.Add(phase,new Metric());}
            return new Sample(this,phase,clock(),frame);
        }
        private sealed class Sample:IDisposable {
            private RecoveryTiming owner; private readonly string phase; private readonly long start; private readonly int frame;
            internal Sample(RecoveryTiming owner,string phase,long start,int frame){this.owner=owner;this.phase=phase;this.start=start;this.frame=frame;}
            public void Dispose(){var timing=owner;if(timing==null)return;owner=null;try{timing.Record(phase,start,frame);}catch{}}
        }
        private void Record(string phase,long start,int frame){
            double elapsed=Math.Max(0,(clock()-start)*1000.0/frequency);var metric=metrics[phase];
            if(metric.Count==0||metric.Frame!=frame){metric.Frame=frame;metric.FrameCount=0;metric.FrameTotal=0;}
            metric.Count++;metric.Total+=elapsed;metric.Max=Math.Max(metric.Max,elapsed);
            metric.FrameCount++;metric.FrameTotal+=elapsed;metric.MaxFrameCount=Math.Max(metric.MaxFrameCount,metric.FrameCount);metric.MaxFrameTotal=Math.Max(metric.MaxFrameTotal,metric.FrameTotal);
        }
        internal void BeginUndo(int frame){if(undo)return;undo=true;undoFrame=frame;undoStarted=clock();undoCollections=Enumerable.Range(0,3).Select(collections).ToArray();}
        internal bool ObserveMismatch(string reason){
            RefusedSamples++;
            // Bounded first-failure evidence, not a per-frame transcript.
            if(reason==null||mismatches.Count>=3)return false;
            return mismatches.Add(reason.Length>6000?reason.Substring(0,6000):reason);
        }
        internal string Summary(int frame){
            string Number(double n)=>n.ToString("F3",CultureInfo.InvariantCulture);
            var measured=string.Join("; ",metrics.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Key+" count="+p.Value.Count+" totalMs="+Number(p.Value.Total)+" maxMs="+Number(p.Value.Max)+" maxPerFrameMs="+Number(p.Value.MaxFrameTotal)+" maxPerFrameCount="+p.Value.MaxFrameCount));
            return (undo?"undoWallMs="+Number(Math.Max(0,(clock()-undoStarted)*1000.0/frequency))+" undoFrames="+(frame-undoFrame):"undoNotStarted")+
                " previousFrameMaxMs="+Number(previousFrameMax)+" gcSinceCapture="+string.Join("/",Enumerable.Range(0,3).Select(i=>Math.Max(0,collections(i)-initialCollections[i])))+
                (undoCollections==null?"":" gcSinceUndo="+string.Join("/",Enumerable.Range(0,3).Select(i=>Math.Max(0,collections(i)-undoCollections[i]))))+
                " refusedSamples="+RefusedSamples+"; "+measured;
        }
    }
}
