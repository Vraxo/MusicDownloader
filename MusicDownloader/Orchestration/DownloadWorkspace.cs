using MusicDownloader.Core;
using MusicDownloader.Infrastructure;
using MusicDownloader.Stages.Fetching;
using MusicDownloader.Stages.Processing;
using Spectre.Console;
using System.ComponentModel;

namespace MusicDownloader.Orchestration;

internal sealed class DownloadWorkspace(string albumDir)
{
    public string TempFileBase { get; } = Path.Combine(albumDir, "temp");
    public string FinalTempOut { get; } = Path.Combine(albumDir, "out.opus");

    public async Task<bool> DownloadAsync(Track track, bool downloadThumbnail)
    {
        ProcessArguments command = new YtDlpCommandBuilder(track, TempFileBase, downloadThumbnail).Build();
        string ytDlpPath = ExecutableFinder.GetFullPath(SettingsManager.Current.YtDlpExe, SettingsManager.Current.YtDlpDir);

        try
        {
            int exitCode = await Task.Run(() => ProcessExecutor.Run(ytDlpPath, command));
            return exitCode == 0;
        }
        catch (Win32Exception)
        {
            AnsiConsole.MarkupLine($"[red]Could not find '{SettingsManager.Current.YtDlpExe}'.[/]");
            return false;
        }
    }

    public async Task<bool> DownloadThumbnailOnlyAsync(Track track, string tempThumbBase)
    {
        List<string> args = [
            "--skip-download",
            "--write-thumbnail",
            "--convert-thumbnails", "png"
        ];

        if (!string.IsNullOrWhiteSpace(track.Source))
        {
            args.Add(track.Source);
        }

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

        args.AddRange(["-o", $"{tempThumbBase}.%(ext)s"]);

        ProcessArguments command = args;
        string ytDlpPath = ExecutableFinder.GetFullPath(SettingsManager.Current.YtDlpExe, SettingsManager.Current.YtDlpDir);

        try
        {
            int exitCode = await Task.Run(() => ProcessExecutor.Run(ytDlpPath, command));
            return exitCode == 0;
        }
        catch (Win32Exception)
        {
            AnsiConsole.MarkupLine($"[red]Could not find '{SettingsManager.Current.YtDlpExe}'.[/]");
            return false;
        }
    }

    public async Task<bool> ProcessAudioAsync(Track track, string inputFile)
    {
        ProcessArguments command = new FfmpegCommandBuilder(track, inputFile, FinalTempOut).Build();
        string ffmpegPath = ExecutableFinder.GetFullPath(SettingsManager.Current.FfmpegExe, SettingsManager.Current.FfmpegDir);

        try
        {
            int exitCode = await Task.Run(() => ProcessExecutor.Run(ffmpegPath, command));
            if (exitCode != 0)
            {
                AnsiConsole.MarkupLine("[red]ffmpeg processing failed.[/]");
                return false;
            }
        }
        catch (Win32Exception)
        {
            AnsiConsole.MarkupLine($"[red]Could not find '{SettingsManager.Current.FfmpegExe}'.[/]");
            return false;
        }

        return true;
    }

    public string? FindDownloadedAudio()
    {
        return FindFileByExtensions(TempFileBase, [".webp", ".jpg", ".png", ".json", ".part", ".ytdl"], inverse: true);
    }

    public string? FindDownloadedCover(string baseName)
    {
        return FindFileByExtensions(baseName, [".webp", ".jpg", ".png", ".jpeg"], inverse: false);
    }

    public void Cleanup()
    {
        if (!Directory.Exists(albumDir))
        {
            return;
        }

        IEnumerable<string> tempFiles = Directory.EnumerateFiles(albumDir, "temp.*")
            .Concat(Directory.EnumerateFiles(albumDir, "out.*"));

        foreach (string file in tempFiles)
        {
            try
            {
                File.Delete(file);
            }
            catch { }
        }
    }

    private static string? FindFileByExtensions(string baseName, string[] extensions, bool inverse)
    {
        string dir = Path.GetDirectoryName(baseName)!;
        string fileName = Path.GetFileName(baseName);

        if (!Directory.Exists(dir))
        {
            return null;
        }

        string[] candidates = Directory.GetFiles(dir, $"{fileName}.*");

        return candidates.FirstOrDefault(f =>
        {
            string ext = Path.GetExtension(f).ToLowerInvariant();
            bool matchesExt = extensions.Contains(ext);
            return inverse ? !matchesExt : matchesExt;
        });
    }
}