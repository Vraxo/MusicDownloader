using MusicDownloader;

namespace PlaylistSorterApp;

public static class PlaylistSorter
{
    public static void SortAllPlaylists()
    {
        Log.Info("Starting playlist sorting...");

        try
        {
            List<string> playlistFiles = Directory.EnumerateFiles(AppSettings.BaseDataDir, "*.m3u", SearchOption.AllDirectories).ToList();
            int sortedCount = 0;

            foreach (string playlistFile in playlistFiles)
            {
                if (!ProcessSinglePlaylist(playlistFile))
                {
                    continue;
                }

                sortedCount++;
            }

            Console.WriteLine();
            LogSummary(playlistFiles.Count, sortedCount);
        }
        catch (DirectoryNotFoundException)
        {
            Log.Warning($"Base data directory '{AppSettings.BaseDataDir}' not found. Skipping playlist sort.");
        }
        catch (Exception ex)
        {
            Log.Error($"An error occurred during playlist sorting: {ex.Message}");
        }
    }

    private static bool ProcessSinglePlaylist(string playlistFile)
    {
        string shortName = Path.GetFileName(playlistFile);

        try
        {
            Log.Action($"Checking playlist: {shortName}");

            string[] allLines = File.ReadAllLines(playlistFile);

            if (allLines.Length > 0 && allLines[0].Contains("MANUAL", StringComparison.OrdinalIgnoreCase))
            {
                Log.Info($"Playlist '{shortName}' is marked for manual sorting, skipping.");
                return false;
            }

            List<string> originalLines = allLines
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            if (originalLines.Count == 0)
            {
                Log.Info($"Playlist '{shortName}' is empty, skipping.");
                return false;
            }

            List<string> sortedLines = [.. originalLines.OrderBy(line => Path.GetFileNameWithoutExtension(line), StringComparer.OrdinalIgnoreCase)];

            if (originalLines.SequenceEqual(sortedLines))
            {
                Log.Info($"Playlist '{shortName}' is already sorted.");
                return false;
            }

            File.WriteAllLines(playlistFile, sortedLines);
            Log.Success($"Successfully sorted {shortName}");

            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to process playlist '{shortName}': {ex.Message}");
            return false;
        }
    }

    private static void LogSummary(int foundCount, int sortedCount)
    {
        if (foundCount == 0)
        {
            Log.Info("No .m3u playlists found to process.");
            return;
        }

        if (sortedCount > 0)
        {
            Log.Success($"Playlist sorting finished. Sorted {sortedCount} of {foundCount} playlist(s).");
        }
        else
        {
            Log.Info("Playlist sorting finished. All found playlists were already sorted.");
        }
    }
}