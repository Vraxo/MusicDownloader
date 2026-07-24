using MusicDownloader.Commands;
using MusicDownloader.Common;
using MusicDownloader.Infrastructure;
using Spectre.Console;
using System.ComponentModel;

namespace MusicDownloader.Workflows;

internal class TrackProcessor
{
    public static readonly string[] SupportedExtensions = [".opus", ".m4a", ".mp3", ".flac", ".ogg", ".wav", ".aac"];

    private Track _track;
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
        string albumDir = Path.Combine(
            SettingsManager.Current.BaseDataDir,
            PathUtils.SafeFileName(track.Album));

        string baseFileName = PathUtils.SafeFileName(track.Title);

        if (Directory.Exists(albumDir))
        {
            foreach (string ext in SupportedExtensions)
            {
                string candidate = Path.Combine(albumDir, baseFileName + ext);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return Path.Combine(albumDir, baseFileName + ".opus");
    }

    public async Task<TrackProcessStatus> ProcessAsync()
    {
        Directory.CreateDirectory(_albumDir);
        string outputFile = GetOutputFile(_track);

        if (File.Exists(outputFile))
        {
            if (IsFileCorrupted(outputFile))
            {
                Log.Warning($"Existing file '{Path.GetFileName(outputFile)}' is corrupted. Deleting and queuing for clean redownload...");
                try
                {
                    File.Delete(outputFile);
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to delete corrupted file '{Path.GetFileName(outputFile)}': {ex.Message}");
                    return TrackProcessStatus.Failed;
                }
            }
            else
            {
                return await HandleExistingFileAsync(outputFile);
            }
        }

        return await DownloadAndProcessNewTrackAsync(outputFile);
    }

    private static bool IsFileCorrupted(string filePath)
    {
        try
        {
            using TagLib.File file = TagLib.File.Create(filePath);
            _ = file.Tag;
            return false;
        }
        catch
        {
            return true;
        }
    }

    private async Task<TrackProcessStatus> HandleExistingFileAsync(string outputFile)
    {
        if (AudioProber.IsMetadataUpToDate(outputFile, _track, out string? mismatch, Log.Success))
        {
            return TrackProcessStatus.Skipped;
        }

        string coverFileName = PathUtils.GetCoverFileName(_track);
        string coverPath = Path.Combine(SettingsManager.Current.CoversDir, coverFileName);

        if (!File.Exists(coverPath) && !string.IsNullOrWhiteSpace(_track.Source))
        {
            string tempFileBase = Path.Combine(_albumDir, "temp_thumb");
            try
            {
                AnsiConsole.MarkupLine($"{GetLogPrefix().EscapeMarkup()}[cyan]Downloading missing cover art from source for: [white]{_track.Title.EscapeMarkup()}[/][/]");
                bool downloaded = await DownloadThumbnailOnlyAsync(tempFileBase);
                if (downloaded)
                {
                    string? downloadedCover = FindCoverFile(tempFileBase);
                    if (downloadedCover is not null)
                    {
                        _track = await TrackTagger.UpdateMarkdownCoverPropertyAsync(_track, downloadedCover);
                        Log.Success($"Successfully downloaded and placed cover art: '{coverFileName}'");
                    }
                }
                else
                {
                    Log.Warning($"Failed to download thumbnail for '{_track.Title}' from source.");
                }
            }
            finally
            {
                IEnumerable<string> tempFiles = Directory.EnumerateFiles(_albumDir, "temp_thumb.*");
                foreach (string file in tempFiles)
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }

        bool updated = await UpdateMetadataInPlaceAsync(outputFile);
        if (updated)
        {
            AnsiConsole.MarkupLine($"{GetLogPrefix().EscapeMarkup()}[green]Updated metadata: [white]{_track.Title.EscapeMarkup()}[/][/]");
            if (!string.IsNullOrEmpty(mismatch))
            {
                string[] lines = mismatch.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                List<string> displayLines = [];
                foreach (string line in lines)
                {
                    if (line.Contains("Scheduled for download from source", StringComparison.OrdinalIgnoreCase))
                    {
                        if (File.Exists(coverPath))
                        {
                            displayLines.Add("[gray]    - Cover Art: '[/][purple]Missing[/][gray]' -> '[/][green]Downloaded & Embedded[/][gray]'[/]");
                        }
                        else
                        {
                            displayLines.Add("[gray]    - Cover Art: '[/][purple]Missing[/][gray]' -> '[/][red]Download Failed[/][gray]'[/]");
                        }
                    }
                    else
                    {
                        displayLines.Add(line);
                    }
                }

                if (displayLines.Count > 0)
                {
                    AnsiConsole.MarkupLine(string.Join(Environment.NewLine, displayLines));
                }
            }
            return TrackProcessStatus.MetadataUpdated;
        }

        // Self-Healing Recovery:
        // If an in-place update fails, the file has pre-existing container/page corruption (likely from old image tag attempts).
        // Automatically delete the corrupted file and perform a clean redownload.
        Log.Warning($"Failed to update metadata in-place for '{Path.GetFileName(outputFile)}' due to container corruption. Deleting and queuing for a clean download...");
        try
        {
            File.Delete(outputFile);
            return await DownloadAndProcessNewTrackAsync(outputFile);
        }
        catch (Exception ex)
        {
            Log.Error($"Self-healing failed. Could not recover '{Path.GetFileName(outputFile)}': {ex.Message}");
            return TrackProcessStatus.Failed;
        }
    }

    private async Task<TrackProcessStatus> DownloadAndProcessNewTrackAsync(string outputFile)
    {
        string tempFileBase = Path.Combine(_albumDir, "temp");
        string finalTempOut = Path.Combine(_albumDir, "out.opus");

        string coverFileName = PathUtils.GetCoverFileName(_track);

        Directory.CreateDirectory(SettingsManager.Current.CoversDir);
        string finalCoverPath = Path.Combine(SettingsManager.Current.CoversDir, coverFileName);
        bool coverExistsLocally = File.Exists(finalCoverPath);

        try
        {
            AnsiConsole.MarkupLine($"{GetLogPrefix().EscapeMarkup()}[cyan]Downloading & processing: [white]{_track.Title.EscapeMarkup()}[/][/]");

            if (!await RunFullDownloadAsync(tempFileBase, downloadThumbnail: !coverExistsLocally))
            {
                Log.Warning("Download failed. Cleaning partial download files and retrying once...");
                CleanupTempFiles();

                if (!await RunFullDownloadAsync(tempFileBase, downloadThumbnail: !coverExistsLocally))
                {
                    AnsiConsole.MarkupLine("[red]Download failed permanently.[/]");
                    return TrackProcessStatus.Failed;
                }
            }

            string? downloadedAudio = FindDownloadedFile(tempFileBase);
            if (downloadedAudio is null)
            {
                AnsiConsole.MarkupLine("[red]Download reported success, but no audio file was found.[/]");
                return TrackProcessStatus.Failed;
            }

            if (!coverExistsLocally)
            {
                string? downloadedCover = FindCoverFile(tempFileBase);
                if (downloadedCover is not null)
                {
                    _track = await TrackTagger.UpdateMarkdownCoverPropertyAsync(_track, downloadedCover);
                }
            }
            else
            {
                Log.Info($"Re-using existing cover art: 'Covers/{coverFileName}'");

                string expectedCoverLink = $"[[Covers/{coverFileName}]]";
                if (!string.Equals(_track.Cover, expectedCoverLink, StringComparison.OrdinalIgnoreCase))
                {
                    _track = await TrackTagger.UpdateMarkdownCoverPropertyAsync(_track, finalCoverPath);
                }
            }

            if (!await ProcessAudioAsync(_track, downloadedAudio, finalTempOut))
            {
                return TrackProcessStatus.Failed;
            }

            if (!TrackTagger.Apply(_track, finalTempOut))
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
            return Path.GetExtension(f).ToLowerInvariant()
                is not ".webp"
                and not ".jpg"
                and not ".png"
                and not ".json"
                and not ".part"
                and not ".ytdl";
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

    private async Task<bool> RunFullDownloadAsync(string tempFileBase, bool downloadThumbnail)
    {
        ProcessArguments command = new YtDlpCommandBuilder(_track, tempFileBase, downloadThumbnail).Build();
        string ytDlpPath = ExecutableFinder.GetFullPath(SettingsManager.Current.YtDlpExe, SettingsManager.Current.YtDlpDir);

        try
        {
            int exitCode = await Task.Run(() => ProcessExecutor.Run(ytDlpPath, command));

            if (exitCode != 0)
            {
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

    private async Task<bool> DownloadThumbnailOnlyAsync(string tempFileBase)
    {
        List<string> args = [
            "--skip-download",
            "--write-thumbnail",
            "--convert-thumbnails", "png"
        ];

        if (!string.IsNullOrWhiteSpace(_track.Source))
        {
            args.Add(_track.Source);
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

        args.AddRange(["-o", $"{tempFileBase}.%(ext)s"]);

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

    private static async Task<bool> ProcessAudioAsync(Track track, string inputFile, string outputFile)
    {
        ProcessArguments command = new FfmpegCommandBuilder(track, inputFile, outputFile).Build();
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

    private async Task<bool> UpdateMetadataInPlaceAsync(string outputFile)
    {
        return await Task.Run(() => TrackTagger.Apply(_track, outputFile));
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