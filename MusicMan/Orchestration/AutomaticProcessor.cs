using MusicMan.Core;
using MusicMan.Infrastructure;
using MusicMan.Stages.Storage;
using Spectre.Console;

namespace MusicMan.Orchestration;

internal static class AutomaticProcessor
{
    public static async Task RunAsync(CsvTrackRepository repository)
    {
        List<Track> allTracks = await repository.ReadAllTracksAsync();
        if (allTracks.Count == 0)
        {
            return;
        }

        LibraryScanner scanner = new(SettingsManager.Current.BaseDataDir);
        Dictionary<string, string> sourceMap = await scanner.ScanBySourceAsync();
        LibraryScanner.ReconcileRenamed(allTracks, sourceMap);

        TrackQueueFilter filter = new();
        (List<Track> pendingTracks, int upToDateCount, int metadataCount, int newCount) = await TrackQueueFilter.FilterAsync(allTracks);

        if (pendingTracks.Count == 0)
        {
            Log.Success($"All {allTracks.Count} tracks are already downloaded and up to date!");
            return;
        }

        PrintPreFlightStats(allTracks.Count, upToDateCount, pendingTracks.Count, metadataCount, newCount);

        QueueProcessor queueProcessor = new(repository);
        (int downloaded, int metadataUpdated, int failed, int finalUpToDate) = await queueProcessor.ProcessAsync(pendingTracks, upToDateCount);

        PrintPostFlightStats(downloaded, metadataUpdated, failed, finalUpToDate);
    }

    private static void PrintPreFlightStats(int total, int upToDate, int pending, int metadataUpdates, int newDownloads)
    {
        AnsiConsole.MarkupLine($"[gray]Database tracks:[/] [white]{total}[/]");
        AnsiConsole.MarkupLine($"[gray]Up to date:[/]      [white]{upToDate}[/]");
        AnsiConsole.MarkupLine($"[cyan]Pending actions:[/] [white]{pending}[/] [gray]({metadataUpdates} metadata updates, {newDownloads} new downloads)[/]");
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
    }
}