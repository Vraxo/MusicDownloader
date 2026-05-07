using System.Text.Json;

namespace MusicDownloader;

public static class SettingsManager
{
    private const string SettingsDir = "Data";
    private const string SettingsFile = "Settings.json";
    private static readonly string SettingsPath = Path.Combine(SettingsDir, SettingsFile);

    public static Settings Current { get; private set; } = new();

    public static void LoadOrCreate()
    {
        _ = Directory.CreateDirectory(SettingsDir);

        if (File.Exists(SettingsPath))
        {
            try
            {
                string json = File.ReadAllText(SettingsPath);
                Current = JsonSerializer.Deserialize<Settings>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new Settings();

                // Save back immediately to ensure any new fields (like CookiesBrowser) are added to the file.
                SaveSettings();
            }
            catch (Exception ex)
            {
                Log.Error($"Error reading settings file: {ex.Message}");
                Log.Warning("Using default settings.");
                Current = new Settings();
            }
        }
        else
        {
            CreateDefaultSettingsFile();
        }
    }

    private static void CreateDefaultSettingsFile()
    {
        Log.Info($"Settings file not found. Creating a new one at '{SettingsPath}'");

        Current = new Settings
        {
            CsvDir = @"E:\Parsa Stuff\Documents\Music Collections",
            BaseDataDir = @"E:\Parsa Stuff\Audio\Music",
            CookieFile = @"Data\Cookies.txt",
            PlaylistCheckReportFile = @"Data\PlaylistReport.txt",
            YtDlpDir = @"C:\Program Files\yt-dlp",
            FfmpegDir = @"C:\Program Files\ffmpeg\bin",
            YtDlpExe = "yt-dlp.exe",
            FfmpegExe = "ffmpeg.exe",
            AudioFormat = "m4a",
            AudioBitrateKbps = 320,
            PreservePitchWhenChangingTempo = false,
            DelayBetweenDownloadsMs = 2500,
            CookiesBrowser = "chrome" // Default suggestion
        };

        SaveSettings();
    }

    private static void SaveSettings()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(Current, options);
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save settings: {ex.Message}");
        }
    }
}