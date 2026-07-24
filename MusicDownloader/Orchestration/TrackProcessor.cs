using MusicDownloader.Core;
using MusicDownloader.Infrastructure;
using MusicDownloader.Stages.Processing;
using MusicDownloader.Stages.Storage;
using Spectre.Console;

namespace MusicDownloader.Orchestration;

internal sealed class TrackProcessor(Track track, int index, int total, ITrackRepository repository)
{
    public static readonly string[] SupportedExtensions = [".opus", ".m4a", ".mp3", ".flac", ".ogg", ".wav", ".aac"];

    private Track _track = track;
    private readonly string _albumDir = Path.Combine(SettingsManager.Current.BaseDataDir, PathUtils.SafeFileName(track.Album));

    public static string GetOutputFile(Track track)
    {
        string albumDir = Path.Combine(SettingsManager.Current.BaseDataDir, PathUtils.SafeFileName(track.Album));
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

        DownloadWorkspace workspace = new(_albumDir);
        CoverArtHandler coverHandler = new(_track, repository);

        await coverHandler.EnsureCoverArtExistsAsync(workspace);
        _track = coverHandler.CurrentTrack;

        bool updated = await Task.Run(() => TrackTagger.Apply(_track, outputFile));
        if (updated)
        {
            AnsiConsole.MarkupLine($"{GetLogPrefix().EscapeMarkup()}[green]Updated metadata: [white]{_track.Title.EscapeMarkup()}[/][/]");
            PrintMismatchDetails(mismatch);
            return TrackProcessStatus.MetadataUpdated;
        }

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
        DownloadWorkspace workspace = new(_albumDir);
        CoverArtHandler coverHandler = new(_track, repository);

        try
        {
            AnsiConsole.MarkupLine($"{GetLogPrefix().EscapeMarkup()}[cyan]Downloading & processing: [white]{_track.Title.EscapeMarkup()}[/][/]");

            if (!await workspace.DownloadAsync(_track, downloadThumbnail: !coverHandler.CoverExistsLocally()))
            {
                Log.Warning("Download failed. Cleaning partial download files and retrying once...");
                workspace.Cleanup();

                if (!await workspace.DownloadAsync(_track, downloadThumbnail: !coverHandler.CoverExistsLocally()))
                {
                    AnsiConsole.MarkupLine("[red]Download failed permanently.[/]");
                    return TrackProcessStatus.Failed;
                }
            }

            string? downloadedAudio = workspace.FindDownloadedAudio();
            if (downloadedAudio is null)
            {
                AnsiConsole.MarkupLine("[red]Download reported success, but no audio file was found.[/]");
                return TrackProcessStatus.Failed;
            }

            _track = await coverHandler.ResolveCoverArtAsync(workspace);

            if (!await workspace.ProcessAudioAsync(_track, downloadedAudio))
            {
                return TrackProcessStatus.Failed;
            }

            if (!TrackTagger.Apply(_track, workspace.FinalTempOut))
            {
                return TrackProcessStatus.Failed;
            }

            File.Move(workspace.FinalTempOut, outputFile, true);
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
            workspace.Cleanup();
        }
    }

    private string GetLogPrefix()
    {
        return total > 0 ? $"[{index}/{total}] " : string.Empty;
    }

    private void PrintMismatchDetails(string? mismatch)
    {
        if (string.IsNullOrEmpty(mismatch))
        {
            return;
        }

        string coverFileName = PathUtils.GetCoverFileName(_track);
        string coverPath = Path.Combine(SettingsManager.Current.CoversDir, coverFileName);

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
}