using FluentAssertions;
using MusicDownloader.Commands;
using MusicDownloader.Common;

namespace MusicDownloader.Tests.Commands;

public sealed class FfmpegCommandBuilderTests
{
    [Fact]
    public void Build_WithBasicTrack_ReturnsSimpleCommand()
    {
        Track track = new()
        {
            Title = "Song Title",
            Artist = "Artist Name",
            Album = "Album Name",
            Source = "https://www.youtube.com/watch?v=123",
            TrackNumber = 1,
            Loop = 1
        };

        FfmpegCommandBuilder builder = new(track, "input.m4a", "output.m4a");
        string command = builder.Build();

        command.Should().Contain("-metadata title=\"Song Title\"");
        command.Should().Contain("-metadata track=\"1\"");
        command.Should().Contain("-c:a copy");
    }

    [Fact]
    public void Build_WithLoop_ReturnsComplexFilter()
    {
        Track track = new()
        {
            Title = "Song Title",
            Artist = "Artist Name",
            Album = "Album Name",
            Source = "https://www.youtube.com/watch?v=123",
            Loop = 3,
            Range = ["00:15", "01:30"]
        };

        FfmpegCommandBuilder builder = new(track, "input.m4a", "output.m4a");
        string command = builder.Build();

        command.Should().Contain("-filter_complex");
        command.Should().Contain("aloop=loop=2");
    }

    [Fact]
    public void Build_WithTempo_ReturnsTempoFilter()
    {
        Track track = new()
        {
            Title = "Song Title",
            Artist = "Artist Name",
            Album = "Album Name",
            Source = "https://www.youtube.com/watch?v=123",
            Tempo = 110.0
        };

        FfmpegCommandBuilder builder = new(track, "input.m4a", "output.m4a");
        string command = builder.Build();

        command.Should().Contain("-filter:a");
    }

    [Fact]
    public void BuildMetadataUpdate_WithValidTrack_ReturnsCopyCommand()
    {
        Track track = new()
        {
            Title = "Song Title",
            Artist = "Artist Name",
            Album = "Album Name"
        };

        FfmpegCommandBuilder builder = new(track, "input.m4a", "output.m4a");
        string command = builder.BuildMetadataUpdate("input.m4a", "output.m4a");

        command.Should().Contain("-metadata title=\"Song Title\"");
        command.Should().Contain("-c copy");
        command.Should().Contain("-map 0");
    }
}