using MusicDownloader.Core;
using MusicDownloader.Infrastructure;

namespace MusicDownloader.Stages.Fetching;

internal sealed class YtDlpCommandBuilder(Track track, string tempFileBase, bool downloadThumbnail)
{
    public ProcessArguments Build()
    {
        List<string> args = [
            "-f", "bestaudio[acodec=opus]/bestaudio"
        ];

        if (!string.IsNullOrWhiteSpace(track.Source))
        {
            args.Add(track.Source);
        }

        args.AddRange([
            "-x",
            "--audio-format", "opus"
        ]);

        if (downloadThumbnail)
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

        args.AddRange(["-o", $"{tempFileBase}.%(ext)s"]);

        return args;
    }
}