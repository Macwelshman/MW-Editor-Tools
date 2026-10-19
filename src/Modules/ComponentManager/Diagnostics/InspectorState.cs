using System;

namespace MW.EditorTools.Modules.ComponentManager.Diagnostics
{
    [Serializable]
    public sealed class InspectorState
    {
        public bool available;
        public bool visible;
        public bool hasAsset;
        public string assetName = "";
        public string prefabType = "";
        public string selectionKind = "";
        public string status = "Select an asset in Object Info.";
        public string selectionToken = "";
        public string clipboardSummary = "Clipboard empty.";
        public string copyResult = "";
        public int[] selectedIndices = new int[0];
        public string transferMode = "ComponentsAndSettings";
        public string previewSummary = "Choose a mode and preview the destination.";
        public bool canApply, canRestore;
        public ComponentRow[] components = new ComponentRow[0];
    }

    [Serializable]
    public sealed class ComponentRow
    {
        public int index;
        public string typeName;
        public bool active;
        public bool valid;
    }
}
