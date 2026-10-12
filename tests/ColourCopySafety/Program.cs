using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Game.Prefabs;
using Game.Rendering;
using Unity.Mathematics;
using UnityEngine;
using MW.EditorTools.Modules.ColourVariations;

class CopyTests
{
    static int checks;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; }
    static void Main(string[] args)
    {
        if(args.Length==0)args=new[]{@"C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed"};
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs e) {
            string name = new AssemblyName(e.Name).Name + ".dll";
            foreach (string dir in args) {
                string path = Path.Combine(dir, name);
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        Run();
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run()
    {
        var source = new PaletteWindow.PropertiesCopy {
            active = false, externalSource = ColorSourceType.Parent,
            variationRanges = new int3(1,2,3), alphaRanges = new int3(4,5,6),
            variations = new List<ColorProperties.VariationSet> { new ColorProperties.VariationSet {
                m_VariationGroup = "A", m_Colors = new[] { new Color(.1234567f,.2f,.3f,.4f), Color.white, Color.black } } },
            bindings = new List<ColorProperties.ColorChannelBinding> { new ColorProperties.ColorChannelBinding { m_ChannelId = 2, m_CanBeModifiedByExternal = true } },
            groups = new List<ColorProperties.VariationGroup> { new ColorProperties.VariationGroup {
                m_Name = "A", m_Probability = 37, m_MeshSyncMode = ColorSyncFlags.SameIndex,
                m_OverrideRandomness = true, m_VariationRanges = new int3(7,8,9), m_AlphaRanges = new int3(10,11,12) } }
        };
        var clipboard = source.DeepClone();
        var destination = clipboard.DeepClone();
        var second = clipboard.DeepClone();
        Check(!destination.active, "active flag");
        Check(destination.externalSource == ColorSourceType.Parent, "external source");
        Check(destination.variationRanges.Equals(source.variationRanges) && destination.alphaRanges.Equals(source.alphaRanges), "global ranges");
        Check(destination.variations[0].m_Colors[0].r == .1234567f && destination.variations[0].m_Colors[0].a == .4f, "full float and alpha precision");
        Check(destination.variations[0].m_VariationGroup == "A", "group assignment");
        Check(destination.bindings[0].m_ChannelId == 2 && destination.bindings[0].m_CanBeModifiedByExternal, "binding fields");
        var group = destination.groups[0];
        Check(group.m_Name == "A" && group.m_Probability == 37 && group.m_MeshSyncMode == ColorSyncFlags.SameIndex && group.m_OverrideRandomness, "group settings");
        Check(group.m_VariationRanges.Equals(new int3(7,8,9)) && group.m_AlphaRanges.Equals(new int3(10,11,12)), "group ranges");
        source.variations[0].m_Colors[0] = Color.black;
        source.groups[0].m_Name = "changed";
        source.bindings[0].m_ChannelId = 0;
        Check(clipboard.variations[0].m_Colors[0].r == .1234567f && clipboard.groups[0].m_Name == "A" && clipboard.bindings[0].m_ChannelId == 2, "source changes isolated");
        destination.variations[0].m_Colors[0] = Color.white;
        destination.groups[0].m_Probability = 99;
        destination.bindings[0].m_ChannelId = 1;
        Check(second.variations[0].m_Colors[0].r == .1234567f && second.groups[0].m_Probability == 37 && second.bindings[0].m_ChannelId == 2, "destinations independent");
        Check(clipboard.variations[0].m_Colors[0].r == .1234567f && clipboard.groups[0].m_Probability == 37, "clipboard unchanged by paste edit");
        var empty = new PaletteWindow.PropertiesCopy().DeepClone();
        Check(empty.groups == null && empty.bindings == null && empty.variations == null, "null fields preserved");
        var previewRows = new List<ColorProperties.VariationSet>();
        for (int i = 0; i < 4; i++) previewRows.Add(new ColorProperties.VariationSet {
            m_Colors = new[] { Color.red, Color.green, new Color(0, 0, 1, .5f) }, m_VariationGroup = null });
        var transport = typeof(PaletteWindow).GetMethod("PreviewRows", BindingFlags.Static | BindingFlags.NonPublic);
        var firstPage = (string[])transport.Invoke(null, new object[] { previewRows, new[] { 0, 1, 2 }, 0 });
        Check(firstPage.Length == 3, "preview transport is bounded to one page");
        Check(firstPage[0] == "1|#FF0000FF #00FF00FF #0000FF80", "preview transport preserves channels and alpha");
        var lastPage = (string[])transport.Invoke(null, new object[] { previewRows, new[] { 2, 0, 1 }, 3 });
        Check(lastPage.Length == 1 && lastPage[0] == "4|#0000FF80 #FF0000FF #00FF00FF", "preview final page preserves row number and channel bindings");
        var emptyPage = (string[])transport.Invoke(null, new object[] { new List<ColorProperties.VariationSet>(), new[] { 0, 1, 2 }, 0 });
        Check(emptyPage.Length == 0, "empty preview page");
        for (int i = previewRows.Count; i < Palette.Limit; i++) previewRows.Add(previewRows[0]);
        var fullPage = (string[])transport.Invoke(null, new object[] { previewRows, new[] { 0, 1, 2 }, 2046 });
        Check(fullPage.Length == 2 && fullPage[1].StartsWith("2048|"), "maximum preview stays bounded and reaches final variation");
        Console.WriteLine("PASS: " + checks + " Color Properties copy and preview transport checks");
    }
}

