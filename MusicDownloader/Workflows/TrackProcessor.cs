using MusicDownloader.Commands;
using MusicDownloader.Common;
using MusicDownloader.Infrastructure;
using Spectre.Console;
using System.ComponentModel;

namespace MusicDownloader.Workflows;

internal class TrackProcessor
{
    private readonly Track _track;
    private readonly string _albumDir;
    private readonly int _index;
    private readonly int _total;

    public TrackProcessor(Track track, int index = 0, int total = 0)
    {
        _track = track;
        _albumDir = Path.Combine(SettingsManager.Current.BaseDataDir, PathUtils.SafeFileName(_track.Album));
        _index = index;
        _total = total;
    }

    public static string GetOutputFile(Track track)
    {
        string albumDir = Path.Combine(SettingsManager.Current.BaseDataDir, PathUtils.SafeFileName(track.Album));
        string finalFormat = SettingsManager.Current.AudioFormat;
        return Path.Combine(albumDir, PathUtils.SafeFileName(track.Title) + $".{finalFormat}");
    }

    public async Task<TrackProcessStatus> ProcessAsync()
    {
        _ = Directory.CreateDirectory(_albumDir);

        string outputFile = GetOutputFile(_track);

        if (File.Exists(outputFile))
        {
            if (AudioProber.IsMetadataUpToDate(outputFile, _track, out string? mismatch))
            {
                return TrackProcessStatus.Skipped;
            }

            bool updated = await UpdateMetadataInPlaceAsync(outputFile);
            if (updated)
            {
                AnsiConsole.MarkupLine($"{GetLogPrefix().EscapeMarkup()}[green]Updated metadata: [white]{_track.Title.EscapeMarkup()}[/][/]");
                if (!string.IsNullOrEmpty(mismatch))
                {
                    AnsiConsole.MarkupLine(mismatch);
                }
            }
            return updated ? TrackProcessStatus.MetadataUpdated : TrackProcessStatus.Failed;
        }

        string tempFileBase = Path.Combine(_albumDir, "temp");
        string finalTempOut = Path.Combine(_albumDir, "out." + SettingsManager.Current.AudioFormat);

        try
        {
            AnsiConsole.MarkupLine($"{GetLogPrefix().EscapeMarkup()}[cyan]Downloading & processing: [white]{_track.Title.EscapeMarkup()}[/][/]");

            if (!await RunFullDownloadAsync(tempFileBase))
            {
                return TrackProcessStatus.Failed;
            }

            string? downloadedAudio = FindDownloadedFile(tempFileBase);
            if (downloadedAudio is null)
            {
                AnsiConsole.MarkupLine($"[red]Download reported success, but no audio file was found.[/]");
                return TrackProcessStatus.Failed;
            }

            string? downloadedCover = FindCoverFile(tempFileBase);
            if (downloadedCover is not null)
            {
                Log.Info($"Found cover art: {Path.GetFileName(downloadedCover)}");
            }

            if (!await ProcessAudioAsync(_track, downloadedAudio, downloadedCover, finalTempOut))
            {
                return TrackProcessStatus.Failed;
            }

            File.Move(finalTempOut, outputFile, true);
            AnsiConsole.MarkupLine($"[green]Done[/] -> {outputFile.EscapeMarkup()}");
            return TrackProcessStatus.Success;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed processing: {ex.Message.EscapeMarkup()}[/]");
            return TrackProcessStatus.Failed;
        }
        finally
        {
            CleanupTempFiles();
        }
    }

    private string GetLogPrefix()
    {
        return _total > 0 ? $"[{_index}/{_total}] " : string.Empty;
    }

    private static string? FindDownloadedFile(string baseName)
    {
        string dir = Path.GetDirectoryName(baseName)!;
        string fileName = Path.GetFileName(baseName);

        string[] candidates = Directory.GetFiles(dir, $"{fileName}.*");

        return candidates.FirstOrDefault(f =>
        {
            string ext = Path.GetExtension(f).ToLowerInvariant();
            return ext is not ".webp" and not ".jpg" and not ".png" and not ".json" and not ".part" and not ".ytdl";
        });
    }

    private static string? FindCoverFile(string baseName)
    {
        string dir = Path.GetDirectoryName(baseName)!;
        string fileName = Path.GetFileName(baseName);

        string[] candidates = Directory.GetFiles(dir, $"{fileName}.*");

        return candidates.FirstOrDefault(f => f.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(f => f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> RunFullDownloadAsync(string tempFileBase)
    {
        ProcessArguments command = new YtDlpCommandBuilder(_track, tempFileBase).Build();
        string ytDlpPath = ExecutableFinder.GetFullPath(SettingsManager.Current.YtDlpExe, SettingsManager.Current.YtDlpDir);

        try
        {
            int exitCode = await Task.Run(() => ProcessExecutor.Run(ytDlpPath, command));

            if (exitCode != 0)
            {
                AnsiConsole.MarkupLine($"[red]Download failed.[/]");
                return false;
            }

            return true;
        }
        catch (Win32Exception)
        {
            AnsiConsole.MarkupLine($"[red]Could not find '{SettingsManager.Current.YtDlpExe}'.[/]");
            return false;
        }
    }

    private async Task<bool> ProcessAudioAsync(Track track, string inputFile, string? coverFile, string outputFile)
    {
        ProcessArguments command = new FfmpegCommandBuilder(track, inputFile, outputFile, coverFile).Build();
        string ffmpegPath = ExecutableFinder.GetFullPath(SettingsManager.Current.FfmpegExe, SettingsManager.Current.FfmpegDir);

        try
        {
            int exitCode = await Task.Run(() => ProcessExecutor.Run(ffmpegPath, command));

            if (exitCode != 0)
            {
                AnsiConsole.MarkupLine($"[red]ffmpeg processing failed.[/]");
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

    private async Task<bool> UpdateMetadataInPlaceAsync(string outputFile)
    {
        string tempFile = Path.Combine(_albumDir, "temp_meta_update." + SettingsManager.Current.AudioFormat);

        try
        {
            FfmpegCommandBuilder builder = new(_track, outputFile, tempFile);
            ProcessArguments command = builder.BuildMetadataUpdate(outputFile, tempFile);
            string ffmpegPath = ExecutableFinder.GetFullPath(SettingsManager.Current.FfmpegExe, SettingsManager.Current.FfmpegDir);

            int exitCode = await Task.Run(() => ProcessExecutor.Run(ffmpegPath, command));

            if (exitCode != 0)
            {
                AnsiConsole.MarkupLine($"[red]ffmpeg metadata update failed.[/]");
                return false;
            }

            File.Move(tempFile, outputFile, true);
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to update metadata in-place: {ex.Message.EscapeMarkup()}[/]");
            return false;
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch { }
            }
        }
    }

    private void CleanupTempFiles()
    {
        if (!Directory.Exists(_albumDir))
        {
            return;
        }

        IEnumerable<string> tempFiles = Directory.EnumerateFiles(_albumDir, "temp.*")
            .Concat(Directory.EnumerateFiles(_albumDir, "out.*"));

        foreach (string file in tempFiles)
        {
            try
            {
                File.Delete(file);
            }
            catch { }
        }
    }
}