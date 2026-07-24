using MusicDownloader.Common;
using MusicDownloader.Infrastructure;
using Spectre.Console;
using System.Collections.Concurrent;

namespace MusicDownloader.Workflows;

internal static class AutomaticProcessor
{
    public static async Task RunAsync()
    {
        List<Track> allTracks = await MarkdownTrackReader.ReadAllTracksAsync();
        if (allTracks.Count == 0)
        {
            return;
        }

        Dictionary<string, string> sourceMap = await ScanExistingFilesBySourceAsync();
        ReconcileRenamedTracks(allTracks, sourceMap);

        (List<Track>? pendingTracks, int alreadyDownloadedCount, int metadataUpdatesCount, int newDownloadsCount) = await FilterPendingTracksAsync(allTracks);

        if (pendingTracks.Count == 0)
        {
            Log.Success($"All {allTracks.Count} tracks are already downloaded and up to date!");
            return;
        }

        PrintPreFlightStats(allTracks.Count, alreadyDownloadedCount, pendingTracks.Count, metadataUpdatesCount, newDownloadsCount);

        (int downloaded, int metadataUpdated, int failed, int updatedCount) = await ProcessQueueAsync(pendingTracks, alreadyDownloadedCount);

        PrintPostFlightStats(downloaded, metadataUpdated, failed, updatedCount);
        Log.Success("All downloads and processing finished.");
    }

    private static async Task<Dictionary<string, string>> ScanExistingFilesBySourceAsync()
    {
        string baseDir = SettingsManager.Current.BaseDataDir;
        if (!Directory.Exists(baseDir))
        {
            return [];
        }

        List<string> allFiles = [];
        try
        {
            allFiles = [.. Directory.EnumerateFiles(baseDir, "*.*", SearchOption.AllDirectories)
                .Where(f => TrackProcessor.SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))];
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to scan local music directory: {ex.Message}");
            return [];
        }

        if (allFiles.Count == 0)
        {
            return [];
        }

        ConcurrentDictionary<string, string> sourceMap = new(StringComparer.OrdinalIgnoreCase);
        int processed = 0;
        int total = allFiles.Count;

        await AnsiConsole.Status()
            .StartAsync("Scanning library source URLs...", async ctx =>
            {
                await Parallel.ForEachAsync(allFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (file, cancellationToken) =>
                {
                    string? source = AudioProber.GetSource(file);
                    if (!string.IsNullOrWhiteSpace(source))
                    {
                        sourceMap.TryAdd(source, file);
                    }

                    int current = Interlocked.Increment(ref processed);
                    ctx.Status = $"Scanning library source URLs ({current}/{total})...";
                    return ValueTask.CompletedTask;
                });
            });

