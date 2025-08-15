namespace MusicDownloader;

public static class AppSettings
{
    // Paths
    public const string CsvFile = "Data\\Collection.csv";
    public const string BaseDataDir = "D:\\Parsa Stuff\\Music";
    public const string CookieFile = "Data\\Cookies.txt";
    public const string PlaylistCheckReportFile = "Data\\PlaylistReport.txt";

    // Executables
    public const string YtDlpExe = "yt-dlp";
    public const string FfmpegExe = "ffmpeg";
    public const string FallbackYtDlpExe = @"D:\Program Files\yt-dlp\yt-dlp.exe";

    // Audio Settings
    public const string AudioFormat = "m4a";
    public const int AudioBitrateKbps = 320;

    public const bool PreservePitchWhenChangingTempo = false;
}