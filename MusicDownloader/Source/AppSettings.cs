namespace MusicDownloader;

public static class AppSettings
{
    // Paths
    public const string CsvDir = "E:\\Parsa Stuff\\Documents\\Music Collections";
    public const string BaseDataDir = "E:\\Parsa Stuff\\Audio\\Music";
    public const string CookieFile = "Data\\Cookies.txt";
    public const string PlaylistCheckReportFile = "Data\\PlaylistReport.txt";

    // Executables
    public const string YtDlpExe = "yt-dlp";
    public const string FfmpegExe = "ffmpeg";
    public const string FallbackYtDlpExe = @"C:\Program Files\yt-dlp\yt-dlp.exe";

    // Audio Settings
    public const string AudioFormat = "m4a";
    public const int AudioBitrateKbps = 320;

    public const bool PreservePitchWhenChangingTempo = false;
}