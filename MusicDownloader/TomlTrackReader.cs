using Tomlyn;
using Tomlyn.Model;

namespace MusicDownloader;

public static class TomlTrackReader
{
    public static List<Track> ReadAllTracks()
    {
        string tomlDir = SettingsManager.Current.CsvDir; // Reusing CsvDir setting for TOML files

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

        foreach (string tomlFile in tomlFiles)
        {
            Console.WriteLine();
            Log.Action($"Reading collection: {Path.GetFileName(tomlFile)}");
            tracks.AddRange(GetTracksFromSingleToml(tomlFile));
        }

        return tracks;
    }

    public static IEnumerable<Track> GetTracksFromSingleToml(string filePath)
    {
        try
        {
            string content = File.ReadAllText(filePath);
            TomlTable toml = Toml.ToModel(content);

            // Try to find track arrays - support multiple possible names
            string[] possibleArrayNames = ["track", "song", "tracks", "songs"];
            TomlTableArray? trackTables = null;
            string foundName = "";

            foreach (string name in possibleArrayNames)
            {
                if (toml.ContainsKey(name) && toml[name] is TomlTableArray array)
                {
                    trackTables = array;
                    foundName = name;
                    break;
                }
            }

            if (trackTables is null)
            {
                Log.Warning($"No track/song array found in {Path.GetFileName(filePath)}. Expected [[track]], [[song]], [[tracks]], or [[songs]]");
                return [];
            }

            Log.Info($"Found {trackTables.Count} entries in '{foundName}' array in {Path.GetFileName(filePath)}");
            List<Track> tracks = [];

            foreach (TomlTable trackTable in trackTables)
            {
                Track track = ParseTrack(trackTable);
                if (!string.IsNullOrWhiteSpace(track.Url))
                {
                    tracks.Add(track);
                }
            }

            return tracks;
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
        
        // Convert YouTube ID to full URL if needed
        if (!string.IsNullOrWhiteSpace(url) && !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            url = $"https://www.youtube.com/watch?v={url.Trim()}";
        }

        // Parse tags - support both TOML array and comma-separated string
        IReadOnlyList<string> tags = ParseTags(table);
        
        return new Track
        {
            Title = table.GetValueOrDefault<string>("title") ?? string.Empty,
            Artist = table.GetValueOrDefault<string>("artist") ?? string.Empty,
            Album = table.GetValueOrDefault<string>("album") ?? string.Empty,
            Url = url,
            Range = table.GetValueOrDefault<string>("range") ?? string.Empty,
            Tempo = table.GetValueOrDefault<string>("tempo") ?? string.Empty,
            Loop = table.GetValueOrDefault<string>("loop") ?? "1",
            TrackNumber = table.GetValueOrDefault<string>("tracknumber") ?? string.Empty,
            DiscNumber = table.GetValueOrDefault<string>("discnumber") ?? string.Empty,
            Tags = tags
        };
    }

    private static IReadOnlyList<string> ParseTags(TomlTable table)
    {
        // Try to get tags as TOML array first
        if (table.ContainsKey("tags") && table["tags"] is TomlArray tagArray)
        {
            List<string> tags = [];
            foreach (var item in tagArray)
            {
                if (item is string tag && !string.IsNullOrWhiteSpace(tag))
                {
                    tags.Add(tag.Trim());
                }
            }
            return tags;
        }
        
        // Fallback to comma-separated string
        string? tagsString = table.GetValueOrDefault<string>("tags");
        if (string.IsNullOrWhiteSpace(tagsString))
        {
            return [];
        }

        return tagsString.Split(',')
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrEmpty(tag))
            .ToList();
    }

    private static T? GetValueOrDefault<T>(this TomlTable table, string key) where T : class
    {
        return table.ContainsKey(key) ? table[key] as T : null;
    }
}