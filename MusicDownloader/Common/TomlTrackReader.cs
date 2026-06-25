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

        List<string> tomlFiles = [.. Directory.EnumerateFiles(tomlDir, "*.toml", SearchOption.AllDirectories)];

        if (tomlFiles.Count == 0)
        {
            Log.Warning($"No .toml files found in '{tomlDir}'.");
            return [];
        }

        Log.Info("Verifying database file formatting...");

        List<Track> tracks = [];
        int successfullyLoadedFiles = 0;
        int reformattedCount = 0;

        foreach (string tomlFile in tomlFiles)
        {
            (List<Track> fileTracks, bool wasReformatted) = await GetTracksFromSingleTomlAsync(tomlFile);
            if (fileTracks.Count == 0)
            {
                continue;
            }

            if (wasReformatted)
            {
                reformattedCount++;
            }

            tracks.AddRange(fileTracks);
            successfullyLoadedFiles++;
        }

        if (reformattedCount > 0)
        {
            Log.Success($"Reformatted {reformattedCount} database collections.");
        }
        else
        {
            Log.Info("All database collections are already perfectly formatted.");
        }

        if (tracks.Count > 0)
        {
            Log.Success($"Successfully loaded {tracks.Count} tracks from {successfullyLoadedFiles} collections.");
        }

        return tracks;
    }

    public static async Task<(List<Track> Tracks, bool WasReformatted)> GetTracksFromSingleTomlAsync(string filePath)
    {
        try
        {
            string content = await File.ReadAllTextAsync(filePath);
            SongCollection? collection = TomlSerializer.Deserialize<SongCollection?>(content);

            if (collection?.Song is null)
            {
                return ([], false);
            }

            string formatted = TomlTrackFormatter.Format(collection);
            string contentNormalized = content.Replace("\r\n", "\n").Trim();
            string formattedNormalized = formatted.Replace("\r\n", "\n").Trim();
            bool wasReformatted = false;

            if (!string.Equals(contentNormalized, formattedNormalized, StringComparison.Ordinal))
            {
                await File.WriteAllTextAsync(filePath, formatted);
                Log.Info($"Updated formatting for '{Path.GetFileName(filePath)}'");
                wasReformatted = true;
            }

            List<Track> validTracks = [.. collection.Song.Where(t => !string.IsNullOrWhiteSpace(t.Source))];
            return (validTracks, wasReformatted);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to read or parse TOML file '{filePath}': {ex.Message}");
            return ([], false);
        }
    }
}