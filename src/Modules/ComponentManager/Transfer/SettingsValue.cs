using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Game.Prefabs;
using UnityEngine;

namespace MW.EditorTools.Modules.ComponentManager.Transfer
{
    // Detached public game settings DTOs and value fields; prefab references keep
    // their identity. Runtime entities, native memory and Unity objects are refused.
    internal sealed class SettingsValue
    {
        private sealed class Node
        {
            internal Type Type;
            internal object Scalar;
            internal FieldInfo[] Fields;
            internal Node[] Children;
            internal Keyframe[] Keys;
            internal WrapMode Pre, Post;
        }
        private readonly Node root;
        public int Weight { get; private set; }
        private SettingsValue(object value) { int budget = 100000; root = Capture(value, ref budget, 0); Weight = 100000 - budget; }
        public static SettingsValue Take(object value) => new SettingsValue(value);
        public object Materialize() => Restore(root);
        public bool Matches(object value)
        {
            try { return Equal(root, Take(value).root); }
            catch { return false; }
        }
        public bool MatchesNativeGeometry(object value) {try{return Equal(root,Take(value).root,true);}catch{return false;}}
        public bool ReferencesValid => Valid(root);

        internal string Fingerprint {
            get { using(var stream=new System.IO.MemoryStream())using(var writer=new System.IO.BinaryWriter(stream))using(var hash=System.Security.Cryptography.SHA256.Create()){Digest(root,writer);writer.Flush();return Convert.ToBase64String(hash.ComputeHash(stream.ToArray()));} }
        }
        private static void Digest(Node node,System.IO.BinaryWriter w){
            w.Write(node.Type?.AssemblyQualifiedName??"null");
            if(node.Type==null)return;
            if(node.Scalar is PrefabBase p){w.Write(RuntimeHelpers.GetHashCode(p));return;}
            if(node.Scalar!=null){
                if(node.Scalar is float f)w.Write(f);else if(node.Scalar is double d)w.Write(d);
                else w.Write(Convert.ToString(node.Scalar,System.Globalization.CultureInfo.InvariantCulture));return;
            }
            if(node.Keys!=null){w.Write((int)node.Pre);w.Write((int)node.Post);w.Write(node.Keys.Length);foreach(var k in node.Keys){w.Write(k.time);w.Write(k.value);w.Write(k.inTangent);w.Write(k.outTangent);w.Write(k.inWeight);w.Write(k.outWeight);w.Write((int)k.weightedMode);}return;}
            w.Write(node.Children.Length);for(int i=0;i<node.Children.Length;i++){if(node.Fields!=null)w.Write(node.Fields[i].Name);Digest(node.Children[i],w);}
        }
        private static Node Capture(object value, ref int budget, int depth)
        {
            if (--budget < 0 || depth > 32) throw new NotSupportedException("Snapshot exceeds safety budget.");
            if (value == null) return new Node();
            var type = value.GetType();
            var node = new Node { Type = type };
            if (value is PrefabBase prefab)
            {
                if (!PrefabAlive(prefab)) throw new InvalidOperationException("Destroyed prefab reference.");
                node.Scalar = prefab; return node;
            }
            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
            { if(value is float f&&(float.IsNaN(f)||float.IsInfinity(f))||value is double d&&(double.IsNaN(d)||double.IsInfinity(d)))throw new NotSupportedException("Nonfinite settings value.");node.Scalar = value; return node; }
            if (value is AnimationCurve curve)
            { CaptureCurve(node, curve); return node; }
            if (value is Array array)
            {
                if (array.Rank != 1) throw new NotSupportedException("Multidimensional payload.");
                if (array.Length > budget) throw new NotSupportedException("Snapshot collection exceeds safety budget.");
                node.Children = new Node[array.Length];
                for (int i = 0; i < array.Length; i++) node.Children[i] = Capture(array.GetValue(i), ref budget, depth + 1);
                return node;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var list = (IList)value;
                if (list.Count > budget) throw new NotSupportedException("Snapshot collection exceeds safety budget.");
                node.Children = new Node[list.Count];
                for (int i = 0; i < list.Count; i++) node.Children[i] = Capture(list[i], ref budget, depth + 1);
                return node;
            }
            bool dto = type.Assembly == typeof(ComponentBase).Assembly && !typeof(ComponentBase).IsAssignableFrom(type)
                && !typeof(UnityEngine.Object).IsAssignableFrom(type) && !typeof(Delegate).IsAssignableFrom(type);
            bool valueType = type.IsValueType && (type.Namespace == "Unity.Mathematics" || type.Namespace == "Colossal.Mathematics" || type.Namespace == "UnityEngine");
            if (!dto && !valueType) throw new NotSupportedException("Unsupported settings value: " + type.FullName);
            var fieldList=new List<FieldInfo>();for(var level=type;level!=null&&level!=typeof(object)&&level!=typeof(ValueType);level=level.BaseType)fieldList.AddRange(level.GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly).Where(f=>!f.IsNotSerialized));
            var allFields=fieldList.ToArray();
            bool knownUnityValue=type.Namespace=="UnityEngine"&&new[]{"Bounds","BoundsInt","Rect","RectInt","LayerMask"}.Contains(type.Name);
            if (allFields.Any(f => (!f.IsPublic&&!knownUnityValue) || f.IsInitOnly)) throw new NotSupportedException("Opaque settings field in " + type.Name);
            node.Fields = allFields.OrderBy(f => f.DeclaringType.FullName+"."+f.Name, StringComparer.Ordinal).ToArray();
            node.Children = new Node[node.Fields.Length];
            for (int i = 0; i < node.Fields.Length; i++) node.Children[i] = Capture(node.Fields[i].GetValue(value), ref budget, depth + 1);
            return node;
        }

