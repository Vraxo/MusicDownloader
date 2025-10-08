using System.Text;
using MusicDownloader;

namespace PlaylistGenerator;

public static class PlaylistWriter
{
    public static void GeneratePlaylists()
    {
        List<Track> allTracks = CsvTrackReader.ReadAllTracks();

        if (allTracks.Count == 0)
        {
            Log.Info("No tracks found to process.");
            return;
        }

        Console.WriteLine();

        Dictionary<string, List<Track>> tracksByTag = GroupTracksByTag(allTracks);
        if (tracksByTag.Count == 0)
        {
            Log.Info("No tags found on any tracks. No playlists will be generated.");
            return;
        }

        Log.Info($"Found {allTracks.Count} tracks with {tracksByTag.Count} unique tags.");
        Console.WriteLine();

        foreach ((string tag, List<Track> tracks) in tracksByTag)
        {
            GenerateSinglePlaylist(tag, tracks);
        }
    }

    private static Dictionary<string, List<Track>> GroupTracksByTag(IEnumerable<Track> tracks)
    {
        Dictionary<string, List<Track>> map = new(StringComparer.OrdinalIgnoreCase);

        foreach (Track track in tracks)
        {
            foreach (string tag in track.Tags)
            {
                if (!map.TryGetValue(tag, out List<Track>? trackList))
                {
                    trackList = new List<Track>();
                    map[tag] = trackList;
                }

                trackList.Add(track);
            }
        }

        return map;
    }

    private static void GenerateSinglePlaylist(string tag, IReadOnlyList<Track> tracks)
    {
        Log.Action($"Generating playlist for tag: '{tag}'...");

        List<Track> sortedTracks = [.. tracks
            .OrderBy(t => int.TryParse(t.TrackNumber, out int num) ? num : int.MaxValue)
            .ThenBy(t => t.Title, StringComparer.OrdinalIgnoreCase)];

        StringBuilder contentBuilder = new();

        for (int i = 0; i < sortedTracks.Count; i++)
        {
            Track track = sortedTracks[i];
            string relativePath = Path.Combine(PathUtils.SafeFileName(track.Album), $"{PathUtils.SafeFileName(track.Title)}.{AppSettings.AudioFormat}");

            contentBuilder.AppendLine(relativePath);

            if ((i + 1) % 5 == 0 && (i + 1) < sortedTracks.Count)
            {
                contentBuilder.AppendLine();
            }
        }

        try
        {
            string safeTagFileName = PathUtils.SafeFileName(tag);
            string playlistPath = Path.Combine(AppSettings.BaseDataDir, $"{safeTagFileName}.m3u");
            Directory.CreateDirectory(AppSettings.BaseDataDir);
            File.WriteAllText(playlistPath, contentBuilder.ToString());
            Log.Success($"Successfully created playlist: {Path.GetFileName(playlistPath)} with {tracks.Count} songs.");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to write playlist for tag '{tag}': {ex.Message}");
        }
    }
}