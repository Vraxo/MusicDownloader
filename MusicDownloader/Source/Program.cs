namespace MusicDownloader;

class Program
{
    static async Task Main()
    {
        try
        {
            Directory.CreateDirectory(AppSettings.BaseDataDir);
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
        Console.ReadKey();
    }

    static async Task ProcessTracksFromCsvAsync()
    {
        List<Track> allTracks = CsvTrackReader.ReadAllTracks();
        if (allTracks.Count == 0)
        {
            return;
        }

        ParallelOptions options = new()
        {
            MaxDegreeOfParallelism = 1
        };

        await Parallel.ForEachAsync(allTracks, options, async (track, _) =>
        {
            try
            {
                TrackProcessor trackProcessor = new(track);
                await trackProcessor.ProcessAsync();
            }
            catch (Exception ex)
            {
                Log.Error($"Processing failed for track '{track.Title}': {ex.Message}");
            }
        });
    }
}