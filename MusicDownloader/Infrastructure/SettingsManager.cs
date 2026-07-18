using MusicDownloader.Common;
using System.Text.Json;

namespace MusicDownloader.Infrastructure;

internal static class SettingsManager
{
    private static readonly string SettingsFile = Path.Combine("Data", "settings.json");
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static Settings Current { get; }

    static SettingsManager()
    {
        Settings settings = new();

        try
        {
            if (File.Exists(SettingsFile))
            {
                string content = File.ReadAllText(SettingsFile);
                Settings? loaded = JsonSerializer.Deserialize<Settings>(content, SerializerOptions);
                if (loaded is not null)
                {
                    settings = loaded;
                }
            }
            else
            {
                Directory.CreateDirectory("Data");
                string serialized = JsonSerializer.Serialize(settings, SerializerOptions);
                File.WriteAllText(SettingsFile, serialized);
                Log.Info($"Created default configuration file: '{SettingsFile}'");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(
                $"Failed to load or write '{SettingsFile}'. " +
                $"Using default settings. Error: {ex.Message}");
        }

        Current = settings;
    }
}