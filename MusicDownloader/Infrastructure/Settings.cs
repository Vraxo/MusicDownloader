namespace MusicDownloader.Infrastructure;

internal sealed class Settings
{
    public string DatabaseDir { get; set; } = "Database";

    public string BaseDataDir { get; set; } = "Music";

    public string CookieFile { get; set; } = Path.Combine("Data", "cookies.txt");

    public string PlaylistCheckReportFile { get; set; } = Path.Combine("Data", "PlaylistReport.txt");

    public string YtDlpDir { get; set; } = string.Empty;

    public string FfmpegDir { get; set; } = string.Empty;

    public string YtDlpExe { get; set; } = "yt-dlp.exe";

    public string FfmpegExe { get; set; } = "ffmpeg.exe";

    public string AudioFormat { get; set; } = "m4a";

    public int AudioBitrateKbps { get; set; } = 320;

    public bool PreservePitchWhenChangingTempo { get; set; }

    public int DelayBetweenDownloadsMs { get; set; } = 2500;

    public string? CookiesBrowser { get; set; }
}