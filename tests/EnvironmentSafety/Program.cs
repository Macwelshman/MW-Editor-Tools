using System;
using System.Collections.Generic;
using Game;
using Game.Simulation;
using Game.SceneFlow;
using Game.Rendering;
using MW.EditorTools.Modules.EditorEnvironment;
using MW.EditorTools.Settings;

namespace Colossal.Serialization.Entities { public enum Purpose { Test } }
namespace Unity.Entities {
    public struct Entity { public int Id; public static Entity Null { get { return new Entity(); } } public static bool operator ==(Entity a, Entity b) { return a.Id == b.Id; } public static bool operator !=(Entity a, Entity b) { return a.Id != b.Id; } public override bool Equals(object o) { return o is Entity && this == (Entity)o; } public override int GetHashCode() { return Id; } }
    public class EntityManager { public bool Exists(Entity e) { return e.Id != 0; } }
    public class World { public Dictionary<Type, object> Systems = new Dictionary<Type, object>(); public T GetExistingSystemManaged<T>() where T : class { object value; return Systems.TryGetValue(typeof(T), out value) ? (T)value : null; } }
}
namespace Game {
    public enum GameMode { Game = 2, Editor = 4 }
    public class Property { public bool overrideState; public float overrideValue; }
    public class GameSystemBase { public bool Enabled=true;
        public Unity.Entities.World World = new Unity.Entities.World(); public Unity.Entities.EntityManager EntityManager = new Unity.Entities.EntityManager();
        protected virtual void OnGamePreload(Colossal.Serialization.Entities.Purpose p, GameMode m) {} protected virtual void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose p, GameMode m) {} protected virtual void OnUpdate() {} protected virtual void OnDestroy() {}
        public void Load(GameMode m) { OnGameLoadingComplete(0, m); } public void Tick() { OnUpdate(); } public void Preload(GameMode m) { OnGamePreload(0, m); }
    }
}
namespace Game.SceneFlow { public class GameManager { public static GameManager instance; public GameMode gameMode; public bool isGameLoading; } }
namespace Game.Rendering { public class PhotoModeRenderSystem { public bool Enabled; } }
namespace Game.Simulation {
    public class ClimateSystem { public Unity.Entities.Entity currentClimate = new Unity.Entities.Entity { Id = 1 }; public Property precipitation = new Property(), cloudiness = new Property(), currentDate = new Property(), temperature = new Property(); public float freezingTemperature; public int Updates; public void Update() { Updates++; } }
    public class PlanetarySystem { public int day = 7; public float time = 6; public bool overrideTime; }
    public class SnowSystem { public bool Ready=true;public bool Loaded=>Ready;private UnityEngine.RenderTexture[] m_SnowHeights=GraphicsState.Buffers(); public UnityEngine.RenderTexture SnowDepth=>m_SnowHeights[0]; public UnityEngine.RenderTexture[] Buffers{get=>m_SnowHeights;set=>m_SnowHeights=value;} }
}
class Checks {
    static int count; static EnvironmentSettings Preset;
    static void Check(bool condition, string label) { if (!condition) throw new Exception(label); count++; }
    static EditorEnvironmentSystem Create(bool snow) {
        Preset = new EnvironmentSettings(null) { Snow = snow }; GameManager.instance = new GameManager { gameMode = GameMode.Editor, isGameLoading = true }; GraphicsState.Reset();
        var s = new EditorEnvironmentSystem(); typeof(EditorEnvironmentSystem).GetField("options",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(s,Preset); s.World.Systems[typeof(ClimateSystem)] = new ClimateSystem(); s.World.Systems[typeof(PlanetarySystem)] = new PlanetarySystem(); s.World.Systems[typeof(SnowSystem)] = new SnowSystem(); s.World.Systems[typeof(PhotoModeRenderSystem)] = new PhotoModeRenderSystem(); return s;
    }
    static int Main() {
        var s = Create(true); var c = s.World.GetExistingSystemManaged<ClimateSystem>(); var p = s.World.GetExistingSystemManaged<PlanetarySystem>();
        s.Load(GameMode.Editor);
        Check(p.time == 12 && p.overrideTime && c.cloudiness.overrideValue == .15f && p.day == 91, "Preset during load callback, before any tick");
        Check(GraphicsState.SnowApplies == 1 && c.temperature.overrideValue == -10 && c.temperature.overrideState, "Snow seeded while simulation has never run");
        GameManager.instance.isGameLoading = false; s.Tick(); s.Tick(); Check(GraphicsState.SnowApplies == 1, "No continuous snow writes");
        Check(c.Updates == 1, "Stopped editor render ticks refresh climate once without simulation ticks");
        var photo = s.World.GetExistingSystemManaged<PhotoModeRenderSystem>(); photo.Enabled = true; s.Tick(); p.time = 23; Preset.Snow = false; photo.Enabled = false; s.Tick();
        Check(p.time == 12 && GraphicsState.SnowApplies == 2, "Photo exit retains session snow preset");
        Check(c.Updates == 2, "Photo exit refreshes climate once");
        s.Preload(GameMode.Game); Check(p.time == 6 && p.day == 7 && !c.temperature.overrideState && GraphicsState.SnowReleases == 2, "Preload restores temperature and both coverage buffers");
        s = Create(false); c = s.World.GetExistingSystemManaged<ClimateSystem>(); s.World.Systems.Remove(typeof(SnowSystem)); c.temperature.overrideValue = 20; s.Load(GameMode.Editor);
        Check(GraphicsState.SnowApplies == 0 && c.temperature.overrideValue == 20, "Snow disabled leaves temperature unchanged and needs no snow system");
        s = Create(true); var snow = s.World.GetExistingSystemManaged<SnowSystem>(); snow.Ready = false; s.Load(GameMode.Editor); Check(GraphicsState.SnowApplies == 0, "Unready snow defers whole preset"); snow.Ready = true; GameManager.instance.isGameLoading = false; s.Tick(); Check(GraphicsState.SnowApplies == 1, "Readiness retry on main loop");
        s.Preload(GameMode.Game); s = Create(true); GameManager.instance.gameMode = GameMode.Game; s.Load(GameMode.Game); s.Tick(); Check(GraphicsState.SnowApplies == 0 && s.World.GetExistingSystemManaged<PlanetarySystem>().time == 6, "City load untouched");
        MoreChecks(); Console.WriteLine("PASS: " + count + " actual-source environment lifecycle/settings/snow ownership checks; native GPU/menu acceptance pending."); return 0;
    }
    static void MoreChecks() {
        var legacy=Colossal.IO.AssetDatabase.AssetDatabase.global;
        legacy.Legacy["Hour"]=9.25f;legacy.Legacy["DayOfYear"]=274;legacy.Legacy["Precipitation"]=35;legacy.Legacy["CloudCoverage"]=65;legacy.Legacy["Snow"]=true;
        var saved=EnvironmentSettings.Load(null);
        Check(legacy.LoadedKey=="MWEditorEnvironment"&&saved.Hour==9.25f&&saved.DayOfYear==274&&saved.Precipitation==35&&saved.CloudCoverage==65&&saved.Snow,"legacy fragment key and five persisted property names preserved");
        Check(saved.EnableEnvironment,"old settings without enable flag preserve enabled behavior");
        legacy.Legacy["EnableEnvironment"]=false;Check(!EnvironmentSettings.Load(null).EnableEnvironment,"persisted disabled flag is loaded");
        var location=(Colossal.IO.AssetDatabase.FileLocationAttribute)Attribute.GetCustomAttribute(typeof(EnvironmentSettings),typeof(Colossal.IO.AssetDatabase.FileLocationAttribute));
        Check(location.Path=="ModsSettings/MWEditorEnvironment","legacy settings location retained");
        saved.Hour=float.PositiveInfinity;saved.DayOfYear=-1;saved.Precipitation=200;saved.CloudCoverage=-2;saved.Normalize();
        Check(saved.Hour==12&&saved.DayOfYear==1&&saved.Precipitation==100&&saved.CloudCoverage==0,"loaded out-of-range settings normalized");
        saved.SetDefaults();Check(saved.Hour==12&&saved.DayOfYear==91&&saved.Precipitation==0&&saved.CloudCoverage==15&&!saved.Snow,"standalone defaults retained");
        Check(saved.EnableEnvironment,"defaults enable environment");
        var off=Create(true);Preset.EnableEnvironment=false;var offPlanet=off.World.GetExistingSystemManaged<PlanetarySystem>();var offClimate=off.World.GetExistingSystemManaged<ClimateSystem>();
        off.Load(GameMode.Editor);GameManager.instance.isGameLoading=false;off.Tick();var offPhoto=off.World.GetExistingSystemManaged<PhotoModeRenderSystem>();offPhoto.Enabled=true;off.Tick();offPhoto.Enabled=false;off.Tick();
        Check(offPlanet.time==6&&offPlanet.day==7&&!offPlanet.overrideTime&&!offClimate.temperature.overrideState&&offClimate.Updates==0&&UnityEngine.Graphics.Copies==0,"disabled editor load and photo-mode return make no environment or snow writes");
        Preset.EnableEnvironment=true;off.Tick();Check(offPlanet.time==6,"enable change waits for next editor load");off.Preload(GameMode.Editor);off.Load(GameMode.Editor);Check(offPlanet.time==12,"next load applies re-enabled preset");
        Preset.EnableEnvironment=false;offPhoto.Enabled=true;off.Tick();offPlanet.time=23;offPhoto.Enabled=false;off.Tick();Check(offPlanet.time==12,"disable draft preserves current session preset through photo return");
        off.Preload(GameMode.Editor);off.Load(GameMode.Editor);Check(offPlanet.time==6&&!offPlanet.overrideTime,"next load with disabled flag leaves native environment restored");
        var unknown=new EditorEnvironmentSystem();bool refused=false;try{unknown.Initialize(saved);}catch(NotSupportedException){refused=true;}Check(refused,"unknown native build refused by actual initialization guard");
        var s=Create(false);var p=s.World.GetExistingSystemManaged<PlanetarySystem>();var c=s.World.GetExistingSystemManaged<ClimateSystem>();s.Load(GameMode.Editor);
        p.time=18;p.day=200;c.precipitation.overrideValue=.7f;c.cloudiness.overrideState=false;c.currentDate.overrideValue=.8f;
        s.Release();Check(p.time==18&&p.day==200&&c.precipitation.overrideValue==.7f&&!c.cloudiness.overrideState&&c.currentDate.overrideValue==.8f,"release preserves later native/user override changes");
        s=Create(false);p=s.World.GetExistingSystemManaged<PlanetarySystem>();s.Load(GameMode.Editor);s.Stop();p.time=9;s.Load(GameMode.Editor);GameManager.instance.isGameLoading=false;s.Tick();Check(p.time==9&&!s.Enabled,"disposed integrated system cannot apply from retained native callbacks");
        s=Create(true);p=s.World.GetExistingSystemManaged<PlanetarySystem>();s.Load(GameMode.Editor);int copies=UnityEngine.Graphics.Copies;GameManager.instance.gameMode=GameMode.Game;s.Tick();
        Check(p.time==12&&p.day==91&&UnityEngine.Graphics.Copies==copies,"missed-preload fallback discards snapshots without writing old values into city");
        s=Create(true);var snow=s.World.GetExistingSystemManaged<SnowSystem>();var beforeActive=new UnityEngine.RenderTexture(default);UnityEngine.RenderTexture.active=beforeActive;s.Load(GameMode.Editor);
        Check(snow.Buffers[0].Value==.125f&&snow.Buffers[1].Value==.125f,"actual snow helper seeds both buffers");
        Check(UnityEngine.RenderTexture.active==beforeActive&&UnityEngine.Shader.Scale==8,"snow helper restores active render target and applies native scale");
        s.Release();Check(snow.Buffers[0].Value==.02f&&snow.Buffers[1].Value==.04f,"actual snow helper restores both original values");int releases=GraphicsState.SnowReleases;s.Release();Check(GraphicsState.SnowReleases==releases,"repeat release idempotent");
        var coverage=new SnowCoverage();snow=new SnowSystem();coverage.Apply(snow);var fresh=GraphicsState.Buffers();snow.Buffers=fresh;coverage.Release();Check(fresh[0].Value==.02f&&fresh[1].Value==.04f,"world-replaced snow buffers never overwritten on release");
        coverage=new SnowCoverage();snow=new SnowSystem();UnityEngine.RenderTexture.FailNextCreate=true;bool failed=false;try{coverage.Apply(snow);}catch(InvalidOperationException){failed=true;}coverage.Release();Check(failed&&snow.Buffers[0].Value==.02f&&snow.Buffers[1].Value==.04f,"snapshot creation failure precedes snow writes and can be cleaned up");
        Game.Settings.SharedSettings.instance.gameplay.snowVisual=false;coverage=new SnowCoverage();coverage.Apply(snow);Check(UnityEngine.Shader.Scale==0,"snow visuals honor gameplay setting");coverage.Release();Game.Settings.SharedSettings.instance.gameplay.snowVisual=true;
        System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(new System.Reflection.AssemblyName("MWEditorEnvironment"),System.Reflection.Emit.AssemblyBuilderAccess.Run);
        refused=false;try{EnvironmentSettings.Load(null);}catch(NotSupportedException){refused=true;}Check(refused,"loaded standalone owner refuses duplicate settings/system initialization");
        s=Create(false);p=s.World.GetExistingSystemManaged<PlanetarySystem>();s.Load(GameMode.Editor);Check(p.time==6&&!p.overrideTime,"late-loaded standalone owner blocks integrated preset writes");
        Check(MW.EditorTools.Mod.Log.Errors.Count==0,"normal lifecycle has no swallowed environment exceptions");
    }
}
