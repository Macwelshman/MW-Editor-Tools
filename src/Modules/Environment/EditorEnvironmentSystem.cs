using System;
using System.Diagnostics;
using Colossal.Serialization.Entities;
using Game;
using Game.SceneFlow;
using Game.Rendering;
using Game.Simulation;
using Unity.Entities;

namespace MW.EditorTools.Modules.EditorEnvironment
{
    public sealed partial class EditorEnvironmentSystem : GameSystemBase
    {
        private MW.EditorTools.Settings.EnvironmentSettings options;
        internal void Initialize(MW.EditorTools.Settings.EnvironmentSettings value)
        {
            if (!VerifiedGameBuild.Recovery(typeof(ClimateSystem).Module.ModuleVersionId, typeof(World).Module.ModuleVersionId))
                throw new NotSupportedException("Unverified editor environment API build.");
            if (MW.EditorTools.Settings.EnvironmentSettings.StandaloneLoaded)
                throw new NotSupportedException("Standalone MW Editor Environment is still loaded; integrated environment disabled.");
            options = value ?? throw new ArgumentNullException(nameof(value));
            Enabled = true;
        }
        internal void Stop()
        {
            try { if(GameManager.instance?.gameMode == GameMode.Editor) Release(); else Abandon(); }
            finally { options = null; Enabled = false; }
        }
        // Preload restores before deserialization. If that callback was missed,
        // discard ownership rather than writing old editor values into a city.
        private void Abandon()
        {
            pending = refreshPending = photoWasEnabled = false;
            wait.Reset();
            try { snowCoverage.Discard(); }
            finally { owned = false; climate = null; planet = null; }
        }
        private ClimateSystem climate;
        private PlanetarySystem planet;
        private bool pending, owned;
        private bool photoWasEnabled;
        private bool refreshPending;
        private readonly Stopwatch wait = new Stopwatch();
        private bool oldTimeState, oldRainState, oldCloudState, oldDateState;
        private int oldDay, appliedDay;
        private float oldDate, appliedDate;
        private float oldTime, oldRain, oldCloud;
        private float appliedTime, appliedRain, appliedCloud;
        private bool appliedSnow, oldTemperatureState;
        private float oldTemperature, appliedTemperature;
        private readonly SnowCoverage snowCoverage = new SnowCoverage();

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            // Restore before the next world is deserialized, never after city initialization.
            Release();
            base.OnGamePreload(purpose, mode);
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (options == null || mode != GameMode.Editor) return;
            if (!options.EnableEnvironment)
            {
                Mod.Log.Info("Editor environment preset disabled; native environment retained for this session.");
                return;
            }
            pending = true;
            wait.Restart();
            Mod.Log.Info("Editor session detected; waiting for initialized climate and planetary systems.");
            // Apply during loading completion without requiring simulation to run.
            // PreCulling retries even when editor simulation is stopped.
            TryApplyPreset();
        }

        protected override void OnUpdate()
        {
            var manager = GameManager.instance;
            if (manager == null) return;
            if (manager.gameMode != GameMode.Editor) { if (pending || owned) Abandon(); return; }
            if (manager.isGameLoading) return;
            if (!pending && !owned) return;
            try
            {
                // Enabled stays true until photo mode's exit blend has finished.
                // Only observe it: do not create, activate or modify photo mode.
                var photo = World.GetExistingSystemManaged<PhotoModeRenderSystem>();
                bool photoEnabled = photo != null && photo.Enabled;
                if (photoEnabled) { photoWasEnabled = true; return; }
                if (photoWasEnabled)
                {
                    photoWasEnabled = false;
                    if (owned || pending)
                    {
                        pending = true;
                        wait.Restart();
                        Mod.Log.Info("Photo mode exit completed; reapplying the editor session preset once.");
                    }
                }
                if (pending) TryApplyPreset();
                if (refreshPending && owned && !pending)
                {
                    // Climate normally runs only in EditorSimulation. Recompute
                    // season/weather once without advancing simulation or time.
                    climate.Update();
                    refreshPending = false;
                    Mod.Log.Info("Editor climate refreshed before rendering without simulation playback.");
                }
            }
            catch (Exception ex)
            {
                Mod.Log.Error("Failed to apply editor preset: " + ex);
                Release();
            }
        }

