# Prop Replacer

Version 0.4.144 adds an Editor-session clipboard for supported Object, Area, Net, Effect and Lane list rows. Copy records only the row's referenced prefab. Replace selected changes only that reference, preserving all destination row fields. Batch replace changes every row in the same owned list whose reference matches the selected destination row.

Writes require a selected row belonging to the current writable inspector asset. Source and destination row types must match. Operations clear the older Undo slot, snapshot the complete list before mutation, queue the native prefab refresh and dirty notification, and attempt an exact list rollback on synchronous failure.

Viewport replacement is deferred.
