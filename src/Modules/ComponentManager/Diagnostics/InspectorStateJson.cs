using System.Globalization;
using System.Text;

namespace MW.EditorTools.Modules.ComponentManager.Diagnostics
{
    // Only the inspector's scalar snapshot is encoded; never runtime game objects.
    internal static class InspectorStateJson
    {
        public static string Serialize(InspectorState state)
        {
            var json = new StringBuilder("{");
            json.Append("\"available\":").Append(state.available ? "true" : "false");
            json.Append(",\"visible\":").Append(state.visible ? "true" : "false");
            json.Append(",\"hasAsset\":").Append(state.hasAsset ? "true" : "false");
            json.Append(",\"assetName\":").Append(Quote(state.assetName));
            json.Append(",\"prefabType\":").Append(Quote(state.prefabType));
            json.Append(",\"selectionKind\":").Append(Quote(state.selectionKind));
            json.Append(",\"status\":").Append(Quote(state.status));
            json.Append(",\"selectionToken\":").Append(Quote(state.selectionToken));
            json.Append(",\"clipboardSummary\":").Append(Quote(state.clipboardSummary));
            json.Append(",\"copyResult\":").Append(Quote(state.copyResult));
            json.Append(",\"transferMode\":").Append(Quote(state.transferMode));
            json.Append(",\"previewSummary\":").Append(Quote(state.previewSummary));
            json.Append(",\"canApply\":").Append(state.canApply ? "true" : "false");
            json.Append(",\"canRestore\":").Append(state.canRestore ? "true" : "false");
            json.Append(",\"selectedIndices\":[");
            for (int i = 0; i < state.selectedIndices.Length; i++)
            {
                if (i != 0) json.Append(',');
                json.Append(state.selectedIndices[i].ToString(CultureInfo.InvariantCulture));
            }
            json.Append(']');
            json.Append(",\"components\":[");
            for (int i = 0; i < state.components.Length; i++)
            {
                if (i != 0) json.Append(',');
                var row = state.components[i];
                json.Append("{\"index\":").Append(row.index.ToString(CultureInfo.InvariantCulture));
                json.Append(",\"typeName\":").Append(Quote(row.typeName));
                json.Append(",\"sectionId\":").Append(Quote(row.sectionId));
                json.Append(",\"fields\":[");
                for(int f=0;f<row.fields.Length;f++){if(f!=0)json.Append(',');json.Append(Quote(row.fields[f]));}json.Append(']');
                json.Append(",\"active\":").Append(row.active ? "true" : "false");
                json.Append(",\"valid\":").Append(row.valid ? "true" : "false").Append('}');
            }
            return json.Append("]}").ToString();
        }

        private static string Quote(string value)
        {
            var json = new StringBuilder("\"");
            foreach (char c in value ?? "")
            {
                if (c == '"' || c == '\\') json.Append('\\').Append(c);
                else if (c < 32 || char.IsSurrogate(c))
                    json.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                else json.Append(c);
            }
            return json.Append('"').ToString();
        }
    }
}