        return new Dictionary<string, string>(sourceMap, StringComparer.OrdinalIgnoreCase);
    }

    private static void ReconcileRenamedTracks(List<Track> tracks, Dictionary<string, string> sourceMap)
    {
        int reconciledCount = 0;

        foreach (Track track in tracks)
        {
            if (string.IsNullOrWhiteSpace(track.Source))
            {
                continue;
            }

            string expectedPath = TrackProcessor.GetOutputFile(track);
            if (File.Exists(expectedPath))
            {
                continue;
            }

            if (sourceMap.TryGetValue(track.Source, out string? actualPath) && File.Exists(actualPath))
            {
                string? targetDir = Path.GetDirectoryName(expectedPath);
                if (!string.IsNullOrEmpty(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                try
                {
                    File.Move(actualPath, expectedPath, overwrite: true);
                    reconciledCount++;
                    sourceMap.Remove(track.Source);
                    sourceMap[track.Source] = expectedPath;
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to move reconciled track from '{actualPath}' to '{expectedPath}': {ex.Message}");
                }
            }
        }

        if (reconciledCount > 0)
        {
            Log.Success($"Reconciled {reconciledCount} moved or renamed tracks on disk.");
        }
    }

    private static async Task<(List<Track> Pending, int UpToDate, int MetadataUpdates, int NewDownloads)> FilterPendingTracksAsync(List<Track> tracks)
    {
        List<Track> pending = [];
        int upToDate = 0;
        int metadataUpdates = 0;
        int newDownloads = 0;
        int total = tracks.Count;

        if (total == 0)
        {
            return (pending, upToDate, metadataUpdates, newDownloads);
        }

        (bool IsUpToDate, bool IsNewDownload)[] results = new (bool IsUpToDate, bool IsNewDownload)[total];
        int processed = 0;
        ConcurrentQueue<string> selfHealMessages = new();

        await AnsiConsole.Status()
            .StartAsync("Verifying metadata...", async ctx =>
            {
                await Parallel.ForEachAsync(Enumerable.Range(0, total), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (i, cancellationToken) =>
                {
                    Track track = tracks[i];
                    string outputFile = TrackProcessor.GetOutputFile(track);
                    bool isUpToDate = false;
                    bool isNewDownload = true;

                    if (File.Exists(outputFile))
                    {
                        isNewDownload = false;
                        isUpToDate = AudioProber.IsMetadataUpToDate(outputFile, track, out _, selfHealMessages.Enqueue);
                    }

                    results[i] = (isUpToDate, isNewDownload);

                    int current = Interlocked.Increment(ref processed);
                    ctx.Status = $"Verifying metadata ({current}/{total})...";
                });
            });

        while (selfHealMessages.TryDequeue(out string? message))
        {
            Log.Success(message);
        }

        for (int i = 0; i < total; i++)
        {
            (bool isUpToDate, bool isNewDownload) = results[i];
            if (isUpToDate)
            {
                upToDate++;
            }
            else
            {
                pending.Add(tracks[i]);
                if (isNewDownload)
                {
                    newDownloads++;
                }
                else
                {
                    metadataUpdates++;
                }
            }
        }

        return (pending, upToDate, metadataUpdates, newDownloads);
    }

    private static void PrintPreFlightStats(int total, int upToDate, int pending, int metadataUpdates, int newDownloads)
    {
        AnsiConsole.MarkupLine($"[gray]Database tracks:[/] [white]{total}[/]");
        AnsiConsole.MarkupLine($"[gray]Up to date:[/]      [white]{upToDate}[/]");
        AnsiConsole.MarkupLine($"[cyan]Pending actions:[/]  [white]{pending}[/] [gray]({metadataUpdates} metadata updates, {newDownloads} new downloads)[/]");
        Console.WriteLine();
    }

    private static void PrintPostFlightStats(int downloaded, int metadataUpdated, int failed, int upToDate)
    {
        AnsiConsole.MarkupLine("[green]Processing results:[/]");
        AnsiConsole.MarkupLine($"[gray]  Newly downloaded:[/] [white]{downloaded}[/]");
        AnsiConsole.MarkupLine($"[gray]  Metadata updated:[/] [white]{metadataUpdated}[/]");

        if (failed > 0)
        {
            AnsiConsole.MarkupLine($"[yellow]  Failed downloads:[/] [white]{failed}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[gray]  Failed downloads:[/] [white]{failed}[/]");
        }

        AnsiConsole.MarkupLine($"[gray]  Up to date:[/]       [white]{upToDate}[/]");
        Console.WriteLine();
    }

    private static async Task<(int Downloaded, int MetadataUpdated, int Failed, int UpToDate)> ProcessQueueAsync(List<Track> queue, int alreadyDownloadedCount)
    {
        int downloaded = 0;
        int metadataUpdated = 0;
        int failed = 0;
        int upToDate = alreadyDownloadedCount;
        int total = queue.Count;

        for (int i = 0; i < total; i++)
        {
            Track track = queue[i];
            TrackProcessStatus status = TrackProcessStatus.Failed;

            try
            {
                TrackProcessor trackProcessor = new(track, i + 1, total);
                status = await trackProcessor.ProcessAsync();
            }
            catch (Exception ex)
            {
                Log.Error($"Processing failed for track '{track.Title}': {ex.Message}");
            }

            switch (status)
            {
                case TrackProcessStatus.Success:
                    downloaded++;
                    break;
                case TrackProcessStatus.Failed:
                    failed++;
                    break;
                case TrackProcessStatus.Skipped:
                    upToDate++;
                    break;
                case TrackProcessStatus.MetadataUpdated:
                    metadataUpdated++;
                    break;
            }

            if (status != TrackProcessStatus.Skipped)
            {
                Console.WriteLine();
            }

            bool downloadAttempted = status == TrackProcessStatus.Success;
            if (downloadAttempted && SettingsManager.Current.DelayBetweenDownloadsMs > 0)
            {
                await Task.Delay(SettingsManager.Current.DelayBetweenDownloadsMs);
            }
        }

        return (downloaded, metadataUpdated, failed, upToDate);
    }
}