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
        IEnumerable<Track> tracks = GetTracksFromCsv();
        
        ParallelOptions options = new() 
        { 
            MaxDegreeOfParallelism = 1
        };

        await Parallel.ForEachAsync(tracks, options, async (track, _) =>
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

    static IEnumerable<Track> GetTracksFromCsv()
    {
        if (!File.Exists(AppSettings.CsvFile))
        {
            Log.Error($"CSV input file '{AppSettings.CsvFile}' not found.");
            Environment.Exit(1);
        }

        return File.ReadAllLines(AppSettings.CsvFile)
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line =>
            {
                string[] fields = line.Split('|');
                if (fields.Length < 6)
                {
                    Log.Warning("Skipping invalid line: not enough fields.");
                    return null;
                }
                return new Track(fields);
            })
            .Where(track => track is not null)!; // Filter out nulls from invalid lines
    }
}