using MusicDownloader.Infrastructure;

namespace MusicDownloader.Common;

internal static class MarkdownTrackReader
{
    public static async Task<List<Track>> ReadAllTracksAsync()
    {
        string dbDir = SettingsManager.Current.DatabaseDir;

        if (!Directory.Exists(dbDir))
        {
            Log.Error($"Database directory '{dbDir}' not found.");
            return [];
        }

        List<string> mdFiles = [.. Directory.EnumerateFiles(dbDir, "*.md", SearchOption.AllDirectories)];

        if (mdFiles.Count == 0)
        {
            Log.Warning($"No .md files found in '{dbDir}'.");
            return [];
        }

        Log.Info("Verifying database file formatting...");

        List<Track> tracks = [];
        int successfullyLoadedFiles = 0;
        int reformattedCount = 0;

        foreach (string mdFile in mdFiles)
        {
            (List<Track> fileTracks, bool wasReformatted) = await GetTracksFromSingleMarkdownAsync(mdFile);
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

    public static async Task<(List<Track> Tracks, bool WasReformatted)> GetTracksFromSingleMarkdownAsync(string filePath)
    {
        try
        {
            string content = await File.ReadAllTextAsync(filePath);
            (Track track, string body) = MarkdownTrackFormatter.Parse(content, filePath);

            if (string.IsNullOrWhiteSpace(track.Source))
            {
                return ([], false);
            }

            Track trackWithFile = track with { DatabaseFilePath = filePath };

            string formatted = MarkdownTrackFormatter.Format(trackWithFile, body);
            string contentNormalized = content.Replace("\r\n", "\n").Trim();
            string formattedNormalized = formatted.Replace("\r\n", "\n").Trim();
            bool wasReformatted = false;

            if (!string.Equals(contentNormalized, formattedNormalized, StringComparison.Ordinal))
            {
                await File.WriteAllTextAsync(filePath, formatted);
                Log.Info($"Updated formatting for '{Path.GetFileName(filePath)}'");
                wasReformatted = true;
            }

            return ([trackWithFile], wasReformatted);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to read or parse Markdown file '{filePath}': {ex.Message}");
            return ([], false);
        }
    }
}