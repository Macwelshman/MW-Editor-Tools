using System;

namespace MW.EditorTools.History {
    // One completed deletion. Native completion must succeed before consuming it.
    internal sealed class LastDeletion<T> where T : class {
        private T value;
        public int UndoCount => value == null ? 0 : 1;
        public int RedoCount => 0;
        public T Latest => value;
        public T NextRedo => null;
        public T[] Recent => value == null ? Array.Empty<T>() : new[] { value };
        public T[] All => Recent;
        public void SetDepth(int ignored) { }
        public void Clear() { value = null; }
        public void Record(T item) { value = item ?? throw new ArgumentNullException(nameof(item)); }
        public bool Undo(Func<T,bool> apply) {
            var pending=value;
            if (pending==null || !apply(pending)) return false;
            if (!ReferenceEquals(value,pending)) return false;
            value=null;
            return true;
        }
        public bool Redo(Func<T,bool> apply) => false;
    }
}
