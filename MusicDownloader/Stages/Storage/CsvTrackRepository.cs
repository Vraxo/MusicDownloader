using MusicDownloader.Core;
using MusicDownloader.Infrastructure;
using System.Globalization;
using System.Text;

namespace MusicDownloader.Stages.Storage;

internal sealed class CsvTrackRepository : ITrackRepository
{
    private static readonly string[] Headers = [
        "title", "artist", "album", "track_number", "disc_number",
        "album_artist", "composer", "date", "tags", "cover", "source", "range", "tempo", "loop"
    ];

    private string GetCsvPath()
    {
        return Path.Combine(SettingsManager.Current.DatabaseDir, "tracks.csv");
    }

    public string GetCanonicalCoverLink(string coverFileName)
    {
        return coverFileName;
    }

    public async Task<List<Track>> ReadAllTracksAsync()
    {
        string csvPath = GetCsvPath();
        if (!File.Exists(csvPath))
        {
            await CreateEmptyDatabaseAsync(csvPath);
            return [];
        }

        List<Track> tracks = [];
        try
        {
            string[] lines = await File.ReadAllLinesAsync(csvPath, Encoding.UTF8);
            if (lines.Length <= 1)
            {
                return [];
            }

            List<string> headerLine = ParseCsvLine(lines[0]);
            Dictionary<string, int> headerIndices = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headerLine.Count; i++)
            {
                headerIndices[headerLine[i].Trim()] = i;
            }

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                List<string> fields = ParseCsvLine(line);
                if (fields.Count == 0)
                {
                    continue;
                }

                string GetField(string name)
                {
                    if (headerIndices.TryGetValue(name, out int idx) && idx < fields.Count)
                    {
                        return fields[idx];
                    }
                    return string.Empty;
                }

                int? ParseNullableInt(string val)
                {
                    return int.TryParse(val, out int res) ? res : null;
                }

                double? ParseNullableDouble(string val)
                {
                    return double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out double res) ? res : null;
                }

                List<string> ParseSemicolonList(string val)
                {
                    return string.IsNullOrWhiteSpace(val) ? [] : [.. val.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
                }

                tracks.Add(new Track
                {
                    Title = GetField("title"),
                    Artist = GetField("artist"),
                    Album = GetField("album"),
                    TrackNumber = ParseNullableInt(GetField("track_number")),
                    DiscNumber = ParseNullableInt(GetField("disc_number")),
                    AlbumArtist = string.IsNullOrWhiteSpace(GetField("album_artist")) ? null : GetField("album_artist"),
                    Composer = string.IsNullOrWhiteSpace(GetField("composer")) ? null : GetField("composer"),
                    Date = string.IsNullOrWhiteSpace(GetField("date")) ? null : GetField("date"),
                    Tags = ParseSemicolonList(GetField("tags")),
                    Cover = string.IsNullOrWhiteSpace(GetField("cover")) ? null : GetField("cover"),
                    Source = GetField("source"),
                    Range = ParseSemicolonList(GetField("range")),
                    Tempo = ParseNullableDouble(GetField("tempo")),
                    Loop = int.TryParse(GetField("loop"), out int loopVal) ? loopVal : 1,
                    DatabaseFilePath = csvPath
                });
            }

            Log.Success($"Successfully loaded {tracks.Count} tracks from singular CSV database: '{csvPath}'");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to read CSV database: {ex.Message}");
        }

        return tracks;
    }

    public async Task<Track> UpdateCoverPropertyAsync(Track track, string downloadedCoverPath)
    {
        string csvPath = GetCsvPath();
        if (!File.Exists(csvPath))
        {
            return track;
        }

        string ext = Path.GetExtension(downloadedCoverPath).ToLowerInvariant();
        string coversDir = SettingsManager.Current.CoversDir;
        string destinationFileName;

        if (!string.IsNullOrWhiteSpace(track.Cover))
        {
            destinationFileName = PathUtils.GetCoverFileName(track);
        }
        else
        {
            string safeTitle = PathUtils.SafeFileName(track.Title);
            destinationFileName = $"{safeTitle}{ext}";
        }

        try
        {
            Directory.CreateDirectory(coversDir);
            string destinationPath = Path.Combine(coversDir, destinationFileName);

            if (Path.GetFullPath(downloadedCoverPath) != Path.GetFullPath(destinationPath))
            {
                File.Copy(downloadedCoverPath, destinationPath, overwrite: true);
            }

            string expectedCoverLink = GetCanonicalCoverLink(destinationFileName);
            Track updatedTrack = track with { Cover = expectedCoverLink };

            List<Track> allTracks = await ReadAllTracksAsync();
            for (int i = 0; i < allTracks.Count; i++)
            {
                Track t = allTracks[i];
                bool matches = !string.IsNullOrWhiteSpace(track.Source) && !string.IsNullOrWhiteSpace(t.Source)
                    ? string.Equals(t.Source, track.Source, StringComparison.OrdinalIgnoreCase)
                    : string.Equals(t.Title, track.Title, StringComparison.OrdinalIgnoreCase) && string.Equals(t.Artist, track.Artist, StringComparison.OrdinalIgnoreCase);

                if (matches)
                {
                    allTracks[i] = updatedTrack;
                    break;
                }
            }

            await WriteAllTracksAsync(csvPath, allTracks);
            Log.Success($"Linked CSV database to: '{expectedCoverLink}'");

            return updatedTrack with { DatabaseFilePath = csvPath };
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to update CSV cover property: {ex.Message}");
            return track;
        }
    }

    private async Task CreateEmptyDatabaseAsync(string csvPath)
    {
        try
        {
            string? dir = Path.GetDirectoryName(csvPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string headerLine = string.Join(",", Headers);
            await File.WriteAllTextAsync(csvPath, headerLine + Environment.NewLine, Encoding.UTF8);
            Log.Info($"Created empty CSV template at '{csvPath}'");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create blank database: {ex.Message}");
        }
    }

    private async Task WriteAllTracksAsync(string csvPath, List<Track> tracks)
    {
        StringBuilder sb = new();
        sb.AppendLine(string.Join(",", Headers));

        foreach (Track track in tracks)
        {
            List<string> row = [
                FormatCsvValue(track.Title),
                FormatCsvValue(track.Artist),
                FormatCsvValue(track.Album),
                FormatCsvValue(track.TrackNumber?.ToString()),
                FormatCsvValue(track.DiscNumber?.ToString()),
                FormatCsvValue(track.AlbumArtist),
                FormatCsvValue(track.Composer),
                FormatCsvValue(track.Date),
                FormatCsvValue(track.Tags.Count > 0 ? string.Join(";", track.Tags) : string.Empty),
                FormatCsvValue(track.Cover),
                FormatCsvValue(track.Source),
                FormatCsvValue(track.Range.Count > 0 ? string.Join(";", track.Range) : string.Empty),
                FormatCsvValue(track.Tempo?.ToString(CultureInfo.InvariantCulture)),
                FormatCsvValue(track.Loop.ToString())
            ];
            sb.AppendLine(string.Join(",", row));
        }

        await File.WriteAllTextAsync(csvPath, sb.ToString(), Encoding.UTF8);
    }

    public static List<string> ParseCsvLine(string line)
    {
        List<string> result = [];
        StringBuilder current = new();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result;
    }

    public static string FormatCsvValue(string? val)
    {
        if (val is null)
        {
            return string.Empty;
        }
        if (val.Contains(',') || val.Contains('"') || val.Contains('\n') || val.Contains('\r'))
        {
            return $"\"{val.Replace("\"", "\"\"")}\"";
        }
        return val;
    }
}