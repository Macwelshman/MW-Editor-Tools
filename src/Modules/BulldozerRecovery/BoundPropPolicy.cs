namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class BoundPropPolicy {
        internal static bool Accept(int[] parents,int[] depths,object[] prefabs,bool[] deterministicLinks){
            if(parents?.Length==1)return depths?.Length==1&&prefabs?.Length==1&&deterministicLinks?.Length==1&&parents[0]==-1&&depths[0]==0&&prefabs[0]!=null&&deterministicLinks[0];
            return NestedPropPolicy.Accept(parents,depths,prefabs,deterministicLinks);
        }
    }
}
