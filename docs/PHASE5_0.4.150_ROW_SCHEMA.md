# Object Info copy schema correction — 0.4.150

VERIFIED-RUNTIME: 0.4.149 captures exact hovered labels FenceIndustrialPieceLow02 and GreenBushCity03; ResolveRows then throws the expected-reference exception. Browser hover transport is working.

VERIFIED-STATIC: read-only Cecil inspection of installed Game.dll confirms ObjectSubAreaInfo.m_AreaPrefab and ObjectSubNetInfo.m_NetPrefab. Previous m_Area/m_Net names were incorrect. All five reference names now use nameof against the installed game types so compilation validates their existence. ObjectSubObjectInfo.m_Object, EffectSource.EffectSettings.m_Effect and ObjectSubLaneInfo.m_LanePrefab are retained. Error messages identify the exact unsupported field rather than hiding it.

Native browser-copy success remains pending acceptance. Existing hover identity ambiguity checks and viewport copying are retained.
