namespace MusicDownloader.Infrastructure;

internal class Settings
{
    // Paths
    public string CsvDir { get; set; } = @"E:\Documents\Music Database";
    public string BaseDataDir { get; set; } = @"E:\Audio\Music";
    public string CookieFile { get; set; } = @"Data\Cookies.txt";
    public string PlaylistCheckReportFile { get; set; } = @"Data\PlaylistReport.txt";

    // Executables
    public string YtDlpDir { get; set; } = @"C:\Program Files\yt-dlp";
    public string FfmpegDir { get; set; } = @"C:\Program Files\ffmpeg\bin";
    public string YtDlpExe { get; set; } = "yt-dlp.exe";
    public string FfmpegExe { get; set; } = "ffmpeg.exe";

    // Audio Settings
    public string AudioFormat { get; set; } = "m4a";
    public int AudioBitrateKbps { get; set; } = 320;
    public bool PreservePitchWhenChangingTempo { get; set; } = false;

    // Delays
    public int DelayBetweenDownloadsMs { get; set; } = 2500;

    // Authentication
    // If set (e.g., "chrome", "firefox"), yt-dlp will grab cookies directly from the browser.
    public string? CookiesBrowser { get; set; } = "chrome";
}