namespace MusicDownloader;

public class YtDlpCommandBuilder
{
    private readonly Track _track;
    private readonly string _tempFileBase;

    public YtDlpCommandBuilder(Track track, string tempFileBase)
    {
        _track = track;
        _tempFileBase = tempFileBase;
    }

    public string Build()
    {
        // STRATEGY: STRICT AUDIO-ONLY FULL DOWNLOAD
        // 1. Force native downloader to bypass firewall/ISP throttling on non-native clients.
        // 2. Remove User-Agent completely. This allows yt-dlp to match the UA to the cookies provided.
        // 3. Support both browser cookies and file cookies.

        string cookieArg = GetCookieArgs();
        string ffmpegLocationArg = GetFfmpegArgs();

        // STRICT: Audio Only. Never fallback to video.
        const string formatArg = "-f \"bestaudio[ext=m4a]/bestaudio\" ";

        const string downloaderArgs = "--downloader native --retries 20 --fragment-retries 20 --http-chunk-size 10M --socket-timeout 30 ";

        return $"{formatArg}" +
               $"\"{_track.Url}\" " +
               ffmpegLocationArg +
               "--write-thumbnail " +
               "--no-add-metadata " +
               downloaderArgs +
               $"--no-mtime {cookieArg}" +
               $"-o \"{_tempFileBase}.%(ext)s\"";
    }

    private static string GetCookieArgs()
    {
        // 1. Browser Cookies (Most reliable for bypassing "Sign in" errors)
        if (!string.IsNullOrWhiteSpace(SettingsManager.Current.CookiesBrowser))
        {
            return $"--cookies-from-browser {SettingsManager.Current.CookiesBrowser} ";
        }

        // 2. File Cookies (Fallback)
        // Use absolute path to ensure yt-dlp finds it.
        string relativePath = SettingsManager.Current.CookieFile;
        if (File.Exists(relativePath))
        {
            string absolutePath = Path.GetFullPath(relativePath);
            return $"--cookies \"{absolutePath}\" ";
        }

        return "";
    }

    private static string GetFfmpegArgs()
    {
        return !string.IsNullOrWhiteSpace(SettingsManager.Current.FfmpegDir)
            ? $"--ffmpeg-location \"{SettingsManager.Current.FfmpegDir}\" "
            : "";
    }
}