        private void TryApplyPreset()
        {
            var manager = GameManager.instance;
            if (!pending || manager == null || manager.gameMode != GameMode.Editor) return;
            if (MW.EditorTools.Settings.EnvironmentSettings.StandaloneLoaded)
            {
                pending = false;
                Mod.Log.Warn("Integrated environment preset not applied: standalone MW Editor Environment is loaded. Disable it and reload the editor.");
                return;
            }
            var photo = World.GetExistingSystemManaged<PhotoModeRenderSystem>();
            if (photo != null && photo.Enabled) return;
            try
            {
                climate = World.GetExistingSystemManaged<ClimateSystem>();
                planet = World.GetExistingSystemManaged<PlanetarySystem>();
                var snow = World.GetExistingSystemManaged<SnowSystem>();
                bool needsSnow = owned ? appliedSnow : options != null && options.Snow;
                if (climate == null || planet == null || climate.currentClimate == Entity.Null ||
                    !EntityManager.Exists(climate.currentClimate) || climate.precipitation == null || climate.cloudiness == null || climate.currentDate == null ||
                    (needsSnow && (climate.temperature == null || !SnowCoverage.IsReady(snow))))
                {
                    if (wait.Elapsed.TotalSeconds >= 30)
                    {
                        pending = false;
                        Mod.Log.Error("Preset not applied: environment systems/climate were not ready within 30 seconds. Reload the editor to retry.");
                    }
                    return;
                }
                // Retain the original pre-editor snapshot and session preset on
                // photo-mode return. Settings edits still take effect next load.
                if (!owned)
                {
                    if (options == null) { pending = false; return; }
                    appliedTime = float.IsNaN(options.Hour) || float.IsInfinity(options.Hour) ? MW.EditorTools.Settings.EnvironmentSettings.DefaultHour : Math.Max(0f, Math.Min(23.75f, options.Hour));
                    appliedRain = Math.Max(0, Math.Min(100, options.Precipitation)) / 100f;
                    appliedCloud = Math.Max(0, Math.Min(100, options.CloudCoverage)) / 100f;
                    appliedDay = Math.Max(1, Math.Min(365, options.DayOfYear));
                    appliedDate = (appliedDay - 1) / 365f;
                    appliedSnow = options.Snow;
                    if (appliedSnow)
                    {
                        oldTemperatureState = climate.temperature.overrideState;
                        oldTemperature = climate.temperature.overrideValue;
                        appliedTemperature = climate.freezingTemperature - 10f;
                    }
                    oldDay = planet.day;
                    oldDateState = climate.currentDate.overrideState; oldDate = climate.currentDate.overrideValue;
                    oldTimeState = planet.overrideTime; oldTime = planet.time;
                    oldRainState = climate.precipitation.overrideState; oldRain = climate.precipitation.overrideValue;
                    oldCloudState = climate.cloudiness.overrideState; oldCloud = climate.cloudiness.overrideValue;
                    owned = true;
                }
                planet.day = appliedDay;
                planet.time = appliedTime; planet.overrideTime = true;
                climate.currentDate.overrideValue = appliedDate; climate.currentDate.overrideState = true;
                climate.precipitation.overrideValue = appliedRain; climate.precipitation.overrideState = true;
                climate.cloudiness.overrideValue = appliedCloud; climate.cloudiness.overrideState = true;
                if (appliedSnow)
                {
                    climate.temperature.overrideValue = appliedTemperature; climate.temperature.overrideState = true;
                    snowCoverage.Apply(snow);
                }
                pending = false;
                refreshPending = true;
                wait.Stop();
                Mod.Log.Info(string.Format("Editor preset applied once: time {0:00}:{1:00}, precipitation {2:0}%, clouds {3:0}%, day of year {4}, snow {5}.", (int)appliedTime, (int)Math.Round((appliedTime % 1) * 60), appliedRain * 100, appliedCloud * 100, appliedDay, appliedSnow));
            }
            catch (Exception ex)
            {
                Mod.Log.Error("Failed to apply editor preset: " + ex);
                Release();
            }
        }

        public void Release()
        {
            pending = false;
            refreshPending = false;
            photoWasEnabled = false;
            wait.Reset();
            if (!owned) return;
            try
            {
                try { snowCoverage.Release(); }
                catch (Exception ex) { Mod.Log.Error("Failed to restore snow coverage: " + ex); }
                if (appliedSnow && climate != null && climate.temperature != null && climate.temperature.overrideState && climate.temperature.overrideValue == appliedTemperature)
                { climate.temperature.overrideValue = oldTemperature; climate.temperature.overrideState = oldTemperatureState; }
                // Do not undo later changes made by the user or another mod.
                if (planet != null && planet.day == appliedDay) planet.day = oldDay;
                if (climate != null && climate.currentDate.overrideState && climate.currentDate.overrideValue == appliedDate)
                { climate.currentDate.overrideValue = oldDate; climate.currentDate.overrideState = oldDateState; }
                if (planet != null && planet.overrideTime && Math.Abs(planet.time - appliedTime) < 0.0001f)
                { planet.time = oldTime; planet.overrideTime = oldTimeState; }
                if (climate != null && climate.precipitation.overrideState && climate.precipitation.overrideValue == appliedRain)
                { climate.precipitation.overrideValue = oldRain; climate.precipitation.overrideState = oldRainState; }
                if (climate != null && climate.cloudiness.overrideState && climate.cloudiness.overrideValue == appliedCloud)
                { climate.cloudiness.overrideValue = oldCloud; climate.cloudiness.overrideState = oldCloudState; }
                Mod.Log.Info("Editor preset released; previous override settings restored where still owned.");
            }
            catch (Exception ex) { Mod.Log.Error("Failed to release editor overrides: " + ex); }
            finally { owned = false; }
        }

        protected override void OnDestroy() { Stop(); base.OnDestroy(); }
    }
}
