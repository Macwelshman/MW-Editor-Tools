namespace MW.EditorTools.Services
{
    internal static class ShortcutPolicy
    {
        // -1 means no action, 0 Undo, 1 Redo. Exact modifiers and typing gates are required.
        internal static int Resolve(bool enabled, bool available, bool typing, bool dialog, bool repeat,
            bool ctrl, bool shift, bool alt, bool meta, bool undoPressed, bool redoPressed,
            int undoModifiers = 1, int redoModifiers = 3)
        {
            if (!enabled || !available || typing || dialog || repeat) return -1;
            int actual = (ctrl ? 1 : 0) | (shift ? 2 : 0) | (alt ? 4 : 0) | (meta ? 8 : 0);
            bool undo = undoPressed && actual == undoModifiers;
            bool redo = redoPressed && actual == redoModifiers;
            if (undo == redo) return -1; // Identical shortcuts never perform an arbitrary action.
            return undo ? 0 : 1;
        }
    }
}
