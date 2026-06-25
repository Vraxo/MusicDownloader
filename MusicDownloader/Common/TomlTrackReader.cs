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
            if (fileTracks.Count == 0)
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
            SongCollection? collection = TomlSerializer.Deserialize<SongCollection?>(content);

            if (collection?.Song is null)
            {
                return [];
            }

            string formatted = TomlTrackFormatter.Format(collection);
            string contentNormalized = content.Replace("\r\n", "\n").Trim();
            string formattedNormalized = formatted.Replace("\r\n", "\n").Trim();

            if (!string.Equals(contentNormalized, formattedNormalized, StringComparison.Ordinal))
            {
                await File.WriteAllTextAsync(filePath, formatted);
            }

            return [.. collection.Song.Where(t => !string.IsNullOrWhiteSpace(t.Source))];
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to read or parse TOML file '{filePath}': {ex.Message}");
            return [];
        }
    }
}