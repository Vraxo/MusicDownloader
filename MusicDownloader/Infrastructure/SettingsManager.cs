using MusicDownloader.Common;
using Tomlyn;

namespace MusicDownloader.Infrastructure;

internal static class SettingsManager
{
    private const string SettingsFile = "settings.toml";

    public static Settings Current { get; }

    static SettingsManager()
    {
        Settings settings = new();

        try
        {
            if (File.Exists(SettingsFile))
            {
                string content = File.ReadAllText(SettingsFile);
                Settings? loaded = TomlSerializer.Deserialize<Settings?>(content);
                if (loaded is not null)
                {
                    settings = loaded;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to load '{SettingsFile}'. Using default settings. Error: {ex.Message}");
        }

        Current = settings;
    }
}