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

    public string Build()
    {
        List<string> args = [
            "-f \"bestaudio[ext=m4a]/bestaudio\"",
            $"\"{_track.Source}\""
        ];

        string ffmpegArgs = GetFfmpegArgs();
        if (!string.IsNullOrEmpty(ffmpegArgs))
        {
            args.Add(ffmpegArgs);
        }

        args.Add("--write-thumbnail");
        args.Add("--no-add-metadata");
        args.Add("--downloader native");
        args.Add("--retries 20");
        args.Add("--fragment-retries 20");
        args.Add("--http-chunk-size 10M");
        args.Add("--socket-timeout 30");
        args.Add("--no-mtime");

        string cookieArgs = GetCookieArgs();
        if (!string.IsNullOrEmpty(cookieArgs))
        {
            args.Add(cookieArgs);
        }

        args.Add($"-o \"{_tempFileBase}.%(ext)s\"");

        return string.Join(" ", args);
    }

    private static string GetCookieArgs()
    {
        if (!string.IsNullOrWhiteSpace(SettingsManager.Current.CookiesBrowser))
        {
            return $"--cookies-from-browser {SettingsManager.Current.CookiesBrowser}";
        }

        string relativePath = SettingsManager.Current.CookieFile;
        if (File.Exists(relativePath))
        {
            return $"--cookies \"{Path.GetFullPath(relativePath)}\"";
        }

        return "";
    }

    private static string GetFfmpegArgs()
    {
        return !string.IsNullOrWhiteSpace(SettingsManager.Current.FfmpegDir)
            ? $"--ffmpeg-location \"{SettingsManager.Current.FfmpegDir}\""
            : "";
    }
}