using FluentAssertions;
using MusicDownloader.Commands;

namespace MusicDownloader.Tests.Commands;

public class FlacFfmpegCommandBuilderTests
{
    [Fact]
    public void Build_WithNoTempoAndNoRange_ReturnsCopyCommand()
    {
        FlacFfmpegCommandBuilder builder = new("input.flac", "output.flac", null, [], 44100);
        string command = builder.Build();

        command.Should().Contain("-c copy");
    }

    [Fact]
    public void Build_WithTempo_ReturnsAtempoFilter()
    {
        FlacFfmpegCommandBuilder builder = new("input.flac", "output.flac", 110.0, [], 44100);
        string command = builder.Build();

        command.Should().Contain("-filter:a");
        command.Should().Contain("-c:a flac");
    }

    [Fact]
    public void Build_WithRange_ReturnsTrimCommand()
    {
        FlacFfmpegCommandBuilder builder = new("input.flac", "output.flac", null, ["00:15", "01:30"], 44100);
        string command = builder.Build();

        command.Should().Contain("-ss 00:15 -to 01:30");
    }
}