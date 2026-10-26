using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;
using MW.EditorTools.Modules.ComponentManager.Clipboard;

namespace MW.EditorTools.Modules.ComponentManager.Transfer
{
    internal sealed class ValueField
    {
        public readonly string Name;
        public readonly Action<BinaryWriter, ComponentBase> Write;
        public readonly Action<BinaryReader, ComponentBase> Read;
        public ValueField(string name, Action<BinaryWriter, ComponentBase> write, Action<BinaryReader, ComponentBase> read)
        { Name = name; Write = write; Read = read; }
    }

    internal sealed class ValueAdapter
    {
        public readonly string Id;
        public readonly Type Type;
        public readonly ValueField[] Fields;
        private readonly bool general;
        private readonly Func<ComponentBase, string> validate;
        public ValueAdapter(string id, Type type, Func<ComponentBase, string> validate, params ValueField[] fields)
        { Id = id; Type = type; this.validate = validate; Fields = fields; }
        internal ValueAdapter(Type type){Type=type;Id="Settings:"+type.FullName;Fields=new ValueField[0];general=true;}

        internal string[] CopyFieldNames {
            get {
                if(!general)return Fields.Select(f=>f.Name).ToArray();
                var names=new List<string>();
                for(var t=Type;t!=null&&t!=typeof(ComponentBase);t=t.BaseType)
                    names.AddRange(t.GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.DeclaredOnly).Where(f=>!f.IsNotSerialized&&!f.IsInitOnly).Select(f=>f.Name));
                return names.Where(n=>Type.FullName!="Game.Prefabs.UIObject"||n!="m_Icon").OrderBy(n=>n,StringComparer.Ordinal).ToArray();
            }
        }
        public ValueSnapshot Capture(ComponentBase component, PrefabBase owner)
        {
            if (component.GetType() != Type) throw new InvalidOperationException("Exact adapter type required.");
            if(general)return new ValueSnapshot(Id,owner.name,owner.GetType().FullName,component.active,new Dictionary<string,byte[]>(),ComponentSettingsImage.Capture(component));
            var values = new Dictionary<string, byte[]>();
            foreach (var field in Fields)
                using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
                { field.Write(writer, component); writer.Flush(); values.Add(field.Name, stream.ToArray()); }
            return new ValueSnapshot(Id, owner.name, owner.GetType().FullName, component.active, values);
        }

        public void Apply(ValueSnapshot snapshot, ComponentBase component)
        {
            if(general){if(component.GetType()!=Type||snapshot.AdapterId!=Id||snapshot.Settings==null)throw new InvalidOperationException("Settings schema mismatch.");snapshot.Settings.Apply(component);return;}
            if (component.GetType() != Type || snapshot.AdapterId != Id || !snapshot.Fields.SequenceEqual(Fields.Select(f => f.Name).OrderBy(f => f, StringComparer.Ordinal)))
                throw new InvalidOperationException("Snapshot fields/schema do not match the adapter.");
            foreach (var field in Fields)
                using (var stream = new MemoryStream(snapshot.Get(field.Name))) using (var reader = new BinaryReader(stream))
                { field.Read(reader, component); if (stream.Position != stream.Length) throw new InvalidDataException("Trailing field data."); }
        }

