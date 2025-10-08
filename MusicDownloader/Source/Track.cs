namespace MusicDownloader;

public record Track
{
    public string Title { get; init; } = string.Empty;
    public string Artist { get; init; } = string.Empty;
    public string Album { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Range { get; init; } = string.Empty;
    public string Tempo { get; init; } = string.Empty;
    public string? TrackNumber { get; init; } = string.Empty;
    public string? DiscNumber { get; init; } = string.Empty;
    public IReadOnlyList<string> Tags { get; init; } = [];
}