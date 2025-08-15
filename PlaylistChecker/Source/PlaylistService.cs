namespace MusicDownloader;

public static class PlaylistService
{
    public static Dictionary<string, List<string>> BuildSongPlaylistMap()
    {
        string[] supportedExtensions = GetSupportedExtensions();
        IEnumerable<string> musicFiles = FindMusicFiles(supportedExtensions);

        Dictionary<string, List<string>> songToPlaylistsMap = InitializeSongMap(musicFiles);

        if (songToPlaylistsMap.Count == 0)
        {
            Log.Info($"No music files with supported extensions " +
                $"({string.Join(", ", supportedExtensions)}) " +
                $"found to check.");
            return songToPlaylistsMap;
        }

        List<string> playlistFiles = FindPlaylistFiles();

        if (playlistFiles.Count == 0)
        {
            Log.Info("No .m3u playlists found. All songs will be marked as not in any playlist.");
        }
        else
        {
            ProcessPlaylists(playlistFiles, songToPlaylistsMap);
        }

        return songToPlaylistsMap;
    }

    private static string[] GetSupportedExtensions()
    {
        return new[] { ".mp3", $".{AppSettings.AudioFormat}" }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> FindMusicFiles(string[] supportedExtensions)
    {
        return Directory.EnumerateFiles(AppSettings.BaseDataDir, "*.*", SearchOption.AllDirectories)
            .Where(file => supportedExtensions.Contains(Path.GetExtension(file)?.ToLowerInvariant()));
    }

    private static List<string> FindPlaylistFiles()
    {
        return Directory.EnumerateFiles(AppSettings.BaseDataDir, "*.m3u", SearchOption.AllDirectories).ToList();
    }

    private static Dictionary<string, List<string>> InitializeSongMap(IEnumerable<string> musicFiles)
    {
        Dictionary<string, List<string>> songMap = new(StringComparer.OrdinalIgnoreCase);
        
        foreach (string musicFile in musicFiles)
        {
            songMap[Path.GetFullPath(musicFile)] = [];
        }

        return songMap;
    }

    private static void ProcessPlaylists(IEnumerable<string> playlistFiles, Dictionary<string, List<string>> songToPlaylistsMap)
    {
        foreach (string playlistFile in playlistFiles)
        {
            string? playlistDir = Path.GetDirectoryName(playlistFile);
            
            if (playlistDir is null)
            {
                continue;
            }

            string playlistName = Path.GetFileNameWithoutExtension(playlistFile);
            string[] lines = File.ReadAllLines(playlistFile);

            foreach (string line in lines.Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#")))
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
}