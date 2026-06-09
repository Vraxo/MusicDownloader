namespace MusicDownloader;

public static class SettingsManager
{
    public static Settings Current { get; } = new()
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
}