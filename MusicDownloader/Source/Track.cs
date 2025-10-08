namespace MusicDownloader;

public record Track
{
    public string Title { get; init; }
    public string Artist { get; init; }
    public string Album { get; init; }
    public string Url { get; init; }
    public string Range { get; init; }
    public string Tempo { get; init; }
    public string? TrackNumber { get; init; }
    public string? DiscNumber { get; init; }
    public IReadOnlyList<string> Tags { get; init; }

    public Track()
    {
        Title = string.Empty;
        Artist = string.Empty;
        Album = string.Empty;
        Url = string.Empty;
        Range = string.Empty;
        Tempo = string.Empty;
        Tags = Array.Empty<string>();
    }

    public static string SafeFileName(string name)
    {
        // The ProcessUrl logic is now part of the mapping and no longer needed here,
        // but SafeFileName is still a useful utility.
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return name;
    }
}