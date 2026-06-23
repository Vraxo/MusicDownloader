namespace MusicDownloader.Common;

internal sealed record Track
{
    public string Artist { get; init; } = string.Empty;
    public string Album { get; init; } = string.Empty;
    public int? DiscNumber { get; init; }

    public int? TrackNumber { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Date { get; init; }

    public string Source { get; init; } = string.Empty;

    public IReadOnlyList<string> Range { get; init; } = [];
    public double? Tempo { get; init; }
    public int Loop { get; init; } = 1;

    public IReadOnlyList<string> Tags { get; init; } = [];
}