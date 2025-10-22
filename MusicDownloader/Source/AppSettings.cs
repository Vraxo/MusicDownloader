namespace MusicDownloader;

public static class AppSettings
{
    // Paths
    public const string CsvDir = "E:\\Parsa Stuff\\Documents\\Music Collections";
    public const string BaseDataDir = "E:\\Parsa Stuff\\Audio\\Music";
    public const string CookieFile = "Data\\Cookies.txt";
    public const string PlaylistCheckReportFile = "Data\\PlaylistReport.txt";

    // Executables
    public const string YtDlpExe = "yt-dlp.exe";
    public const string FfmpegExe = "ffmpeg.exe";

    // Optional: Path to the directory containing yt-dlp.exe.
    // If this is empty, the application will assume 'yt-dlp' is in the system's PATH.
    // Example: @"C:\Program Files\yt-dlp"
    public const string YtDlpDir = @"C:\Program Files\yt-dlp";

    // Optional: Path to the directory containing ffmpeg.exe and ffprobe.exe.
    // If this is empty, the application will assume 'ffmpeg' is in the system's PATH.
    // If you get "file not found" errors for ffmpeg, specify the full path here.
    // Example: @"C:\Program Files\ffmpeg\bin"
    public const string FfmpegDir = @"C:\Program Files\ffmpeg\bin";

    // Audio Settings
    public const string AudioFormat = "m4a";
    public const int AudioBitrateKbps = 320;

    public const bool PreservePitchWhenChangingTempo = false;

    // A delay in milliseconds between processing each track to avoid being rate-limited.
    // A value of 2000-5000 (2-5 seconds) is recommended.
    public const int DelayBetweenDownloadsMs = 2500;
}