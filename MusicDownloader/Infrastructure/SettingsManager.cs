using MusicDownloader.Common;
using Tomlyn;

namespace MusicDownloader.Infrastructure;

public static class SettingsManager
{
    private const string SettingsFile = "settings.toml";

    public static Settings Current { get; }

    static SettingsManager()
    {
        Current = new Settings
        {
            CsvDir = @"E:\Documents\Music Database",
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
            CookiesBrowser = "chrome"
        };

        try
        {
            if (File.Exists(SettingsFile))
            {
                string content = File.ReadAllText(SettingsFile);
                Settings? loaded = Toml.ToModel<Settings>(content);
                if (loaded is not null)
                {
                    Current = loaded;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to load '{SettingsFile}'. Using default settings. Error: {ex.Message}");
        }
    }
}