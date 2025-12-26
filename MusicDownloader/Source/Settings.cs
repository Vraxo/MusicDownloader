namespace MusicDownloader;

public class Settings
{
    // Paths
    public string CsvDir { get; set; } = string.Empty;
    public string BaseDataDir { get; set; } = string.Empty;
    public string CookieFile { get; set; } = string.Empty;
    public string PlaylistCheckReportFile { get; set; } = string.Empty;

    // Executables
    public string YtDlpDir { get; set; } = string.Empty;
    public string FfmpegDir { get; set; } = string.Empty;
    public string YtDlpExe { get; set; } = "yt-dlp.exe";
    public string FfmpegExe { get; set; } = "ffmpeg.exe";

    // Audio Settings
    public string AudioFormat { get; set; } = "m4a";
    public int AudioBitrateKbps { get; set; } = 320;
    public bool PreservePitchWhenChangingTempo { get; set; } = false;

    // Delays
    public int DelayBetweenDownloadsMs { get; set; } = 2500;
}