        public string Validate(ValueSnapshot snapshot)
        {
            if(general)return snapshot.Settings!=null&&snapshot.Settings.ReferencesValid?null:"A copied prefab reference is unavailable.";
            var draft = (ComponentBase)ScriptableObject.CreateInstance(Type);
            try { Apply(snapshot, draft); return validate(draft); }
            finally { UnityEngine.Object.Destroy(draft); }
        }
    }

    internal static class Adapters
    {
        public static readonly ValueAdapter[] All = { Colors(), Terraform() };
        public static ValueAdapter For(Type type) { return All.FirstOrDefault(a => a.Type == type) ?? (type.Assembly==typeof(ComponentBase).Assembly&&typeof(ComponentBase).IsAssignableFrom(type)&&!typeof(PrefabBase).IsAssignableFrom(type)&&!type.IsAbstract?new ValueAdapter(type):null); }
        public static ValueAdapter For(string id) { var typed=All.FirstOrDefault(a=>a.Id==id);if(typed!=null)return typed;if(!id.StartsWith("Settings:",StringComparison.Ordinal))return null;var type=typeof(ComponentBase).Assembly.GetType(id.Substring(9),false);return type==null?null:For(type); }

        private static ValueField Field<T>(string name, Action<BinaryWriter, T> write, Action<BinaryReader, T> read) where T : ComponentBase
        { return new ValueField(name, (w, c) => write(w, (T)c), (r, c) => read(r, (T)c)); }

        private static ValueAdapter Colors()
        {
            return new ValueAdapter("ColorProperties.v1", typeof(ColorProperties),
                c => ColorPropertiesTransfer.Validate(new ColorPropertiesSnapshot((ColorProperties)c, "Draft", "Draft")),
                Field<ColorProperties>("Variations", (w, c) => WriteList(w, c.m_ColorVariations, (o, v) => {
                    WriteList(o, v.m_Colors, (p, color) => { p.Write(color.r); p.Write(color.g); p.Write(color.b); p.Write(color.a); }); Text(o, v.m_VariationGroup);
                }), (r, c) => c.m_ColorVariations = ReadList(r, o => new ColorProperties.VariationSet {
                    m_Colors = ToArray(ReadList(o, p => new Color(p.ReadSingle(), p.ReadSingle(), p.ReadSingle(), p.ReadSingle()))), m_VariationGroup = Text(o) })),
                Field<ColorProperties>("Channel bindings", (w, c) => WriteList(w, c.m_ChannelsBinding, (o, b) => { o.Write(b.m_ChannelId); o.Write(b.m_CanBeModifiedByExternal); }),
                    (r, c) => c.m_ChannelsBinding = ReadList(r, o => new ColorProperties.ColorChannelBinding { m_ChannelId = o.ReadSByte(), m_CanBeModifiedByExternal = o.ReadBoolean() })),
                Field<ColorProperties>("Groups", (w, c) => WriteList(w, c.m_VariationGroups, (o, g) => {
                    Text(o, g.m_Name); o.Write(g.m_Probability); o.Write((int)g.m_MeshSyncMode); o.Write(g.m_OverrideRandomness); Int3(o, g.m_VariationRanges); Int3(o, g.m_AlphaRanges);
                }), (r, c) => c.m_VariationGroups = ReadList(r, o => new ColorProperties.VariationGroup {
                    m_Name = Text(o), m_Probability = o.ReadInt32(), m_MeshSyncMode = (Game.Rendering.ColorSyncFlags)o.ReadInt32(),
                    m_OverrideRandomness = o.ReadBoolean(), m_VariationRanges = Int3(o), m_AlphaRanges = Int3(o) })),
                Field<ColorProperties>("Variation ranges", (w, c) => Int3(w, c.m_VariationRanges), (r, c) => c.m_VariationRanges = Int3(r)),
                Field<ColorProperties>("Alpha ranges", (w, c) => Int3(w, c.m_AlphaRanges), (r, c) => c.m_AlphaRanges = Int3(r)),
                Field<ColorProperties>("External source", (w, c) => w.Write((int)c.m_ExternalColorSource), (r, c) => c.m_ExternalColorSource = (Game.Rendering.ColorSourceType)r.ReadInt32()));
        }

        private static ValueAdapter Terraform()
        {
            return new ValueAdapter("BuildingTerraformOverride.v1", typeof(BuildingTerraformOverride), ValidateTerraform,
                Field<BuildingTerraformOverride>("Level min offset", (w,c)=> Float2(w,c.m_LevelMinOffset),(r,c)=>c.m_LevelMinOffset=Float2(r)),
                Field<BuildingTerraformOverride>("Level max offset", (w,c)=> Float2(w,c.m_LevelMaxOffset),(r,c)=>c.m_LevelMaxOffset=Float2(r)),
                Field<BuildingTerraformOverride>("Level front left", (w,c)=> Float2(w,c.m_LevelFrontLeft),(r,c)=>c.m_LevelFrontLeft=Float2(r)),
                Field<BuildingTerraformOverride>("Level front right", (w,c)=> Float2(w,c.m_LevelFrontRight),(r,c)=>c.m_LevelFrontRight=Float2(r)),
                Field<BuildingTerraformOverride>("Level back left", (w,c)=> Float2(w,c.m_LevelBackLeft),(r,c)=>c.m_LevelBackLeft=Float2(r)),
                Field<BuildingTerraformOverride>("Level back right", (w,c)=> Float2(w,c.m_LevelBackRight),(r,c)=>c.m_LevelBackRight=Float2(r)),
                Field<BuildingTerraformOverride>("Smooth min offset", (w,c)=> Float2(w,c.m_SmoothMinOffset),(r,c)=>c.m_SmoothMinOffset=Float2(r)),
                Field<BuildingTerraformOverride>("Smooth max offset", (w,c)=> Float2(w,c.m_SmoothMaxOffset),(r,c)=>c.m_SmoothMaxOffset=Float2(r)),
                Field<BuildingTerraformOverride>("Height offset", (w,c)=>w.Write(c.m_HeightOffset),(r,c)=>c.m_HeightOffset=r.ReadSingle()),
                Field<BuildingTerraformOverride>("Don't raise", (w,c)=>w.Write(c.m_DontRaise),(r,c)=>c.m_DontRaise=r.ReadBoolean()),
                Field<BuildingTerraformOverride>("Don't lower", (w,c)=>w.Write(c.m_DontLower),(r,c)=>c.m_DontLower=r.ReadBoolean()),
                Field<BuildingTerraformOverride>("Additional smooth areas", (w,c)=>WriteList(w,c.m_AdditionalSmoothAreas,(o,a)=>{
                    Float2(o,a.m_Area.min);Float2(o,a.m_Area.max);o.Write(a.m_HeightOffset);o.Write(a.m_Circular);o.Write(a.m_DontRaise);o.Write(a.m_DontLower);
                }),(r,c)=>c.m_AdditionalSmoothAreas=ToArray(ReadList(r,o=>new BuildingTerraformOverride.SubLot {
                    m_Area=new Colossal.Mathematics.Bounds2(Float2(o),Float2(o)),m_HeightOffset=o.ReadSingle(),m_Circular=o.ReadBoolean(),m_DontRaise=o.ReadBoolean(),m_DontLower=o.ReadBoolean() }))));
        }

        private static string ValidateTerraform(ComponentBase component)
        {
            var c=(BuildingTerraformOverride)component;
            foreach(var v in new[]{c.m_LevelMinOffset,c.m_LevelMaxOffset,c.m_LevelFrontLeft,c.m_LevelFrontRight,c.m_LevelBackLeft,c.m_LevelBackRight,c.m_SmoothMinOffset,c.m_SmoothMaxOffset})
                if(!Finite(v.x)||!Finite(v.y)) return "Terraform offsets must be finite.";
            if(!Finite(c.m_HeightOffset))return "Height offset must be finite.";
            if(c.m_AdditionalSmoothAreas!=null)foreach(var a in c.m_AdditionalSmoothAreas)
                if(a==null||!Finite(a.m_HeightOffset)||!Finite(a.m_Area.min.x)||!Finite(a.m_Area.min.y)||!Finite(a.m_Area.max.x)||!Finite(a.m_Area.max.y)
                    ||a.m_Area.min.x>a.m_Area.max.x||a.m_Area.min.y>a.m_Area.max.y)return "Invalid additional smooth area bounds/height.";
            return null;
        }

        private static bool Finite(float v){return !float.IsNaN(v)&&!float.IsInfinity(v);}
        private static T[] ToArray<T>(List<T> list){return list==null?null:list.ToArray();}
        private static void Int3(BinaryWriter w,int3 v){w.Write(v.x);w.Write(v.y);w.Write(v.z);}
        private static int3 Int3(BinaryReader r){return new int3(r.ReadInt32(),r.ReadInt32(),r.ReadInt32());}
        private static void Float2(BinaryWriter w,float2 v){w.Write(v.x);w.Write(v.y);}
        private static float2 Float2(BinaryReader r){return new float2(r.ReadSingle(),r.ReadSingle());}
        private static void Text(BinaryWriter w,string text){w.Write(text!=null);if(text!=null)w.Write(text);}
        private static string Text(BinaryReader r){return r.ReadBoolean()?r.ReadString():null;}
        private static void WriteList<T>(BinaryWriter w,ICollection<T> list,Action<BinaryWriter,T> write)
        {
            w.Write(list==null?-1:list.Count);if(list==null)return;
            foreach(var value in list){bool has=!ReferenceEquals(value,null);w.Write(has);if(has)write(w,value);}
        }
        private static List<T> ReadList<T>(BinaryReader r,Func<BinaryReader,T> read)
        {
            int count=r.ReadInt32();if(count==-1)return null;if(count<0||count>10000)throw new InvalidDataException("List size outside preset limits.");
            var list=new List<T>(count);for(int i=0;i<count;i++)list.Add(r.ReadBoolean()?read(r):default(T));return list;
        }
    }
}
