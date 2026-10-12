using System;
namespace MW.EditorTools.Modules.BulldozerRecovery
{
    // Tracks a single native action. No restoration or entity writes are performed.
    internal sealed class BulldozerTraceSession<T> where T : class
    {
        internal T Asset { get; private set; }
        internal int Started { get; private set; }
        internal bool Accepted { get; private set; }
        internal bool Synchronized { get; private set; }
        internal bool InApply { get; private set; }
        internal void Begin(T asset, int frame) { Reset(); Asset=asset; Started=frame; InApply=asset!=null; }
        internal void AllowApply(bool result) { if (InApply) Accepted=result; }
        internal void EndApply() { InApply=false; if (!Accepted) Reset(); }
        internal bool Matches(T asset) => Asset!=null && ReferenceEquals(Asset,asset);
        internal bool CanSynchronize(T asset) => Accepted && !InApply && !Synchronized && Matches(asset);
        internal void Synchronize(T asset) { if(CanSynchronize(asset)) Synchronized=true; }
        internal bool Expired(int frame) => Asset!=null && frame-Started>120;
        internal void Reset() { Asset=null; Started=0; Accepted=Synchronized=InApply=false; }
    }
}

