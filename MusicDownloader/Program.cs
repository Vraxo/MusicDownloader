using MusicDownloader.Common;
using MusicDownloader.Infrastructure;
using MusicDownloader.Workflows;

namespace MusicDownloader;

internal class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            Directory.CreateDirectory(SettingsManager.Current.BaseDataDir);

            if (args.Any(a => a.Equals("playlist", StringComparison.OrdinalIgnoreCase)))
            {
                PlaylistWriter.GeneratePlaylists();
            }
            else if (args.Any(a => a.Equals("process", StringComparison.OrdinalIgnoreCase)))
            {
                ManualProcessor.Run();
            }
            else
            {
                Log.Info("Starting download processing... (use 'playlist' or 'process' arguments for other tools)");
                await ProcessTracksFromCsvAsync();
                Log.Success("All downloads and processing finished.");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Fatal error: {ex.Message}");
        }

        Console.WriteLine();
        Log.Info("Press any key to exit...");
        Console.ReadKey();
    }

    private static async Task ProcessTracksFromCsvAsync()
    {
        List<Track> allTracks = TomlTrackReader.ReadAllTracks();
        if (allTracks.Count == 0)
        {
            return;
        }

        Console.WriteLine();

        foreach (Track track in allTracks)
        {
            bool downloadAttempted = false;
            try
            {
                TrackProcessor trackProcessor = new(track);
                downloadAttempted = await trackProcessor.ProcessAsync();
            }
            catch (Exception ex)
            {
                Log.Error($"Processing failed for track '{track.Title}': {ex.Message}");
            }

            if (downloadAttempted && SettingsManager.Current.DelayBetweenDownloadsMs > 0)
            {
                await Task.Delay(SettingsManager.Current.DelayBetweenDownloadsMs);
            }
        }
    }
}