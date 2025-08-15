namespace MusicDownloader;

public static class PlaylistChecker
{
    public static void CheckAllPlaylists()
    {
        Log.Info("Starting playlist check...");

        try
        {
            string[] supportedExtensions = new[] { ".mp3", $".{AppSettings.AudioFormat}" }
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            IEnumerable<string> musicFiles = Directory.EnumerateFiles(AppSettings.BaseDataDir, "*.*", SearchOption.AllDirectories)
                .Where(file => supportedExtensions.Contains(Path.GetExtension(file)?.ToLowerInvariant()));

            Dictionary<string, List<string>> songToPlaylistsMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            
            foreach (string musicFile in musicFiles)
            {
                songToPlaylistsMap[Path.GetFullPath(musicFile)] = new List<string>();
            }

            if (songToPlaylistsMap.Count == 0)
            {
                Log.Info($"No music files with supported extensions ({string.Join(", ", supportedExtensions)}) found to check.");
                return;
            }

            List<string> playlistFiles = Directory.EnumerateFiles(AppSettings.BaseDataDir, "*.m3u", SearchOption.AllDirectories).ToList();

            if (!playlistFiles.Any())
            {
                Log.Info("No .m3u playlists found. All songs will be marked as not in any playlist.");
            }
            else
            {
                ProcessPlaylists(playlistFiles, songToPlaylistsMap);
            }

            PrintReport(songToPlaylistsMap);
        }
        catch (DirectoryNotFoundException)
        {
            Log.Warning($"Base data directory '{AppSettings.BaseDataDir}' not found. Skipping playlist check.");
        }
        catch (Exception ex)
        {
            Log.Error($"An error occurred during playlist check: {ex.Message}");
        }
    }

    private static void ProcessPlaylists(IEnumerable<string> playlistFiles, Dictionary<string, List<string>> songToPlaylistsMap)
    {
        foreach (var playlistFile in playlistFiles)
        {
            string? playlistDir = Path.GetDirectoryName(playlistFile);
            
            if (playlistDir is null)
            {
                continue;
            }

            string playlistName = Path.GetFileNameWithoutExtension(playlistFile);

            string[] lines = File.ReadAllLines(playlistFile);
            
            foreach (string line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                string songFullPath = Path.GetFullPath(Path.Combine(playlistDir, line));

                if (songToPlaylistsMap.TryGetValue(songFullPath, out List<string>? playlists))
                {
                    playlists.Add(playlistName);
                }
                else
                {
                    Log.Warning($"Song '{line}' from playlist '{playlistName}' not found in music library, skipping.");
                }
            }
        }
    }

    private static void PrintReport(IReadOnlyDictionary<string, List<string>> songToPlaylistsMap)
    {
        Console.WriteLine();
        Log.Action("=== Playlist Membership Report ===");

        var songsForReport = songToPlaylistsMap
            .Select(kvp => new
            {
                RelativePath = Path.GetRelativePath(AppSettings.BaseDataDir, kvp.Key),
                Playlists = kvp.Value
            })
            .OrderBy(s => s.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (songsForReport.Count == 0)
        {
            return;
        }

        int maxSongPathWidth = songsForReport.Max(s => s.RelativePath.Length);
        maxSongPathWidth = Math.Max(maxSongPathWidth, "Song".Length);

        string songHeader = "Song".PadRight(maxSongPathWidth);
        const string playlistHeader = "Playlists";

        Log.Success($"{songHeader} | {playlistHeader}");
        Log.Success($"{new string('-', maxSongPathWidth)}-|-{new string('-', playlistHeader.Length)}");

        int songsNotInPlaylists = 0;
        
        foreach (var song in songsForReport)
        {
            string songColumn = song.RelativePath.PadRight(maxSongPathWidth);
            string playlistsColumn;

            if (song.Playlists.Any())
            {
                playlistsColumn = string.Join(", ", song.Playlists.OrderBy(p => p, StringComparer.OrdinalIgnoreCase));
            }
            else
            {
                playlistsColumn = "<None>";
                songsNotInPlaylists++;
            }

            Log.Info($"{songColumn} | {playlistsColumn}");
        }

        Console.WriteLine();
        Log.Action("--- Report Summary ---");
        
        int totalSongs = songsForReport.Count;
        int songsInPlaylists = totalSongs - songsNotInPlaylists;
        
        Log.Info($"Total Songs: {totalSongs}");
        Log.Success($"Songs in at least one playlist: {songsInPlaylists}");

        if (songsNotInPlaylists > 0)
        {
            Log.Warning($"Songs not in any playlist: {songsNotInPlaylists}");
        }
        else
        {
            Log.Success("All songs are in at least one playlist!");
        }
    }
}