        private static object Restore(Node node)
        {
            if (node.Type == null) return null;
            if (node.Scalar != null)
            {
                if (node.Scalar is PrefabBase p && !PrefabAlive(p)) throw new InvalidOperationException("Prefab reference no longer exists.");
                return node.Scalar;
            }
            if (node.Keys != null) return RestoreCurve(node);
            if (node.Type.IsArray)
            {
                var array = Array.CreateInstance(node.Type.GetElementType(), node.Children.Length);
                for (int i = 0; i < node.Children.Length; i++) array.SetValue(Restore(node.Children[i]), i);
                return array;
            }
            var result = Activator.CreateInstance(node.Type);
            if (result is IList list) { foreach (var child in node.Children) list.Add(Restore(child)); return result; }
            for (int i = 0; i < node.Fields.Length; i++) node.Fields[i].SetValue(result, Restore(node.Children[i]));
            return result;
        }

        private static bool Equal(Node a, Node b,bool nativeGeometry=false)
        {
            if (a.Type != b.Type) return false;
            if (a.Type == null) return true;
            if (a.Scalar is PrefabBase || b.Scalar is PrefabBase) return ReferenceEquals(a.Scalar, b.Scalar);
            if(nativeGeometry&&a.Scalar is float x&&b.Scalar is float y)return !float.IsNaN(x)&&!float.IsInfinity(x)&&!float.IsNaN(y)&&!float.IsInfinity(y)&&Math.Abs(x-y)<.0001f;
            if (a.Scalar != null || b.Scalar != null) return Equals(a.Scalar, b.Scalar);
            if (a.Keys != null || b.Keys != null)
                return a.Keys != null && b.Keys != null && a.Pre == b.Pre && a.Post == b.Post && a.Keys.SequenceEqual(b.Keys);
            return a.Children.Length == b.Children.Length && a.Children.Zip(b.Children,(x,y)=>Equal(x,y,nativeGeometry)).All(v => v);
        }
        private static bool Valid(Node node)
        {
            if (node.Scalar is PrefabBase prefab && !PrefabAlive(prefab)) return false;
            return node.Children == null || node.Children.All(Valid);
        }

        // Isolate Unity native calls so managed DTO tests can run outside the Unity runtime.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool PrefabAlive(PrefabBase prefab) => prefab != null;
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CaptureCurve(Node node, AnimationCurve curve)
        { node.Keys = curve.keys.ToArray(); node.Pre = curve.preWrapMode; node.Post = curve.postWrapMode; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static AnimationCurve RestoreCurve(Node node) => new AnimationCurve(node.Keys.ToArray()) { preWrapMode = node.Pre, postWrapMode = node.Post };
    }
}

