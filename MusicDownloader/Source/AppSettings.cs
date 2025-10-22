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

    // Path to the directory containing ffmpeg.exe and ffprobe.exe.
    // yt-dlp will use this to find the tools for post-processing.
    // Example: @"C:\Program Files\ffmpeg\bin" or @"C:\ffmpeg\bin"
    public const string FfmpegDir = @"C:\Program Files\ffmpeg\bin";

    // Audio Settings
    public const string AudioFormat = "m4a";
    public const int AudioBitrateKbps = 320;

    public const bool PreservePitchWhenChangingTempo = false;
}