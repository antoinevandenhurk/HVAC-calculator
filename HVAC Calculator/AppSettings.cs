using System;
using System.IO;
using System.Text.Json;

namespace HVACCalculator;

public static class AppSettings
{
    private static readonly string[] SupportedPipeMaterials =
    [
        "Dikwandige CV buis",
        "Dunwandige CV buis",
        "Henco buis",
        "Koperen buis",
        "PE SDR11 buis"
    ];

    private sealed class AppSettingsData
    {
        public double MinVelocityCvgkw { get; set; } = 15.0;
        public double MaxVelocityCvgkw { get; set; } = 200.0;
        public double MinVelocityTapwater { get; set; } = 1.0;
        public double MaxVelocityTapwater { get; set; } = 2.0;
        public string PreferredMaterialCvgkw { get; set; } = "Dunwandige CV buis";
        public string PreferredMaterialTapwater { get; set; } = "Koperen buis";
    }

    private static readonly string FallbackSettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HVAC Calculator");

    private static readonly string ExecutableSettingsFilePath = Path.Combine(AppContext.BaseDirectory, "settings.json");
    private static readonly string FallbackSettingsFilePath = Path.Combine(FallbackSettingsDirectory, "settings.json");

    public static double MinVelocityCvgkw { get; set; } = 15.0;
    public static double MaxVelocityCvgkw { get; set; } = 200.0;

    public static double MinVelocityTapwater { get; set; } = 1.0;
    public static double MaxVelocityTapwater { get; set; } = 2.0;
    public static string PreferredMaterialCvgkw { get; set; } = "Dunwandige CV buis";
    public static string PreferredMaterialTapwater { get; set; } = "Koperen buis";

    public static void Load()
    {
        try
        {
            string settingsFilePath = File.Exists(ExecutableSettingsFilePath)
                ? ExecutableSettingsFilePath
                : FallbackSettingsFilePath;

            if (!File.Exists(settingsFilePath))
            {
                return;
            }

            string json = File.ReadAllText(settingsFilePath);
            var data = JsonSerializer.Deserialize<AppSettingsData>(json);
            if (data == null)
            {
                return;
            }

            // CV/GKW now uses Lin Δp settings in Pa/m; reject legacy low m/s ranges.
            if (data.MinVelocityCvgkw >= 5 && data.MaxVelocityCvgkw > data.MinVelocityCvgkw)
            {
                MinVelocityCvgkw = data.MinVelocityCvgkw;
                MaxVelocityCvgkw = data.MaxVelocityCvgkw;
            }

            if (data.MinVelocityTapwater > 0 && data.MaxVelocityTapwater > data.MinVelocityTapwater)
            {
                MinVelocityTapwater = data.MinVelocityTapwater;
                MaxVelocityTapwater = data.MaxVelocityTapwater;
            }

            if (System.Array.Exists(SupportedPipeMaterials, material => material == data.PreferredMaterialCvgkw))
            {
                PreferredMaterialCvgkw = data.PreferredMaterialCvgkw;
            }

            if (System.Array.Exists(SupportedPipeMaterials, material => material == data.PreferredMaterialTapwater))
            {
                PreferredMaterialTapwater = data.PreferredMaterialTapwater;
            }
        }
        catch
        {
            // Keep defaults if loading fails.
        }
    }

    public static void Save()
    {
        var data = new AppSettingsData
        {
            MinVelocityCvgkw = MinVelocityCvgkw,
            MaxVelocityCvgkw = MaxVelocityCvgkw,
            MinVelocityTapwater = MinVelocityTapwater,
            MaxVelocityTapwater = MaxVelocityTapwater,
            PreferredMaterialCvgkw = PreferredMaterialCvgkw,
            PreferredMaterialTapwater = PreferredMaterialTapwater
        };

        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

        try
        {
            File.WriteAllText(ExecutableSettingsFilePath, json);
        }
        catch
        {
            Directory.CreateDirectory(FallbackSettingsDirectory);
            File.WriteAllText(FallbackSettingsFilePath, json);
        }
    }
}
