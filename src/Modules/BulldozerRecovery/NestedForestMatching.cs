using System.Linq;
namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal sealed partial class BulldozerTrace {
        private static NestedNode[][] MatchForest(NestedNode[][] expected,NestedNode[][] actual){
            if(actual.Length!=expected.Length)return null;
            var ordered=new NestedNode[expected.Length][];var used=new bool[actual.Length];
            for(int g=0;g<expected.Length;g++){
                var matches=Enumerable.Range(0,actual.Length).Where(i=>!used[i]&&SameNestedPlacement(expected[g][0],actual[i][0])).ToArray();
                if(matches.Length!=1)return null;used[matches[0]]=true;ordered[g]=actual[matches[0]];
            }
            return ordered;
        }
    }
}
