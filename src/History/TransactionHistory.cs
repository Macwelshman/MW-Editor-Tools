using System;
using System.Collections.Generic;

namespace MW.EditorTools.History
{
    internal sealed class TransactionHistory<T>
    {
        private readonly List<T> undo = new List<T>();
        private readonly List<T> redo = new List<T>();
        private int depth;
        public TransactionHistory(int depth) { if (depth < 1) throw new ArgumentOutOfRangeException(); this.depth = depth; }
        public int UndoCount => undo.Count;
        public int RedoCount => redo.Count;
        public T Latest => undo.Count == 0 ? default(T) : undo[undo.Count - 1];
        public T NextRedo => redo.Count == 0 ? default(T) : redo[redo.Count - 1];
        public T[] Recent => undo.ToArray();
        public T[] All { get {var items=new List<T>(undo);items.AddRange(redo);return items.ToArray();} }
        public void SetDepth(int value)
        {
            if (value < 1) throw new ArgumentOutOfRangeException();
            depth = value;
            if (undo.Count > depth) undo.RemoveRange(0, undo.Count - depth);
            if (redo.Count > depth) redo.RemoveRange(0, redo.Count - depth);
        }
        public void Clear() { undo.Clear(); redo.Clear(); }
        public void Record(T item)
        {
            redo.Clear(); undo.Add(item);
            if (undo.Count > depth) undo.RemoveAt(0);
        }
        public bool Undo(Func<T, bool> apply) => Move(undo, redo, apply);
        public bool Redo(Func<T, bool> apply) => Move(redo, undo, apply);
        private static bool Move(List<T> from, List<T> to, Func<T, bool> apply)
        {
            if (from.Count == 0) return false;
            var item = from[from.Count - 1];
            if (!apply(item)) return false; // Failure leaves the history cursor unchanged.
            from.RemoveAt(from.Count - 1); to.Add(item); return true;
        }
    }
}
