using MusicDownloader.Common;
using MusicDownloader.Infrastructure;

namespace MusicDownloader.Commands;

internal sealed class YtDlpCommandBuilder(Track track, string tempFileBase, bool downloadThumbnail)
{
    private readonly Track _track = track;
    private readonly string _tempFileBase = tempFileBase;
    private readonly bool _downloadThumbnail = downloadThumbnail;

    public ProcessArguments Build()
    {
        List<string> args = [
            "-f", "bestaudio[acodec=opus]/bestaudio"
        ];

        if (!string.IsNullOrWhiteSpace(_track.Source))
        {
            args.Add(_track.Source);
        }

        args.AddRange([
            "-x",
            "--audio-format", "opus"
        ]);

        if (_downloadThumbnail)
        {
            args.AddRange([
                "--write-thumbnail",
                "--convert-thumbnails", "png"
            ]);
        }

        args.AddRange([
            "--no-add-metadata",
            "--downloader", "native",
            "--retries", "20",
            "--fragment-retries", "20",
            "--http-chunk-size", "10M",
            "--socket-timeout", "30",
            "--no-mtime"
        ]);

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