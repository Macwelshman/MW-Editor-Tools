# Object Info hover capture — 0.4.149

User clarification: the failing browser is the current asset's Object Info list. Viewport copy passes.

HYPOTHESIS: pointer-coordinate lookup and innerText do not expose the hovered native row reliably. Capture now reads the deepest CSS :hover element and textContent, retaining bounded parent/label/title samples and unique prefab/type matching. Native Ctrl+C logs candidate age and bounded samples, including empty samples, so failures distinguish transport, DOM and identity matching. Native acceptance remains UNPROVEN. Release/UI compilation and shared-tab regression checked.
