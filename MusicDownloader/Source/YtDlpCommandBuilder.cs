namespace MusicDownloader;

public class YtDlpCommandBuilder
{
    private readonly Track _track;
    private readonly string _tempFilePath;
    private readonly bool _isPartial;

    public YtDlpCommandBuilder(Track track, string tempFilePath, bool isPartial)
    {
        _track = track;
        _tempFilePath = tempFilePath;
        _isPartial = isPartial;
    }

    public string Build()
    {
        // STRATEGY: Use the default Web Client + Valid Cookies.
        // Since your version does not support the newer clients, we rely on the standard Web client.
        // This is the ONLY client that supports cookies on your build.

        string cookieArg = File.Exists(SettingsManager.Current.CookieFile)
            ? $"--cookies \"{SettingsManager.Current.CookieFile}\" "
            : "";

        // Standard Chrome User-Agent
        string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

        // FIX: Do not force specific clients (like android) because your build skips them.
        // Let yt-dlp default to web.
        string extractorArgs = "";

        // FIX: Prioritize audio-only streams. These are usually easier to download.
        string formatArg = "-f \"bestaudio[ext=m4a]/bestaudio/best\" ";

        string ffmpegLocationArg = !string.IsNullOrWhiteSpace(SettingsManager.Current.FfmpegDir)
            ? $"--ffmpeg-location \"{SettingsManager.Current.FfmpegDir}\" "
            : "";

        // CRITICAL FIX: Force Native Downloader.
        // This prevents the 'Error -138' because yt-dlp handles the download,
        // not ffmpeg. This fixes the network crash.
        string downloaderSelection = "--downloader \"http:native\" ";

        // NETWORK STABILITY:
        // Increased retries and timeouts to bypass bot detection.
        string downloaderArgs = "--retries 20 --fragment-retries 20 --http-chunk-size 10M --socket-timeout 30 ";

        string sectionArg = BuildSectionOption();

        return $"{formatArg}" +
               $"--user-agent \"{userAgent}\" " +
               extractorArgs +
               $"\"{_track.Url}\" " +
               "--extract-audio " +
               $"--audio-format {SettingsManager.Current.AudioFormat} " +
               "--audio-quality 0 " +
               ffmpegLocationArg +
               "--embed-thumbnail " +
               "--no-add-metadata " +
               downloaderSelection +
               downloaderArgs +
               $"--no-mtime {cookieArg}" +
               $"{sectionArg} " +
               $"-o \"{_tempFilePath}\"";
    }

    private string BuildSectionOption()
    {
        if (!_isPartial)
        {
            return "";
        }

        string[] parts = _track.Range.Split('-');
        if (parts.Length != 2)
        {
            return "";
        }

        string start = parts[0].Trim();
        string end = parts[1].Trim();

        return !string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end)
            ? $"--download-sections \"*{start}-{end}\""
            : "";
    }
}