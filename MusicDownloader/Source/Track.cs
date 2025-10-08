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
}