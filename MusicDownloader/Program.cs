namespace MusicDownloader;

internal class Program
{
    private static async Task Main()
    {
        try
        {
            _ = Directory.CreateDirectory(SettingsManager.Current.BaseDataDir);

            // Diagnostic check removed

            Log.Info("Starting processing...");
            await ProcessTracksFromCsvAsync();
            Log.Success("All downloads and processing finished.");
        }
        catch (Exception ex)
        {
            Log.Error($"Fatal error: {ex.Message}");
        }

        Console.WriteLine();
        Log.Info("Press any key to exit...");
        _ = Console.ReadKey();
    }

    private static async Task ProcessTracksFromCsvAsync()
    {
        List<Track> allTracks = TomlTrackReader.ReadAllTracks();
        if (allTracks.Count == 0)
        {
            return;
        }

        // Add a blank line for better readability
        Console.WriteLine();

        foreach (var track in allTracks)
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