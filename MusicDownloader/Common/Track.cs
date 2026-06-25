namespace MusicDownloader.Common;

internal sealed record Track
{
    public string Source { get; init; } = string.Empty;

    public string Artist { get; init; } = string.Empty;

    public string? AlbumArtist { get; init; }

    public string? Composer { get; init; }

    public string Album { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public int? TrackNumber { get; init; }

    public int? DiscNumber { get; init; }

    public string? Date { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public IReadOnlyList<string> Range { get; init; } = [];

    public double? Tempo { get; init; }

    public int Loop { get; init; } = 1;
}