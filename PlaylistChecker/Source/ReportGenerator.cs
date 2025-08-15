using System.Text;

namespace MusicDownloader;

/// <summary>
/// A summary of the playlist report.
/// </summary>
/// <param name="TotalSongs">Total number of songs found.</param>
/// <param name="SongsInPlaylists">Number of songs present in at least one playlist.</param>
/// <param name="SongsNotInPlaylists">Number of songs not present in any playlist.</param>
public record ReportSummary(int TotalSongs, int SongsInPlaylists, int SongsNotInPlaylists);

/// <summary>
/// Generates a formatted report from song-to-playlist data.
/// </summary>
public class ReportGenerator
{
    public (string ReportContent, ReportSummary Summary) Generate(IReadOnlyDictionary<string, List<string>> songToPlaylistsMap)
    {
        var songsForReport = songToPlaylistsMap
            .Select(kvp => new
            {
                RelativePath = Path.GetRelativePath(AppSettings.BaseDataDir, kvp.Key),
                Playlists = kvp.Value
            })
            .OrderBy(s => s.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        StringBuilder reportContent = new();
        int maxSongPathWidth = songsForReport.Any() ? songsForReport.Max(s => s.RelativePath.Length) : 0;
        maxSongPathWidth = int.Max(maxSongPathWidth, "Song".Length);

        string songHeader = "Song".PadRight(maxSongPathWidth);
        const string playlistHeader = "Playlists";

        reportContent.AppendLine("=== Playlist Membership Report ===");
        reportContent.AppendLine();
        reportContent.AppendLine($"{songHeader} | {playlistHeader}");
        reportContent.AppendLine($"{new string('-', maxSongPathWidth)}-|-{new string('-', playlistHeader.Length)}");

        foreach (var song in songsForReport)
        {
            string songColumn = song.RelativePath.PadRight(maxSongPathWidth);
            string playlistsColumn = song.Playlists.Count != 0
                ? string.Join(", ", song.Playlists.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                : "<None>";

            reportContent.AppendLine($"{songColumn} | {playlistsColumn}");
        }

        int totalSongs = songsForReport.Count;
        int songsNotInPlaylists = songsForReport.Count(s => s.Playlists.Count == 0);
        int songsInPlaylists = totalSongs - songsNotInPlaylists;
        var summary = new ReportSummary(totalSongs, songsInPlaylists, songsNotInPlaylists);

        reportContent.AppendLine();
        reportContent.AppendLine("--- Report Summary ---");
        reportContent.AppendLine($"Total Songs: {summary.TotalSongs}");
        reportContent.AppendLine($"Songs in at least one playlist: {summary.SongsInPlaylists}");
        reportContent.AppendLine($"Songs not in any playlist: {summary.SongsNotInPlaylists}");

        return (reportContent.ToString(), summary);
    }
}