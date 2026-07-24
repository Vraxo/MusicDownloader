using MusicDownloader.Core;
using MusicDownloader.Infrastructure;
using MusicDownloader.Stages.Processing;
using Spectre.Console;
using System.Collections.Concurrent;

namespace MusicDownloader.Orchestration;

internal sealed class LibraryScanner(string baseDir)
{
    public async Task<Dictionary<string, string>> ScanBySourceAsync()
    {
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

    public void ReconcileRenamed(List<Track> tracks, Dictionary<string, string> sourceMap)
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
}