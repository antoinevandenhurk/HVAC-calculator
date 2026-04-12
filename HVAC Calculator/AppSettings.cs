using System;
using System.IO;
using System.Text.Json;

namespace HVACCalculator;

public static class AppSettings
{
    private sealed class AppSettingsData
    {
        public double MinVelocityCvgkw { get; set; } = 0.5;
        public double MaxVelocityCvgkw { get; set; } = 1.5;
        public double MinVelocityTapwater { get; set; } = 1.0;
        public double MaxVelocityTapwater { get; set; } = 2.0;
    }

    private static readonly string FallbackSettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HVAC Calculator");

    private static readonly string ExecutableSettingsFilePath = Path.Combine(AppContext.BaseDirectory, "settings.json");
    private static readonly string FallbackSettingsFilePath = Path.Combine(FallbackSettingsDirectory, "settings.json");

    public static double MinVelocityCvgkw { get; set; } = 0.5;
    public static double MaxVelocityCvgkw { get; set; } = 1.5;

    public static double MinVelocityTapwater { get; set; } = 1.0;
    public static double MaxVelocityTapwater { get; set; } = 2.0;

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

            if (data.MinVelocityCvgkw > 0 && data.MaxVelocityCvgkw > data.MinVelocityCvgkw)
            {
                MinVelocityCvgkw = data.MinVelocityCvgkw;
                MaxVelocityCvgkw = data.MaxVelocityCvgkw;
            }

            if (data.MinVelocityTapwater > 0 && data.MaxVelocityTapwater > data.MinVelocityTapwater)
            {
                MinVelocityTapwater = data.MinVelocityTapwater;
                MaxVelocityTapwater = data.MaxVelocityTapwater;
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
            MaxVelocityTapwater = MaxVelocityTapwater
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
