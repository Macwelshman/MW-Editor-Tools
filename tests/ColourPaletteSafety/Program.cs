using System;
using MW.EditorTools.Modules.ColourVariations;
class Tests
{
    static int count;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; }
    static void Reject(Action action, string name)
    {
        try { action(); } catch (ArgumentException) { count++; return; }
        throw new Exception("Expected rejection: " + name);
    }
    static void Main()
    {
        var identity = new[] { 0, 1, 2 };
        var rows = Palette.Generate("#FF0000 #FFFFFF\n#00FF00 #EEEEEE\n#0000FF #DDDDDD", "#000000\n#112233\n#445566\n#778899\n#AABBCC", identity);
        Check(rows.Count == 15, "Cartesian product");
        Check(rows[0][0].Equals(rows[4][0]) && rows[0][1].Equals(rows[4][1]), "Paired Channel 0 and Channel 1");
        Check(!rows[0][2].Equals(rows[4][2]), "Independent Channel 2");
        Check(!object.ReferenceEquals(rows[0], rows[1]), "Independent arrays");
        Check(Rgba.Parse("#12345680").Hex() == "#12345680", "Alpha roundtrip");
        Check(Rgba.Parse("123456").A == 1f, "Opaque default");
        var remapped = Palette.Generate("#FF0000 #00FF00", "#0000FF", new[] { 2, 0, 1 });
        Check(remapped[0][2].R == 1 && remapped[0][0].G == 1 && remapped[0][1].B == 1, "Binding remap");
        Reject(delegate { Palette.Generate("#FF0000 #00FF00", "#0000FF", new[] { 0, 0, 2 }); }, "Aliasing conflict");
        Check(Palette.Generate("#FF0000 #FF0000", "#0000FF", new[] { 0, 0, 2 }).Count == 1, "Compatible aliasing");
        Reject(delegate { Palette.Generate("", "#000000", identity); }, "Empty schemes");
        Reject(delegate { Palette.Generate("#FFFFFF", "#000000", identity); }, "Missing Channel 1");
        Reject(delegate { Rgba.Parse("#GGGGGG"); }, "Invalid hex");
        Reject(delegate { Palette.Generate("#FFFFFF #FFFFFF", "#000000", new[] { 0, 1, 3 }); }, "Invalid binding");
        Reject(delegate { Palette.Generate("#FFFFFF #FFFFFF", string.Join("\n", new string[2049]).Replace("\n", "#000000\n") + "#000000", identity); }, "Size limit");
        int invalidations=0,blocks=0;
        Action invalidate=()=>invalidations++;
        foreach(var action in new[]{"open","close","capture","target","copy","cancel","load","group","preview","page"}){
            ColourWriteGate.Before(action,()=>{blocks++;return "busy";},invalidate);
            Check(blocks==0&&invalidations==0,"read-only palette commands preserve recovery history: "+action);
        }
        foreach(var action in new[]{"apply","paste","confirm","undo","reset"}) {
            bool refused=false;try{ColourWriteGate.Before(action,()=>"Saving",invalidate);}catch(InvalidOperationException){refused=true;}
            Check(refused&&invalidations==0,"busy native mutation blocked before invalidation: "+action);
        }
        foreach(var action in new[]{"apply","paste","confirm","undo","reset"})ColourWriteGate.Before(action,()=>null,invalidate);
        Check(invalidations==5,"each successful colour mutation invalidates unrelated recovery history once");
        Console.WriteLine("PASS: " + count + " palette checks");
    }
}

