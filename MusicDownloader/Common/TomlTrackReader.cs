using MusicDownloader.Infrastructure;
using Tomlyn;

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
            List<Track> fileTracks = GetTracksFromSingleToml(tomlFile);
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

    public static List<Track> GetTracksFromSingleToml(string filePath)
    {
        try
        {
            string content = File.ReadAllText(filePath);
            SongCollection? collection = TomlSerializer.Deserialize<SongCollection>(content);

            if (collection?.Song is null)
            {
                return [];
            }

            return [.. collection.Song.Select(track =>
            {
                if (!string.IsNullOrWhiteSpace(track.Url) && !track.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    return track with
                    {
                        Url = $"https://www.youtube.com/watch?v={track.Url.Trim()}"
                    };
                }
                return track;
            }).Where(t => !string.IsNullOrWhiteSpace(t.Url))];
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to read or parse TOML file '{filePath}': {ex.Message}");
            return [];
        }
    }
}