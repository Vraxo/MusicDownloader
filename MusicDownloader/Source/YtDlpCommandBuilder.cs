namespace MusicDownloader;

public class YtDlpCommandBuilder
{
    private readonly Track _track;
    private readonly string _tempFilePath;

    public YtDlpCommandBuilder(Track track, string tempFilePath)
    {
        _track = track;
        _tempFilePath = tempFilePath;
    }

    public string Build()
    {
        string cookieArg = File.Exists(AppSettings.CookieFile)
            ? $"--cookies \"{AppSettings.CookieFile}\" "
            : "";

        // Prefer Opus when available (usually in WebM). Fall back to generic bestaudio otherwise.
        // This requests the best audio stream that uses the opus codec, then falls back to bestaudio.
        string formatArg = "-f \"bestaudio[acodec=opus]/bestaudio\" ";

        // This argument helps yt-dlp handle YouTube's rate-limiting (HTTP 429) more gracefully.
        string extractorArgs = "--extractor-args \"youtubetab:skip=authcheck\" ";

        return $"{formatArg}" +
               $"\"{_track.Url}\" " +
               extractorArgs +
               "--extract-audio " +
               $"--audio-format {AppSettings.AudioFormat} " +
               "--audio-quality 0 " +
               "--embed-thumbnail " +
               "--no-add-metadata " +
               $"--no-mtime {cookieArg}" +
               $"-o \"{_tempFilePath}\"";
    }
}
