using MusicDownloader.Common;
using MusicDownloader.Infrastructure;

namespace MusicDownloader.Workflows;

internal static class AutomaticProcessor
{
    private static readonly object ConsoleLock = new();

    public static async Task RunAsync()
    {
        List<Track> allTracks = await TomlTrackReader.ReadAllTracksAsync();
        if (allTracks.Count == 0)
        {
            return;
        }

        Console.WriteLine();

        (List<Track> pendingTracks, int alreadyDownloadedCount) = await FilterPendingTracksAsync(allTracks);

        if (pendingTracks.Count == 0)
        {
            Log.Success($"All {allTracks.Count} tracks are already downloaded and up to date!");
            return;
        }

        PrintPreFlightStats(allTracks.Count, alreadyDownloadedCount, pendingTracks.Count);

        (int downloaded, int metadataUpdated, int failed, int updatedCount) = await ProcessQueueAsync(pendingTracks, alreadyDownloadedCount);

        Console.WriteLine();
        Log.Success(
            $"Processing finished: {downloaded} newly downloaded, {metadataUpdated} metadata updated, {failed} failed. " +
            $"({updatedCount} were already up to date)");
    }

    private static async Task<(List<Track> Pending, int UpToDate)> FilterPendingTracksAsync(IReadOnlyList<Track> tracks)
    {
        List<Track> pending = [];
        int upToDate = 0;
        int completed = 0;
        int total = tracks.Count;

        if (total == 0)
        {
            return (pending, upToDate);
        }

        Log.Info("Verifying downloaded tracks...");
        UpdateProgressBar(0, total);

        object lockObj = new();

        await Parallel.ForEachAsync(tracks, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (track, cancellationToken) =>
        {
            string outputFile = TrackProcessor.GetOutputFile(track);
            bool isUpToDate = false;

            if (File.Exists(outputFile))
            {
                isUpToDate = AudioProber.IsMetadataUpToDate(outputFile, track);
            }

            int currentCompleted = Interlocked.Increment(ref completed);
            UpdateProgressBar(currentCompleted, total);

            lock (lockObj)
            {
                if (isUpToDate)
                {
                    upToDate++;
                }
                else
                {
                    pending.Add(track);
                }
            }
        });

        ClearCurrentLine();

        return (pending, upToDate);
    }

    private static void UpdateProgressBar(int current, int total)
    {
        lock (ConsoleLock)
        {
            double percent = (double)current / total * 100;
            int barWidth = 30;
            int filledWidth = (int)Math.Round(percent / 100 * barWidth);

            string filled = new('█', filledWidth);
            string empty = new('░', barWidth - filledWidth);

            Console.Write("\rVerifying metadata: [");

            Console.ForegroundColor = GetProgressColor(percent);
            Console.Write(filled);

            Console.ResetColor();
            Console.Write($"{empty}] {percent:0}% ({current}/{total})");
        }
    }

    private static ConsoleColor GetProgressColor(double percent)
    {
        if (percent >= 90.0)
        {
            return ConsoleColor.Green;
        }
        if (percent >= 50.0)
        {
            return ConsoleColor.Yellow;
        }
        return ConsoleColor.Cyan;
    }

    private static void ClearCurrentLine()
    {
        lock (ConsoleLock)
        {
            Console.Write($"\r{new string(' ', 79)}\r");
        }
    }

    private static void PrintPreFlightStats(int total, int upToDate, int pending)
    {
        Log.Info($"Database tracks: {total}");
        Log.Info($"Up to date:      {upToDate}");
        Log.Action($"Pending:         {pending}");
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

            bool downloadAttempted = status == TrackProcessStatus.Success;
            if (downloadAttempted && SettingsManager.Current.DelayBetweenDownloadsMs > 0)
            {
                await Task.Delay(SettingsManager.Current.DelayBetweenDownloadsMs);
            }
        }

        return (downloaded, metadataUpdated, failed, upToDate);
    }
}