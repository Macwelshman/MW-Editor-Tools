namespace MW.EditorTools.Modules.BulldozerRecovery {
    internal static class BikeRackMoveEligibility {
        internal static bool Supports(string name,bool bound,int count,bool spawn,bool takeoff,bool access,bool route,bool lanes)=>
            name=="BikeRack01"&&bound&&count==1&&(spawn||lanes)&&!takeoff&&!access&&!route;
        internal static bool Dispatch(bool spawnAccess,bool bikeRack)=>spawnAccess||bikeRack;
    }
}
