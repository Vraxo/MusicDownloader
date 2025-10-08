namespace MusicDownloader;

public class Track
{
    public string Title { get; }
    public string Artist { get; }
    public string Album { get; }
    public string Url { get; }
    public string Range { get; }
    public string Tempo { get; }
    public string? TrackNumber { get; }
    public string? DiscNumber { get; }

    public Track(IReadOnlyList<string> fields)
    {
        Title = Clean(fields[0]);
        Artist = Clean(fields[1]);
        Album = Clean(fields[2]);
        Url = ProcessUrl(Clean(fields[3]));
        Range = Clean(fields[4]);
        Tempo = Clean(fields[5]);
        TrackNumber = fields.Count > 6 ? Clean(fields[6]) : null;
        DiscNumber = fields.Count > 7 ? Clean(fields[7]) : null;
    }

    public static string SafeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return name;
    }

    private static string ProcessUrl(string urlOrId)
    {
        return !string.IsNullOrWhiteSpace(urlOrId) && !urlOrId.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? $"https://www.youtube.com/watch?v={urlOrId}"
            : urlOrId;
    }

    private static string Clean(string s)
    {
        return s.Trim();
    }
}