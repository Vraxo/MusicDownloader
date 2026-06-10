using MusicDownloader.Infrastructure;
using Tomlyn;

namespace MusicDownloader.Common;

internal static class TomlTrackReader
{
    public static async Task<List<Track>> ReadAllTracksAsync()
    {
        string tomlDir = SettingsManager.Current.DatabaseDir;

        if (!Directory.Exists(tomlDir))
        {
            Log.Error($"TOML input directory '{tomlDir}' not found.");
            return [];
        }

        List<string> tomlFiles = [.. Directory.EnumerateFiles(tomlDir, "*.toml")];

        if (tomlFiles.Count == 0)
        {
            Log.Warning($"No .toml files found in '{tomlDir}'.");
            return [];
        }

        List<Track> tracks = [];
        int successfullyLoadedFiles = 0;

        foreach (string tomlFile in tomlFiles)
        {
            List<Track> fileTracks = await GetTracksFromSingleTomlAsync(tomlFile);
            if (fileTracks.Count <= 0)
            {
                continue;
            }

            tracks.AddRange(fileTracks);
            successfullyLoadedFiles++;
        }

        if (tracks.Count > 0)
        {
            Log.Success($"Successfully loaded {tracks.Count} tracks from {successfullyLoadedFiles} collections.");
        }

        return tracks;
    }

    public static async Task<List<Track>> GetTracksFromSingleTomlAsync(string filePath)
    {
        try
        {
            string content = await File.ReadAllTextAsync(filePath);
            SongCollection? collection = TomlSerializer.Deserialize<SongCollection>(content);

            if (collection?.Song is null)
            {
                return [];
            }

            return [.. collection.Song.Select(track =>
            {
                if (!string.IsNullOrWhiteSpace(track.Source))
                {
                    return track with
                    {
                        Source = NormalizeSource(track.Source)
                    };
                }
                return track;
            }).Where(t => !string.IsNullOrWhiteSpace(t.Source))];
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to read or parse TOML file '{filePath}': {ex.Message}");
            return [];
        }
    }

    private static string NormalizeSource(string source)
    {
        string trimmed = source.Trim();

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        if (trimmed.Contains('.') || trimmed.Contains('/'))
        {
            return $"https://{trimmed}";
        }

        if (trimmed.Length == 11)
        {
            return $"https://www.youtube.com/watch?v={trimmed}";
        }

        return trimmed;
    }
}