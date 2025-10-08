using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace MusicDownloader;

public static class CsvTrackReader
{
    public static List<Track> ReadAllTracks()
    {
        if (!Directory.Exists(AppSettings.CsvDir))
        {
            Log.Error($"CSV input directory '{AppSettings.CsvDir}' not found.");
            return new List<Track>();
        }

        List<string> csvFiles = Directory.EnumerateFiles(AppSettings.CsvDir, "*.csv").ToList();
        if (csvFiles.Count == 0)
        {
            Log.Warning($"No .csv files found in '{AppSettings.CsvDir}'.");
            return new List<Track>();
        }

        List<Track> tracks = new();
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
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = "|",
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null, // Suppress errors for optional fields
            HeaderValidated = null, // Don't throw an exception if headers are missing.
            PrepareHeaderForMatch = args => args.Header.ToLowerInvariant(), // Match headers case-insensitively.
            BadDataFound = context =>
            {
                Log.Warning($"Skipping bad CSV line in {Path.GetFileName(filePath)} at row {context.RawRecord.Length}: {context.RawRecord}");
            }
        };

        try
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, config);

            csv.Context.RegisterClassMap<TrackMap>();

            // Process URLs on-the-fly after reading
            var records = csv.GetRecords<Track>().Select(ProcessUrl).ToList();
            return records;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to read or parse CSV file '{filePath}': {ex.Message}");
            return Enumerable.Empty<Track>();
        }
    }

    private static Track ProcessUrl(Track track)
    {
        if (!string.IsNullOrWhiteSpace(track.Url) && !track.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return track with { Url = $"https://www.youtube.com/watch?v={track.Url.Trim()}" };
        }
        return track;
    }
}