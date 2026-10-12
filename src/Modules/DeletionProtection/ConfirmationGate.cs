namespace MW.EditorTools.Modules.DeletionProtection
{
    // Only tokens and a deadline live here. Cancellation has no mutation callback.
    internal sealed class ConfirmationGate
    {
        private long sequence;
        private long pending;
        private double deadline;

        public bool Busy => pending != 0;

        public long Begin(double now)
        {
            if (Busy) return 0;
            pending = ++sequence;
            deadline = now + 60;
            return pending;
        }

        public bool Consume(long token, bool confirmed, double now)
        {
            if (token == 0 || token != pending) return false;
            pending = 0;
            return confirmed && now < deadline;
        }

        public void Invalidate() { pending = 0; }
        public bool Expired(double now) => Busy && now >= deadline;
    }
}
