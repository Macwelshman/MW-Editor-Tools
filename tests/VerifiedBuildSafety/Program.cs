using System;
using MW.EditorTools;

class Program
{
    static int checks;
    static void Check(bool result, string message) { if (!result) throw new Exception(message); checks++; }
    static Guid Id(string value) => new Guid(value);
    static void Main()
    {
        var games = new[] { Id("e15551e9-3dea-4855-8dde-1da3cea7d7ef"), Id("e8325128-556a-460d-a950-497d90b3627c"), Guid.Empty };
        var entities = new[] { Id("3d6b23ae-b281-44a6-a5f4-cbd5fd3d75e6"), Id("17879230-8daa-4d82-a555-54adcb15b372"), Guid.Empty };
        var cores = new[] { Id("b5ff7e4e-51b0-4c1f-a177-2afdfc22906b"), Id("f90c5161-26b4-48fb-85ee-6833d6a86540"), Guid.Empty };
        var collections = new[] { Id("0e5688e6-8747-4130-b712-7b3c25780e8b"), Id("f4863c05-b364-48ed-a003-9c869d32c740"), Guid.Empty };
        var unity = new[] { Id("f4ddbc30-cb47-4061-9107-4a184ca0481e"), Id("249ce684-a5c2-4e09-bd3b-cb6d8d7a862e"), Guid.Empty };
        for (int g = 0; g < games.Length; g++)
        {
            Check(VerifiedGameBuild.Game(games[g]) == (g < 2), "known Game builds only");
            for (int d = 0; d < 3; d++)
            {
                Check(VerifiedGameBuild.Recovery(games[g], entities[d]) == (g < 2 && g == d), "Undo rejects mismatched or unknown ECS");
                Check(VerifiedGameBuild.Outlines(games[g], cores[d]) == (g < 2 && g == d), "outlines reject mismatched or unknown rendering core");
                for (int u = 0; u < 3; u++)
                    Check(VerifiedGameBuild.AreaCollections(games[g], collections[d], unity[u]) == (g < 2 && g == d && g == u), "allocator rejects mixed collection builds");
            }
        }
        Console.WriteLine("PASS: " + checks + " stable/beta/unknown and mixed-build profile checks.");
    }
}
