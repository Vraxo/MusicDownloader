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

        // Provide yt-dlp with the directory containing ffmpeg if specified.
        // This is necessary if ffmpeg is in a path not visible to the yt-dlp process.
        string ffmpegLocationArg = !string.IsNullOrWhiteSpace(AppSettings.FfmpegDir)
            ? $"--ffmpeg-location \"{AppSettings.FfmpegDir}\" "
            : "";

        return $"{formatArg}" +
               $"\"{_track.Url}\" " +
               "--extract-audio " +
               $"--audio-format {AppSettings.AudioFormat} " +
               "--audio-quality 0 " +
               ffmpegLocationArg +
               "--embed-thumbnail " +
               "--no-add-metadata " +
               $"--no-mtime {cookieArg}" +
               $"-o \"{_tempFilePath}\"";
    }
}