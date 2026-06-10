using MusicDownloader.Infrastructure;
using System.Globalization;
using Tomlyn;
using Tomlyn.Model;

namespace MusicDownloader.Common;

internal static class TomlTrackReader
{
    public static List<Track> ReadAllTracks()
    {
        string tomlDir = SettingsManager.Current.CsvDir;

        if (!Directory.Exists(tomlDir))
        {
            Log.Error($"TOML input directory '{tomlDir}' not found.");
            return [];
        }

        List<string> tomlFiles = Directory.EnumerateFiles(tomlDir, "*.toml").ToList();

        if (tomlFiles.Count == 0)
        {
            Log.Warning($"No .toml files found in '{tomlDir}'.");
            return [];
        }

        List<Track> tracks = [];
        int successfullyLoadedFiles = 0;

        foreach (string tomlFile in tomlFiles)
        {
            List<Track> fileTracks = GetTracksFromSingleToml(tomlFile).ToList();
            if (fileTracks.Count > 0)
            {
                tracks.AddRange(fileTracks);
                successfullyLoadedFiles++;
            }
        }

        if (tracks.Count > 0)
        {
            Log.Success($"Successfully loaded {tracks.Count} tracks from {successfullyLoadedFiles} collections.");
        }

        return tracks;
    }

    public static IEnumerable<Track> GetTracksFromSingleToml(string filePath)
    {
        try
        {
            string content = File.ReadAllText(filePath);
            TomlTable toml = TomlSerializer.Deserialize<TomlTable>(content);

            string[] possibleNames = ["track", "song", "tracks", "songs"];
            string? foundName = possibleNames.FirstOrDefault(toml.ContainsKey);

            if (foundName is null || toml[foundName] is not TomlTableArray trackTables)
            {
                Log.Warning($"No track/song array found in {Path.GetFileName(filePath)}. Expected [[track]], [[song]], [[tracks]], or [[songs]]");
                return [];
            }

            return trackTables
                .Select(ParseTrack)
                .Where(t => !string.IsNullOrWhiteSpace(t.Url))
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to read or parse TOML file '{filePath}': {ex.Message}");
            return [];
        }
    }

    private static Track ParseTrack(TomlTable table)
    {
        string url = table.GetValueOrDefault<string>("url") ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(url) && !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            url = $"https://www.youtube.com/watch?v={url.Trim()}";
        }

        return new Track
        {
            Title = table.GetValueOrDefault<string>("title") ?? string.Empty,
            Artist = table.GetValueOrDefault<string>("artist") ?? string.Empty,
            Album = table.GetValueOrDefault<string>("album") ?? string.Empty,
            Url = url,
            Range = table.GetValueOrDefault<string>("range") ?? string.Empty,
            Tempo = ParseTempo(table),
            Loop = table.GetValueOrDefault<string>("loop") ?? "1",
            TrackNumber = table.GetValueOrDefault<string>("tracknumber") ?? string.Empty,
            DiscNumber = table.GetValueOrDefault<string>("discnumber") ?? string.Empty,
            Tags = ParseTags(table)
        };
    }

    private static double? ParseTempo(TomlTable table)
    {
        if (!table.TryGetValue("tempo", out object? val) || val is null)
        {
            return null;
        }

        if (val is double d)
        {
            return d;
        }
        if (val is long l)
        {
            return l;
        }
        if (val is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed))
        {
            return parsed;
        }

        return null;
    }

    private static IReadOnlyList<string> ParseTags(TomlTable table)
    {
        if (table.TryGetValue("tags", out object? tagsObj) && tagsObj is TomlArray tagArray)
        {
            return tagArray
                .OfType<string>()
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .ToList();
        }

        string tagsString = table.GetValueOrDefault<string>("tags") ?? string.Empty;

        return string.IsNullOrWhiteSpace(tagsString)
            ? []
            : tagsString.Split(',').Select(tag => tag.Trim()).Where(tag => !string.IsNullOrEmpty(tag)).ToList();
    }

    private static T? GetValueOrDefault<T>(this TomlTable table, string key) where T : class
    {
        return table.TryGetValue(key, out object? value) ? value as T : null;
    }
}