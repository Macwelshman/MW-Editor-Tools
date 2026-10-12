namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class NestedElevationPolicy {
        internal static bool MatchesState(bool presentA,byte stateA,bool presentB,byte stateB)=>presentA==presentB&&(!presentA||stateA==stateB);
        internal static bool Matches(bool presentA,float heightA,byte flagsA,bool presentB,float heightB,byte flagsB)=>presentA==presentB&&(!presentA||(heightA.Equals(heightB)&&flagsA==flagsB));
    }
}
