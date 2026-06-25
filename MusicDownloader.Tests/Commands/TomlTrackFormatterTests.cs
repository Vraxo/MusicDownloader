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
                    Album = "Album Name",
                    Title = "Song Title"
                }
            ]
        };

        string formatted = TomlTrackFormatter.Format(collection);

        formatted.Should().Contain("Source = \"https://www.youtube.com/watch?v=123\"");

        int sourceIndex = formatted.IndexOf("Source =");
        int artistIndex = formatted.IndexOf("Artist =");
        int albumIndex = formatted.IndexOf("Album =");
        int titleIndex = formatted.IndexOf("Title =");

        sourceIndex.Should().BeLessThan(artistIndex);
        artistIndex.Should().BeLessThan(albumIndex);
        albumIndex.Should().BeLessThan(titleIndex);
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