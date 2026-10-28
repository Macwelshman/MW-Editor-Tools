using System;
using System.Collections.Generic;
using System.Reflection;

namespace MW.EditorTools {
    static class Mod {
        public static Logger Log = new Logger();
        public class Logger { public readonly List<string> Errors=new List<string>(); public void Info(string value){} public void Warn(string value){} public void Error(string value){Errors.Add(value);} }
    }
}
namespace Game.Modding {
    public interface IMod {}
    public class ModSetting {
        public int Saves;
        public ModSetting(IMod mod){}
        public virtual void SetDefaults(){}
        public virtual void ApplyAndSave(){Saves++;}
    }
}
namespace Colossal.IO.AssetDatabase {
    public class FileLocationAttribute:Attribute { public string Path;public FileLocationAttribute(string value){Path=value;} }
    public class AssetDatabase {
        public static AssetDatabase global=new AssetDatabase();
        public string LoadedKey;
        public readonly Dictionary<string,object> Legacy=new Dictionary<string,object>();
        public void LoadSettings(string key,object value,object defaults){LoadedKey=key;foreach(var pair in Legacy)value.GetType().GetProperty(pair.Key).SetValue(value,pair.Value);}
    }
}
namespace UnityEngine {
    public class Object {public static int Destroyed;public static void Destroy(Object value){Destroyed++;}}
    public struct RenderTextureDescriptor {}
    public class RenderTexture:Object {
        public static RenderTexture active;
        public static bool FailNextCreate;
        public float Value;public bool Created;public RenderTextureDescriptor descriptor;
        public RenderTexture(RenderTextureDescriptor value){descriptor=value;}
        public bool Create(){if(FailNextCreate){FailNextCreate=false;return false;}Created=true;return true;}
        public bool IsCreated()=>Created;
        public void Release(){Created=false;GraphicsState.SnowReleases++;}
    }
    public struct Color {public float r;public Color(float r,float g,float b,float a){this.r=r;}}
    public struct Vector4 {public float x;public Vector4(float x,float y,float z,float w){this.x=x;}}
    public static class GL {public static void Clear(bool depth,bool color,Color value){RenderTexture.active.Value=value.r;GraphicsState.Clears++;if(GraphicsState.Clears%2==0)GraphicsState.SnowApplies++;}}
    public static class Graphics {public static int FailCopyAt,Copies;public static void CopyTexture(RenderTexture source,RenderTexture target){Copies++;if(Copies==FailCopyAt)throw new InvalidOperationException("copy failure");target.Value=source.Value;}}
    public static class Shader {public static object SnowMap;public static float Scale;public static void SetGlobalTexture(string name,object value){SnowMap=value;}public static void SetGlobalVector(string name,Vector4 value){Scale=value.x;}}
}
namespace Game.Settings {
    public class SharedSettings {public static SharedSettings instance=new SharedSettings();public Gameplay gameplay=new Gameplay();}
    public class Gameplay {public bool snowVisual=true;}
}
static class GraphicsState {
    public static int SnowApplies,SnowReleases,Clears;
    public static void Reset(){SnowApplies=SnowReleases=Clears=UnityEngine.Graphics.Copies=UnityEngine.Graphics.FailCopyAt=0;UnityEngine.RenderTexture.FailNextCreate=false;}
    public static UnityEngine.RenderTexture[] Buffers(){var a=new UnityEngine.RenderTexture(default);var b=new UnityEngine.RenderTexture(default);a.Create();b.Create();a.Value=.02f;b.Value=.04f;return new[]{a,b};}
}
