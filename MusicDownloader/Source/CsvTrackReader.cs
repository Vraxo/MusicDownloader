using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace MusicDownloader;

public static class CsvTrackReader
{
    public static List<Track> ReadAllTracks()
    {
        string csvDir = SettingsManager.Current.CsvDir;

        if (!Directory.Exists(csvDir))
        {
            Log.Error($"CSV input directory '{csvDir}' not found.");
            return [];
        }

        List<string> csvFiles = Directory.EnumerateFiles(csvDir, "*.csv").ToList();

        if (csvFiles.Count == 0)
        {
            Log.Warning($"No .csv files found in '{csvDir}'.");
            return [];
        }

        List<Track> tracks = [];

        foreach (string csvFile in csvFiles)
        {
            Console.WriteLine();
            Log.Action($"Reading collection: {Path.GetFileName(csvFile)}");
            tracks.AddRange(GetTracksFromSingleCsv(csvFile));
        }

        return tracks;
    }

    public static IEnumerable<Track> GetTracksFromSingleCsv(string filePath)
    {
        CsvConfiguration config = new(CultureInfo.InvariantCulture)
        {
            Delimiter = "|",
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            HeaderValidated = null,
            PrepareHeaderForMatch = args => args.Header.ToLowerInvariant(),
            BadDataFound = context =>
            {
                Log.Warning($"Skipping bad CSV line in {Path.GetFileName(filePath)} at row {context.RawRecord.Length}: {context.RawRecord}");
            }
        };

        try
        {
            using StreamReader reader = new(filePath);
            using CsvReader csv = new(reader, config);

            _ = csv.Context.RegisterClassMap<TrackMap>();

            var records = csv.GetRecords<Track>().Select(ProcessUrl).ToList();

            return records;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to read or parse CSV file '{filePath}': {ex.Message}");
            return [];
        }
    }

    private static Track ProcessUrl(Track track)
    {
        return string.IsNullOrWhiteSpace(track.Url) || track.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? track
            : (track with
            {
                Url = $"https://www.youtube.com/watch?v={track.Url.Trim()}"
            });
    }
}