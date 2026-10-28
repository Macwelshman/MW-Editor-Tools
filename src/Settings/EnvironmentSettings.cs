using System;
using System.Linq;
using Colossal.IO.AssetDatabase;
using Game.Modding;

namespace MW.EditorTools.Settings
{
    // Keep the standalone store key, location and property names. LoadSettings
    // populates this object from the named JSON fragment, independent of its type.
    // Only EditorToolsOptions is registered in the native Options menu.
    [FileLocation("ModsSettings/MWEditorEnvironment")]
    public sealed class EnvironmentSettings : ModSetting
    {
        public const float DefaultHour = 12f;
        public const int DefaultDayOfYear = 91, DefaultPrecipitation = 0, DefaultCloudCoverage = 15;
        public bool EnableEnvironment { get; set; }
        public float Hour { get; set; }
        public int Precipitation { get; set; }
        public int CloudCoverage { get; set; }
        public int DayOfYear { get; set; }
        public bool Snow { get; set; }
        public EnvironmentSettings(IMod mod) : base(mod) { SetDefaults(); }
        internal static bool StandaloneLoaded => AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "MWEditorEnvironment");
        internal static EnvironmentSettings Load(IMod mod)
        {
            if (StandaloneLoaded) throw new NotSupportedException("Disable standalone MW Editor Environment before using the integrated Environment tab.");
            var value = new EnvironmentSettings(mod);
            AssetDatabase.global.LoadSettings("MWEditorEnvironment", value, new EnvironmentSettings(mod));
            value.Normalize();
            return value;
        }
        internal void Normalize()
        {
            Hour = float.IsNaN(Hour) || float.IsInfinity(Hour) ? DefaultHour : Math.Max(0f, Math.Min(23.75f, Hour));
            DayOfYear = Math.Max(1, Math.Min(365, DayOfYear));
            Precipitation = Math.Max(0, Math.Min(100, Precipitation));
            CloudCoverage = Math.Max(0, Math.Min(100, CloudCoverage));
        }
        public override void SetDefaults()
        {
            EnableEnvironment = true;
            Hour = DefaultHour; DayOfYear = DefaultDayOfYear;
            Precipitation = DefaultPrecipitation; CloudCoverage = DefaultCloudCoverage; Snow = false;
        }
    }
}
