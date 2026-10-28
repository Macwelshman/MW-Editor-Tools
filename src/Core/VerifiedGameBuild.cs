using System;

namespace MW.EditorTools
{
    // Exact profiles, not independent allowlists: never accept a mixed installation.
    // Selected IL, private fields and dependency contracts are recorded in
    // docs/evidence/performance-test-20261028. Unknown builds remain disabled.
    internal static class VerifiedGameBuild
    {
        internal static readonly Guid StableGame = new Guid("e15551e9-3dea-4855-8dde-1da3cea7d7ef");
        internal static readonly Guid PerformanceGame = new Guid("e8325128-556a-460d-a950-497d90b3627c");
        private static readonly Guid StableEntities = new Guid("3d6b23ae-b281-44a6-a5f4-cbd5fd3d75e6");
        private static readonly Guid PerformanceEntities = new Guid("17879230-8daa-4d82-a555-54adcb15b372");
        private static readonly Guid StableCore = new Guid("b5ff7e4e-51b0-4c1f-a177-2afdfc22906b");
        private static readonly Guid PerformanceCore = new Guid("f90c5161-26b4-48fb-85ee-6833d6a86540");
        private static readonly Guid StableCollections = new Guid("0e5688e6-8747-4130-b712-7b3c25780e8b");
        private static readonly Guid PerformanceCollections = new Guid("f4863c05-b364-48ed-a003-9c869d32c740");
        private static readonly Guid StableUnityCollections = new Guid("f4ddbc30-cb47-4061-9107-4a184ca0481e");
        private static readonly Guid PerformanceUnityCollections = new Guid("249ce684-a5c2-4e09-bd3b-cb6d8d7a862e");

        internal static bool Game(Guid game) => game == StableGame || game == PerformanceGame;
        internal static bool Recovery(Guid game, Guid entities) =>
            game == StableGame && entities == StableEntities ||
            game == PerformanceGame && entities == PerformanceEntities;
        internal static bool Outlines(Guid game, Guid core) =>
            game == StableGame && core == StableCore ||
            game == PerformanceGame && core == PerformanceCore;
        internal static bool AreaCollections(Guid game, Guid collections, Guid unityCollections) =>
            game == StableGame && collections == StableCollections && unityCollections == StableUnityCollections ||
            game == PerformanceGame && collections == PerformanceCollections && unityCollections == PerformanceUnityCollections;
    }
}
