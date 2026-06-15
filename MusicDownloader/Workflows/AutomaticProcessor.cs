using MusicDownloader.Common;
using MusicDownloader.Infrastructure;
using Spectre.Console;

namespace MusicDownloader.Workflows;

internal static class AutomaticProcessor
{
    public static async Task RunAsync()
    {
        List<Track> allTracks = await TomlTrackReader.ReadAllTracksAsync();
        if (allTracks.Count == 0)
        {
            return;
        }

        (List<Track> pendingTracks, int alreadyDownloadedCount, int metadataUpdatesCount, int newDownloadsCount) = await FilterPendingTracksAsync(allTracks);

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

    private static async Task<(List<Track> Pending, int UpToDate, int MetadataUpdates, int NewDownloads)> FilterPendingTracksAsync(IReadOnlyList<Track> tracks)
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

        await AnsiConsole.Progress()
            .Columns([
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn()
            ])
            .StartAsync(async ctx =>
            {
                ProgressTask progressTask = ctx.AddTask("[cyan]Verifying metadata[/]", autoStart: true, maxValue: total);

                await Parallel.ForEachAsync(Enumerable.Range(0, total), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (i, cancellationToken) =>
                {
                    Track track = tracks[i];
                    string outputFile = TrackProcessor.GetOutputFile(track);
                    bool isUpToDate = false;
                    bool isNewDownload = true;

                    if (File.Exists(outputFile))
                    {
                        isNewDownload = false;
                        isUpToDate = AudioProber.IsMetadataUpToDate(outputFile, track, out _);
                    }

                    results[i] = (isUpToDate, isNewDownload);
                    progressTask.Increment(1);
                });
            });

        for (int i = 0; i < total; i++)
        {
            var (isUpToDate, isNewDownload) = results[i];
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

    private static async Task<(int Downloaded, int MetadataUpdated, int Failed, int UpToDate)> ProcessQueueAsync(IReadOnlyList<Track> queue, int alreadyDownloadedCount)
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