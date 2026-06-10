namespace MusicDownloader.Common;

internal sealed record Track
{
    public string Artist { get; init; } = string.Empty;
    public string Album { get; init; } = string.Empty;
    public int? DiscNumber { get; init; } = null;

    public int? TrackNumber { get; init; } = null;
    public string Title { get; init; } = string.Empty;
    public DateTime? Date { get; init; } = null;

    public string Url { get; init; } = string.Empty;

    public IReadOnlyList<string> Range { get; init; } = [];
    public double? Tempo { get; init; } = null;
    public int Loop { get; init; } = 1;

    public IReadOnlyList<string> Tags { get; init; } = [];
}