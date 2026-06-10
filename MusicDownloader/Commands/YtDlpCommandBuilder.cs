using MusicDownloader.Common;
using MusicDownloader.Infrastructure;

namespace MusicDownloader.Commands;

internal sealed class YtDlpCommandBuilder
{
    private readonly Track _track;
    private readonly string _tempFileBase;

    public YtDlpCommandBuilder(Track track, string tempFileBase)
    {
        _track = track;
        _tempFileBase = tempFileBase;
    }

    public ProcessArguments Build()
    {
        List<string> args = [
            "-f", "bestaudio[ext=m4a]/bestaudio",
            _track.Source,
            "--write-thumbnail",
            "--no-add-metadata",
            "--downloader", "native",
            "--retries", "20",
            "--fragment-retries", "20",
            "--http-chunk-size", "10M",
            "--socket-timeout", "30",
            "--no-mtime"
        ];

        if (!string.IsNullOrWhiteSpace(SettingsManager.Current.FfmpegDir))
        {
            args.AddRange(["--ffmpeg-location", SettingsManager.Current.FfmpegDir]);
        }

        if (!string.IsNullOrWhiteSpace(SettingsManager.Current.CookiesBrowser))
        {
            args.AddRange(["--cookies-from-browser", SettingsManager.Current.CookiesBrowser]);
        }
        else
        {
            string relativePath = SettingsManager.Current.CookieFile;
            if (File.Exists(relativePath))
            {
                args.AddRange(["--cookies", Path.GetFullPath(relativePath)]);
            }
        }

        args.AddRange(["-o", $"{_tempFileBase}.%(ext)s"]);

        return args;
    }
}