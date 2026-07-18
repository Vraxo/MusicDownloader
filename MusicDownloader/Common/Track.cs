using System.ComponentModel;
using YamlDotNet.Serialization;

namespace MusicDownloader.Common;

internal sealed record Track
{
    // --- 1. Canonical Track Metadata (Universal Song Details) ---

    public string Title { get; init; } = string.Empty;

    public string Artist { get; init; } = string.Empty;

    public string Album { get; init; } = string.Empty;

    public int? TrackNumber { get; init; }

    public int? DiscNumber { get; init; }

    public string? AlbumArtist { get; init; }

    public string? Composer { get; init; }

    public string? Date { get; init; }

    public List<string> Tags { get; init; } = [];


    // --- 2. Downloader Control & Media Assets (Local File System Details) ---

    public string? Cover { get; init; }

    public string Source { get; init; } = string.Empty;

    public List<string> Range { get; init; } = [];

    public double? Tempo { get; init; }

    [DefaultValue(1)]
    public int Loop { get; init; } = 1;


    // --- 3. Backend System Metadata (Ignored by Obsidian) ---

    [YamlIgnore]
    public string? DatabaseFilePath { get; init; }
}