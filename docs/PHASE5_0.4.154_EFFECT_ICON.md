# Effect icon target resolution — 0.4.154

VERIFIED-RUNTIME (user report): nested-prop viewport paste and replace-all pass on 0.4.153; effects fail. Log shows effect attempts resolve ownerless hit entities and stop before writes.

VERIFIED-STATIC: read-only Cecil inspection of installed Game.dll, DefaultToolSystem.SelectEntityJob.Execute, IL_0139–01ec, confirms native selection follows Target.m_Target for an ownerless Notifications.Icon, then Temp.m_Original if necessary. The existing clipboard reader omitted that native marker indirection. Effect hits using this icon path are a HYPOTHESIS until runtime retest.

Viewport copy/paste now follows that same bounded explicit icon Target/original link after validating the raw hit against the hover cache. Missing/deleted/temporary final targets refuse. Existing exact effect-source placement and owner checks remain mandatory; there is no nearest-effect search. Unowned-hit component types are logged to distinguish any other marker representation.

Release/UI builds and 14 existing target/path policy assertions pass. Native effect copy/paste/replace-all remains UNPROVEN. Nested-prop source matching and regeneration are unchanged.
