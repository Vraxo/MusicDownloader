using FluentAssertions;
using MusicDownloader.Common;

namespace MusicDownloader.Tests.Commands;

public sealed class TomlTrackFormatterTests
{
    [Fact]
    public void Format_WithTrack_OutputsSourceAsFirstProperty()
    {
        SongCollection collection = new()
        {
            Song =
            [
                new Track
                {
                    Source = "https://www.youtube.com/watch?v=123",
                    Artist = "Artist Name",
                    AlbumArtist = "Album Artist Name",
                    Composer = "Composer Name",
                    Album = "Album Name",
                    Title = "Song Title"
                }
            ]
        };

        string formatted = TomlTrackFormatter.Format(collection);

        formatted.Should().Contain("Source = \"https://www.youtube.com/watch?v=123\"");

        int sourceIndex = formatted.IndexOf("Source =");
        int artistIndex = formatted.IndexOf("Artist =");
        int albumArtistIndex = formatted.IndexOf("AlbumArtist =");
        int composerIndex = formatted.IndexOf("Composer =");
        int albumIndex = formatted.IndexOf("Album =");

        sourceIndex.Should().BeLessThan(artistIndex);
        artistIndex.Should().BeLessThan(albumArtistIndex);
        albumArtistIndex.Should().BeLessThan(composerIndex);
        composerIndex.Should().BeLessThan(albumIndex);
    }

    [Fact]
    public void Format_IgnoresEmptyListsAndDefaultLoop()
    {
        SongCollection collection = new()
        {
            Song =
            [
                new Track
                {
                    Title = "Title",
                    Tags = [],
                    Range = [],
                    Loop = 1
                }
            ]
        };

        string formatted = TomlTrackFormatter.Format(collection);

        formatted.Should().NotContain("Tags");
        formatted.Should().NotContain("Range");
        formatted.Should().NotContain("Loop");
        formatted.Should().Contain("Title = \"Title\"");
    }
}