using MusicDownloader.Core;
using MusicDownloader.Infrastructure;
using MusicDownloader.Stages.Processing;
using Spectre.Console;
using System.Collections.Concurrent;

namespace MusicDownloader.Orchestration;

internal sealed class TrackQueueFilter
{
    public async Task<(List<Track> Pending, int UpToDate, int MetadataUpdates, int NewDownloads)> FilterAsync(List<Track> tracks)
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
                await Parallel.ForEachAsync(Enumerable.Range(0, total), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (i, cancellationToken) =>
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
                    return ValueTask.CompletedTask;
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
}