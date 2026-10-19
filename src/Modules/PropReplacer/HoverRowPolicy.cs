using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace MW.EditorTools.Modules.PropReplacer
{
    internal static class HoverRowPolicy
    {
        // Samples are ordered from the hovered label towards its ancestors.
        // Never infer one occurrence from a prefab name shared by several rows.
        internal static int Resolve(string[] samples,int[] indices,string[] names)
        {
            if(samples==null||indices==null||names==null||indices.Length!=names.Length)return -1;
            var candidates=Enumerable.Range(0,names.Length).Where(i=>names[i]!=null&&samples.Any(s=>string.Equals(s?.Trim(),names[i],StringComparison.Ordinal))).ToArray();
            foreach(var sample in samples.Where(s=>s!=null))
            {
                var numbers=Regex.Matches(sample,@"Element\s+(\d+)");
                if(numbers.Count!=1)continue;
                int index;if(!int.TryParse(numbers[0].Groups[1].Value,out index))continue;
                var exact=candidates.Where(i=>indices[i]==index&&sample.Contains(names[i])).ToArray();
                if(exact.Length==1)return exact[0];
                // A row-number-only label may be paired with one exact prefab label.
                if(sample.Trim()==numbers[0].Value&&candidates.Length>0){exact=candidates.Where(i=>indices[i]==index).ToArray();if(exact.Length==1)return exact[0];}
            }
            return candidates.Length==1?candidates[0]:-1;
        }
    }
}
